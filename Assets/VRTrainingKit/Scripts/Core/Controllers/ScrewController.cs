// ScrewController.cs (formerly ValveController.cs)
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;
using System.Collections;
//using System.Diagnostics;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Screw operation states for complex forward/reverse flow interactions
/// </summary>
public enum ScrewState
{
    Unlocked,    // Screw can be grabbed and moved freely
    Locked       // Screw is locked to socket position, only rotation allowed (has substates)
}

/// <summary>
/// Substates within LOCKED state for screw workflow
/// </summary>
public enum ScrewSubstate
{
    None,        // Used when screw is UNLOCKED
    Loose,       // Screw just snapped to socket, needs tightening
    Tight        // Screw is properly tightened, needs loosening to remove
}

/// <summary>
/// Controls complex screw behavior with grab → snap → tighten → loosen → remove workflow
/// Manages screw state transitions with proper substates and socket interaction
/// </summary>
public class ScrewController : MonoBehaviour
{
    [Header("Profile Configuration")]
    [SerializeField] private ScrewProfile profile;
    
    [Header("Runtime State")]
    [SerializeField] private ScrewState currentState = ScrewState.Unlocked;
    [SerializeField] private ScrewSubstate currentSubstate = ScrewSubstate.None;
    [SerializeField] private float currentRotationAngle = 0f;
    [SerializeField] private bool isInitialized = false;
    
    // Component references
    private XRGrabInteractable grabInteractable;
    private Rigidbody rigidBody;
    private Transform originalParent;
    private Renderer valveRenderer;
    private Material originalMaterial;
    
    // Socket interaction
    private GameObject currentSocket;
    private XRSocketInteractor currentSocketInteractor;
    private Vector3 snapPosition;
    private Quaternion snapRotation;
    
    // Rotation tracking
    private Vector3 lastRotation;
    private float totalRotation = 0f;
    private float lastLoggedRotation = 0f;
    private float baselineAxisRotation = 0f; // Baseline rotation for the specific axis after snap
    
    // State management
    private bool isWaitingForGrabRelease = false;
    private bool readyForSocketReEnable = false;
    
    // Events - Screw-specific
    public event Action<ScrewState> OnStateChanged;
    public event Action<ScrewSubstate> OnSubstateChanged;
    public event Action<float> OnRotationChanged;

    // Lifecycle events
    public event Action OnScrewSnapped;      // Screw snapped to socket
    public event Action OnScrewTightened;    // Tightening completed
    public event Action OnScrewLoosened;     // Loosening completed
    public event Action OnScrewRemoved;      // Screw removed from socket
    
    // Progress events
    public event Action<float> OnTighteningProgress;  // 0-1 tightening progress
    public event Action<float> OnLooseningProgress;   // 0-1 loosening progress
    
    // Error events
    public event Action OnInvalidRotation;           // Wrong direction rotation
    public event Action OnForceRemovalAttempt;       // Try to remove when tight
    
    // Public properties
    public ScrewState CurrentState => currentState;
    public ScrewSubstate CurrentSubstate => currentSubstate;
    public float CurrentRotationAngle => currentRotationAngle;
    public bool IsUnlocked => currentState == ScrewState.Unlocked;
    public bool IsLocked => currentState == ScrewState.Locked;
    public bool IsLoose => currentState == ScrewState.Locked && currentSubstate == ScrewSubstate.Loose;
    public bool IsTight => currentState == ScrewState.Locked && currentSubstate == ScrewSubstate.Tight;
    
    private void Awake()
    {
        // Get component references
        grabInteractable = GetComponent<XRGrabInteractable>();
        rigidBody = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        valveRenderer = GetComponent<Renderer>();
        if (valveRenderer != null)
        {
            originalMaterial = valveRenderer.material;
        }
        
        // Initialize with unlocked state
        currentState = ScrewState.Unlocked;
        currentSubstate = ScrewSubstate.None;
        lastRotation = transform.localEulerAngles;

        Debug.Log($"[ScrewController] {gameObject.name} Awake() - Initial state: {currentState}");
    }
    
    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    /// <summary>
    /// Configure this valve with a ScrewProfile
    /// </summary>
    public void Configure(ScrewProfile valveProfile)
    {
        // Prevent redundant configuration
        if (profile == valveProfile && isInitialized)
        {
            Debug.Log($"[ScrewController] {gameObject.name} already configured with {valveProfile.profileName}, skipping...");
            return;
        }

        var previousProfile = profile?.profileName ?? "NULL";
        profile = valveProfile;

        // Reset to unlocked state only if needed
        if (currentState != ScrewState.Unlocked || currentSubstate != ScrewSubstate.None)
        {
            SetState(ScrewState.Unlocked, ScrewSubstate.None);
            Debug.Log("SET STATE : 1");
        }

        // Reset rotation tracking
        currentRotationAngle = 0f;
        totalRotation = 0f;
        lastRotation = transform.localEulerAngles;

        isInitialized = true;

        Debug.Log($"[ScrewController] Configure() called for {gameObject.name}: " +
                 $"Previous={previousProfile} → New={profile.profileName}, " +
                 $"State={currentState}, Substate={currentSubstate}");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Configure this controller with a ValveProfile (backward compatibility)
    /// Converts ValveProfile to ScrewProfile internally
    /// </summary>
    public void Configure(ValveProfile valveProfile)
    {
        Debug.Log($"[ScrewController] Converting ValveProfile '{valveProfile.profileName}' to ScrewProfile for backward compatibility");

        // Create a temporary ScrewProfile with ValveProfile settings
        ScrewProfile screwProfile = ScriptableObject.CreateInstance<ScrewProfile>();
        screwProfile.profileName = valveProfile.profileName;
        screwProfile.rotationAxis = valveProfile.rotationAxis;
        screwProfile.tightenThreshold = valveProfile.tightenThreshold;
        screwProfile.loosenThreshold = valveProfile.loosenThreshold;
        screwProfile.angleTolerance = valveProfile.angleTolerance;
        screwProfile.compatibleSocketTags = valveProfile.compatibleSocketTags;
        screwProfile.specificCompatibleSockets = valveProfile.specificCompatibleSockets;
        screwProfile.requireSpecificSockets = valveProfile.requireSpecificSockets;
        screwProfile.movementType = valveProfile.movementType;
        screwProfile.trackPosition = valveProfile.trackPosition;
        screwProfile.trackRotation = valveProfile.trackRotation;
        screwProfile.throwOnDetach = valveProfile.throwOnDetach;
        screwProfile.rotationDampening = valveProfile.rotationDampening;
        screwProfile.dampeningSpeed = valveProfile.dampeningSpeed;
        screwProfile.lockedRotationSpeed = valveProfile.lockedRotationSpeed;
        screwProfile.hapticIntensity = valveProfile.hapticIntensity;

        // Configure with the converted profile
        Configure(screwProfile);
    }
    
    private void Update()
    {
        if (!isInitialized || profile == null) return;
        
        if (currentState == ScrewState.Locked)
        {
            if (grabInteractable.isSelected)
            {
                // Track rotation when grabbed in locked state
                TrackRotation();
            }
            else
            {
                // Apply rotation dampening when not grabbed to stop spinning
                ApplyRotationDampening();
            }
        }
    }
    
    /// <summary>
    /// Check if a socket is compatible with this valve
    /// </summary>
    public bool IsSocketCompatible(GameObject socket)
    {
        if (profile == null) return false;
        return profile.IsSocketCompatible(socket);
    }
    
    /// <summary>
    /// Called when screw is grabbed
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} grabbed in state: {currentState}-{currentSubstate}");

        switch (currentState)
        {
            case ScrewState.Unlocked:
                // Normal grab behavior - screw can be moved freely
                break;

            case ScrewState.Locked:
                // Screw is locked - prevent movement, only allow rotation
                // This applies to both LOOSE and TIGHT substates
                break;
        }
    }
    
    /// <summary>
    /// Called when screw is released
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} released in state: {currentState}-{currentSubstate}");

        // Check if we need to enable socket and transition after loosening
        if (isWaitingForGrabRelease && currentState == ScrewState.Locked && currentSubstate == ScrewSubstate.Loose && readyForSocketReEnable)
        {
            VRTrainingDebug.LogEvent($"[ScrewController] Grab released after loosening - enabling socket for snap-back");
            
            // Enable socket just as user releases - object will naturally fall/snap into socket
            EnableSocketInteractor();
            
            // Start coroutine to wait for snap-back and then transition to unlocked
            StartCoroutine(TransitionToUnlockedAfterSnap());
            
            // Clear flags
            isWaitingForGrabRelease = false;
            readyForSocketReEnable = false;
        }
    }
    
    /// <summary>
    /// Set the valve state and substate
    /// </summary>
    private void SetState(ScrewState newState, ScrewSubstate newSubstate = ScrewSubstate.None)
    {
        if (currentState == newState && currentSubstate == newSubstate) return;

        ScrewState previousState = currentState;
        ScrewSubstate previousSubstate = currentSubstate;
        
        currentState = newState;
        currentSubstate = newSubstate;
        
        // Add call context for debugging
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(1).GetMethod().Name;
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} state changed: {previousState}-{previousSubstate} → {currentState}-{currentSubstate} [Called by: {callingMethod}]");
        
        // Handle state-specific setup
        switch (currentState)
        {
            case ScrewState.Unlocked:
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} applying UNLOCKED state setup");
                UnlockValve();
                break;
                
            case ScrewState.Locked:
                if (currentSubstate == ScrewSubstate.Loose)
                {
                    VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} applying LOCKED-LOOSE specific state setup");
                    Debug.Log("CONSTRAINTS : Applying Locked-Loose Constraints");
                    ApplyLockedLooseConstraints();
                }
                else
                {
                    VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} applying LOCKED state setup (substate: {currentSubstate})");
                    Debug.Log("CONSTRAINTS : Applying Locked Constraints");
                    ApplyLockedConstraints();
                }
                break;
        }
        
        // Update visual feedback
        UpdateVisualFeedback();
        
        // Fire events
        OnStateChanged?.Invoke(currentState);
        OnSubstateChanged?.Invoke(currentSubstate);
    }
    
    /// <summary>
    /// Apply constraints for locked state - handles LOOSE and TIGHT substates differently
    /// </summary>
    private void ApplyLockedConstraints()
    {
        if (rigidBody != null && grabInteractable != null && profile != null)
        {
            // Only apply if not already applied
            if (grabInteractable.trackPosition == true || rigidBody.isKinematic == false)
            {
                // Configure grab interactable for rotation-only
                grabInteractable.trackPosition = false;  // Prevent movement
                grabInteractable.trackRotation = true;   // Allow rotation
                
                // Make rigidbody kinematic to prevent physics movement
                rigidBody.isKinematic = true;
                
                // Set constraints to freeze position and all rotation axes except the specified one
                RigidbodyConstraints constraints = RigidbodyConstraints.FreezePosition;

                // Directly map profile axis to constraints - simple and reliable
                if (profile.rotationAxis == Vector3.right) // X axis
                {
                    constraints |= RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                }
                else if (profile.rotationAxis == Vector3.up) // Y axis
                {
                    constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
                else if (profile.rotationAxis == Vector3.forward) // Z axis
                {
                    constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
                }
                else
                {
                    // Default case - freeze all rotation if axis not recognized
                    constraints |= RigidbodyConstraints.FreezeRotation;
                    VRTrainingDebug.LogWarning($"[ScrewController] {gameObject.name} unrecognized rotation axis {profile.rotationAxis}, freezing all rotation");
                }

                rigidBody.constraints = constraints;
                
                // Handle socket interactor based on substate
                HandleSocketInteractorForSubstate();
                
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOCKED - position fixed, rotation allowed on local {profile.rotationAxis}");
            }
        }
    }

    /// <summary>
    /// Apply constraints specifically for LOCKED-LOOSE state - allows for potential removal
    /// </summary>
    private void ApplyLockedLooseConstraints()
    {
        if (rigidBody != null && grabInteractable != null && profile != null)
        {
            // Configure grab interactable for rotation-only (same as locked tight)
            grabInteractable.trackPosition = false;  // Prevent movement
            grabInteractable.trackRotation = true;   // Allow rotation

            // For LOOSE state, we want less rigid constraints to allow potential removal
            rigidBody.isKinematic = true;  // Keep kinematic to maintain position control

            // Set constraints to freeze position and all rotation axes except the specified one
            RigidbodyConstraints constraints = RigidbodyConstraints.FreezePosition;

            // Apply same rotation constraints as tight state for consistency
            if (profile.rotationAxis == Vector3.right) // X axis
            {
                constraints |= RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            }
            else if (profile.rotationAxis == Vector3.up) // Y axis
            {
                constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            else if (profile.rotationAxis == Vector3.forward) // Z axis
            {
                constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            }
            else
            {
                constraints |= RigidbodyConstraints.FreezeRotation;
                VRTrainingDebug.LogWarning($"[ScrewController] {gameObject.name} unrecognized rotation axis {profile.rotationAxis}, freezing all rotation");
            }

            rigidBody.constraints = constraints;

            // Handle socket interactor based on LOOSE substate logic
            HandleSocketInteractorForSubstate();

            VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOCKED-LOOSE - position fixed, rotation allowed on local {profile.rotationAxis}, socket handling per substate");
        }
    }

    /// <summary>
    /// Handle socket interactor enable/disable based on current substate
    /// </summary>
    private void HandleSocketInteractorForSubstate()
    {
        switch (currentSubstate)
        {
            case ScrewSubstate.Loose:
                // For LOOSE substate, check if we should keep socket enabled
                if (readyForSocketReEnable)
                {
                    // Socket should remain enabled - valve was just loosened and ready for removal
                    VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOCKED-LOOSE - keeping socket ENABLED for removal");
                }
                else
                {
                    // First time entering LOOSE (from snap) - disable socket
                    DisableSocketInteractor();
                    VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOCKED-LOOSE - socket disabled (initial snap)");
                }
                break;
                
            case ScrewSubstate.Tight:
                // TIGHT substate always disables socket
                DisableSocketInteractor();
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOCKED-TIGHT - socket disabled");
                break;
                
            default:
                // Default case - disable socket
                DisableSocketInteractor();
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOCKED - socket disabled (default)");
                break;
        }
    }
    
    /// <summary>
    /// Unlock valve for free movement
    /// </summary>
    private void UnlockValve()
    {
        if (rigidBody != null && grabInteractable != null)
        {
            // Only apply if not already unlocked
            if (grabInteractable.trackPosition == false || rigidBody.isKinematic == true)
            {
                // Configure grab interactable for full movement
                grabInteractable.trackPosition = true;
                grabInteractable.trackRotation = true;
                
                // Remove kinematic mode
                rigidBody.isKinematic = false;
                
                // Remove all constraints
                rigidBody.constraints = RigidbodyConstraints.None;
                
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} UNLOCKED for free movement");
            }
        }
    }
    
    /// <summary>
    /// Disable socket interactor to prevent valve removal when locked
    /// </summary>
    private void DisableSocketInteractor()
    {
        if (currentSocket != null)
        {
            XRSocketInteractor socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null)
            {
                socketInteractor.socketActive = false;
                // Add call context for debugging
                var stackTrace = new System.Diagnostics.StackTrace();
                var callingMethod = stackTrace.GetFrame(1).GetMethod().Name;
                VRTrainingDebug.LogEvent($"[ScrewController] Disabled socket interactor on {currentSocket.name} [Called by: {callingMethod}]");
            }
        }
    }
    
    /// <summary>
    /// Re-enable socket interactor when valve can be removed
    /// </summary>
    private void EnableSocketInteractor()
    {
        if (currentSocket != null)
        {
            XRSocketInteractor socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null)
            {
                socketInteractor.socketActive = true;
                // Add call context for debugging
                var stackTrace = new System.Diagnostics.StackTrace();
                var callingMethod = stackTrace.GetFrame(1).GetMethod().Name;
                VRTrainingDebug.LogEvent($"[ScrewController] Re-enabled socket interactor on {currentSocket.name} [Called by: {callingMethod}]");
            }
        }
    }
    
    /// <summary>
    /// Reset rotation tracking when valve snaps to socket or completes transitions
    /// </summary>
    private void ResetRotationTracking()
    {
        totalRotation = 0f;
        currentRotationAngle = 0f;
        lastRotation = transform.localEulerAngles;

        // Store the current rotation value for the specific axis as baseline
        if (profile != null)
        {
            baselineAxisRotation = GetAxisRotationValue(transform.localEulerAngles, profile.rotationAxis);
            VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} rotation tracking reset - baseline for {profile.rotationAxis}: {baselineAxisRotation:F2}°");
        }
        else
        {
            baselineAxisRotation = 0f;
            VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} rotation tracking reset - no profile, baseline set to 0°");
        }
    }

    /// <summary>
    /// Get the rotation value for a specific axis from euler angles
    /// </summary>
    private float GetAxisRotationValue(Vector3 eulerAngles, Vector3 axis)
    {
        if (axis == Vector3.right) // X axis
            return eulerAngles.x;
        else if (axis == Vector3.up) // Y axis
            return eulerAngles.y;
        else if (axis == Vector3.forward) // Z axis
            return eulerAngles.z;
        else
        {
            VRTrainingDebug.LogWarning($"[ScrewController] Unrecognized rotation axis {axis}, defaulting to Y axis");
            return eulerAngles.y;
        }
    }

    /// <summary>
    /// Called by SnapValidator when valve is snapped to a compatible socket
    /// </summary>
    public void OnSocketSnapped(GameObject socket)
    {
        currentSocket = socket;
        snapPosition = socket.transform.position;
        snapRotation = socket.transform.rotation;
        
        // Store socket interactor reference for debugging
        currentSocketInteractor = socket.GetComponent<XRSocketInteractor>();

        // Start position monitoring coroutine instead of event listening
        StartCoroutine(MonitorSocketPositioning(socket));
        
        OnScrewSnapped?.Invoke();
        
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} detected by socket: {socket.name} → Monitoring position stability");
    }
    
    /// <summary>
    /// Monitors socket positioning until object is stable, then applies constraints
    /// </summary>
    private IEnumerator MonitorSocketPositioning(GameObject socket)
    {
        if (profile == null)
        {
            VRTrainingDebug.LogWarning($"[ScrewController] No profile found for {gameObject.name}, applying constraints immediately");
            FinalizeLockToSocket();
            yield break;
        }
        
        float startTime = Time.time;
        float lastLogTime = startTime;
        float positionTolerance = profile.positionTolerance;
        float velocityThreshold = profile.velocityThreshold;
        float timeout = profile.positioningTimeout;
        
        Vector3 socketCenter = snapPosition;
        
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} monitoring positioning: tolerance={positionTolerance:F4}, velocity={velocityThreshold:F4}, timeout={timeout}s");
        
        while (Time.time - startTime < timeout)
        {
            // Check if we're still connected to the same socket
            if (currentSocket != socket)
            {
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} socket changed during monitoring, aborting");
                yield break;
            }
            
            // Calculate current distance from socket center
            float distance = Vector3.Distance(transform.position, socketCenter);
            
            // Check velocity (both linear and angular)
            float linearVelocity = rigidBody != null ? rigidBody.linearVelocity.magnitude : 0f;
            float angularVelocity = rigidBody != null ? rigidBody.angularVelocity.magnitude : 0f;
            float totalVelocity = linearVelocity + angularVelocity;
            
            // Log progress every 0.2 seconds for debugging
            if (Time.time - lastLogTime > 0.2f)
            {
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} positioning: distance={distance:F4}, velocity={totalVelocity:F4}");
                lastLogTime = Time.time;
            }
            
            // Check if object is positioned and stabilized
            if (distance <= positionTolerance && totalVelocity <= velocityThreshold)
            {
                VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} positioning complete: distance={distance:F4}, velocity={totalVelocity:F4} (took {Time.time - startTime:F2}s)");
                FinalizeLockToSocket();
                yield break;
            }
            
            yield return new WaitForFixedUpdate();
        }
        
        // Timeout reached - apply constraints anyway with warning
        VRTrainingDebug.LogWarning($"[ScrewController] {gameObject.name} positioning timeout ({timeout}s) - applying constraints anyway");
        FinalizeLockToSocket();
    }
    
    /// <summary>
    /// Apply final locked state and constraints after socket positioning is complete
    /// </summary>
    private void FinalizeLockToSocket()
    {
        // Reset flag for new interaction cycle
        readyForSocketReEnable = false;

        // Reset rotation tracking AFTER object is fully positioned by attach transform
        ResetRotationTracking();

        // Now transition to LOCKED-LOOSE state
        SetState(ScrewState.Locked, ScrewSubstate.Loose);
        Debug.Log("SET STATE : 2");
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} → LOCKED-LOOSE after confirmed socket positioning");
    }
    
    /// <summary>
    /// Called by SnapValidator when valve is removed from socket
    /// </summary>
    public void OnSocketReleased(GameObject socket)
    {
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} released from socket: {socket.name}");
        
        // Clean up socket reference and stop any monitoring
        if (currentSocket == socket)
        {
            // Stop any running positioning coroutines
            StopAllCoroutines();
            
            // Only change state if we're currently unlocked (removable)
            if (currentState == ScrewState.Unlocked)
            {
                currentSocket = null;
                currentSocketInteractor = null;
                OnScrewRemoved?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Track rotation changes and check for state transitions
    /// </summary>
    private void TrackRotation()
    {
        if (profile == null) return;

        // Get current rotation value for the specific axis
        Vector3 currentRotationVector = transform.localEulerAngles;
        float currentAxisValue = GetAxisRotationValue(currentRotationVector, profile.rotationAxis);

        // Calculate rotation relative to baseline (post-snap position)
        float rotationFromBaseline = currentAxisValue - baselineAxisRotation;

        // Handle angle wrapping for the specific axis
        if (rotationFromBaseline > 180) rotationFromBaseline -= 360;
        if (rotationFromBaseline < -180) rotationFromBaseline += 360;

        // Update total rotation (this represents user motion from snap point)
        currentRotationAngle = rotationFromBaseline;
        totalRotation = rotationFromBaseline;

        lastRotation = currentRotationVector;

        // Check for state transitions based on rotation
        CheckRotationThresholds();

        // Fire events
        OnRotationChanged?.Invoke(currentRotationAngle);
        UpdateProgressEvents();
    }
    
    /// <summary>
    /// Apply dampening to rotation when valve is not grabbed to prevent endless spinning
    /// </summary>
    private void ApplyRotationDampening()
    {
        if (rigidBody != null && profile != null && profile.rotationDampening > 0)
        {
            // Get current angular velocity
            Vector3 angularVelocity = rigidBody.angularVelocity;
            
            // Calculate dampening force based on profile settings
            float dampingFactor = 1f - (profile.rotationDampening * profile.dampeningSpeed * Time.deltaTime);
            dampingFactor = Mathf.Clamp01(dampingFactor);
            
            // Apply dampening by reducing angular velocity
            rigidBody.angularVelocity = angularVelocity * dampingFactor;
            
            // If angular velocity is very low, stop it completely to avoid micro-movements
            if (rigidBody.angularVelocity.magnitude < 0.1f)
            {
                rigidBody.angularVelocity = Vector3.zero;
            }
        }
    }
    
    /// <summary>
    /// Check if rotation thresholds are met for state transitions
    /// </summary>
    private void CheckRotationThresholds()
    {
        if (profile == null) return;
        
        switch (currentSubstate)
        {
            case ScrewSubstate.Loose:
                // Check if tightened enough to transition to TIGHT
                float tighteningProgress = totalRotation;
                if (tighteningProgress >= profile.tightenThreshold - profile.angleTolerance)
                {
                    VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} TIGHTENED! {tighteningProgress:F1}° reached (threshold: {profile.tightenThreshold}°)");
                    TransitionToTight();
                }
                else if (tighteningProgress > 0) // Only log positive tightening progress
                {
                    // Log rotation progress periodically
                    if (Mathf.FloorToInt(tighteningProgress / 10f) != Mathf.FloorToInt(lastLoggedRotation / 10f))
                    {
                        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} tightening: {tighteningProgress:F1}° (threshold: {profile.tightenThreshold}°)");
                        lastLoggedRotation = tighteningProgress;
                    }
                }
                else if (tighteningProgress < -5f) // Log if user is rotating backwards significantly
                {
                    // Only log occasionally to avoid spam
                    if (Mathf.FloorToInt(Mathf.Abs(tighteningProgress) / 20f) != Mathf.FloorToInt(Mathf.Abs(lastLoggedRotation) / 20f))
                    {
                        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} rotating backwards: {tighteningProgress:F1}° (need positive rotation to tighten)");
                        lastLoggedRotation = tighteningProgress;
                    }
                }
                break;
                
            case ScrewSubstate.Tight:
                // Check if loosened enough to allow removal
                float looseningProgress = -totalRotation; // Negative rotation becomes positive progress
                if (looseningProgress >= profile.loosenThreshold - profile.angleTolerance)
                {
                    VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} LOOSENED! {looseningProgress:F1}° loosening completed");
                    TransitionToLooseAfterTight();
                }
                else if (looseningProgress > 0) // Only log positive loosening progress
                {
                    // Log loosening progress periodically
                    if (Mathf.FloorToInt(looseningProgress / 10f) != Mathf.FloorToInt(lastLoggedRotation / 10f))
                    {
                        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} loosening: {looseningProgress:F1}° (threshold: {profile.loosenThreshold}°)");
                        lastLoggedRotation = looseningProgress;
                    }
                }
                else if (looseningProgress < -5f) // Log if user is tightening further (wrong direction)
                {
                    // Only log occasionally to avoid spam
                    if (Mathf.FloorToInt(Mathf.Abs(looseningProgress) / 20f) != Mathf.FloorToInt(Mathf.Abs(lastLoggedRotation) / 20f))
                    {
                        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} tightening further: {totalRotation:F1}° (need negative rotation to loosen)");
                        lastLoggedRotation = looseningProgress;
                    }
                }
                break;
        }
    }
    
    /// <summary>
    /// Transition from LOOSE to TIGHT substate
    /// </summary>
    private void TransitionToTight()
    {
        SetState(ScrewState.Locked, ScrewSubstate.Tight);
        ResetRotationTracking(); // Reset for loosening phase
        Debug.Log("SET STATE : 3");
        OnScrewTightened?.Invoke();
    }
    
    /// <summary>
    /// Transition from TIGHT back to LOOSE (after loosening)
    /// Set flag for release-triggered socket re-enable to prevent visual jarring
    /// </summary>
    private void TransitionToLooseAfterTight()
    {
        // Set flag to enable socket when user releases grab (prevents visual jarring during rotation)
        readyForSocketReEnable = true;
        
        SetState(ScrewState.Locked, ScrewSubstate.Loose);
        Debug.Log("SET STATE : 4");
        
        // IMPORTANT: Reset rotation tracking to prevent confusion when back in LOOSE state
        ResetRotationTracking();
        
        // Set flag to wait for grab release
        isWaitingForGrabRelease = true;
        
        OnScrewLoosened?.Invoke();
        
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} loosened - ready for socket re-enable on release, rotation tracking reset");
    }
    
    /// <summary>
    /// Wait for valve to snap back to socket, then transition to unlocked
    /// </summary>
    private IEnumerator TransitionToUnlockedAfterSnap()
    {
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} waiting for snap-back to socket...");
        
        // Wait a moment for physics to settle and object to snap back
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        
        // Transition to unlocked state - object should now be in socket but removable
        TransitionToUnlocked();
        
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} snap-back complete - now UNLOCKED in socket");
    }
    
    /// <summary>
    /// Final transition to UNLOCKED state (after grab release)
    /// </summary>
    private void TransitionToUnlocked()
    {
        SetState(ScrewState.Unlocked, ScrewSubstate.None);
        Debug.Log("SET STATE : 5");
        currentSocket = null;
        
        VRTrainingDebug.LogEvent($"[ScrewController] {gameObject.name} now UNLOCKED and removable");
    }
    
    /// <summary>
    /// Update progress events based on current substate
    /// </summary>
    private void UpdateProgressEvents()
    {
        switch (currentSubstate)
        {
            case ScrewSubstate.Loose:
                float tighteningProgress = Mathf.Clamp01(totalRotation / profile.tightenThreshold);
                OnTighteningProgress?.Invoke(tighteningProgress);
                break;
                
            case ScrewSubstate.Tight:
                float looseningProgress = Mathf.Clamp01(-totalRotation / profile.loosenThreshold);
                OnLooseningProgress?.Invoke(looseningProgress);
                break;
        }
    }
    
    /// <summary>
    /// Update visual feedback based on current state
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (valveRenderer == null || profile == null) return;
        
        switch (currentSubstate)
        {
            case ScrewSubstate.Loose:
                if (profile.looseMaterial != null)
                    valveRenderer.material = profile.looseMaterial;
                break;
                
            case ScrewSubstate.Tight:
                if (profile.tightMaterial != null)
                    valveRenderer.material = profile.tightMaterial;
                break;
                
            default:
                if (originalMaterial != null)
                    valveRenderer.material = originalMaterial;
                break;
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Update debug info in editor
        if (profile != null)
        {
            Debug.Log($"[ScrewController] {gameObject.name} OnValidate() - Profile: {profile.profileName}, State: {currentState}-{currentSubstate}");
        }
    }
    
    /// <summary>
    /// Manual test methods for debugging socket interaction (Editor only)
    /// </summary>
    [ContextMenu("Test: Force Enable Socket")]
    private void TestForceEnableSocket()
    {
        EnableSocketInteractor();
        Debug.Log($"[ScrewController] TEST: Manually enabled socket for {gameObject.name}");
    }
    
    [ContextMenu("Test: Force Disable Socket")]
    private void TestForceDisableSocket()
    {
        DisableSocketInteractor();
        Debug.Log($"[ScrewController] TEST: Manually disabled socket for {gameObject.name}");
    }
    
    [ContextMenu("Test: Transition to Unlocked")]
    private void TestTransitionToUnlocked()
    {
        TransitionToUnlocked();
        Debug.Log($"[ScrewController] TEST: Manually transitioned to unlocked for {gameObject.name}");
    }
    #endif
}
// ValveController.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;
using System.Collections;
//using System.Diagnostics;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Valve operation states for complex forward/reverse flow interactions
/// </summary>
public enum ValveState
{
    Unlocked,    // Valve can be grabbed and moved freely
    Locked       // Valve is locked to socket position, only rotation allowed (has substates)
}

/// <summary>
/// Substates within LOCKED state for valve workflow
/// </summary>
public enum ValveSubstate
{
    None,        // Used when valve is UNLOCKED
    Loose,       // Valve just snapped to socket, needs tightening
    Tight        // Valve is properly tightened, needs loosening to remove
}

/// <summary>
/// Controls complex valve behavior with grab → snap → tighten → loosen → remove workflow
/// Manages valve state transitions with proper substates and socket interaction
/// </summary>
public class ValveController : MonoBehaviour
{
    [Header("Profile Configuration")]
    [SerializeField] private ValveProfile profile;
    
    [Header("Runtime State")]
    [SerializeField] private ValveState currentState = ValveState.Unlocked;
    [SerializeField] private ValveSubstate currentSubstate = ValveSubstate.None;
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
    
    // State management
    private bool isWaitingForGrabRelease = false;
    private bool readyForSocketReEnable = false;
    
    // Events - Valve-specific
    public event Action<ValveState> OnStateChanged;
    public event Action<ValveSubstate> OnSubstateChanged;
    public event Action<float> OnRotationChanged;
    
    // Lifecycle events
    public event Action OnValveSnapped;      // Valve snapped to socket
    public event Action OnValveTightened;    // Tightening completed
    public event Action OnValveLoosened;     // Loosening completed
    public event Action OnValveRemoved;      // Valve removed from socket
    
    // Progress events
    public event Action<float> OnTighteningProgress;  // 0-1 tightening progress
    public event Action<float> OnLooseningProgress;   // 0-1 loosening progress
    
    // Error events
    public event Action OnInvalidRotation;           // Wrong direction rotation
    public event Action OnForceRemovalAttempt;       // Try to remove when tight
    
    // Public properties
    public ValveState CurrentState => currentState;
    public ValveSubstate CurrentSubstate => currentSubstate;
    public float CurrentRotationAngle => currentRotationAngle;
    public bool IsUnlocked => currentState == ValveState.Unlocked;
    public bool IsLocked => currentState == ValveState.Locked;
    public bool IsLoose => currentState == ValveState.Locked && currentSubstate == ValveSubstate.Loose;
    public bool IsTight => currentState == ValveState.Locked && currentSubstate == ValveSubstate.Tight;
    
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
        currentState = ValveState.Unlocked;
        currentSubstate = ValveSubstate.None;
        lastRotation = transform.eulerAngles;
        
        Debug.Log($"[ValveController] {gameObject.name} Awake() - Initial state: {currentState}");
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
    /// Configure this valve with a ValveProfile
    /// </summary>
    public void Configure(ValveProfile valveProfile)
    {
        // Prevent redundant configuration
        if (profile == valveProfile && isInitialized)
        {
            Debug.Log($"[ValveController] {gameObject.name} already configured with {valveProfile.profileName}, skipping...");
            return;
        }
        
        var previousProfile = profile?.profileName ?? "NULL";
        profile = valveProfile;
        
        // Reset to unlocked state only if needed
        if (currentState != ValveState.Unlocked || currentSubstate != ValveSubstate.None)
        {
            SetState(ValveState.Unlocked, ValveSubstate.None);
        }
        
        // Reset rotation tracking
        currentRotationAngle = 0f;
        totalRotation = 0f;
        lastRotation = transform.eulerAngles;
        
        isInitialized = true;
        
        Debug.Log($"[ValveController] Configure() called for {gameObject.name}: " +
                 $"Previous={previousProfile} → New={profile.profileName}, " +
                 $"State={currentState}, Substate={currentSubstate}");
                 
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    private void Update()
    {
        if (!isInitialized || profile == null) return;
        
        if (currentState == ValveState.Locked)
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
    /// Called when valve is grabbed
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} grabbed in state: {currentState}-{currentSubstate}");
        
        switch (currentState)
        {
            case ValveState.Unlocked:
                // Normal grab behavior - valve can be moved freely
                break;
                
            case ValveState.Locked:
                // Valve is locked - prevent movement, only allow rotation
                // This applies to both LOOSE and TIGHT substates
                break;
        }
    }
    
    /// <summary>
    /// Called when valve is released
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} released in state: {currentState}-{currentSubstate}");
        
        // Check if we need to enable socket and transition after loosening
        if (isWaitingForGrabRelease && currentState == ValveState.Locked && currentSubstate == ValveSubstate.Loose && readyForSocketReEnable)
        {
            VRTrainingDebug.LogEvent($"[ValveController] Grab released after loosening - enabling socket for snap-back");
            
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
    private void SetState(ValveState newState, ValveSubstate newSubstate = ValveSubstate.None)
    {
        if (currentState == newState && currentSubstate == newSubstate) return;
        
        ValveState previousState = currentState;
        ValveSubstate previousSubstate = currentSubstate;
        
        currentState = newState;
        currentSubstate = newSubstate;
        
        // Add call context for debugging
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(1).GetMethod().Name;
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} state changed: {previousState}-{previousSubstate} → {currentState}-{currentSubstate} [Called by: {callingMethod}]");
        
        // Handle state-specific setup
        switch (currentState)
        {
            case ValveState.Unlocked:
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} applying UNLOCKED state setup");
                UnlockValve();
                break;
                
            case ValveState.Locked:
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} applying LOCKED state setup (substate: {currentSubstate})");
                ApplyLockedConstraints();
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
                
                // Set constraints to freeze position but allow rotation on specified axis
                RigidbodyConstraints constraints = RigidbodyConstraints.FreezePosition;
                
                // Allow rotation only on the specified axis
                if (profile.rotationAxis.x == 0) constraints |= RigidbodyConstraints.FreezeRotationX;
                if (profile.rotationAxis.y == 0) constraints |= RigidbodyConstraints.FreezeRotationY;
                if (profile.rotationAxis.z == 0) constraints |= RigidbodyConstraints.FreezeRotationZ;
                
                rigidBody.constraints = constraints;
                
                // Handle socket interactor based on substate
                HandleSocketInteractorForSubstate();
                
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOCKED - position fixed, rotation on {profile.rotationAxis}");
            }
        }
    }
    
    /// <summary>
    /// Handle socket interactor enable/disable based on current substate
    /// </summary>
    private void HandleSocketInteractorForSubstate()
    {
        switch (currentSubstate)
        {
            case ValveSubstate.Loose:
                // For LOOSE substate, check if we should keep socket enabled
                if (readyForSocketReEnable)
                {
                    // Socket should remain enabled - valve was just loosened and ready for removal
                    VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOCKED-LOOSE - keeping socket ENABLED for removal");
                }
                else
                {
                    // First time entering LOOSE (from snap) - disable socket
                    DisableSocketInteractor();
                    VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOCKED-LOOSE - socket disabled (initial snap)");
                }
                break;
                
            case ValveSubstate.Tight:
                // TIGHT substate always disables socket
                DisableSocketInteractor();
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOCKED-TIGHT - socket disabled");
                break;
                
            default:
                // Default case - disable socket
                DisableSocketInteractor();
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOCKED - socket disabled (default)");
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
                
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} UNLOCKED for free movement");
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
                VRTrainingDebug.LogEvent($"[ValveController] Disabled socket interactor on {currentSocket.name} [Called by: {callingMethod}]");
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
                VRTrainingDebug.LogEvent($"[ValveController] Re-enabled socket interactor on {currentSocket.name} [Called by: {callingMethod}]");
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
        lastRotation = transform.eulerAngles;
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} rotation tracking reset");
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
        
        // Reset rotation tracking for new snap
        ResetRotationTracking();
        
        // Start position monitoring coroutine instead of event listening
        StartCoroutine(MonitorSocketPositioning(socket));
        
        OnValveSnapped?.Invoke();
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} detected by socket: {socket.name} → Monitoring position stability");
    }
    
    /// <summary>
    /// Monitors socket positioning until object is stable, then applies constraints
    /// </summary>
    private IEnumerator MonitorSocketPositioning(GameObject socket)
    {
        if (profile == null)
        {
            VRTrainingDebug.LogWarning($"[ValveController] No profile found for {gameObject.name}, applying constraints immediately");
            FinalizeLockToSocket();
            yield break;
        }
        
        float startTime = Time.time;
        float lastLogTime = startTime;
        float positionTolerance = profile.positionTolerance;
        float velocityThreshold = profile.velocityThreshold;
        float timeout = profile.positioningTimeout;
        
        Vector3 socketCenter = snapPosition;
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} monitoring positioning: tolerance={positionTolerance:F4}, velocity={velocityThreshold:F4}, timeout={timeout}s");
        
        while (Time.time - startTime < timeout)
        {
            // Check if we're still connected to the same socket
            if (currentSocket != socket)
            {
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} socket changed during monitoring, aborting");
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
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} positioning: distance={distance:F4}, velocity={totalVelocity:F4}");
                lastLogTime = Time.time;
            }
            
            // Check if object is positioned and stabilized
            if (distance <= positionTolerance && totalVelocity <= velocityThreshold)
            {
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} positioning complete: distance={distance:F4}, velocity={totalVelocity:F4} (took {Time.time - startTime:F2}s)");
                FinalizeLockToSocket();
                yield break;
            }
            
            yield return new WaitForFixedUpdate();
        }
        
        // Timeout reached - apply constraints anyway with warning
        VRTrainingDebug.LogWarning($"[ValveController] {gameObject.name} positioning timeout ({timeout}s) - applying constraints anyway");
        FinalizeLockToSocket();
    }
    
    /// <summary>
    /// Apply final locked state and constraints after socket positioning is complete
    /// </summary>
    private void FinalizeLockToSocket()
    {
        // Reset flag for new interaction cycle
        readyForSocketReEnable = false;
        
        // Now transition to LOCKED-LOOSE state
        SetState(ValveState.Locked, ValveSubstate.Loose);
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} → LOCKED-LOOSE after confirmed socket positioning");
    }
    
    /// <summary>
    /// Called by SnapValidator when valve is removed from socket
    /// </summary>
    public void OnSocketReleased(GameObject socket)
    {
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} released from socket: {socket.name}");
        
        // Clean up socket reference and stop any monitoring
        if (currentSocket == socket)
        {
            // Stop any running positioning coroutines
            StopAllCoroutines();
            
            // Only change state if we're currently unlocked (removable)
            if (currentState == ValveState.Unlocked)
            {
                currentSocket = null;
                currentSocketInteractor = null;
                OnValveRemoved?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Track rotation changes and check for state transitions
    /// </summary>
    private void TrackRotation()
    {
        if (profile == null) return;
        
        Vector3 currentRotationVector = transform.eulerAngles;
        Vector3 deltaRotation = currentRotationVector - lastRotation;
        
        // Handle angle wrapping
        if (deltaRotation.x > 180) deltaRotation.x -= 360;
        if (deltaRotation.y > 180) deltaRotation.y -= 360;
        if (deltaRotation.z > 180) deltaRotation.z -= 360;
        if (deltaRotation.x < -180) deltaRotation.x += 360;
        if (deltaRotation.y < -180) deltaRotation.y += 360;
        if (deltaRotation.z < -180) deltaRotation.z += 360;
        
        // Calculate rotation based on specified axis
        float axisRotation = Vector3.Dot(deltaRotation, profile.rotationAxis);
        totalRotation += axisRotation;
        currentRotationAngle = totalRotation;
        
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
            case ValveSubstate.Loose:
                // Check if tightened enough to transition to TIGHT
                float tighteningProgress = totalRotation;
                if (tighteningProgress >= profile.tightenThreshold - profile.angleTolerance)
                {
                    VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} TIGHTENED! {tighteningProgress:F1}° reached (threshold: {profile.tightenThreshold}°)");
                    TransitionToTight();
                }
                else
                {
                    // Log rotation progress periodically
                    if (Mathf.FloorToInt(tighteningProgress / 10f) != Mathf.FloorToInt(lastLoggedRotation / 10f))
                    {
                        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} tightening: {tighteningProgress:F1}° (threshold: {profile.tightenThreshold}°)");
                        lastLoggedRotation = tighteningProgress;
                    }
                }
                break;
                
            case ValveSubstate.Tight:
                // Check if loosened enough to allow removal
                float looseningProgress = -totalRotation; // Negative rotation becomes positive progress
                if (looseningProgress >= profile.loosenThreshold - profile.angleTolerance)
                {
                    VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOOSENED! {looseningProgress:F1}° loosening completed");
                    TransitionToLooseAfterTight();
                }
                else
                {
                    // Log loosening progress periodically
                    if (Mathf.FloorToInt(looseningProgress / 10f) != Mathf.FloorToInt(lastLoggedRotation / 10f))
                    {
                        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} loosening: {looseningProgress:F1}° (threshold: {profile.loosenThreshold}°)");
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
        SetState(ValveState.Locked, ValveSubstate.Tight);
        ResetRotationTracking(); // Reset for loosening phase
        
        OnValveTightened?.Invoke();
    }
    
    /// <summary>
    /// Transition from TIGHT back to LOOSE (after loosening)
    /// Set flag for release-triggered socket re-enable to prevent visual jarring
    /// </summary>
    private void TransitionToLooseAfterTight()
    {
        // Set flag to enable socket when user releases grab (prevents visual jarring during rotation)
        readyForSocketReEnable = true;
        
        SetState(ValveState.Locked, ValveSubstate.Loose);
        
        // Set flag to wait for grab release
        isWaitingForGrabRelease = true;
        
        OnValveLoosened?.Invoke();
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} loosened - ready for socket re-enable on release");
    }
    
    /// <summary>
    /// Wait for valve to snap back to socket, then transition to unlocked
    /// </summary>
    private IEnumerator TransitionToUnlockedAfterSnap()
    {
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} waiting for snap-back to socket...");
        
        // Wait a moment for physics to settle and object to snap back
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        
        // Transition to unlocked state - object should now be in socket but removable
        TransitionToUnlocked();
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} snap-back complete - now UNLOCKED in socket");
    }
    
    /// <summary>
    /// Final transition to UNLOCKED state (after grab release)
    /// </summary>
    private void TransitionToUnlocked()
    {
        SetState(ValveState.Unlocked, ValveSubstate.None);
        currentSocket = null;
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} now UNLOCKED and removable");
    }
    
    /// <summary>
    /// Update progress events based on current substate
    /// </summary>
    private void UpdateProgressEvents()
    {
        switch (currentSubstate)
        {
            case ValveSubstate.Loose:
                float tighteningProgress = Mathf.Clamp01(totalRotation / profile.tightenThreshold);
                OnTighteningProgress?.Invoke(tighteningProgress);
                break;
                
            case ValveSubstate.Tight:
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
            case ValveSubstate.Loose:
                if (profile.looseMaterial != null)
                    valveRenderer.material = profile.looseMaterial;
                break;
                
            case ValveSubstate.Tight:
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
            Debug.Log($"[ValveController] {gameObject.name} OnValidate() - Profile: {profile.profileName}, State: {currentState}-{currentSubstate}");
        }
    }
    
    /// <summary>
    /// Manual test methods for debugging socket interaction (Editor only)
    /// </summary>
    [ContextMenu("Test: Force Enable Socket")]
    private void TestForceEnableSocket()
    {
        EnableSocketInteractor();
        Debug.Log($"[ValveController] TEST: Manually enabled socket for {gameObject.name}");
    }
    
    [ContextMenu("Test: Force Disable Socket")]
    private void TestForceDisableSocket()
    {
        DisableSocketInteractor();
        Debug.Log($"[ValveController] TEST: Manually disabled socket for {gameObject.name}");
    }
    
    [ContextMenu("Test: Transition to Unlocked")]
    private void TestTransitionToUnlocked()
    {
        TransitionToUnlocked();
        Debug.Log($"[ValveController] TEST: Manually transitioned to unlocked for {gameObject.name}");
    }
    #endif
}
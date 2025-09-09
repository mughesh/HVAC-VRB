// ValveController.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;
using System.Collections;

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
    private Vector3 snapPosition;
    private Quaternion snapRotation;
    
    // Rotation tracking
    private Vector3 lastRotation;
    private float totalRotation = 0f;
    private float lastLoggedRotation = 0f;
    
    // State management
    private bool isWaitingForGrabRelease = false;
    
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
        
        // Track rotation when grabbed AND in locked state
        if (grabInteractable.isSelected && currentState == ValveState.Locked)
        {
            TrackRotation();
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
        
        // Check if we're waiting for release to unlock (after loosening)
        if (isWaitingForGrabRelease && currentState == ValveState.Locked && currentSubstate == ValveSubstate.Loose)
        {
            VRTrainingDebug.LogEvent($"[ValveController] Grab released after loosening - transitioning to UNLOCKED");
            isWaitingForGrabRelease = false;
            TransitionToUnlocked();
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
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} state changed: {previousState}-{previousSubstate} → {currentState}-{currentSubstate}");
        
        // Handle state-specific setup
        switch (currentState)
        {
            case ValveState.Unlocked:
                UnlockValve();
                break;
                
            case ValveState.Locked:
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
    /// Apply constraints for locked state (both LOOSE and TIGHT substates)
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
                
                // Disable socket interactor so valve cannot be removed
                DisableSocketInteractor();
                
                VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} LOCKED - position fixed, rotation on {profile.rotationAxis}, socket disabled");
            }
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
                VRTrainingDebug.LogEvent($"[ValveController] Disabled socket interactor on {currentSocket.name}");
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
                VRTrainingDebug.LogEvent($"[ValveController] Re-enabled socket interactor on {currentSocket.name}");
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
        
        // Reset rotation tracking for new snap
        ResetRotationTracking();
        
        // Transition to LOCKED-LOOSE state
        SetState(ValveState.Locked, ValveSubstate.Loose);
        
        OnValveSnapped?.Invoke();
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} snapped to socket: {socket.name} → LOCKED-LOOSE");
    }
    
    /// <summary>
    /// Called by SnapValidator when valve is removed from socket
    /// </summary>
    public void OnSocketReleased(GameObject socket)
    {
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} released from socket: {socket.name}");
        
        // Only change state if we're currently in this socket and unlocked
        if (currentSocket == socket && currentState == ValveState.Unlocked)
        {
            currentSocket = null;
            OnValveRemoved?.Invoke();
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
    /// CRITICAL: Re-enable socket while user still holds grab
    /// </summary>
    private void TransitionToLooseAfterTight()
    {
        // CRITICAL: Re-enable socket interactor BEFORE state change
        EnableSocketInteractor();
        
        SetState(ValveState.Locked, ValveSubstate.Loose);
        
        // Set flag to wait for grab release
        isWaitingForGrabRelease = true;
        
        OnValveLoosened?.Invoke();
        
        VRTrainingDebug.LogEvent($"[ValveController] {gameObject.name} loosened - socket re-enabled, waiting for grab release");
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
    #endif
}
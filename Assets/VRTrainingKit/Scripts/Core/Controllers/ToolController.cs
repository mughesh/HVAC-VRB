// ToolController.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Controls complex tool behavior with grab → snap → rotate interactions
/// Manages tool state transitions and socket compatibility
/// </summary>
public class ToolController : MonoBehaviour
{
    [Header("Profile Configuration")]
    [SerializeField] private ToolProfile profile;
    
    [Header("Runtime State")]
    [SerializeField] private ToolState currentState;
    [SerializeField] private float currentRotationAngle = 0f;
    [SerializeField] private bool isInitialized = false;
    
    // Component references
    private XRGrabInteractable grabInteractable;
    private Rigidbody rigidBody;
    private Transform originalParent;
    
    // Socket interaction
    private GameObject currentSocket;
    private Vector3 snapPosition;
    private Quaternion snapRotation;
    
    // Rotation tracking
    private Vector3 lastRotation;
    private float totalRotation = 0f;
    private float lastLoggedRotation = 0f;
    
    // Events
    public event Action<ToolState> OnStateChanged;
    public event Action<float> OnRotationChanged;
    public event Action OnTightened;
    public event Action OnLoosened;
    
    // Public properties
    public ToolState CurrentState => currentState;
    public float CurrentRotationAngle => currentRotationAngle;
    public bool IsLocked => currentState == ToolState.Locked;
    public bool IsSnapped => currentState == ToolState.Snapped;
    public bool IsUnlocked => currentState == ToolState.Unlocked;
    
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rigidBody = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        
        // Initialize with unlocked state
        currentState = ToolState.Unlocked;
        lastRotation = transform.eulerAngles;
        
        Debug.Log($"[ToolController] {gameObject.name} Awake() - Initial state: {currentState}");
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
    /// Configure this tool with a profile
    /// </summary>
    public void Configure(ToolProfile toolProfile)
    {
        var previousProfile = profile?.profileName ?? "NULL";
        profile = toolProfile;
        
        // Set initial state from profile
        SetState(profile.initialState);
        
        // Reset rotation tracking
        currentRotationAngle = 0f;
        totalRotation = 0f;
        lastRotation = transform.eulerAngles;
        
        isInitialized = true;
        
        Debug.Log($"[ToolController] Configure() called for {gameObject.name}: " +
                 $"Previous={previousProfile} → New={profile.profileName}, " +
                 $"InitialState={profile.initialState}");
                 
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    private void Update()
    {
        if (!isInitialized || profile == null) return;
        
        // Track rotation when grabbed AND in snapped or locked state
        if (grabInteractable.isSelected && (currentState == ToolState.Snapped || currentState == ToolState.Locked))
        {
            TrackRotation();
        }
    }
    
    /// <summary>
    /// Check if a socket is compatible with this tool (used by SnapValidator)
    /// </summary>
    public List<GameObject> FindCompatibleSockets(float maxDistance = 1.0f)
    {
        List<GameObject> compatibleSockets = new List<GameObject>();
        
        if (profile == null) return compatibleSockets;
        
        // Find all potential sockets
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            // Skip if too far away
            if (Vector3.Distance(transform.position, obj.transform.position) > maxDistance)
                continue;
                
            // Check socket compatibility
            if (IsSocketCompatible(obj))
            {
                compatibleSockets.Add(obj);
            }
        }
        
        return compatibleSockets;
    }
    
    /// <summary>
    /// Check if a socket is compatible with this tool
    /// </summary>
    public bool IsSocketCompatible(GameObject socket)
    {
        if (profile == null) return false;
        
        // Check if socket has XRSocketInteractor (required for snapping)
        if (socket.GetComponent<XRSocketInteractor>() == null) return false;
        
        // Check specific sockets first
        if (profile.requireSpecificSockets && profile.specificCompatibleSockets != null)
        {
            foreach (var socketRef in profile.specificCompatibleSockets)
            {
                if (socketRef.GameObject == socket)
                    return true;
            }
            return false;
        }
        
        // Check compatible tags
        if (profile.compatibleSocketTags != null && profile.compatibleSocketTags.Length > 0)
        {
            foreach (string tag in profile.compatibleSocketTags)
            {
                if (socket.CompareTag(tag))
                    return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Called when tool is grabbed
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} grabbed in state: {currentState}");
        
        switch (currentState)
        {
            case ToolState.Unlocked:
                // Normal grab behavior - tool can be moved freely
                break;
                
            case ToolState.Snapped:
                // Tool is snapped but not locked - could transition to locked state
                break;
                
            case ToolState.Locked:
                // Tool is locked - prevent movement, only allow rotation
                LockToolPosition();
                break;
        }
    }
    
    /// <summary>
    /// Called when tool is released
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} released in state: {currentState}");
        
        // Note: Socket snapping is now handled by XRI socket system + SnapValidator
        // No manual socket detection needed here
    }
    
    /// <summary>
    /// Set the tool state
    /// </summary>
    private void SetState(ToolState newState)
    {
        if (currentState == newState) return;
        
        ToolState previousState = currentState;
        currentState = newState;
        
        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} state changed: {previousState} → {currentState}");
        
        // Handle state-specific setup
        switch (currentState)
        {
            case ToolState.Unlocked:
                UnlockTool();
                break;
                
            case ToolState.Snapped:
                ApplySnappedConstraints();
                break;
                
            case ToolState.Locked:
                LockToolPosition();
                break;
        }
        
        OnStateChanged?.Invoke(currentState);
    }
    
    /// <summary>
    /// Lock tool in position, only allow rotation (same as ApplySnappedConstraints but for locked state)
    /// </summary>
    private void LockToolPosition()
    {
        if (rigidBody != null)
        {
            // Make kinematic to prevent physics movement
            rigidBody.isKinematic = true;
            
            // Set constraints to freeze position but allow rotation on specified axis
            RigidbodyConstraints constraints = RigidbodyConstraints.FreezePosition;
            
            if (profile != null)
            {
                // Allow rotation only on the specified axis
                if (profile.rotationAxis.x == 0) constraints |= RigidbodyConstraints.FreezeRotationX;
                if (profile.rotationAxis.y == 0) constraints |= RigidbodyConstraints.FreezeRotationY;
                if (profile.rotationAxis.z == 0) constraints |= RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                // Default to Y-axis rotation only
                constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            rigidBody.constraints = constraints;
            
            // Disable socket interactor so tool cannot be removed
            DisableSocketInteractor();
            
            VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} LOCKED - position fixed, socket disabled");
        }
    }
    
    /// <summary>
    /// Disable socket interactor to prevent tool removal when locked
    /// </summary>
    private void DisableSocketInteractor()
    {
        if (currentSocket != null)
        {
            XRSocketInteractor socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null)
            {
                socketInteractor.socketActive = false;
                VRTrainingDebug.LogEvent($"[ToolController] Disabled socket interactor on {currentSocket.name}");
            }
        }
    }
    
    /// <summary>
    /// Re-enable socket interactor when tool is unlocked
    /// </summary>
    private void EnableSocketInteractor()
    {
        if (currentSocket != null)
        {
            XRSocketInteractor socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null)
            {
                socketInteractor.socketActive = true;
                VRTrainingDebug.LogEvent($"[ToolController] Re-enabled socket interactor on {currentSocket.name}");
            }
        }
    }
    
    /// <summary>
    /// Unlock tool for free movement
    /// </summary>
    private void UnlockTool()
    {
        if (rigidBody != null)
        {
            // Remove kinematic mode
            rigidBody.isKinematic = false;
            
            // Remove all constraints
            rigidBody.constraints = RigidbodyConstraints.None;
            
            // Re-enable socket interactor if it was disabled
            EnableSocketInteractor();
            
            VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} unlocked for free movement");
        }
    }
    
    /// <summary>
    /// Apply physics constraints when tool is snapped (allows rotation on specified axis)
    /// </summary>
    private void ApplySnappedConstraints()
    {
        if (rigidBody != null && profile != null)
        {
            // Make kinematic to prevent physics movement but allow XRI manipulation
            rigidBody.isKinematic = true;
            
            // Set constraints to freeze position but allow rotation on specified axis
            RigidbodyConstraints constraints = RigidbodyConstraints.FreezePosition;
            
            // Allow rotation only on the specified axis
            if (profile.rotationAxis.x == 0) constraints |= RigidbodyConstraints.FreezeRotationX;
            if (profile.rotationAxis.y == 0) constraints |= RigidbodyConstraints.FreezeRotationY;
            if (profile.rotationAxis.z == 0) constraints |= RigidbodyConstraints.FreezeRotationZ;
            
            rigidBody.constraints = constraints;
            
            VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} snapped - applying kinematic constraints (rotation axis: {profile.rotationAxis})");
        }
        else
        {
            VRTrainingDebug.LogError($"[ToolController] Cannot apply snapped constraints - missing rigidbody or profile");
        }
    }
    
    /// <summary>
    /// Reset rotation tracking when tool snaps to socket
    /// </summary>
    private void ResetRotationTracking()
    {
        totalRotation = 0f;
        currentRotationAngle = 0f;
        lastRotation = transform.eulerAngles;
        
        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} rotation tracking reset");
    }
    
    /// <summary>
    /// Called by SnapValidator when tool is snapped to a compatible socket
    /// </summary>
    public void OnSocketSnapped(GameObject socket)
    {
        currentSocket = socket;
        snapPosition = socket.transform.position;
        snapRotation = socket.transform.rotation;
        
        // Reset rotation tracking for new snap
        ResetRotationTracking();
        
        SetState(ToolState.Snapped);
        
        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} snapped to socket: {socket.name}");
    }
    
    /// <summary>
    /// Called by SnapValidator when tool is removed from socket
    /// </summary>
    public void OnSocketReleased(GameObject socket)
    {
        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} released from socket: {socket.name}");
        
        // Only change state if we're currently snapped to this socket
        if (currentSocket == socket && currentState == ToolState.Snapped)
        {
            SetState(ToolState.Unlocked);
            currentSocket = null;
        }
    }
    
    /// <summary>
    /// Track rotation changes
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
        
        OnRotationChanged?.Invoke(currentRotationAngle);
    }
    
    /// <summary>
    /// Check if rotation thresholds are met
    /// </summary>
    private void CheckRotationThresholds()
    {
        if (profile == null) return;
        
        switch (currentState)
        {
            case ToolState.Snapped:
                // Check if tightened enough to lock
                float rotationProgress = Mathf.Abs(currentRotationAngle);
                if (rotationProgress >= profile.tightenThreshold - profile.angleTolerance)
                {
                    VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} TIGHTENED! {rotationProgress:F1}° reached (threshold: {profile.tightenThreshold}°)");
                    SetState(ToolState.Locked);
                    OnTightened?.Invoke();
                    
                    // Reset rotation tracking for loosening
                    totalRotation = 0f;
                    currentRotationAngle = 0f;
                }
                else
                {
                    // Log rotation progress periodically
                    if (Mathf.FloorToInt(rotationProgress / 10f) != Mathf.FloorToInt(lastLoggedRotation / 10f))
                    {
                        VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} rotation: {rotationProgress:F1}° (threshold: {profile.tightenThreshold}°)");
                        lastLoggedRotation = rotationProgress;
                    }
                }
                break;
                
            case ToolState.Locked:
                // Check if loosened enough to unlock
                if (Mathf.Abs(currentRotationAngle) >= profile.loosenThreshold - profile.angleTolerance)
                {
                    VRTrainingDebug.LogEvent($"[ToolController] {gameObject.name} LOOSENED! Unlocking...");
                    SetState(ToolState.Unlocked);
                    OnLoosened?.Invoke();
                    
                    // Reset socket reference
                    currentSocket = null;
                }
                break;
        }
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Update debug info in editor
        if (profile != null)
        {
            Debug.Log($"[ToolController] {gameObject.name} OnValidate() - Profile: {profile.profileName}, State: {currentState}");
        }
    }
    #endif
}
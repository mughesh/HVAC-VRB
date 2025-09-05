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
        
        // Track rotation when grabbed or locked
        if (grabInteractable.isSelected)
        {
            TrackRotation();
        }
    }
    
    /// <summary>
    /// Find compatible sockets in range
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
            return profile.specificCompatibleSockets.Contains(socket);
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
        Debug.Log($"[ToolController] {gameObject.name} grabbed in state: {currentState}");
        
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
        Debug.Log($"[ToolController] {gameObject.name} released in state: {currentState}");
        
        // Check for snap to compatible socket
        if (currentState == ToolState.Unlocked)
        {
            CheckForSnapToSocket();
        }
    }
    
    /// <summary>
    /// Set the tool state
    /// </summary>
    private void SetState(ToolState newState)
    {
        if (currentState == newState) return;
        
        ToolState previousState = currentState;
        currentState = newState;
        
        Debug.Log($"[ToolController] {gameObject.name} state changed: {previousState} → {currentState}");
        
        // Handle state-specific setup
        switch (currentState)
        {
            case ToolState.Unlocked:
                UnlockTool();
                break;
                
            case ToolState.Snapped:
                // Tool is snapped but not locked yet
                break;
                
            case ToolState.Locked:
                LockToolPosition();
                break;
        }
        
        OnStateChanged?.Invoke(currentState);
    }
    
    /// <summary>
    /// Lock tool in position, only allow rotation
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
            
            Debug.Log($"[ToolController] {gameObject.name} position locked, rotation constraints set");
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
            
            Debug.Log($"[ToolController] {gameObject.name} unlocked for free movement");
        }
    }
    
    /// <summary>
    /// Check if tool should snap to nearby socket
    /// </summary>
    private void CheckForSnapToSocket()
    {
        List<GameObject> compatibleSockets = FindCompatibleSockets(0.5f); // 50cm range
        
        if (compatibleSockets.Count > 0)
        {
            // Find closest socket
            GameObject closestSocket = compatibleSockets
                .OrderBy(socket => Vector3.Distance(transform.position, socket.transform.position))
                .First();
                
            // Check if socket can accept this tool
            XRSocketInteractor socketInteractor = closestSocket.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null && !socketInteractor.hasSelection)
            {
                // Snap to socket
                SnapToSocket(closestSocket);
            }
        }
    }
    
    /// <summary>
    /// Snap tool to socket
    /// </summary>
    private void SnapToSocket(GameObject socket)
    {
        currentSocket = socket;
        snapPosition = socket.transform.position;
        snapRotation = socket.transform.rotation;
        
        // Move tool to socket position
        transform.position = snapPosition;
        transform.rotation = snapRotation;
        
        SetState(ToolState.Snapped);
        
        Debug.Log($"[ToolController] {gameObject.name} snapped to socket: {socket.name}");
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
                if (Mathf.Abs(currentRotationAngle) >= profile.tightenThreshold - profile.angleTolerance)
                {
                    SetState(ToolState.Locked);
                    OnTightened?.Invoke();
                    
                    // Reset rotation tracking for loosening
                    totalRotation = 0f;
                    currentRotationAngle = 0f;
                }
                break;
                
            case ToolState.Locked:
                // Check if loosened enough to unlock
                if (Mathf.Abs(currentRotationAngle) >= profile.loosenThreshold - profile.angleTolerance)
                {
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
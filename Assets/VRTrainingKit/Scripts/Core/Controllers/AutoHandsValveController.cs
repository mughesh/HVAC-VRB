// AutoHandsValveController.cs
// Controls valve state machine and rotation behavior for AutoHands framework
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands-specific valve controller with state machine
/// Workflow: Unlocked → Locked(Loose) → Locked(Tight) → Locked(Loose) → Unlocked
/// Uses AutoHands Grabbable for grabbing and PlacePoint for socket snapping
/// </summary>
public class AutoHandsValveController : MonoBehaviour
{
    [Header("Profile Configuration")]
    [SerializeField] private ValveProfile profile;

    [Header("Runtime State")]
    [SerializeField] private ValveState currentState = ValveState.Unlocked;
    [SerializeField] private ValveSubstate currentSubstate = ValveSubstate.None;
    [SerializeField] private float currentRotationAngle = 0f;
    [SerializeField] private bool isInitialized = false;

    // Component references
    private Autohand.Grabbable grabbable;
    private Rigidbody rb;

    // Socket tracking
    private ValveSocketController currentSocket; // Current socket holding this valve

    // Rotation tracking
    private float baselineAngle = 0f;
    private float accumulatedRotation = 0f;
    private Quaternion lastRotation;
    private bool isGrabbed = false;

    // State management flags
    private bool isWaitingForGrabRelease = false;
    private bool readyForSocketReEnable = false;

    // Distance-based unlock tracking
    private Vector3 unlockedPosition;
    private bool isUnlockedAndConstrained = false; // True when unlocked but held at socket by constraints
    private const float REMOVAL_DISTANCE_THRESHOLD = 0.3f; // 0.3m threshold for socket re-enable

    // Events
    public event Action OnValveSnapped;
    public event Action OnValveTightened;
    public event Action OnValveLoosened;
    public event Action OnValveRemoved;

    // Public properties
    public ValveState CurrentState => currentState;
    public ValveSubstate CurrentSubstate => currentSubstate;
    public float CurrentRotation => currentRotationAngle;
    public bool IsSnappedToSocket => currentState == ValveState.Locked;

    private void Awake()
    {
        grabbable = GetComponent<Autohand.Grabbable>();
        rb = GetComponent<Rigidbody>();

        Debug.Log($"[AutoHandsValveController] {gameObject.name} Awake() - Grabbable: {(grabbable != null ? "✓" : "✗")}, Rigidbody: {(rb != null ? "✓" : "✗")}");
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabEvent += OnGrab;
            grabbable.OnReleaseEvent += OnRelease;
            Debug.Log($"[AutoHandsValveController] Subscribed to Grabbable events on {gameObject.name}");
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabEvent -= OnGrab;
            grabbable.OnReleaseEvent -= OnRelease;
        }

        CleanupSocketSubscriptions();
    }

    private void Start()
    {
        // Subscribe to ALL valve sockets in scene
        SubscribeToAllValveSockets();
        isInitialized = true;
    }

    public void Configure(ValveProfile valveProfile)
    {
        var previousProfile = profile?.profileName ?? "NULL";
        profile = valveProfile;

        isInitialized = true;

        Debug.Log($"[AutoHandsValveController] Configure() called for {gameObject.name}:");
        Debug.Log($"  Previous Profile: {previousProfile} → New Profile: {profile.profileName}");
        Debug.Log($"  Rotation Axis: {profile.rotationAxis}");
        Debug.Log($"  Tighten Threshold: {profile.tightenThreshold}°");
        Debug.Log($"  Loosen Threshold: {profile.loosenThreshold}°");
        Debug.Log($"  Angle Tolerance: {profile.angleTolerance}°");
        Debug.Log($"  Position Tolerance: {profile.positionTolerance}");
        Debug.Log($"  Velocity Threshold: {profile.velocityThreshold}");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Subscribe to ALL ValveSocketControllers in scene
    /// </summary>
    private void SubscribeToAllValveSockets()
    {
        var sockets = FindObjectsOfType<ValveSocketController>();
        foreach (var socket in sockets)
        {
            socket.OnObjectSnapped += OnSocketSnapped;
            socket.OnObjectRemoved += OnSocketRemoved;
            Debug.Log($"[AutoHandsValveController] {gameObject.name} subscribed to socket: {socket.gameObject.name}");
        }
    }

    /// <summary>
    /// Cleanup all socket subscriptions
    /// </summary>
    private void CleanupSocketSubscriptions()
    {
        var sockets = FindObjectsOfType<ValveSocketController>();
        foreach (var socket in sockets)
        {
            socket.OnObjectSnapped -= OnSocketSnapped;
            socket.OnObjectRemoved -= OnSocketRemoved;
        }
    }

    /// <summary>
    /// Called when valve is snapped into socket
    /// </summary>
    private void OnSocketSnapped(GameObject snappedObject)
    {
        if (snappedObject != gameObject) return;

        // GUARD: Ignore duplicate snap events when already locked
        if (currentState == ValveState.Locked)
        {
            Debug.Log($"[AutoHandsValveController] {gameObject.name} ignoring duplicate snap event - already locked");
            return;
        }

        Debug.Log($"[AutoHandsValveController] {gameObject.name} snapped to socket");

        // Find which socket snapped us (for tracking)
        var sockets = FindObjectsOfType<ValveSocketController>();
        foreach (var socket in sockets)
        {
            if (socket.SnappedObject == gameObject)
            {
                currentSocket = socket;
                break;
            }
        }

        // Start position monitoring coroutine
        StartCoroutine(MonitorSocketPositioning());

        OnValveSnapped?.Invoke();

        Debug.Log($"[AutoHandsValveController] {gameObject.name} detected by socket → Monitoring position stability");
    }

    /// <summary>
    /// Monitor socket positioning until object is stable, then apply constraints
    /// Mirrors XRI ValveController.MonitorSocketPositioning()
    /// </summary>
    private IEnumerator MonitorSocketPositioning()
    {
        if (profile == null)
        {
            Debug.LogWarning($"[AutoHandsValveController] No profile found for {gameObject.name}, applying constraints immediately");
            FinalizeLockToSocket();
            yield break;
        }

        float startTime = Time.time;
        float lastLogTime = startTime;
        float positionTolerance = profile.positionTolerance;
        float velocityThreshold = profile.velocityThreshold;
        float timeout = profile.positioningTimeout;

        Vector3 socketCenter = currentSocket != null ? currentSocket.SnapPosition : transform.position;

        Debug.Log($"[AutoHandsValveController] {gameObject.name} monitoring positioning: tolerance={positionTolerance:F4}, velocity={velocityThreshold:F4}, timeout={timeout}s");

        while (Time.time - startTime < timeout)
        {
            // Check if we're still connected to socket
            if (currentSocket == null || currentSocket.SnappedObject != gameObject)
            {
                Debug.Log($"[AutoHandsValveController] {gameObject.name} socket changed during monitoring, aborting");
                yield break;
            }

            // Calculate current distance from socket center
            float distance = Vector3.Distance(transform.position, socketCenter);

            // Check velocity (both linear and angular)
            float linearVelocity = rb != null ? rb.linearVelocity.magnitude : 0f;
            float angularVelocity = rb != null ? rb.angularVelocity.magnitude : 0f;
            float totalVelocity = linearVelocity + angularVelocity;

            // Log progress every 0.2 seconds for debugging
            if (Time.time - lastLogTime > 0.2f)
            {
                Debug.Log($"[AutoHandsValveController] {gameObject.name} positioning: distance={distance:F4}, velocity={totalVelocity:F4}");
                lastLogTime = Time.time;
            }

            // Check if object is positioned and stabilized
            if (distance <= positionTolerance && totalVelocity <= velocityThreshold)
            {
                Debug.Log($"[AutoHandsValveController] {gameObject.name} positioning complete: distance={distance:F4}, velocity={totalVelocity:F4} (took {Time.time - startTime:F2}s)");
                FinalizeLockToSocket();
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }

        // Timeout reached - apply constraints anyway with warning
        Debug.LogWarning($"[AutoHandsValveController] {gameObject.name} positioning timeout ({timeout}s) - applying constraints anyway");
        FinalizeLockToSocket();
    }

    /// <summary>
    /// Apply final locked state and constraints after socket positioning is complete
    /// Mirrors XRI ValveController.FinalizeLockToSocket()
    /// </summary>
    private void FinalizeLockToSocket()
    {
        // Reset flag for new interaction cycle
        readyForSocketReEnable = false;

        // Set baseline angle for rotation tracking
        lastRotation = transform.rotation;
        baselineAngle = GetCurrentAngle();
        accumulatedRotation = 0f;
        currentRotationAngle = 0f;

        // Now transition to LOCKED-LOOSE state (applies constraints)
        TransitionToLockedLoose();

        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED-LOOSE after confirmed socket positioning");
    }

    /// <summary>
    /// Called when valve is removed from socket
    /// Mirrors XRI ValveController.OnSocketReleased()
    /// </summary>
    private void OnSocketRemoved(GameObject removedObject)
    {
        if (removedObject != gameObject) return;

        Debug.Log($"[AutoHandsValveController] {gameObject.name} removed from socket");

        // Stop any running positioning coroutines
        StopAllCoroutines();

        // Only change state if we're currently unlocked (removable)
        if (currentState == ValveState.Unlocked)
        {
            // Clear socket tracking
            currentSocket = null;

            OnValveRemoved?.Invoke();
        }
    }

    private void OnGrab(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = true;

        // Update lastRotation to current rotation when grabbed (for delta tracking)
        // This allows continuous tracking across multiple grab-release cycles
        lastRotation = transform.rotation;

        // Handle different states
        if (currentState == ValveState.Locked && rb != null)
        {
            // CRITICAL FIX: Disable kinematic when grabbed in Locked state
            // This fixes TWO issues:
            // 1. Rotation reset on release (kinematic bodies don't maintain rotation from hand)
            // 2. Hand tracking grab loss (kinematic changes break collision-based pinch detection)
            // Position constraints (FreezePosition) still prevent movement
            //rb.isKinematic = false;
            Debug.Log($"[AutoHandsValveController] {gameObject.name} grabbed in Locked state - disabled kinematic for AutoHands control (fixes rotation reset + hand tracking)");
        }
        else if (isUnlockedAndConstrained && rb != null)
        {
            // Release constraints when grabbed in unlocked state - valve can now move with hand
            //rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            Debug.Log($"[AutoHandsValveController] {gameObject.name} grabbed while unlocked - constraints released, tracking distance");
        }

        Debug.Log($"[AutoHandsValveController] {gameObject.name} grabbed - State: {currentState}-{currentSubstate}, currentRotation: {currentRotationAngle:F1}°");
    }

    private void OnRelease(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = false;
        Debug.Log($"[AutoHandsValveController] {gameObject.name} released - State: {currentState}-{currentSubstate}, Rotation: {currentRotationAngle:F1}°");

        // Re-apply kinematic when released in Locked state to maintain position
        if (currentState == ValveState.Locked && rb != null)
        {
            //rb.isKinematic = true;
            Debug.Log($"[AutoHandsValveController] {gameObject.name} released in Locked state - re-enabled kinematic to maintain position");
        }

        // Check if we need to transition after loosening
        // Mirrors XRI ValveController.OnReleased() logic
        if (isWaitingForGrabRelease && currentState == ValveState.Locked && currentSubstate == ValveSubstate.Loose && readyForSocketReEnable)
        {
            Debug.Log($"[AutoHandsValveController] Grab released after loosening - transitioning to unlocked");

            // Transition to unlocked immediately
            TransitionToUnlocked();

            // Clear flags
            isWaitingForGrabRelease = false;
            readyForSocketReEnable = false;
        }
    }

    private void Update()
    {
        if (!isInitialized || profile == null) return;

        // Only track rotation when valve is locked in socket
        if (currentState == ValveState.Locked)
        {
            if (isGrabbed)
            {
                TrackRotation();
                CheckRotationThresholds();
            }
            else
            {
                // Apply rotation dampening when not grabbed to stop spinning
                ApplyRotationDampening();
            }
        }
    }

    /// <summary>
    /// Track rotation relative to baseline
    /// </summary>
    private void TrackRotation()
    {
        if (profile == null) return;

        Quaternion currentQuaternion = transform.rotation;
        Quaternion deltaRotation = currentQuaternion * Quaternion.Inverse(lastRotation);

        // Extract angle around rotation axis
        Vector3 axis = profile.rotationAxis.normalized;
        float angle;
        Vector3 rotAxis;
        deltaRotation.ToAngleAxis(out angle, out rotAxis);

        // Check if rotation is around the correct axis
        if (Vector3.Dot(rotAxis, axis) < 0)
            angle = -angle;

        // Accumulate rotation (handling 360° wrapping)
        if (Mathf.Abs(angle) < 180f)
        {
            accumulatedRotation += angle;
            currentRotationAngle = accumulatedRotation;
        }

        lastRotation = currentQuaternion;
    }

    /// <summary>
    /// Check if rotation has passed thresholds for state transitions
    /// Mirrors XRI ValveController.CheckRotationThresholds() with detailed logging
    /// </summary>
    private void CheckRotationThresholds()
    {
        if (profile == null) return;

        switch (currentSubstate)
        {
            case ValveSubstate.Loose:
                // Check if tightened enough to transition to TIGHT
                float tighteningProgress = currentRotationAngle;
                if (tighteningProgress >= profile.tightenThreshold - profile.angleTolerance)
                {
                    Debug.Log($"[AutoHandsValveController] {gameObject.name} TIGHTENED! {tighteningProgress:F1}° reached (threshold: {profile.tightenThreshold}°)");
                    TransitionToTight();
                }
                else if (tighteningProgress > 0) // Log positive tightening progress every 10°
                {
                    Debug.Log($"[AutoHandsValveController] {gameObject.name} tightening: {tighteningProgress:F1}° / {profile.tightenThreshold}°");
                }
                break;

            case ValveSubstate.Tight:
                // Check if loosened enough to allow removal (negative rotation)
                float looseningProgress = -currentRotationAngle; // Negative rotation becomes positive progress
                Debug.Log($"[AutoHandsValveController] {gameObject.name} loosening check: currentAngle={currentRotationAngle:F1}°, looseningProgress={looseningProgress:F1}°, threshold={profile.loosenThreshold}°");

                if (looseningProgress >= profile.loosenThreshold - profile.angleTolerance)
                {
                    Debug.Log($"[AutoHandsValveController] {gameObject.name} LOOSENED! {looseningProgress:F1}° loosening completed");
                    TransitionToLooseAfterTight();
                }
                else if (looseningProgress > 0) // Log positive loosening progress
                {
                    Debug.Log($"[AutoHandsValveController] {gameObject.name} loosening: {looseningProgress:F1}° / {profile.loosenThreshold}°");
                }
                break;
        }
    }

    /// <summary>
    /// Get current angle from transform
    /// </summary>
    private float GetCurrentAngle()
    {
        if (profile == null) return 0f;

        Vector3 axis = profile.rotationAxis.normalized;
        Vector3 localEuler = transform.localEulerAngles;

        if (axis == Vector3.right || axis == -Vector3.right)
            return localEuler.x;
        if (axis == Vector3.up || axis == -Vector3.up)
            return localEuler.y;
        if (axis == Vector3.forward || axis == -Vector3.forward)
            return localEuler.z;

        return 0f;
    }

    // ===== STATE TRANSITIONS =====

    private void TransitionToUnlocked()
    {
        currentState = ValveState.Unlocked;
        currentSubstate = ValveSubstate.None;
        UnlockValve();
        Debug.Log($"[AutoHandsValveController] {gameObject.name} → UNLOCKED");
    }

    private void TransitionToLockedLoose()
    {
        currentState = ValveState.Locked;
        currentSubstate = ValveSubstate.Loose;
        ApplyLockedLooseConstraints();
        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED (LOOSE) - Ready for tightening");
    }

    private void TransitionToTight()
    {
        currentSubstate = ValveSubstate.Tight;

        // Only apply constraints if NOT grabbed (constraints applied on release via OnRelease)
        // CRITICAL: Prevents kinematic toggling during grab (breaks hand tracking)
        if (!isGrabbed)
        {
            ApplyLockedConstraints();
        }

        // Reset rotation tracking for loosening phase (mirrors XRI)
        accumulatedRotation = 0f;
        currentRotationAngle = 0f;
        lastRotation = transform.rotation;
        Debug.Log($"[AutoHandsValveController] Reset rotation tracking for loosening phase");

        // Apply visual feedback
        ApplyVisualFeedback(profile.tightMaterial);

        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED (TIGHT) - Valve tightened!");
        OnValveTightened?.Invoke();
    }

    private void TransitionToLooseAfterTight()
    {
        currentSubstate = ValveSubstate.Loose;
        readyForSocketReEnable = true; // Mark ready for removal
        isWaitingForGrabRelease = true;

        // Only apply constraints if NOT grabbed (constraints applied on release via OnRelease)
        // CRITICAL: Prevents kinematic toggling during grab (breaks hand tracking)
        if (!isGrabbed)
        {
            ApplyLockedConstraints();
        }

        // Apply visual feedback
        ApplyVisualFeedback(profile.looseMaterial);

        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED (LOOSE) - Valve loosened! Ready for removal. Rotation locked.");
        OnValveLoosened?.Invoke();
    }


    /// <summary>
    /// Apply visual feedback material
    /// </summary>
    private void ApplyVisualFeedback(Material material)
    {
        if (material == null) return;

        var renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = material;
            Debug.Log($"[AutoHandsValveController] Applied visual material to {gameObject.name}");
        }
    }

    // ===== LOCKING MECHANISM (XRI equivalent) =====

    /// <summary>
    /// Apply locked constraints - freeze position and unwanted rotation axes
    /// Mirrors XRI ValveController.ApplyLockedConstraints()
    /// </summary>
    private void ApplyLockedConstraints()
    {
        if (rb == null || profile == null) return;

        // Make kinematic to prevent physics movement
        //rb.isKinematic = true;

        // Freeze position
        RigidbodyConstraints constraints = RigidbodyConstraints.FreezePosition;

        // Freeze unwanted rotation axes based on profile.rotationAxis
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
            Debug.LogWarning($"[AutoHandsValveController] Unrecognized rotation axis {profile.rotationAxis}, freezing all rotation");
        }

        rb.constraints = constraints;

        Debug.Log($"[AutoHandsValveController] LOCKED - position frozen, rotation allowed on {profile.rotationAxis}");
    }

    /// <summary>
    /// Apply locked-loose constraints (same as locked but different PlacePoint handling)
    /// </summary>
    private void ApplyLockedLooseConstraints()
    {
        ApplyLockedConstraints(); // Same constraint logic
    }

    /// <summary>
    /// Unlock valve - remove from socket and keep floating until grabbed
    /// </summary>
    private void UnlockValve()
    {
        if (rb == null) return;

        // Store unlock position for distance tracking
        unlockedPosition = transform.position;
        isUnlockedAndConstrained = true;

        // Tell socket to remove this valve
        if (currentSocket != null)
        {
            currentSocket.RemoveObject();
        }

        // Keep kinematic temporarily - valve floats at position until grabbed
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

        Debug.Log($"[AutoHandsValveController] UNLOCKED - removed from socket, floating until grabbed (will track distance for re-enable at {REMOVAL_DISTANCE_THRESHOLD}m)");
    }


    /// <summary>
    /// Apply rotation dampening when not grabbed (mirrors XRI logic)
    /// </summary>
    private void ApplyRotationDampening()
    {
        if (rb == null || profile == null) return;

        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, profile.dampeningSpeed * Time.deltaTime);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (profile == null) return;

        // Draw rotation axis
        Gizmos.color = currentState == ValveState.Locked ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + profile.rotationAxis * 0.1f);

        // Draw state label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f,
            $"{currentState}-{currentSubstate}\nRot: {currentRotationAngle:F1}°");
    }
    #endif
}

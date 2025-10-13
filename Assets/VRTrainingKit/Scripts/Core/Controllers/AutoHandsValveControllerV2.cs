// AutoHandsValveControllerV2.cs
// CLEAN IMPLEMENTATION: HingeJoint-based valve controller for AutoHands
// Uses HingeJoint for rotation constraints instead of Rigidbody constraints
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Clean AutoHands valve controller using HingeJoint for locking mechanism
/// Phase 1: HingeJoint lifecycle and PlacePoint integration
/// </summary>
public class AutoHandsValveControllerV2 : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ValveProfile profile;

    [Header("Runtime State")]
    [SerializeField] private ValveState currentState = ValveState.Unlocked;
    [SerializeField] private ValveSubstate currentSubstate = ValveSubstate.None;
    [SerializeField] private float currentRotationAngle = 0f;

    // Component references
    private Autohand.Grabbable grabbable;
    private Rigidbody rb;
    private HingeJoint hingeJoint;
    private Component currentPlacePoint;

    // State tracking
    private bool isGrabbed = false;

    // Events
    public event Action OnValveSnapped;
    public event Action OnValveTightened;
    public event Action OnValveLoosened;
    public event Action OnValveRemoved;

    // Public properties
    public ValveState CurrentState => currentState;
    public ValveSubstate CurrentSubstate => currentSubstate;
    public float CurrentRotation => currentRotationAngle;

    private void Awake()
    {
        grabbable = GetComponent<Autohand.Grabbable>();
        rb = GetComponent<Rigidbody>();

        if (grabbable == null) Debug.LogError($"[AutoHandsValveControllerV2] {gameObject.name} missing Grabbable component!");
        if (rb == null) Debug.LogError($"[AutoHandsValveControllerV2] {gameObject.name} missing Rigidbody component!");

        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} initialized - Grabbable: {(grabbable != null ? "✓" : "✗")}, Rigidbody: {(rb != null ? "✓" : "✗")}");
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabEvent += OnGrab;
            grabbable.OnReleaseEvent += OnRelease;
        }

        // Subscribe to all PlacePoints in scene
        SubscribeToAllPlacePoints();
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabEvent -= OnGrab;
            grabbable.OnReleaseEvent -= OnRelease;
        }
    }

    /// <summary>
    /// Configure valve with ValveProfile
    /// </summary>
    public void Configure(ValveProfile valveProfile)
    {
        profile = valveProfile;
        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} configured with profile: {profile.profileName}");
        Debug.Log($"  - Rotation Axis: {profile.rotationAxis}");
        Debug.Log($"  - Tighten Threshold: {profile.tightenThreshold}°");
        Debug.Log($"  - Loosen Threshold: {profile.loosenThreshold}°");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Subscribe to all PlacePoints in scene using reflection
    /// </summary>
    private void SubscribeToAllPlacePoints()
    {
        var allComponents = FindObjectsOfType<MonoBehaviour>();
        foreach (var component in allComponents)
        {
            if (component.GetType().Name == "PlacePoint")
            {
                SubscribeToPlacePointEvents(component);
            }
        }
    }

    /// <summary>
    /// Subscribe to PlacePoint OnPlaceEvent using reflection
    /// </summary>
    private void SubscribeToPlacePointEvents(Component placePoint)
    {
        if (placePoint == null) return;

        try
        {
            // Subscribe to OnPlaceEvent
            FieldInfo placeEventField = placePoint.GetType().GetField("OnPlaceEvent");
            if (placeEventField != null)
            {
                Action<Component, Component> wrapper = (point, grb) => OnSocketSnapped(point, grb);
                Delegate eventDelegate = Delegate.CreateDelegate(placeEventField.FieldType, wrapper.Target, wrapper.Method);

                Delegate currentDelegate = placeEventField.GetValue(placePoint) as Delegate;
                Delegate newDelegate = Delegate.Combine(currentDelegate, eventDelegate);
                placeEventField.SetValue(placePoint, newDelegate);

                Debug.Log($"[AutoHandsValveControllerV2] Subscribed to PlacePoint: {placePoint.gameObject.name}");
            }

            // Subscribe to OnRemoveEvent
            FieldInfo removeEventField = placePoint.GetType().GetField("OnRemoveEvent");
            if (removeEventField != null)
            {
                Action<Component, Component> wrapper = (point, grb) => OnSocketUnsnapped(point, grb);
                Delegate eventDelegate = Delegate.CreateDelegate(removeEventField.FieldType, wrapper.Target, wrapper.Method);

                Delegate currentDelegate = removeEventField.GetValue(placePoint) as Delegate;
                Delegate newDelegate = Delegate.Combine(currentDelegate, eventDelegate);
                removeEventField.SetValue(placePoint, newDelegate);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsValveControllerV2] Failed to subscribe to PlacePoint: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when valve is snapped to PlacePoint socket
    /// ONLY processes first-time snap (when state is Unlocked)
    /// Ignores this event if valve is already Locked (already in socket)
    /// </summary>
    private void OnSocketSnapped(Component placePoint, Component snappedGrabbable)
    {
        if (snappedGrabbable.gameObject != gameObject) return;

        // CRITICAL: Only process snap if valve is Unlocked (first-time snap)
        // If valve is already Locked, it's already in socket - ignore this event
        if (currentState != ValveState.Unlocked)
        {
            Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} already locked - ignoring PlacePoint snap event");
            return;
        }

        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} snapped to PlacePoint: {placePoint.gameObject.name}");

        currentPlacePoint = placePoint;

        // Wait a moment for PlacePoint to finish positioning, then add HingeJoint
        StartCoroutine(WaitAndAddHingeJoint());

        OnValveSnapped?.Invoke();
    }

    /// <summary>
    /// Wait for PlacePoint positioning to stabilize, then add HingeJoint
    /// </summary>
    private IEnumerator WaitAndAddHingeJoint()
    {
        // Wait for PlacePoint to finish positioning the valve
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Add HingeJoint to lock valve in socket
        AddHingeJoint();

        // Transition to Locked-Loose state
        SetState(ValveState.Locked, ValveSubstate.Loose);
    }

    /// <summary>
    /// Add and configure HingeJoint for valve rotation constraints
    /// </summary>
    private void AddHingeJoint()
    {
        if (profile == null)
        {
            Debug.LogError($"[AutoHandsValveControllerV2] Cannot add HingeJoint - no profile configured!");
            return;
        }

        // Create HingeJoint
        hingeJoint = gameObject.AddComponent<HingeJoint>();

        // Configure axis based on profile
        Vector3 axis = Vector3.zero;
        if (profile.rotationAxis == Vector3.right) axis = Vector3.right;
        else if (profile.rotationAxis == Vector3.up) axis = Vector3.up;
        else if (profile.rotationAxis == Vector3.forward) axis = Vector3.forward;
        else
        {
            Debug.LogWarning($"[AutoHandsValveControllerV2] Unrecognized rotation axis {profile.rotationAxis}, defaulting to Y-axis");
            axis = Vector3.up;
        }

        hingeJoint.axis = axis;
        hingeJoint.autoConfigureConnectedAnchor = true;
        hingeJoint.connectedBody = null; // Connect to world space

        // Calculate limits from tighten/loosen thresholds
        // Min = -loosenThreshold (how far back you can loosen)
        // Max = +tightenThreshold (how far forward you can tighten)
        hingeJoint.useLimits = true;
        JointLimits limits = new JointLimits();
        limits.min = -profile.loosenThreshold;
        limits.max = profile.tightenThreshold;
        limits.bounceMinVelocity = 0.2f;
        limits.contactDistance = 0f;
        hingeJoint.limits = limits;

        hingeJoint.enablePreprocessing = true;

        Debug.Log($"[AutoHandsValveControllerV2] ✅ Added HingeJoint to {gameObject.name}");
        Debug.Log($"  - Axis: {axis}");
        Debug.Log($"  - Limits: [{limits.min}° to {limits.max}°]");

        // Disable PlacePoint's matchRotation to prevent rotation reset
        DisableMatchRotation();
    }

    /// <summary>
    /// Disable PlacePoint's matchRotation to prevent it from resetting valve rotation
    /// </summary>
    private void DisableMatchRotation()
    {
        if (currentPlacePoint == null) return;

        try
        {
            var matchRotationField = currentPlacePoint.GetType().GetField("matchRotation");
            if (matchRotationField != null)
            {
                matchRotationField.SetValue(currentPlacePoint, false);
                Debug.Log($"[AutoHandsValveControllerV2] ✅ Disabled matchRotation on PlacePoint {currentPlacePoint.gameObject.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsValveControllerV2] Failed to disable matchRotation: {ex.Message}");
        }
    }

    /// <summary>
    /// Re-enable PlacePoint's matchRotation after valve is removed
    /// </summary>
    private void EnableMatchRotation()
    {
        if (currentPlacePoint == null) return;

        try
        {
            var matchRotationField = currentPlacePoint.GetType().GetField("matchRotation");
            if (matchRotationField != null)
            {
                matchRotationField.SetValue(currentPlacePoint, true);
                Debug.Log($"[AutoHandsValveControllerV2] ✅ Re-enabled matchRotation on PlacePoint {currentPlacePoint.gameObject.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsValveControllerV2] Failed to enable matchRotation: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when valve is removed from PlacePoint socket
    /// ONLY processes removal when valve is in Unlocked state (ready to be removed)
    /// Ignores this event when valve is Locked (user just grabbing to rotate)
    /// </summary>
    private void OnSocketUnsnapped(Component placePoint, Component removedGrabbable)
    {
        if (removedGrabbable.gameObject != gameObject) return;

        // CRITICAL: Only process removal if valve is in Unlocked state
        // If valve is Locked, user is just grabbing to rotate - ignore removal event
        if (currentState != ValveState.Unlocked)
        {
            Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} grabbed in Locked state - ignoring PlacePoint removal event");
            return;
        }

        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} removed from PlacePoint: {placePoint.gameObject.name}");

        // Re-enable matchRotation for next snap
        EnableMatchRotation();

        // Clear PlacePoint reference
        currentPlacePoint = null;

        OnValveRemoved?.Invoke();
    }

    /// <summary>
    /// Called when valve is grabbed
    /// </summary>
    private void OnGrab(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = true;
        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} grabbed - State: {currentState}-{currentSubstate}");
    }

    /// <summary>
    /// Called when valve is released
    /// </summary>
    private void OnRelease(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = false;
        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} released - State: {currentState}-{currentSubstate}");
    }

    /// <summary>
    /// Update - Track rotation when grabbed and locked
    /// </summary>
    private void Update()
    {
        if (!isGrabbed || currentState != ValveState.Locked || hingeJoint == null) return;

        // Track rotation from HingeJoint
        TrackRotation();
    }

    /// <summary>
    /// Track rotation from HingeJoint.angle and check thresholds
    /// </summary>
    private void TrackRotation()
    {
        if (profile == null || hingeJoint == null) return;

        // Read angle from HingeJoint (relative to starting position)
        currentRotationAngle = hingeJoint.angle;

        // Check if thresholds are reached based on substate
        CheckRotationThresholds();
    }

    /// <summary>
    /// Check if rotation thresholds met for state transitions
    /// </summary>
    private void CheckRotationThresholds()
    {
        if (profile == null) return;

        switch (currentSubstate)
        {
            case ValveSubstate.Loose:
                // Check if tightened enough (positive rotation)
                if (currentRotationAngle >= profile.tightenThreshold - profile.angleTolerance)
                {
                    Debug.Log($"[AutoHandsValveControllerV2] ✅ TIGHTENED! Angle: {currentRotationAngle:F1}° (threshold: {profile.tightenThreshold}°)");
                    TransitionToTight();
                }
                break;

            case ValveSubstate.Tight:
                // Check if loosened enough (negative rotation from tight position)
                // After tight, angle goes back towards 0, then negative
                if (currentRotationAngle <= -(profile.loosenThreshold - profile.angleTolerance))
                {
                    Debug.Log($"[AutoHandsValveControllerV2] ✅ LOOSENED! Angle: {currentRotationAngle:F1}° (threshold: -{profile.loosenThreshold}°)");
                    TransitionToUnlocked();
                }
                break;
        }
    }

    /// <summary>
    /// Transition from Loose to Tight substate
    /// </summary>
    private void TransitionToTight()
    {
        SetState(ValveState.Locked, ValveSubstate.Tight);
        OnValveTightened?.Invoke();
    }

    /// <summary>
    /// Transition from Tight to Unlocked (after loosening)
    /// Remove HingeJoint to allow removal from socket
    /// </summary>
    private void TransitionToUnlocked()
    {
        // Remove HingeJoint - valve can now be removed from socket
        if (hingeJoint != null)
        {
            Destroy(hingeJoint);
            hingeJoint = null;
            Debug.Log($"[AutoHandsValveControllerV2] ✅ Removed HingeJoint - valve can now be removed from socket");
        }

        SetState(ValveState.Unlocked, ValveSubstate.None);
        OnValveLoosened?.Invoke();

        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} is now UNLOCKED and removable from socket");
    }

    /// <summary>
    /// Set valve state and substate
    /// </summary>
    private void SetState(ValveState newState, ValveSubstate newSubstate = ValveSubstate.None)
    {
        if (currentState == newState && currentSubstate == newSubstate) return;

        ValveState previousState = currentState;
        ValveSubstate previousSubstate = currentSubstate;

        currentState = newState;
        currentSubstate = newSubstate;

        Debug.Log($"[AutoHandsValveControllerV2] {gameObject.name} state changed: {previousState}-{previousSubstate} → {currentState}-{currentSubstate}");
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
            $"{currentState}-{currentSubstate}\nHingeJoint: {(hingeJoint != null ? "✓" : "✗")}");
    }
    #endif
}

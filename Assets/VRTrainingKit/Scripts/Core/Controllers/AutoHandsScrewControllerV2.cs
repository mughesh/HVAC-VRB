// AutoHandsScrewControllerV2.cs (formerly AutoHandsValveControllerV2.cs)
// CLEAN IMPLEMENTATION: HingeJoint-based screw controller for AutoHands
// Uses HingeJoint for rotation constraints instead of Rigidbody constraints
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Clean AutoHands screw controller using HingeJoint for locking mechanism
/// Phase 1: HingeJoint lifecycle and PlacePoint integration
/// </summary>
public class AutoHandsScrewControllerV2 : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ScrewProfile profile;

    [Header("Runtime State")]
    [SerializeField] private ScrewState currentState = ScrewState.Unlocked;
    [SerializeField] private ScrewSubstate currentSubstate = ScrewSubstate.None;
    [SerializeField] private float currentRotationAngle = 0f;

    // Component references
    private Autohand.Grabbable grabbable;
    private Rigidbody rb;
    private HingeJoint hingeJoint;
    private Component currentPlacePoint;

    // State tracking
    private bool isGrabbed = false;

    // Events
    public event Action OnScrewSnapped;
    public event Action OnScrewTightened;
    public event Action OnScrewLoosened;
    public event Action OnScrewRemoved;

    // Public properties
    public ScrewState CurrentState => currentState;
    public ScrewSubstate CurrentSubstate => currentSubstate;
    public float CurrentRotation => currentRotationAngle;

    private void Awake()
    {
        grabbable = GetComponent<Autohand.Grabbable>();
        rb = GetComponent<Rigidbody>();

        if (grabbable == null) Debug.LogError($"[AutoHandsScrewControllerV2] {gameObject.name} missing Grabbable component!");
        if (rb == null) Debug.LogError($"[AutoHandsScrewControllerV2] {gameObject.name} missing Rigidbody component!");

        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} initialized - Grabbable: {(grabbable != null ? "✓" : "✗")}, Rigidbody: {(rb != null ? "✓" : "✗")}");
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
    /// Configure valve with ScrewProfile
    /// </summary>
    public void Configure(ScrewProfile valveProfile)
    {
        profile = valveProfile;
        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} configured with profile: {profile.profileName}");
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

                Debug.Log($"[AutoHandsScrewControllerV2] Subscribed to PlacePoint: {placePoint.gameObject.name}");
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
            Debug.LogError($"[AutoHandsScrewControllerV2] Failed to subscribe to PlacePoint: {ex.Message}");
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
        if (currentState != ScrewState.Unlocked)
        {
            Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} already locked - ignoring PlacePoint snap event");
            return;
        }

        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} snapped to PlacePoint: {placePoint.gameObject.name}");

        currentPlacePoint = placePoint;

        // Wait a moment for PlacePoint to finish positioning, then add HingeJoint
        StartCoroutine(WaitAndAddHingeJoint());

        OnScrewSnapped?.Invoke();
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
        SetState(ScrewState.Locked, ScrewSubstate.Loose);
    }

    /// <summary>
    /// Add and configure HingeJoint for valve rotation constraints
    /// </summary>
    private void AddHingeJoint()
    {
        if (profile == null)
        {
            Debug.LogError($"[AutoHandsScrewControllerV2] Cannot add HingeJoint - no profile configured!");
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
            Debug.LogWarning($"[AutoHandsScrewControllerV2] Unrecognized rotation axis {profile.rotationAxis}, defaulting to Y-axis");
            axis = Vector3.up;
        }

        hingeJoint.axis = axis;
        hingeJoint.anchor = Vector3.zero; // CRITICAL: Set anchor to center of object (not edge)
        hingeJoint.autoConfigureConnectedAnchor = profile.autoConfigureConnectedAnchor;
        hingeJoint.connectedBody = null; // Connect to world space

        // Configure spring (for friction/resistance)
        hingeJoint.useSpring = profile.useSpring;
        if (profile.useSpring)
        {
            JointSpring spring = new JointSpring();
            spring.spring = profile.springValue;
            spring.damper = profile.springDamper;
            spring.targetPosition = profile.springTargetPosition;
            hingeJoint.spring = spring;
            Debug.Log($"  - Spring: value={spring.spring}, damper={spring.damper}, targetPos={spring.targetPosition}");
        }

        // Calculate limits from tighten/loosen thresholds
        // Min = -loosenThreshold (how far back you can loosen)
        // Max = +tightenThreshold (how far forward you can tighten)
        hingeJoint.useLimits = true;
        JointLimits limits = new JointLimits();
        limits.min = -profile.loosenThreshold;
        limits.max = profile.tightenThreshold;
        limits.bounceMinVelocity = profile.bounceMinVelocity;
        limits.contactDistance = profile.contactDistance;
        hingeJoint.limits = limits;

        hingeJoint.enablePreprocessing = true;

        Debug.Log($"[AutoHandsScrewControllerV2] ✅ Added HingeJoint to {gameObject.name}");
        Debug.Log($"  - Axis: {axis}");
        Debug.Log($"  - Anchor: {hingeJoint.anchor} (centered at object origin)");
        Debug.Log($"  - Limits: [{limits.min}° to {limits.max}°]");
        Debug.Log($"  - Spring Enabled: {profile.useSpring}");

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
                Debug.Log($"[AutoHandsScrewControllerV2] ✅ Disabled matchRotation on PlacePoint {currentPlacePoint.gameObject.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsScrewControllerV2] Failed to disable matchRotation: {ex.Message}");
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
                Debug.Log($"[AutoHandsScrewControllerV2] ✅ Re-enabled matchRotation on PlacePoint {currentPlacePoint.gameObject.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsScrewControllerV2] Failed to enable matchRotation: {ex.Message}");
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
        if (currentState != ScrewState.Unlocked)
        {
            Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} grabbed in Locked state - ignoring PlacePoint removal event");
            return;
        }

        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} removed from PlacePoint: {placePoint.gameObject.name}");

        // Re-enable matchRotation for next snap
        EnableMatchRotation();

        // Clear PlacePoint reference
        currentPlacePoint = null;

        OnScrewRemoved?.Invoke();
    }

    /// <summary>
    /// Called when valve is grabbed
    /// </summary>
    private void OnGrab(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = true;
        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} grabbed - State: {currentState}-{currentSubstate}");

        // If valve is Unlocked and in socket, re-enable matchRotation when grabbed
        // (User is about to pull it out)
        if (currentState == ScrewState.Unlocked && currentPlacePoint != null)
        {
            EnableMatchRotation();
            Debug.Log($"[AutoHandsScrewControllerV2] Valve grabbed while Unlocked - re-enabled matchRotation for removal");
        }
    }

    /// <summary>
    /// Called when valve is released
    /// </summary>
    private void OnRelease(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = false;
        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} released - State: {currentState}-{currentSubstate}");

        // No removal logic needed here - HingeJoint is removed immediately when loosened (in CheckRotationThresholds)
    }

    /// <summary>
    /// Update - Track rotation when grabbed and locked
    /// </summary>
    private void Update()
    {
        if (!isGrabbed || currentState != ScrewState.Locked || hingeJoint == null) return;

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
            case ScrewSubstate.Loose:
                // Check if tightened enough (positive rotation)
                if (currentRotationAngle >= profile.tightenThreshold - profile.angleTolerance)
                {
                    Debug.Log($"[AutoHandsScrewControllerV2] ✅ TIGHTENED! Angle: {currentRotationAngle:F1}° (threshold: {profile.tightenThreshold}°)");
                    TransitionToTight();
                }
                break;

            case ScrewSubstate.Tight:
                // Check if loosened enough (negative rotation from tight position)
                // After tight, angle goes back towards 0, then negative
                if (currentRotationAngle <= -(profile.loosenThreshold - profile.angleTolerance))
                {
                    Debug.Log($"[AutoHandsScrewControllerV2] ✅ LOOSENED! Angle: {currentRotationAngle:F1}° (threshold: -{profile.loosenThreshold}°)");
                    Debug.Log($"[AutoHandsScrewControllerV2] Removing HingeJoint immediately - valve comes free in hand");

                    // Remove HingeJoint immediately while grabbed (realistic behavior)
                    RemoveHingeJoint();
                    SetState(ScrewState.Unlocked, ScrewSubstate.None);
                    OnScrewLoosened?.Invoke();
                    Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} is now UNLOCKED - valve will come free with hand");
                }
                break;
        }
    }

    /// <summary>
    /// Transition from Loose to Tight substate
    /// </summary>
    private void TransitionToTight()
    {
        SetState(ScrewState.Locked, ScrewSubstate.Tight);
        OnScrewTightened?.Invoke();
    }

    /// <summary>
    /// Remove HingeJoint to allow valve removal from socket
    /// </summary>
    private void RemoveHingeJoint()
    {
        if (hingeJoint != null)
        {
            Destroy(hingeJoint);
            hingeJoint = null;
            Debug.Log($"[AutoHandsScrewControllerV2] ✅ Removed HingeJoint - screw can now be removed from socket");
        }
    }

    /// <summary>
    /// Set screw state and substate
    /// </summary>
    private void SetState(ScrewState newState, ScrewSubstate newSubstate = ScrewSubstate.None)
    {
        if (currentState == newState && currentSubstate == newSubstate) return;

        ScrewState previousState = currentState;
        ScrewSubstate previousSubstate = currentSubstate;

        currentState = newState;
        currentSubstate = newSubstate;

        Debug.Log($"[AutoHandsScrewControllerV2] {gameObject.name} state changed: {previousState}-{previousSubstate} → {currentState}-{currentSubstate}");
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (profile == null) return;

        // Draw rotation axis
        Gizmos.color = currentState == ScrewState.Locked ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + profile.rotationAxis * 0.1f);

        // Draw state label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f,
            $"{currentState}-{currentSubstate}\nHingeJoint: {(hingeJoint != null ? "✓" : "✗")}");
    }
    #endif
}

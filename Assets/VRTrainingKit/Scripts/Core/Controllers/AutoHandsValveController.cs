// AutoHandsValveController.cs
// Controls valve state machine and rotation behavior for AutoHands framework
using UnityEngine;
using System;
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
    private Dictionary<Component, System.Delegate> placeEventDelegates = new Dictionary<Component, System.Delegate>();
    private Dictionary<Component, System.Delegate> removeEventDelegates = new Dictionary<Component, System.Delegate>();

    // Rotation tracking
    private float baselineAngle = 0f;
    private float accumulatedRotation = 0f;
    private Quaternion lastRotation;
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

        CleanupPlacePointSubscriptions();
    }

    private void Start()
    {
        // Subscribe to ALL PlacePoints in scene (let PlacePoint handle compatibility)
        SubscribeToAllPlacePoints();
        isInitialized = true;
    }

    public void Configure(ValveProfile valveProfile)
    {
        var previousProfile = profile?.profileName ?? "NULL";
        profile = valveProfile;

        Debug.Log($"[AutoHandsValveController] Configure() called for {gameObject.name}:");
        Debug.Log($"  Previous Profile: {previousProfile} → New Profile: {profile.profileName}");
        Debug.Log($"  Rotation Axis: {profile.rotationAxis}");
        Debug.Log($"  Tighten Threshold: {profile.tightenThreshold}°");
        Debug.Log($"  Loosen Threshold: {profile.loosenThreshold}°");
        Debug.Log($"  Angle Tolerance: {profile.angleTolerance}°");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    /// <summary>
    /// Subscribe to ALL PlacePoints in scene (they handle compatibility)
    /// </summary>
    private void SubscribeToAllPlacePoints()
    {
        var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (var component in allMonoBehaviours)
        {
            if (component.GetType().Name == "PlacePoint")
            {
                SubscribeToPlacePointEvents(component);
            }
        }
    }

    /// <summary>
    /// Subscribe to PlacePoint events using reflection (same pattern as AutoHandsSnapStepHandler)
    /// </summary>
    private void SubscribeToPlacePointEvents(Component placePoint)
    {
        if (placePoint == null) return;

        try
        {
            // OnPlaceEvent
            FieldInfo placeEventField = placePoint.GetType().GetField("OnPlaceEvent");
            if (placeEventField != null)
            {
                Action<Component, Component> wrapper = (point, grb) => OnSocketSnapped(point, grb);
                Delegate eventDelegate = Delegate.CreateDelegate(placeEventField.FieldType, wrapper.Target, wrapper.Method);

                Delegate currentDelegate = placeEventField.GetValue(placePoint) as Delegate;
                Delegate newDelegate = Delegate.Combine(currentDelegate, eventDelegate);
                placeEventField.SetValue(placePoint, newDelegate);

                placeEventDelegates[placePoint] = eventDelegate;
            }

            // OnRemoveEvent
            FieldInfo removeEventField = placePoint.GetType().GetField("OnRemoveEvent");
            if (removeEventField != null)
            {
                Action<Component, Component> wrapper = (point, grb) => OnSocketUnsnapped(point, grb);
                Delegate eventDelegate = Delegate.CreateDelegate(removeEventField.FieldType, wrapper.Target, wrapper.Method);

                Delegate currentDelegate = removeEventField.GetValue(placePoint) as Delegate;
                Delegate newDelegate = Delegate.Combine(currentDelegate, eventDelegate);
                removeEventField.SetValue(placePoint, newDelegate);

                removeEventDelegates[placePoint] = eventDelegate;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsValveController] Failed to subscribe to PlacePoint: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleanup all PlacePoint subscriptions
    /// </summary>
    private void CleanupPlacePointSubscriptions()
    {
        foreach (var kvp in placeEventDelegates)
        {
            try
            {
                FieldInfo eventField = kvp.Key.GetType().GetField("OnPlaceEvent");
                if (eventField != null)
                {
                    Delegate currentDelegate = eventField.GetValue(kvp.Key) as Delegate;
                    Delegate newDelegate = Delegate.Remove(currentDelegate, kvp.Value);
                    eventField.SetValue(kvp.Key, newDelegate);
                }
            }
            catch { }
        }

        foreach (var kvp in removeEventDelegates)
        {
            try
            {
                FieldInfo eventField = kvp.Key.GetType().GetField("OnRemoveEvent");
                if (eventField != null)
                {
                    Delegate currentDelegate = eventField.GetValue(kvp.Key) as Delegate;
                    Delegate newDelegate = Delegate.Remove(currentDelegate, kvp.Value);
                    eventField.SetValue(kvp.Key, newDelegate);
                }
            }
            catch { }
        }

        placeEventDelegates.Clear();
        removeEventDelegates.Clear();
    }

    /// <summary>
    /// Called when valve is snapped into socket
    /// </summary>
    private void OnSocketSnapped(Component placePoint, Component snappedGrabbable)
    {
        if (snappedGrabbable.gameObject != gameObject) return;

        Debug.Log($"[AutoHandsValveController] {gameObject.name} snapped to socket {placePoint.gameObject.name}");

        // Transition to Locked(Loose) state
        TransitionToLockedLoose();

        // Set baseline angle for rotation tracking
        lastRotation = transform.rotation;
        baselineAngle = GetCurrentAngle();
        accumulatedRotation = 0f;
        currentRotationAngle = 0f;

        OnValveSnapped?.Invoke();
    }

    /// <summary>
    /// Called when valve is removed from socket
    /// </summary>
    private void OnSocketUnsnapped(Component placePoint, Component removedGrabbable)
    {
        if (removedGrabbable.gameObject != gameObject) return;

        Debug.Log($"[AutoHandsValveController] {gameObject.name} removed from socket {placePoint.gameObject.name}");

        // Transition back to Unlocked state
        TransitionToUnlocked();

        OnValveRemoved?.Invoke();
    }

    private void OnGrab(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = true;
        Debug.Log($"[AutoHandsValveController] {gameObject.name} grabbed - State: {currentState}-{currentSubstate}");
    }

    private void OnRelease(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = false;
        Debug.Log($"[AutoHandsValveController] {gameObject.name} released - State: {currentState}-{currentSubstate}, Rotation: {currentRotationAngle:F1}°");
    }

    private void Update()
    {
        // Only track rotation when valve is locked in socket
        if (currentState == ValveState.Locked && isGrabbed)
        {
            TrackRotation();
            CheckRotationThresholds();
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
    /// </summary>
    private void CheckRotationThresholds()
    {
        if (profile == null) return;

        // Check tighten threshold (Loose → Tight)
        if (currentSubstate == ValveSubstate.Loose)
        {
            if (currentRotationAngle >= profile.tightenThreshold - profile.angleTolerance)
            {
                TransitionToTight();
            }
        }
        // Check loosen threshold (Tight → Loose)
        else if (currentSubstate == ValveSubstate.Tight)
        {
            if (currentRotationAngle <= -(profile.loosenThreshold - profile.angleTolerance))
            {
                TransitionToLooseAfterTight();
            }
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
        Debug.Log($"[AutoHandsValveController] {gameObject.name} → UNLOCKED");
    }

    private void TransitionToLockedLoose()
    {
        currentState = ValveState.Locked;
        currentSubstate = ValveSubstate.Loose;
        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED (LOOSE) - Ready for tightening");
    }

    private void TransitionToTight()
    {
        currentSubstate = ValveSubstate.Tight;
        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED (TIGHT) - Valve tightened!");

        // Apply visual feedback
        ApplyVisualFeedback(profile.tightMaterial);

        OnValveTightened?.Invoke();
    }

    private void TransitionToLooseAfterTight()
    {
        currentSubstate = ValveSubstate.Loose;
        Debug.Log($"[AutoHandsValveController] {gameObject.name} → LOCKED (LOOSE) - Valve loosened!");

        // Apply visual feedback
        ApplyVisualFeedback(profile.looseMaterial);

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

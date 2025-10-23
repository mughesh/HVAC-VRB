// AutoHandsTurnByCountController.cs
// HingeJoint-based turn-by-count controller for AutoHands (e.g., allen keys)
// Follows AutoHandsValveControllerV2 pattern: snap → add HingeJoint → track turns → remove HingeJoint
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands turn-by-count controller using HingeJoint for rotation tracking
/// Workflow: snap to socket → turn N times → complete
/// </summary>
public class AutoHandsTurnByCountController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TurnByCountProfile profile;

    [Header("Runtime State")]
    [SerializeField] private TurnState currentState = TurnState.Unlocked;
    [SerializeField] private float currentRotationAngle = 0f;
    [SerializeField] private float currentTurnCount = 0f;

    // Component references
    private Autohand.Grabbable grabbable;
    private Rigidbody rb;
    private HingeJoint hingeJoint;
    private Component currentPlacePoint;

    // State tracking
    private bool isGrabbed = false;
    private float previousAngle = 0f;
    private float totalRotation = 0f; // Accumulated rotation in degrees

    // Events
    public event Action OnToolSnapped;
    public event Action<float> OnTurnProgress; // Fires with current turn count
    public event Action OnTurnCompleted;
    public event Action OnToolRemoved;

    // Public properties
    public TurnState CurrentState => currentState;
    public float CurrentTurnCount => currentTurnCount;
    public int RequiredTurns => profile != null ? profile.requiredTurnCount : 0;
    public float TotalDegreesRequired => profile != null ? profile.TotalDegreesRequired : 0f;
    public float Progress => profile != null && profile.requiredTurnCount > 0 ? Mathf.Clamp01(currentTurnCount / profile.requiredTurnCount) : 0f;

    /// <summary>
    /// State machine for turn-by-count interaction
    /// </summary>
    public enum TurnState
    {
        Unlocked,   // Not in socket or turns completed (can be removed)
        Locked,     // In socket with HingeJoint, tracking turns
        Completed   // Required turns reached, ready to be removed
    }

    private void Awake()
    {
        grabbable = GetComponent<Autohand.Grabbable>();
        rb = GetComponent<Rigidbody>();

        if (grabbable == null) Debug.LogError($"[AutoHandsTurnByCountController] {gameObject.name} missing Grabbable component!");
        if (rb == null) Debug.LogError($"[AutoHandsTurnByCountController] {gameObject.name} missing Rigidbody component!");

        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} initialized - Grabbable: {(grabbable != null ? "✓" : "✗")}, Rigidbody: {(rb != null ? "✓" : "✗")}");
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
    /// Configure tool with TurnByCountProfile
    /// Called by AutoHandsTurnByCountProfile.ApplyToGameObject() (Phase 2 integration)
    /// </summary>
    public void Configure(TurnByCountProfile turnProfile)
    {
        profile = turnProfile;
        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} configured with profile");
        Debug.Log($"  - Degrees Per Turn: {profile.degreesPerTurn}°");
        Debug.Log($"  - Required Turn Count: {profile.requiredTurnCount} turns");
        Debug.Log($"  - Total Degrees Required: {profile.TotalDegreesRequired}°");
        Debug.Log($"  - Rotation Axis: {profile.rotationAxis}");
        Debug.Log($"  - Direction: {profile.rotationDirection}");
        Debug.Log($"  - Tolerance: ±{profile.angleTolerance}°");

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
    /// Subscribe to PlacePoint OnPlaceEvent and OnRemoveEvent using reflection
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

                Debug.Log($"[AutoHandsTurnByCountController] Subscribed to PlacePoint: {placePoint.gameObject.name}");
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
            Debug.LogError($"[AutoHandsTurnByCountController] Failed to subscribe to PlacePoint: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when tool is snapped to PlacePoint socket
    /// ONLY processes first-time snap (when state is Unlocked)
    /// </summary>
    private void OnSocketSnapped(Component placePoint, Component snappedGrabbable)
    {
        if (snappedGrabbable.gameObject != gameObject) return;

        // Check socket compatibility
        if (profile != null && !IsCompatibleSocket(placePoint.gameObject))
        {
            Debug.LogWarning($"[AutoHandsTurnByCountController] {gameObject.name} snapped to incompatible socket: {placePoint.gameObject.name}");
            return;
        }

        // CRITICAL: Only process snap if tool is Unlocked (first-time snap)
        if (currentState != TurnState.Unlocked)
        {
            Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} already locked - ignoring PlacePoint snap event");
            return;
        }

        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} snapped to PlacePoint: {placePoint.gameObject.name}");

        currentPlacePoint = placePoint;

        // Reset turn tracking
        currentRotationAngle = 0f;
        currentTurnCount = 0f;
        totalRotation = 0f;
        previousAngle = 0f;

        // Wait a moment for PlacePoint to finish positioning, then add HingeJoint
        StartCoroutine(WaitAndAddHingeJoint());

        OnToolSnapped?.Invoke();
    }

    /// <summary>
    /// Check if socket is compatible with this tool based on profile settings
    /// </summary>
    private bool IsCompatibleSocket(GameObject socket)
    {
        if (profile == null) return true;

        // Check specific socket requirements
        if (profile.requireSpecificSockets && profile.specificCompatibleSockets != null)
        {
            foreach (var socketRef in profile.specificCompatibleSockets)
            {
                if (socketRef != null && socketRef.GameObject == socket)
                {
                    return true;
                }
            }
            return false;
        }

        // Check tag-based compatibility
        if (profile.compatibleSocketTags != null)
        {
            foreach (var tag in profile.compatibleSocketTags)
            {
                if (socket.CompareTag(tag))
                {
                    return true;
                }
            }
        }

        return true; // Default to compatible if no restrictions
    }

    /// <summary>
    /// Wait for PlacePoint positioning to stabilize, then add HingeJoint
    /// </summary>
    private IEnumerator WaitAndAddHingeJoint()
    {
        // Wait for PlacePoint to finish positioning the tool
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Add HingeJoint to lock tool in socket
        AddHingeJoint();

        // Transition to Locked state
        SetState(TurnState.Locked);
    }

    /// <summary>
    /// Add and configure HingeJoint for rotation tracking
    /// </summary>
    private void AddHingeJoint()
    {
        if (profile == null)
        {
            Debug.LogError($"[AutoHandsTurnByCountController] Cannot add HingeJoint - no profile configured!");
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
            Debug.LogWarning($"[AutoHandsTurnByCountController] Unrecognized rotation axis {profile.rotationAxis}, defaulting to Z-axis (forward)");
            axis = Vector3.forward;
        }

        hingeJoint.axis = axis;
        hingeJoint.anchor = Vector3.zero; // Center of object
        hingeJoint.autoConfigureConnectedAnchor = profile.autoConfigureConnectedAnchor;
        hingeJoint.connectedBody = null; // Connect to world space

        // Configure spring (for friction/resistance)
        hingeJoint.useSpring = profile.useSpring;
        if (profile.useSpring)
        {
            JointSpring spring = new JointSpring();
            spring.spring = profile.springValue;
            spring.damper = profile.springDamper;
            spring.targetPosition = 0f; // No target position for free rotation
            hingeJoint.spring = spring;
            Debug.Log($"  - Spring: value={spring.spring}, damper={spring.damper}");
        }

        // Configure limits - allow full rotation range for turn counting
        hingeJoint.useLimits = false; // No limits - allow continuous rotation

        hingeJoint.enablePreprocessing = true;

        Debug.Log($"[AutoHandsTurnByCountController] ✅ Added HingeJoint to {gameObject.name}");
        Debug.Log($"  - Axis: {axis}");
        Debug.Log($"  - Anchor: {hingeJoint.anchor} (centered at object origin)");
        Debug.Log($"  - Limits: Disabled (free rotation for turn counting)");
        Debug.Log($"  - Spring Enabled: {profile.useSpring}");

        // Disable PlacePoint's matchRotation to prevent rotation reset
        DisableMatchRotation();
    }

    /// <summary>
    /// Disable PlacePoint's matchRotation to prevent it from resetting tool rotation
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
                Debug.Log($"[AutoHandsTurnByCountController] ✅ Disabled matchRotation on PlacePoint {currentPlacePoint.gameObject.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsTurnByCountController] Failed to disable matchRotation: {ex.Message}");
        }
    }

    /// <summary>
    /// Re-enable PlacePoint's matchRotation after tool is removed
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
                Debug.Log($"[AutoHandsTurnByCountController] ✅ Re-enabled matchRotation on PlacePoint {currentPlacePoint.gameObject.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoHandsTurnByCountController] Failed to enable matchRotation: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when tool is removed from PlacePoint socket
    /// ONLY processes removal when tool is in Unlocked/Completed state
    /// </summary>
    private void OnSocketUnsnapped(Component placePoint, Component removedGrabbable)
    {
        if (removedGrabbable.gameObject != gameObject) return;

        // CRITICAL: Only process removal if tool is Unlocked or Completed
        // If tool is Locked (still turning), user is just grabbing to rotate - ignore removal event
        if (currentState == TurnState.Locked)
        {
            Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} grabbed in Locked state - ignoring PlacePoint removal event");
            return;
        }

        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} removed from PlacePoint: {placePoint.gameObject.name}");

        // Re-enable matchRotation for next snap
        EnableMatchRotation();

        // Clear PlacePoint reference
        currentPlacePoint = null;

        OnToolRemoved?.Invoke();
    }

    /// <summary>
    /// Called when tool is grabbed
    /// </summary>
    private void OnGrab(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = true;
        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} grabbed - State: {currentState}");

        // If tool is Unlocked/Completed and in socket, re-enable matchRotation when grabbed
        // (User is about to pull it out)
        if ((currentState == TurnState.Unlocked || currentState == TurnState.Completed) && currentPlacePoint != null)
        {
            EnableMatchRotation();
            Debug.Log($"[AutoHandsTurnByCountController] Tool grabbed while {currentState} - re-enabled matchRotation for removal");
        }

        // Reset angle tracking when grabbed while locked
        if (currentState == TurnState.Locked && hingeJoint != null)
        {
            previousAngle = hingeJoint.angle;
        }
    }

    /// <summary>
    /// Called when tool is released
    /// </summary>
    private void OnRelease(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = false;
        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} released - State: {currentState}, Turn Count: {currentTurnCount:F2}/{profile?.requiredTurnCount:F2}");
    }

    /// <summary>
    /// Update - Track rotation when grabbed and locked
    /// </summary>
    private void Update()
    {
        if (!isGrabbed || currentState != TurnState.Locked || hingeJoint == null) return;

        // Track rotation from HingeJoint
        TrackRotation();
    }

    /// <summary>
    /// Track rotation from HingeJoint.angle and calculate turn count
    /// </summary>
    private void TrackRotation()
    {
        if (profile == null || hingeJoint == null) return;

        // Read current angle from HingeJoint
        currentRotationAngle = hingeJoint.angle;

        // Calculate angle delta (accounting for wrapping at ±180°)
        float angleDelta = Mathf.DeltaAngle(previousAngle, currentRotationAngle);

        // Check rotation direction if profile specifies
        bool validDirection = true;
        if (profile.rotationDirection == TurnByCountProfile.RotationDirection.Clockwise)
        {
            validDirection = angleDelta > 0; // Positive = clockwise
        }
        else if (profile.rotationDirection == TurnByCountProfile.RotationDirection.CounterClockwise)
        {
            validDirection = angleDelta < 0; // Negative = counter-clockwise
        }

        // Accumulate rotation only in valid direction
        if (validDirection)
        {
            totalRotation += Mathf.Abs(angleDelta);
        }

        // Convert total rotation to turn count (using degreesPerTurn)
        float newTurnCount = totalRotation / profile.degreesPerTurn;

        // Fire progress event if turn count changed significantly
        if (Mathf.Abs(newTurnCount - currentTurnCount) > 0.01f)
        {
            currentTurnCount = newTurnCount;
            OnTurnProgress?.Invoke(currentTurnCount);
        }

        // Check if required turn count reached
        CheckTurnCompletion();

        // Update previous angle for next frame
        previousAngle = currentRotationAngle;
    }

    /// <summary>
    /// Check if required turn count has been reached
    /// </summary>
    private void CheckTurnCompletion()
    {
        if (profile == null) return;

        // Calculate required rotation in degrees
        float requiredDegrees = profile.TotalDegreesRequired;
        float toleranceDegrees = profile.angleTolerance;

        // Check if turn count reached (with tolerance)
        if (totalRotation >= requiredDegrees - toleranceDegrees)
        {
            Debug.Log($"[AutoHandsTurnByCountController] ✅ TURN COMPLETED! Total Rotation: {totalRotation:F1}° ({currentTurnCount:F2} turns)");
            Debug.Log($"[AutoHandsTurnByCountController] Required: {requiredDegrees:F1}° ({profile.requiredTurnCount} turns @ {profile.degreesPerTurn}° per turn)");
            Debug.Log($"[AutoHandsTurnByCountController] Removing HingeJoint - tool can now be removed");

            // Remove HingeJoint immediately
            RemoveHingeJoint();
            SetState(TurnState.Completed);
            OnTurnCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Remove HingeJoint to allow tool removal from socket
    /// </summary>
    private void RemoveHingeJoint()
    {
        if (hingeJoint != null)
        {
            Destroy(hingeJoint);
            hingeJoint = null;
            Debug.Log($"[AutoHandsTurnByCountController] ✅ Removed HingeJoint - tool can now be removed from socket");
        }
    }

    /// <summary>
    /// Set tool state
    /// </summary>
    private void SetState(TurnState newState)
    {
        if (currentState == newState) return;

        TurnState previousState = currentState;
        currentState = newState;

        Debug.Log($"[AutoHandsTurnByCountController] {gameObject.name} state changed: {previousState} → {currentState}");
    }

    /// <summary>
    /// Public method to reset turn count (useful for testing or re-use)
    /// </summary>
    public void ResetTurnCount()
    {
        currentRotationAngle = 0f;
        currentTurnCount = 0f;
        totalRotation = 0f;
        previousAngle = 0f;

        Debug.Log($"[AutoHandsTurnByCountController] Turn count reset");
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (profile == null) return;

        // Draw rotation axis
        Gizmos.color = currentState == TurnState.Locked ? Color.green :
                       currentState == TurnState.Completed ? Color.cyan : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + profile.rotationAxis * 0.1f);

        // Draw state and progress label
        string progressText = profile != null ?
            $"{currentTurnCount:F2}/{profile.requiredTurnCount:F2} turns ({Progress * 100f:F0}%)" :
            "No profile";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f,
            $"{currentState}\nHingeJoint: {(hingeJoint != null ? "✓" : "✗")}\n{progressText}");
    }
    #endif
}

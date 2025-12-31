// AutoHandsKnobController.cs
// Controls knob rotation behavior for AutoHands framework
using UnityEngine;
using UnityEngine.Events;
using System;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands-specific knob controller that tracks HingeJoint angles
/// Mirrors KnobController but uses AutoHands Grabbable events instead of XRI
/// </summary>
public class AutoHandsKnobController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private KnobProfile profile;

    private Autohand.Grabbable grabbable;
    private HingeJoint hingeJoint;
    private float currentAngle = 0f;
    private float startAngle = 0f;
    private bool isGrabbed = false;

    public float CurrentAngle => currentAngle;
    public float NormalizedValue => profile != null && profile.useLimits ?
        (currentAngle - profile.minAngle) / (profile.maxAngle - profile.minAngle) : 0f;

    // HingeJoint-based properties for rotation direction detection
    public float CurrentHingeAngle => hingeJoint != null ? hingeJoint.angle : GetTransformAngle();
    public float HingeMinLimit => hingeJoint != null && hingeJoint.useLimits ? hingeJoint.limits.min : (profile?.minAngle ?? 0f);
    public float HingeMaxLimit => hingeJoint != null && hingeJoint.useLimits ? hingeJoint.limits.max : (profile?.maxAngle ?? 90f);
    public bool IsAtMaxLimit(float tolerance = 5f) => Mathf.Abs(CurrentHingeAngle - HingeMaxLimit) <= tolerance;
    public bool IsAtMinLimit(float tolerance = 5f) => Mathf.Abs(CurrentHingeAngle - HingeMinLimit) <= tolerance;

    // C# events for code-based subscriptions
    public event Action<float> OnAngleChanged;
    public event Action<float> OnSnapToAngle;

    [Header("Callbacks (Optional)")]
    [Tooltip("Event fired during rotation with normalized value (0.0 to 1.0). Wire displays here.")]
    public UnityEvent<float> OnKnobRotated;

    private void Awake()
    {
        grabbable = GetComponent<Autohand.Grabbable>();
        hingeJoint = GetComponent<HingeJoint>();

        Debug.Log($"[AutoHandsKnobController] {gameObject.name} Awake() - Grabbable: {(grabbable != null ? "Yes" : "No")}, HingeJoint: {(hingeJoint != null ? "Yes" : "No")}");
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabEvent += OnGrab;
            grabbable.OnReleaseEvent += OnRelease;
            Debug.Log($"[AutoHandsKnobController] Subscribed to AutoHands Grabbable events on {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[AutoHandsKnobController] No Grabbable component found on {gameObject.name}!");
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabEvent -= OnGrab;
            grabbable.OnReleaseEvent -= OnRelease;
        }
    }

    public void Configure(KnobProfile knobProfile)
    {
        var previousProfile = profile?.profileName ?? "NULL";
        profile = knobProfile;
        currentAngle = GetCurrentAngle();
        startAngle = currentAngle;

        Debug.Log($"[AutoHandsKnobController] Configure() called for {gameObject.name}: " +
                 $"Previous={previousProfile} → New={profile.profileName}, " +
                 $"Axis={profile.rotationAxis}, Range=[{profile.minAngle:F1}° to {profile.maxAngle:F1}°], " +
                 $"HingeJoint={(hingeJoint != null ? "Yes" : "No")}, StartAngle={startAngle:F2}°");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    private void OnGrab(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = true;
        startAngle = GetCurrentAngle();
        Debug.Log($"[AutoHandsKnobController] {gameObject.name} grabbed! Start angle: {startAngle:F2}°");
    }

    private void OnRelease(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        isGrabbed = false;
        Debug.Log($"[AutoHandsKnobController] {gameObject.name} released at angle: {currentAngle:F2}°");

        if (profile != null && profile.snapToAngles)
        {
            SnapToNearestAngle();
        }
    }

    private void Update()
    {
        // Update angle whenever grabbed (AutoHands allows physics-based rotation)
        if (isGrabbed && profile != null)
        {
            UpdateRotation();
        }
    }

    private void UpdateRotation()
    {
        float newAngle = GetCurrentAngle();

        // Apply limits if enabled
        if (profile.useLimits)
        {
            float clampedAngle = Mathf.Clamp(newAngle, profile.minAngle, profile.maxAngle);
            if (clampedAngle != newAngle)
            {
                newAngle = clampedAngle;
                ApplyRotation(newAngle);
            }
        }

        // Fire event if angle changed
        float angleDifference = Mathf.Abs(newAngle - currentAngle);
        if (angleDifference > 0.001f)
        {
            float previousAngle = currentAngle;
            currentAngle = newAngle;

          //  Debug.Log($"[AutoHandsKnobController] {gameObject.name} ANGLE CHANGED! {previousAngle:F3}° → {currentAngle:F3}° (diff: {angleDifference:F3}°) - FIRING EVENT");
            OnAngleChanged?.Invoke(currentAngle);

            // Fire UnityEvent with normalized value for Inspector-wired displays
            OnKnobRotated?.Invoke(NormalizedValue);
        }
    }

    private float GetCurrentAngle()
    {
        // Try to get angle from HingeJoint first (more accurate)
        if (hingeJoint != null)
        {
            return GetHingeAngle();
        }

        // Fallback to transform-based angle reading
        return GetTransformAngle();
    }

    private float GetHingeAngle()
    {
        if (hingeJoint == null)
        {
            Debug.LogWarning($"[AutoHandsKnobController] {gameObject.name} GetHingeAngle(): HingeJoint is null");
            return GetTransformAngle();
        }

        float jointAngle = hingeJoint.angle;

        if (float.IsNaN(jointAngle))
        {
            Debug.LogError($"[AutoHandsKnobController] {gameObject.name} HingeJoint angle is NaN!");
            return 0f;
        }

        return jointAngle;
    }

    private float GetTransformAngle()
    {
        Vector3 euler = transform.localEulerAngles;

        if (float.IsNaN(euler.x) || float.IsNaN(euler.y) || float.IsNaN(euler.z))
        {
            Debug.LogError($"[AutoHandsKnobController] {gameObject.name} Transform euler angles contain NaN!");
            return 0f;
        }

        float angle = 0f;

        switch (profile?.rotationAxis ?? KnobProfile.RotationAxis.Y)
        {
            case KnobProfile.RotationAxis.X:
                angle = euler.x;
                break;
            case KnobProfile.RotationAxis.Y:
                angle = euler.y;
                break;
            case KnobProfile.RotationAxis.Z:
                angle = euler.z;
                break;
        }

        // Convert to -180 to 180 range
        if (angle > 180f) angle -= 360f;

        if (float.IsNaN(angle))
        {
            Debug.LogError($"[AutoHandsKnobController] {gameObject.name} Final angle calculation resulted in NaN!");
            return 0f;
        }

        return angle;
    }

    private void ApplyRotation(float angle)
    {
        Vector3 euler = transform.localEulerAngles;

        switch (profile.rotationAxis)
        {
            case KnobProfile.RotationAxis.X:
                euler.x = angle;
                break;
            case KnobProfile.RotationAxis.Y:
                euler.y = angle;
                break;
            case KnobProfile.RotationAxis.Z:
                euler.z = angle;
                break;
        }

        transform.localEulerAngles = euler;
    }

    private void SnapToNearestAngle()
    {
        float snappedAngle = Mathf.Round(currentAngle / profile.snapAngleIncrement) * profile.snapAngleIncrement;

        if (profile.useLimits)
        {
            snappedAngle = Mathf.Clamp(snappedAngle, profile.minAngle, profile.maxAngle);
        }

        ApplyRotation(snappedAngle);
        currentAngle = snappedAngle;
        OnSnapToAngle?.Invoke(snappedAngle);

        Debug.Log($"[AutoHandsKnobController] {gameObject.name} snapped to angle: {snappedAngle:F1}°");
    }

    public void SetAngle(float angle, bool immediate = false)
    {
        if (profile != null && profile.useLimits)
        {
            angle = Mathf.Clamp(angle, profile.minAngle, profile.maxAngle);
        }

        if (immediate)
        {
            ApplyRotation(angle);
            currentAngle = angle;
            OnAngleChanged?.Invoke(currentAngle);
            OnKnobRotated?.Invoke(NormalizedValue);
        }
        else
        {
            StartCoroutine(SmoothRotateToAngle(angle));
        }
    }

    private System.Collections.IEnumerator SmoothRotateToAngle(float targetAngle)
    {
        float startTime = Time.time;
        float startAngle = currentAngle;
        float duration = 0.5f;

        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            float angle = Mathf.Lerp(startAngle, targetAngle, t);
            ApplyRotation(angle);
            currentAngle = angle;
            OnAngleChanged?.Invoke(currentAngle);
            OnKnobRotated?.Invoke(NormalizedValue);
            yield return null;
        }

        ApplyRotation(targetAngle);
        currentAngle = targetAngle;
        OnAngleChanged?.Invoke(currentAngle);
        OnKnobRotated?.Invoke(NormalizedValue);
    }
}

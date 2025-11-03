// PressureGaugeDisplay.cs
// Controls pressure gauge needle rotation based on system pressure
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Manages pressure gauge needle rotation
/// Rotates needle on Z-axis from 0° (0 psi) to 90° (130 psi)
/// Call UpdatePressureFromKnob from knob rotation events
/// </summary>
public class PressureGaugeDisplay : MonoBehaviour
{
    [Header("Needle Settings")]
    [Tooltip("The Transform of the needle that will rotate")]
    public Transform needleTransform;

    [Tooltip("Starting Z rotation angle (usually 0°)")]
    public float startAngle = 0f;

    [Tooltip("Ending Z rotation angle (usually 90° for 130 psi)")]
    public float endAngle = 90f;

    [Header("Pressure Settings")]
    [Tooltip("Maximum pressure reading in PSI")]
    public float maxPressure = 130f;

    [Header("Animation")]
    [Tooltip("Smooth rotation speed (0 = instant, higher = slower)")]
    public float smoothSpeed = 10f;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLog = true;

    [Tooltip("Show current pressure in debug text")]
    public bool showDebugText = false;

    private float currentPressure = 0f;
    private float targetAngle = 0f;
    private Quaternion targetRotation;

    void Start()
    {
        // Validate references
        if (needleTransform == null)
        {
            needleTransform = transform; // Use self if not assigned
            LogDebug("No needle transform assigned, using self");
        }

        // Set initial rotation
        SetNeedleAngle(startAngle);
        targetAngle = startAngle;
        LogDebug($"Pressure gauge initialized - Start: {startAngle}°, End: {endAngle}°, Max Pressure: {maxPressure} psi");
    }

    void Update()
    {
        // Smooth rotation towards target angle
        if (smoothSpeed > 0)
        {
            needleTransform.localRotation = Quaternion.Lerp(
                needleTransform.localRotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    /// <summary>
    /// Update pressure based on knob rotation (0.0 to 1.0)
    /// Called from AutoHandsKnobController.OnKnobRotated UnityEvent
    /// </summary>
    /// <param name="normalizedValue">Knob rotation from 0.0 (closed) to 1.0 (open)</param>
    public void UpdatePressureFromKnob(float normalizedValue)
    {
        // Pressure goes from 0 to 130 psi
        currentPressure = normalizedValue * maxPressure;

        // Needle rotates from startAngle to endAngle (0° to 90°)
        targetAngle = Mathf.Lerp(startAngle, endAngle, normalizedValue);

        // Set target rotation
        SetTargetRotation(targetAngle);

        LogDebug($"Pressure: {currentPressure:F1} psi, Needle angle: {targetAngle:F1}°");
    }

    /// <summary>
    /// Set pressure directly in PSI
    /// </summary>
    /// <param name="pressure">Pressure value in PSI (0 to maxPressure)</param>
    public void SetPressure(float pressure)
    {
        currentPressure = Mathf.Clamp(pressure, 0f, maxPressure);
        float normalized = currentPressure / maxPressure;
        targetAngle = Mathf.Lerp(startAngle, endAngle, normalized);
        SetTargetRotation(targetAngle);
    }

    /// <summary>
    /// Set pressure from string (for UnityEvent compatibility)
    /// </summary>
    public void SetPressureFromString(string pressureStr)
    {
        if (float.TryParse(pressureStr, out float pressure))
        {
            SetPressure(pressure);
        }
        else
        {
            Debug.LogWarning($"[PressureGaugeDisplay] Invalid pressure string: {pressureStr}");
        }
    }

    /// <summary>
    /// Reset pressure to zero
    /// </summary>
    public void ResetPressure()
    {
        currentPressure = 0f;
        targetAngle = startAngle;
        SetTargetRotation(targetAngle);
        LogDebug("Pressure reset to 0 psi");
    }

    /// <summary>
    /// Set needle angle immediately (no smoothing)
    /// </summary>
    private void SetNeedleAngle(float angle)
    {
        if (needleTransform == null) return;

        Vector3 currentEuler = needleTransform.localEulerAngles;
        needleTransform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, angle);
        targetRotation = needleTransform.localRotation;
    }

    /// <summary>
    /// Set target rotation for smooth animation
    /// </summary>
    private void SetTargetRotation(float angle)
    {
        if (needleTransform == null) return;

        Vector3 currentEuler = needleTransform.localEulerAngles;
        targetRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, angle);

        // If smoothSpeed is 0, apply immediately
        if (smoothSpeed <= 0)
        {
            needleTransform.localRotation = targetRotation;
        }
    }

    /// <summary>
    /// Get current pressure reading
    /// </summary>
    public float GetCurrentPressure()
    {
        return currentPressure;
    }

    /// <summary>
    /// Get current needle angle
    /// </summary>
    public float GetCurrentAngle()
    {
        return targetAngle;
    }

    // Debug logging helper
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PressureGaugeDisplay] {message}", this);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Test method for editor - press P key to test full pressure, R to reset
    /// </summary>
    void OnValidate()
    {
        // Update needle when values change in inspector
        if (Application.isPlaying && needleTransform != null)
        {
            float normalized = currentPressure / maxPressure;
            targetAngle = Mathf.Lerp(startAngle, endAngle, normalized);
            SetTargetRotation(targetAngle);
        }
    }

    private void OnDrawGizmos()
    {
        if (needleTransform != null)
        {
            // Draw needle direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(needleTransform.position, needleTransform.forward * 0.1f);
        }

        if (showDebugText && Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.05f,
                $"{currentPressure:F1} psi\n{targetAngle:F1}°"
            );
        }
    }
#endif
}

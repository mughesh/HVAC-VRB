// ClampMeterDisplay.cs
// Controls clamp meter digital current display
using UnityEngine;
using TMPro;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Manages the clamp meter digital display for current readings
/// Shows current from 0A to 7.6A based on system operation
/// Call UpdateCurrentFromKnob from knob rotation events
/// </summary>
public class ClampMeterDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("The TextMeshPro component showing the current value")]
    public TextMeshProUGUI displayText;

    [Tooltip("Start with display showing zero")]
    public bool startAtZero = true;

    [Header("Current Settings")]
    [Tooltip("Maximum current reading in Amps")]
    public float maxCurrent = 7.6f;

    [Tooltip("Format string for display (e.g., '{0:F1} A' or '{0:F2}A')")]
    public string displayFormat = "{0:F1} A";

    [Tooltip("Decimal places to show (1 = 7.6, 2 = 7.60)")]
    public int decimalPlaces = 1;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLog = true;

    private float currentReading = 0f;

    void Start()
    {
        // Validate references
        if (displayText == null)
        {
            Debug.LogError($"[ClampMeterDisplay] No TextMeshProUGUI assigned! Please assign the display text component.", this);
            return;
        }

        // Set initial state
        if (startAtZero)
        {
            ShowCurrent(0f);
            LogDebug("Display initialized at 0A");
        }
    }

    /// <summary>
    /// Update current based on knob rotation (0.0 to 1.0)
    /// Called from AutoHandsKnobController.OnKnobRotated UnityEvent
    /// </summary>
    /// <param name="normalizedValue">Knob rotation from 0.0 (off) to 1.0 (full power)</param>
    public void UpdateCurrentFromKnob(float normalizedValue)
    {
        // Current goes from 0A to 7.6A
        float current = normalizedValue * maxCurrent;
        ShowCurrent(current);
    }

    /// <summary>
    /// Show a specific current value
    /// </summary>
    /// <param name="current">Current in Amps (0 to maxCurrent)</param>
    public void ShowCurrent(float current)
    {
        if (displayText == null) return;

        currentReading = Mathf.Clamp(current, 0f, maxCurrent);

        // Format based on decimal places setting
        string formatStr = displayFormat;
        if (formatStr.Contains("{0}"))
        {
            formatStr = formatStr.Replace("{0}", $"{{0:F{decimalPlaces}}}");
        }

        displayText.text = string.Format(formatStr, currentReading);
        LogDebug($"Current: {currentReading:F2}A");
    }

    /// <summary>
    /// Show current from string (for UnityEvent compatibility)
    /// </summary>
    public void ShowCurrentFromString(string currentStr)
    {
        if (float.TryParse(currentStr, out float current))
        {
            ShowCurrent(current);
        }
        else
        {
            Debug.LogWarning($"[ClampMeterDisplay] Invalid current string: {currentStr}");
        }
    }

    /// <summary>
    /// Reset display to zero
    /// </summary>
    public void ResetToZero()
    {
        ShowCurrent(0f);
        LogDebug("Display reset to 0A");
    }

    /// <summary>
    /// Enable or disable the display
    /// </summary>
    public void SetDisplayActive(bool active)
    {
        if (displayText == null) return;
        displayText.gameObject.SetActive(active);
    }

    /// <summary>
    /// Turn display off completely
    /// </summary>
    public void TurnOff()
    {
        SetDisplayActive(false);
        LogDebug("Display turned OFF");
    }

    /// <summary>
    /// Turn display on
    /// </summary>
    public void TurnOn()
    {
        SetDisplayActive(true);
        LogDebug("Display turned ON");
    }

    /// <summary>
    /// Get current reading
    /// </summary>
    public float GetCurrentReading()
    {
        return currentReading;
    }

    /// <summary>
    /// Check if current is above threshold (for training validation)
    /// </summary>
    public bool IsCurrentAboveThreshold(float threshold)
    {
        return currentReading >= threshold;
    }

    // Debug logging helper
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ClampMeterDisplay] {message}", this);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Test method for editor - press C key to test max current, R to reset
    /// </summary>
    void Update()
    {
        if (!Application.isPlaying) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            ShowCurrent(maxCurrent);
            Debug.Log($"[ClampMeterDisplay] Test: Showing max current {maxCurrent}A");
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ResetToZero();
            Debug.Log("[ClampMeterDisplay] Test: Reset to zero");
        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            // Test with random value
            float testCurrent = Random.Range(0f, maxCurrent);
            ShowCurrent(testCurrent);
            Debug.Log($"[ClampMeterDisplay] Test: Random current {testCurrent:F2}A");
        }
    }

    void OnDrawGizmos()
    {
        if (displayText != null && Application.isPlaying)
        {
            // Draw debug info
            Gizmos.color = currentReading >= maxCurrent ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.02f);
        }
    }
#endif
}

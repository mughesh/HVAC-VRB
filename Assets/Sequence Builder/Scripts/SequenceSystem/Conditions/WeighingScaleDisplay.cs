// WeighingScaleDisplay.cs
// Controls the digital display on the weighing scale
using UnityEngine;
using TMPro;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Manages the weighing scale digital display text
/// Call methods from button UnityEvents or sequence events to update display
/// </summary>
public class WeighingScaleDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("The TextMeshPro component showing the weight value")]
    public TextMeshProUGUI displayText;

    [Tooltip("Start with display off (will enable when ON button pressed)")]
    public bool startDisabled = true;

    [Header("Weight Values")]
    [Tooltip("Weight displayed when ON button is pressed (cylinder weight in grams)")]
    public int cylinderWeight = 5000;

    [Tooltip("Format string for display (e.g., '{0} g' or '{0}'")]
    public string displayFormat = "{0} g";

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLog = true;

    private int currentWeight = 0;

    void Start()
    {
        // Validate references
        if (displayText == null)
        {
            Debug.LogError($"[WeighingScaleDisplay] No TextMeshProUGUI assigned! Please assign the display text component.", this);
            return;
        }

        // Set initial state
        if (startDisabled)
        {
            SetDisplayActive(false);
            LogDebug("Display initialized as OFF");
        }
        else
        {
            ShowWeight(0);
            LogDebug("Display initialized showing 0");
        }
    }

    /// <summary>
    /// Show the cylinder weight (called when ON button is pressed)
    /// Can be wired to InteractableButtonCondition.OnButtonStateChanged UnityEvent
    /// </summary>
    public void ShowCylinderWeight()
    {
        SetDisplayActive(true);
        ShowWeight(cylinderWeight);
        LogDebug($"Showing cylinder weight: {cylinderWeight}g");
    }

    /// <summary>
    /// Reset display to zero (called when ZERO button is pressed)
    /// Can be wired to InteractableButtonCondition.OnButtonStateChanged UnityEvent
    /// </summary>
    public void ResetToZero()
    {
        ShowWeight(0);
        LogDebug("Display reset to 0g");
    }

    /// <summary>
    /// Show a specific weight value
    /// </summary>
    /// <param name="weight">Weight in grams (can be negative during charging)</param>
    public void ShowWeight(int weight)
    {
        if (displayText == null) return;

        currentWeight = weight;
        SetDisplayActive(true);
        displayText.text = string.Format(displayFormat, weight);
    }

    /// <summary>
    /// Show weight from string (for UnityEvent compatibility)
    /// </summary>
    public void ShowWeightFromString(string weightStr)
    {
        if (int.TryParse(weightStr, out int weight))
        {
            ShowWeight(weight);
        }
        else
        {
            Debug.LogWarning($"[WeighingScaleDisplay] Invalid weight string: {weightStr}");
        }
    }

    /// <summary>
    /// Update weight based on knob rotation (0.0 to 1.0)
    /// Called from AutoHandsKnobController.OnKnobRotated UnityEvent
    /// </summary>
    /// <param name="normalizedValue">Knob rotation from 0.0 (start) to 1.0 (fully open)</param>
    public void UpdateWeightFromKnob(float normalizedValue)
    {
        // Weight goes from 0 to 950 grams
        float exactWeight = normalizedValue * 950f;

        // Round to nearest 100g for readability (0, 100, 200, ..., 900, 950)
        int roundedWeight;
        if (exactWeight >= 900f)
        {
            // Final range: round to nearest 50 to show 900, 950
            roundedWeight = Mathf.RoundToInt(exactWeight / 50f) * 50;
        }
        else
        {
            // Earlier range: round to nearest 100
            roundedWeight = Mathf.RoundToInt(exactWeight / 100f) * 100;
        }

        ShowWeight(roundedWeight);
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
    /// Get current displayed weight
    /// </summary>
    public int GetCurrentWeight()
    {
        return currentWeight;
    }

    /// <summary>
    /// Method to be called by button event with state string ("On" or "Off")
    /// This allows direct wiring from InteractableButtonCondition.OnButtonStateChanged
    /// </summary>
    /// <param name="buttonState">State string from button: "On" or "Off"</param>
    public void OnButtonStateChanged(string buttonState)
    {
        LogDebug($"Button state changed to: {buttonState}");

        // You can implement state-specific behavior here if needed
        // For now, this is just a placeholder for future functionality
    }

    /// <summary>
    /// Method specifically for ON button - shows cylinder weight
    /// </summary>
    public void OnPowerButtonPressed(string buttonState)
    {
        Debug.Log("power pressed");
        if (buttonState == "On")
        {
            ShowCylinderWeight();
        }
    }

    /// <summary>
    /// Method specifically for ZERO button - resets to zero
    /// </summary>
    public void OnZeroButtonPressed(string buttonState)
    {
        if (buttonState == "On")
        {
            ResetToZero();
        }
    }

    // Debug logging helper
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[WeighingScaleDisplay] {message}", this);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Test method for editor - press T key to test cylinder weight, Z key for zero
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowCylinderWeight();
            Debug.Log("[WeighingScaleDisplay] Test: Showing cylinder weight");
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            ResetToZero();
            Debug.Log("[WeighingScaleDisplay] Test: Reset to zero");
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            TurnOff();
            Debug.Log("[WeighingScaleDisplay] Test: Display off");
        }
    }
#endif
}

// InteractableButtonCondition.cs
// Button/switch condition with trigger detection and animation support
using UnityEngine;
using UnityEngine.Events;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Interactable button condition for WaitForScriptCondition steps
/// Supports flip switches (with Y rotation animation) and push buttons
/// Detects hand collisions via VRHandColliderRegistry
/// </summary>
public class InteractableButtonCondition : BaseSequenceCondition
{
    public enum ButtonType { FlipSwitch, PushButton }
    public enum SwitchState { Off, On }

    [Header("Button Settings")]
    [Tooltip("Type of button - FlipSwitch rotates, PushButton just triggers")]
    public ButtonType buttonType = ButtonType.PushButton;

    [Tooltip("Allow toggling back to OFF state (if false, one-time press only)")]
    public bool allowToggle = false;

    [Tooltip("Initial state of the button/switch")]
    public SwitchState initialState = SwitchState.Off;

    [Header("Flip Switch Animation")]
    [Tooltip("The GameObject to rotate (usually a child object like 'SwitchHandle')")]
    public Transform switchPivot;

    [Tooltip("Y rotation when switch is OFF (degrees)")]
    public float offRotationY = 0f;

    [Tooltip("Y rotation when switch is ON (degrees)")]
    public float onRotationY = 180f;

    [Tooltip("Animation speed (higher = faster)")]
    public float animationSpeed = 5f;

    [Header("Push Button Animation (Optional)")]
    [Tooltip("How far button pushes in when pressed (local Z axis)")]
    public float pushDepth = 0.01f;

    [Tooltip("Button push animation speed")]
    public float pushSpeed = 10f;

    [Header("Trigger Settings")]
    [Tooltip("Require trigger collision (recommended: true)")]
    public bool requireTrigger = true;

    [Tooltip("Allow multiple presses (if allowToggle is true)")]
    public bool allowMultiplePresses = false;

    [Header("Audio (Optional)")]
    [Tooltip("Play audio from AudioSource component when pressed")]
    public bool playAudioOnPress = true;

    [Header("Callbacks (Optional)")]
    [Tooltip("Event fired when button state changes (passes new state as string: 'On' or 'Off')")]
    public UnityEvent<string> OnButtonStateChanged;

    // State tracking
    private SwitchState currentState;
    private bool hasBeenPressed = false;
    private AudioSource audioSource;
    private Collider buttonCollider;

    // Animation tracking
    private Quaternion targetRotation;
    private Vector3 originalPosition;
    private Vector3 pushedPosition;
    private bool isAnimating = false;

    void Start()
    {
        // Initialize state
        currentState = initialState;

        // Get components
        audioSource = GetComponent<AudioSource>();
        buttonCollider = GetComponent<Collider>();

        if (buttonCollider == null)
        {
            LogError("No Collider found! Add a Collider component with isTrigger = true");
        }
        else if (requireTrigger && !buttonCollider.isTrigger)
        {
            LogWarning("Collider.isTrigger should be TRUE for button detection");
        }

        // Setup flip switch
        if (buttonType == ButtonType.FlipSwitch)
        {
            if (switchPivot == null)
            {
                LogError("FlipSwitch mode requires a Switch Pivot GameObject!");
            }
            else
            {
                // Set initial rotation
                SetSwitchRotation(currentState == SwitchState.On);
            }
        }

        // Setup push button
        if (buttonType == ButtonType.PushButton && pushDepth > 0)
        {
            originalPosition = transform.localPosition;
            pushedPosition = originalPosition - new Vector3(0, 0, pushDepth);
        }

        // Validate VRHandColliderRegistry
        if (VRHandColliderRegistry.Instance == null)
        {
            LogWarning("VRHandColliderRegistry not found! Button may not detect hand touches. " +
                      "Add VRHandColliderRegistry component to your scene and assign finger colliders.");
        }
        else
        {
            LogDebug($"VRHandColliderRegistry found with {VRHandColliderRegistry.Instance.GetColliderCount()} finger colliders");
        }

        // If starting in ON state, mark condition as met
        if (currentState == SwitchState.On)
        {
            SetConditionMet();
        }

        LogInfo($"{buttonType} initialized - State: {currentState}, Toggle: {allowToggle}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!requireTrigger) return;

        // Check if collider is a hand finger tip
        if (VRHandColliderRegistry.Instance == null ||
            !VRHandColliderRegistry.Instance.IsHandCollider(other))
        {
            return;
        }

        LogDebug($"Hand finger detected: {other.name}");
        TryPressButton();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (requireTrigger) return; // Only process if not using trigger

        // Check if collider is a hand finger tip
        if (VRHandColliderRegistry.Instance == null ||
            !VRHandColliderRegistry.Instance.IsHandCollider(collision.collider))
        {
            return;
        }

        LogDebug($"Hand finger collision: {collision.collider.name}");
        TryPressButton();
    }

    /// <summary>
    /// Attempt to press/toggle the button
    /// </summary>
    void TryPressButton()
    {
        // Check if already pressed and not allowing multiple presses
        if (hasBeenPressed && !allowMultiplePresses && !allowToggle)
        {
            LogDebug("Button already pressed and multiple presses not allowed");
            return;
        }

        // Toggle state
        if (currentState == SwitchState.Off)
        {
            SetButtonState(SwitchState.On);
            hasBeenPressed = true;
            LogInfo("Button pressed - State: ON");
        }
        else if (allowToggle)
        {
            SetButtonState(SwitchState.Off);
            LogInfo("Button toggled - State: OFF");
        }
        else
        {
            LogDebug("Button already ON and toggle not allowed");
        }
    }

    /// <summary>
    /// Set button state and trigger animations/audio
    /// </summary>
    void SetButtonState(SwitchState newState)
    {
        currentState = newState;

        // Play audio
        if (playAudioOnPress && audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            LogDebug("Playing button audio");
        }

        // Handle animations based on button type
        if (buttonType == ButtonType.FlipSwitch)
        {
            SetSwitchRotation(newState == SwitchState.On);
        }
        else if (buttonType == ButtonType.PushButton && pushDepth > 0)
        {
            StartCoroutine(AnimatePushButton());
        }

        // Invoke callback event
        string stateString = newState.ToString();
        LogDebug($"Invoking OnButtonStateChanged with state: {stateString}");
        OnButtonStateChanged?.Invoke(stateString);

        // Mark condition as met if button is ON
        if (newState == SwitchState.On)
        {
            SetConditionMet();
        }
        else
        {
            // If toggled back to OFF, reset condition
            conditionMet = false;
        }
    }

    /// <summary>
    /// Set flip switch rotation (instant or animated)
    /// </summary>
    void SetSwitchRotation(bool isOn)
    {
        if (switchPivot == null) return;

        float targetY = isOn ? onRotationY : offRotationY;
        targetRotation = Quaternion.Euler(switchPivot.localRotation.eulerAngles.x,
                                          targetY,
                                          switchPivot.localRotation.eulerAngles.z);
        isAnimating = true;
    }

    /// <summary>
    /// Animate push button press (quick push and return)
    /// </summary>
    System.Collections.IEnumerator AnimatePushButton()
    {
        // Push down
        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, pushedPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = pushedPosition;

        // Wait briefly
        yield return new WaitForSeconds(0.05f);

        // Return to original position
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localPosition = Vector3.Lerp(pushedPosition, originalPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    void Update()
    {
        // Animate flip switch rotation
        if (isAnimating && switchPivot != null)
        {
            switchPivot.localRotation = Quaternion.Lerp(
                switchPivot.localRotation,
                targetRotation,
                Time.deltaTime * animationSpeed);

            // Stop animating when close enough
            if (Quaternion.Angle(switchPivot.localRotation, targetRotation) < 0.1f)
            {
                switchPivot.localRotation = targetRotation;
                isAnimating = false;
                LogDebug($"Switch animation complete - Rotation: {switchPivot.localRotation.eulerAngles.y:F1}°");
            }
        }
    }

    public override string GetStatusMessage()
    {
        if (conditionMet)
        {
            return $"{buttonType} PRESSED ✓ (State: {currentState})";
        }

        return $"{buttonType}: {currentState} - Touch to press";
    }

    public override void ResetCondition()
    {
        base.ResetCondition();
        currentState = initialState;
        hasBeenPressed = false;

        // Reset animations
        if (buttonType == ButtonType.FlipSwitch && switchPivot != null)
        {
            SetSwitchRotation(currentState == SwitchState.On);
        }
        else if (buttonType == ButtonType.PushButton)
        {
            transform.localPosition = originalPosition;
        }

        LogDebug("Button condition reset");
    }

    /// <summary>
    /// Public method to manually press button (for testing or scripted events)
    /// </summary>
    public void PressButton()
    {
        TryPressButton();
    }

    /// <summary>
    /// Get current button state
    /// </summary>
    public SwitchState GetCurrentState()
    {
        return currentState;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw button collider bounds
        var col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = conditionMet ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        // Draw switch pivot if assigned
        if (buttonType == ButtonType.FlipSwitch && switchPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(switchPivot.position, switchPivot.position + switchPivot.up * 0.05f);
        }
    }
#endif
}

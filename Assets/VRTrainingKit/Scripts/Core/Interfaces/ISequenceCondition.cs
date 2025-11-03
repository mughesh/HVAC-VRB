// ISequenceCondition.cs
// Interface for custom condition scripts in WaitForScriptCondition steps
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Interface for condition scripts that can be used with WaitForScriptCondition steps
/// Implement this interface on MonoBehaviour components to create custom step completion conditions
/// </summary>
public interface ISequenceCondition
{
    /// <summary>
    /// Returns true when the condition is met and the step should complete
    /// This property is polled by the handler to check completion status
    /// </summary>
    bool IsConditionMet { get; }

    /// <summary>
    /// Reset the condition to its initial state
    /// Called when the step starts or when restarting sequences
    /// </summary>
    void ResetCondition();

    /// <summary>
    /// Get a human-readable status message for debugging and UI display
    /// Example: "Button pressed", "Waiting for button press", "Pressure: 120/130 psi"
    /// </summary>
    string GetStatusMessage();
}

/// <summary>
/// Base class for sequence conditions providing common functionality
/// Inherit from this instead of implementing ISequenceCondition directly
/// </summary>
public abstract class BaseSequenceCondition : MonoBehaviour, ISequenceCondition
{
    [Header("Condition Status")]
    [SerializeField] protected bool conditionMet = false;

    [Header("Debug Settings")]
    [SerializeField] protected bool enableDebugLogging = false;

    /// <summary>
    /// Returns true when condition is met
    /// </summary>
    public virtual bool IsConditionMet => conditionMet;

    /// <summary>
    /// Reset condition to initial state
    /// Override this to add custom reset logic
    /// </summary>
    public virtual void ResetCondition()
    {
        conditionMet = false;
        LogDebug("Condition reset");
    }

    /// <summary>
    /// Get status message for debugging
    /// Override this to provide meaningful status information
    /// </summary>
    public abstract string GetStatusMessage();

    /// <summary>
    /// Mark condition as met (call from derived classes)
    /// </summary>
    protected virtual void SetConditionMet()
    {
        if (!conditionMet)
        {
            conditionMet = true;
            LogInfo($"Condition met! {GetStatusMessage()}");
            OnConditionMet();
        }
    }

    /// <summary>
    /// Called when condition becomes met (override for custom behavior)
    /// </summary>
    protected virtual void OnConditionMet()
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Logging helpers
    /// </summary>
    protected void LogInfo(string message)
    {
        Debug.Log($"[{GetType().Name}] {gameObject.name}: {message}");
    }

    protected void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[{GetType().Name}] {gameObject.name}: {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{GetType().Name}] {gameObject.name}: {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[{GetType().Name}] {gameObject.name}: {message}");
    }
}

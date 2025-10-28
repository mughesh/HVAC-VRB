// DummyCondition.cs
// Simple test condition for testing WaitForScriptCondition system
// Press Space key to complete the condition
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Dummy condition for testing WaitForScriptCondition system
/// Press Space key (or wait for timer) to mark condition as met
/// </summary>
public class DummyCondition : BaseSequenceCondition
{
    [Header("Dummy Condition Settings")]
    [SerializeField] private bool useSpaceKey = true;
    [Tooltip("If > 0, auto-complete after this many seconds")]
    [SerializeField] private float autoCompleteAfterSeconds = 0f;

    private float startTime;
    private bool hasStarted = false;

    void Start()
    {
        startTime = Time.time;
        hasStarted = true;
        LogInfo("Dummy condition started - Press SPACE to complete" +
                (autoCompleteAfterSeconds > 0 ? $" or wait {autoCompleteAfterSeconds}s" : ""));
    }

    void Update()
    {
        // Skip if already completed
        if (conditionMet) return;

        // Check for space key press
        if (useSpaceKey && Input.GetKeyDown(KeyCode.Space))
        {
            LogInfo("Space key pressed - marking condition as met!");
            SetConditionMet();
            return;
        }

        // Check for auto-complete timer
        if (autoCompleteAfterSeconds > 0 && hasStarted)
        {
            float elapsed = Time.time - startTime;
            if (elapsed >= autoCompleteAfterSeconds)
            {
                LogInfo($"Auto-complete timer reached ({autoCompleteAfterSeconds}s)");
                SetConditionMet();
            }
        }
    }

    public override string GetStatusMessage()
    {
        if (conditionMet)
        {
            return "Dummy condition MET ✓";
        }

        if (autoCompleteAfterSeconds > 0 && hasStarted)
        {
            float elapsed = Time.time - startTime;
            float remaining = Mathf.Max(0, autoCompleteAfterSeconds - elapsed);
            return $"Press SPACE or wait {remaining:F1}s...";
        }

        return "Press SPACE to complete";
    }

    public override void ResetCondition()
    {
        base.ResetCondition();
        startTime = Time.time;
        hasStarted = true;
        LogDebug("Dummy condition reset");
    }
}

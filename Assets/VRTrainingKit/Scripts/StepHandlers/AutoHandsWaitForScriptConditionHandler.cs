// AutoHandsWaitForScriptConditionHandler.cs
// Handles WaitForScriptCondition steps by polling ISequenceCondition components
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for WaitForScriptCondition steps using AutoHands framework
/// Polls ISequenceCondition components to determine step completion
/// </summary>
public class AutoHandsWaitForScriptConditionHandler : BaseAutoHandsStepHandler
{
    [Header("Polling Settings")]
    [SerializeField] private float pollInterval = 0.1f; // Check condition every 0.1 seconds

    // Active step tracking
    private class ConditionTracker
    {
        public InteractionStep step;
        public GameObject targetObject;
        public ISequenceCondition condition;
        public float nextPollTime;
    }

    private Dictionary<InteractionStep, ConditionTracker> activeConditions = new Dictionary<InteractionStep, ConditionTracker>();

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.WaitForScriptCondition;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("⏳ AutoHandsWaitForScriptConditionHandler initialized");
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"⏳ Starting WaitForScriptCondition step: {step.stepName}");

        var targetObject = step.targetObject.GameObject;
        if (targetObject == null)
        {
            LogError($"Target object is null for step: {step.stepName}");
            return;
        }

        // Find ISequenceCondition component on target object
        var condition = targetObject.GetComponent<ISequenceCondition>();
        if (condition == null)
        {
            LogError($"No ISequenceCondition component found on {targetObject.name} for step: {step.stepName}");
            LogWarning($"Make sure the target object has a component that implements ISequenceCondition interface");
            return;
        }

        LogInfo($"⏳ Found condition component: {condition.GetType().Name} on {targetObject.name}");

        // Reset condition to initial state
        condition.ResetCondition();
        LogDebug($"⏳ Condition reset. Initial status: {condition.GetStatusMessage()}");

        // Create tracker for this step
        var tracker = new ConditionTracker
        {
            step = step,
            targetObject = targetObject,
            condition = condition,
            nextPollTime = Time.time // Poll immediately on first frame
        };

        activeConditions[step] = tracker;

        // Check immediately if condition is already met
        if (condition.IsConditionMet)
        {
            LogInfo($"⏳ Condition already met on start: {condition.GetStatusMessage()}");
            CompleteConditionStep(tracker);
        }
        else
        {
            LogDebug($"⏳ Waiting for condition: {condition.GetStatusMessage()}");
        }
    }

    public override void StopStep(InteractionStep step)
    {
        if (activeConditions.ContainsKey(step))
        {
            var tracker = activeConditions[step];
            LogDebug($"⏳ Stopping condition tracking for step: {step.stepName}");
            activeConditions.Remove(step);
        }
    }

    public override void Cleanup()
    {
        LogDebug("⏳ Cleaning up AutoHands WaitForScriptCondition handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeConditions.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        activeConditions.Clear();
        base.Cleanup();
    }

    /// <summary>
    /// Update - Poll all active conditions at regular intervals
    /// </summary>
    void Update()
    {
        if (activeConditions.Count == 0) return;

        float currentTime = Time.time;

        // Check each active condition
        foreach (var kvp in new List<KeyValuePair<InteractionStep, ConditionTracker>>(activeConditions))
        {
            var step = kvp.Key;
            var tracker = kvp.Value;

            // Skip if already completed
            if (step.isCompleted) continue;

            // Poll at intervals to avoid excessive checks
            if (currentTime >= tracker.nextPollTime)
            {
                PollCondition(tracker);
                tracker.nextPollTime = currentTime + pollInterval;
            }
        }
    }

    /// <summary>
    /// Poll a single condition to check if it's met
    /// </summary>
    void PollCondition(ConditionTracker tracker)
    {
        if (tracker.condition == null)
        {
            LogError($"Condition component removed from {tracker.targetObject.name}!");
            return;
        }

        // Check if condition is met
        bool wasMetBefore = tracker.step.isCompleted;
        bool isMetNow = tracker.condition.IsConditionMet;

        if (!wasMetBefore && isMetNow)
        {
            // Condition just became met
            LogInfo($"⏳ ✅ Condition met! {tracker.condition.GetStatusMessage()}");
            CompleteConditionStep(tracker);
        }
        else if (controller?.enableDebugLogging == true)
        {
            // Log periodic status updates in debug mode
            LogDebug($"⏳ Polling {tracker.targetObject.name}: {tracker.condition.GetStatusMessage()}");
        }
    }

    /// <summary>
    /// Complete a condition step
    /// </summary>
    void CompleteConditionStep(ConditionTracker tracker)
    {
        if (tracker.step.isCompleted) return;

        string reason = $"Script condition met: {tracker.condition.GetStatusMessage()}";
        CompleteStep(tracker.step, reason);

        // Remove from active tracking
        activeConditions.Remove(tracker.step);
    }

    /// <summary>
    /// Get status of all active conditions (for debugging/UI)
    /// </summary>
    public Dictionary<string, string> GetActiveConditionStatuses()
    {
        var statuses = new Dictionary<string, string>();

        foreach (var kvp in activeConditions)
        {
            var step = kvp.Key;
            var tracker = kvp.Value;

            if (tracker.condition != null)
            {
                statuses[step.stepName] = tracker.condition.GetStatusMessage();
            }
        }

        return statuses;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Display active conditions in editor for debugging
    /// </summary>
    void OnGUI()
    {
        if (!controller?.enableDebugLogging == true) return;
        if (activeConditions.Count == 0) return;

        // Show condition status overlay
        GUILayout.BeginArea(new Rect(10, 200, 400, 300));
        GUILayout.BeginVertical("box");
        GUILayout.Label("Active Script Conditions", UnityEditor.EditorStyles.boldLabel);

        foreach (var kvp in activeConditions)
        {
            var step = kvp.Key;
            var tracker = kvp.Value;

            if (tracker.condition != null)
            {
                Color statusColor = tracker.condition.IsConditionMet ? Color.green : Color.yellow;
                GUI.color = statusColor;
                GUILayout.Label($"{step.stepName}: {tracker.condition.GetStatusMessage()}");
                GUI.color = Color.white;
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
#endif
}

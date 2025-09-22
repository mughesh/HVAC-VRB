// KnobStepHandler.cs
// Handles knob rotation steps in training sequences
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for knob rotation interaction steps (TurnKnob)
/// Manages KnobController event subscriptions and angle completion detection
/// </summary>
public class KnobStepHandler : BaseStepHandler
{
    // Component cache for knob controllers
    private Dictionary<GameObject, KnobController> knobControllers = new Dictionary<GameObject, KnobController>();

    // Active step tracking with event delegates for proper cleanup
    private Dictionary<KnobController, System.Action<float>> knobEventDelegates = new Dictionary<KnobController, System.Action<float>>();
    private Dictionary<InteractionStep, KnobController> activeStepKnobs = new Dictionary<InteractionStep, KnobController>();

    void Awake()
    {
        CacheKnobControllers();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.TurnKnob;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🔄 KnobStepHandler initialized");

        // Refresh cache in case scene changed
        CacheKnobControllers();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔄 Starting knob step: {step.stepName}");

        var targetObject = step.targetObject.GameObject;
        if (targetObject == null)
        {
            LogError($"Target object is null for knob step: {step.stepName}");
            return;
        }

        if (!knobControllers.ContainsKey(targetObject))
        {
            LogError($"No knob controller found for object: {targetObject.name} in step: {step.stepName}");
            return;
        }

        var knobController = knobControllers[targetObject];

        // Apply parameter overrides if needed
        ApplyKnobStepParameters(step, knobController);

        // Subscribe to angle change events
        System.Action<float> angleDelegate = (angle) => OnKnobAngleChanged(step, angle);
        knobEventDelegates[knobController] = angleDelegate;
        knobController.OnAngleChanged += angleDelegate;

        // Track this active step
        activeStepKnobs[step] = knobController;

        LogDebug($"🔄 Subscribed to knob events for: {targetObject.name} (Current: {knobController.CurrentAngle:F1}°, Target: {step.targetAngle:F1}°)");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔄 Stopping knob step: {step.stepName}");

        if (activeStepKnobs.ContainsKey(step))
        {
            var knobController = activeStepKnobs[step];

            // Unsubscribe from events
            if (knobEventDelegates.ContainsKey(knobController))
            {
                knobController.OnAngleChanged -= knobEventDelegates[knobController];
                knobEventDelegates.Remove(knobController);
            }

            // Remove from tracking
            activeStepKnobs.Remove(step);

            LogDebug($"🔄 Unsubscribed from knob events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🔄 Cleaning up knob step handler...");

        // Stop all active steps
        foreach (var step in activeStepKnobs.Keys)
        {
            StopStep(step);
        }

        // Clear cache
        knobControllers.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all knob controllers in the scene
    /// </summary>
    void CacheKnobControllers()
    {
        LogDebug("🔄 Caching knob controllers...");

        knobControllers.Clear();

        var knobControllerComponents = FindObjectsOfType<KnobController>();
        foreach (var knobController in knobControllerComponents)
        {
            knobControllers[knobController.gameObject] = knobController;
            LogDebug($"🔄 Cached knob controller: {knobController.name}");
        }

        LogInfo($"🔄 Cached {knobControllers.Count} knob controllers");
    }

    /// <summary>
    /// Apply sequence builder parameter overrides to knob (if needed)
    /// </summary>
    void ApplyKnobStepParameters(InteractionStep step, KnobController knobController)
    {
        // For now, knobs don't need parameter overrides like valves do
        // Target angle and tolerance are handled in the completion check
        // Future enhancement: Could override knob profile settings if needed

        LogDebug($"🔄 PARAMS: Knob step parameters - Target: {step.targetAngle}°, Tolerance: ±{step.angleTolerance}°");
    }

    /// <summary>
    /// Handle knob angle change event
    /// </summary>
    void OnKnobAngleChanged(InteractionStep step, float currentAngle)
    {
        if (step.isCompleted) return;

        float targetAngle = step.targetAngle;
        float tolerance = step.angleTolerance;

        // Calculate angle difference using Unity's DeltaAngle for proper wrapping
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        // Enhanced debug logging with progress indication
        LogDebug($"🔄 Knob rotation - {step.stepName}: Current: {currentAngle:F2}°, Target: {targetAngle:F2}°, Diff: {angleDifference:F2}°, Tolerance: ±{tolerance:F2}°");

        // Check if target angle is reached within tolerance
        if (angleDifference <= tolerance)
        {
            LogDebug($"🔄 Knob target reached! Completing step: {step.stepName}");
            CompleteStep(step, $"Knob rotated to {currentAngle:F1}° (target: {targetAngle}°, tolerance: ±{tolerance}°)");
        }
        else
        {
            // Show progress toward target (optional, can be disabled for performance)
            if (controller?.enableDebugLogging == true)
            {
                float progress = Mathf.Max(0f, 1f - (angleDifference / (tolerance * 3f))); // 3x tolerance = 0% progress
                if (progress > 0.1f)
                {
                    LogDebug($"🔄 Knob progress: {(progress * 100f):F0}% toward target");
                }
            }
        }
    }
}
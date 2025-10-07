// AutoHandsKnobStepHandler.cs
// Handles knob rotation steps in training sequences using AutoHands framework
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for knob rotation interaction steps (TurnKnob) using AutoHands framework
/// Manages KnobController event subscriptions and angle completion detection
/// KnobController is framework-agnostic (works with both XRI and AutoHands)
/// </summary>
public class AutoHandsKnobStepHandler : BaseAutoHandsStepHandler
{
    // Component cache for AutoHands knob controllers
    private Dictionary<GameObject, AutoHandsKnobController> knobControllers = new Dictionary<GameObject, AutoHandsKnobController>();

    // Active step tracking with event delegates for proper cleanup
    private Dictionary<AutoHandsKnobController, System.Action<float>> knobEventDelegates = new Dictionary<AutoHandsKnobController, System.Action<float>>();
    private Dictionary<InteractionStep, AutoHandsKnobController> activeStepKnobs = new Dictionary<InteractionStep, AutoHandsKnobController>();

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
        LogInfo("🔄 AutoHandsKnobStepHandler initialized");

        // Refresh cache in case scene changed
        CacheKnobControllers();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔄 Starting AutoHands knob step: {step.stepName}");

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

        // Subscribe to angle change events
        System.Action<float> angleDelegate = (angle) => OnKnobAngleChanged(step, angle);
        knobEventDelegates[knobController] = angleDelegate;
        knobController.OnAngleChanged += angleDelegate;

        // Track this active step
        activeStepKnobs[step] = knobController;

        LogDebug($"🔄 Subscribed to AutoHands knob events for: {targetObject.name} (Current: {knobController.CurrentAngle:F1}°, Target: {step.targetAngle:F1}°)");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔄 Stopping AutoHands knob step: {step.stepName}");

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

            LogDebug($"🔄 Unsubscribed from AutoHands knob events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🔄 Cleaning up AutoHands knob step handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepKnobs.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        // Clear cache
        knobControllers.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all AutoHands knob controllers in the scene
    /// </summary>
    void CacheKnobControllers()
    {
        LogDebug("🔄 Caching AutoHands knob controllers...");

        knobControllers.Clear();

        var knobControllerComponents = FindObjectsOfType<AutoHandsKnobController>();
        foreach (var knobController in knobControllerComponents)
        {
            knobControllers[knobController.gameObject] = knobController;
            LogDebug($"🔄 Cached AutoHands knob controller: {knobController.name}");
        }

        LogInfo($"🔄 Cached {knobControllers.Count} AutoHands knob controllers");
    }

    /// <summary>
    /// Handle knob angle change event
    /// KnobController.OnAngleChanged fires when HingeJoint.angle changes
    /// </summary>
    void OnKnobAngleChanged(InteractionStep step, float currentAngle)
    {
        if (step.isCompleted) return;

        float targetAngle = step.targetAngle;
        float tolerance = step.angleTolerance;

        // Calculate angle difference using Unity's DeltaAngle for proper wrapping
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        // Enhanced debug logging with progress indication
        LogDebug($"🔄 AutoHands knob rotation - {step.stepName}: Current: {currentAngle:F2}°, Target: {targetAngle:F2}°, Diff: {angleDifference:F2}°, Tolerance: ±{tolerance:F2}°");

        // Check if target angle is reached within tolerance
        if (angleDifference <= tolerance)
        {
            LogDebug($"🔄 AutoHands knob target reached! Completing step: {step.stepName}");
            CompleteStep(step, $"Knob rotated to {currentAngle:F1}° (target: {targetAngle}°, tolerance: ±{tolerance}°) with AutoHands");
        }
        else
        {
            // Show progress toward target (optional, can be disabled for performance)
            if (controller?.enableDebugLogging == true)
            {
                float progress = Mathf.Max(0f, 1f - (angleDifference / (tolerance * 3f))); // 3x tolerance = 0% progress
                if (progress > 0.1f)
                {
                    LogDebug($"🔄 AutoHands knob progress: {(progress * 100f):F0}% toward target");
                }
            }
        }
    }
}

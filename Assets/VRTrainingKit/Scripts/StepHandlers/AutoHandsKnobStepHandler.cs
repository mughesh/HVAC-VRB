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

    // Track initial angles for rotation direction detection
    private Dictionary<InteractionStep, float> initialAngles = new Dictionary<InteractionStep, float>();

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
        Debug.Log($"🔄 [AutoHandsKnobStepHandler] Starting AutoHands knob step: {step.stepName}");

        var targetObject = step.targetObject.GameObject;
        if (targetObject == null)
        {
            Debug.LogError($"🔄 [AutoHandsKnobStepHandler] Target object is null for knob step: {step.stepName}");
            return;
        }

        Debug.Log($"🔄 [AutoHandsKnobStepHandler] Target object found: {targetObject.name}");

        if (!knobControllers.ContainsKey(targetObject))
        {
            Debug.LogError($"🔄 [AutoHandsKnobStepHandler] No knob controller found for object: {targetObject.name} in step: {step.stepName}. Cached knobs: {knobControllers.Count}");
            foreach (var kvp in knobControllers)
            {
                Debug.Log($"  - Cached knob: {kvp.Key.name}");
            }
            return;
        }

        var knobController = knobControllers[targetObject];

        // Store initial angle for direction detection
        float initialAngle = knobController.CurrentHingeAngle;
        initialAngles[step] = initialAngle;

        Debug.Log($"🔄 [AutoHandsKnobStepHandler] Initial angle captured: {initialAngle:F1}°");

        // Subscribe to angle change events
        System.Action<float> angleDelegate = (angle) => OnKnobAngleChanged(step, angle);
        knobEventDelegates[knobController] = angleDelegate;
        knobController.OnAngleChanged += angleDelegate;

        // Track this active step
        activeStepKnobs[step] = knobController;

        Debug.Log($"🔄 [AutoHandsKnobStepHandler] ✅ Subscribed to AutoHands knob events for: {targetObject.name} (Initial: {initialAngle:F1}°, Target: {step.targetAngle:F1}°, Direction: {step.knobRotationType})");
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
            initialAngles.Remove(step);

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
        Debug.Log($"🔄 [AutoHandsKnobStepHandler] OnKnobAngleChanged called! Step: {step.stepName}, Angle: {currentAngle:F2}°, isCompleted: {step.isCompleted}");

        if (step.isCompleted)
        {
            Debug.LogWarning($"🔄 [AutoHandsKnobStepHandler] ⚠️ Step '{step.stepName}' already completed but still receiving events! This should not happen - event not cleaned up properly!");
            return;
        }

        // Get the knob controller to access HingeJoint data
        if (!activeStepKnobs.ContainsKey(step))
        {
            Debug.LogWarning($"🔄 [AutoHandsKnobStepHandler] Step not in activeStepKnobs dictionary!");
            return;
        }
        var knobController = activeStepKnobs[step];

        float targetAngle = step.targetAngle;
        float tolerance = step.angleTolerance;

        // Calculate angle difference using Unity's DeltaAngle for proper wrapping
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        Debug.Log($"🔄 [AutoHandsKnobStepHandler] Angle check - Current: {currentAngle:F2}°, Target: {targetAngle:F2}°, Diff: {angleDifference:F2}°, Tolerance: ±{tolerance:F2}°");

        // Check if target angle is reached within tolerance
        Debug.Log($"🔄 [AutoHandsKnobStepHandler] About to check if {angleDifference:F2} <= {tolerance:F2}... Result: {(angleDifference <= tolerance)}");

        if (angleDifference <= tolerance)
        {
            Debug.Log($"🔄 [AutoHandsKnobStepHandler] ✅ Target angle reached! Now validating direction...");

            try
            {
                // Validate rotation direction if required
                bool directionValid = ValidateRotationDirection(step, knobController, currentAngle);

                Debug.Log($"🔄 [AutoHandsKnobStepHandler] Direction validation result: {directionValid}");

                if (directionValid)
                {
                    Debug.Log($"🔄 [AutoHandsKnobStepHandler] ✅ Direction valid! Completing step: {step.stepName}");
                    CompleteStep(step, $"Knob rotated to {currentAngle:F1}° (target: {targetAngle}°, direction: {step.knobRotationType}) with AutoHands");
                }
                else
                {
                    Debug.LogWarning($"🔄 [AutoHandsKnobStepHandler] ❌ WRONG direction! Required: {step.knobRotationType}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"🔄 [AutoHandsKnobStepHandler] ❌ Exception during direction validation: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            Debug.Log($"🔄 [AutoHandsKnobStepHandler] Not at target yet. Diff: {angleDifference:F2}° > Tolerance: {tolerance:F2}°");
        }
    }

    /// <summary>
    /// Validates if the rotation direction matches the required direction
    /// Uses HingeJoint angle to determine if rotation was toward min or max limit
    /// </summary>
    private bool ValidateRotationDirection(InteractionStep step, AutoHandsKnobController knobController, float currentAngle)
    {
        Debug.Log($"🔄 [AutoHandsKnobStepHandler] ValidateRotationDirection called for step: {step.stepName}, Type: {step.knobRotationType}");

        // If direction doesn't matter, always return true
        if (step.knobRotationType == InteractionStep.KnobRotationType.Any)
        {
            Debug.Log($"🔄 [AutoHandsKnobStepHandler] Rotation type is 'Any', returning true");
            return true;
        }

        // Get initial angle that was captured when step started
        if (!initialAngles.ContainsKey(step))
        {
            Debug.LogWarning($"🔄 [AutoHandsKnobStepHandler] ⚠️ No initial angle recorded for step {step.stepName}, accepting any direction");
            return true;
        }

        float initialAngle = initialAngles[step];
        float hingeMinLimit = knobController.HingeMinLimit;
        float hingeMaxLimit = knobController.HingeMaxLimit;

        Debug.Log($"🔄 [AutoHandsKnobStepHandler] Direction validation - Initial: {initialAngle:F1}°, Current: {currentAngle:F1}°, Limits: [{hingeMinLimit:F1}° to {hingeMaxLimit:F1}°]");

        switch (step.knobRotationType)
        {
            case InteractionStep.KnobRotationType.OpenToMax:
                // Must have rotated toward max limit (increasing angle)
                bool movedTowardMax = currentAngle > initialAngle;
                Debug.Log($"🔄 [AutoHandsKnobStepHandler] OpenToMax validation: Current({currentAngle:F1}°) > Initial({initialAngle:F1}°) = {movedTowardMax}");
                return movedTowardMax;

            case InteractionStep.KnobRotationType.CloseToMin:
                // Must have rotated toward min limit (decreasing angle)
                bool movedTowardMin = currentAngle < initialAngle;
                Debug.Log($"🔄 [AutoHandsKnobStepHandler] CloseToMin validation: Current({currentAngle:F1}°) < Initial({initialAngle:F1}°) = {movedTowardMin}");
                return movedTowardMin;

            default:
                Debug.Log($"🔄 [AutoHandsKnobStepHandler] Default case, returning true");
                return true;
        }
    }
}

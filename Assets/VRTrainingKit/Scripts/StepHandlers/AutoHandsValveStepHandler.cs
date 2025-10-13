// AutoHandsValveStepHandler.cs
// Handles valve interaction steps in training sequences using AutoHands framework
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for valve interaction steps (TightenValve, LoosenValve) using AutoHands framework
/// Manages AutoHandsValveControllerV2 event subscriptions and state-based completion detection
/// </summary>
public class AutoHandsValveStepHandler : BaseAutoHandsStepHandler
{
    // Component cache for AutoHands valve controllers
    private Dictionary<GameObject, AutoHandsValveControllerV2> valveControllers = new Dictionary<GameObject, AutoHandsValveControllerV2>();

    // Active step tracking with event delegates for proper cleanup
    private Dictionary<AutoHandsValveControllerV2, System.Action> tightenEventDelegates = new Dictionary<AutoHandsValveControllerV2, System.Action>();
    private Dictionary<AutoHandsValveControllerV2, System.Action> loosenEventDelegates = new Dictionary<AutoHandsValveControllerV2, System.Action>();
    private Dictionary<InteractionStep, AutoHandsValveControllerV2> activeStepValves = new Dictionary<InteractionStep, AutoHandsValveControllerV2>();

    void Awake()
    {
        CacheValveControllers();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.TightenValve ||
               stepType == InteractionStep.StepType.LoosenValve ||
               stepType == InteractionStep.StepType.InstallValve ||
               stepType == InteractionStep.StepType.RemoveValve;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🔧 AutoHandsValveStepHandler initialized");

        // Refresh cache in case scene changed
        CacheValveControllers();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔧 Starting AutoHands valve step: {step.stepName} (Type: {step.type})");

        var targetObject = step.targetObject.GameObject;
        if (targetObject == null)
        {
            LogError($"Target object is null for valve step: {step.stepName}");
            return;
        }

        if (!valveControllers.ContainsKey(targetObject))
        {
            LogError($"No valve controller found for object: {targetObject.name} in step: {step.stepName}");
            return;
        }

        var valveController = valveControllers[targetObject];

        // Apply parameter overrides from InteractionStep to valve controller
        ApplyStepParameterOverrides(step, valveController);

        // Subscribe to appropriate events based on step type
        switch (step.type)
        {
            case InteractionStep.StepType.TightenValve:
                SubscribeToTightenEvent(step, valveController);
                break;

            case InteractionStep.StepType.LoosenValve:
                SubscribeToLoosenEvent(step, valveController);
                break;

            case InteractionStep.StepType.InstallValve:
                SubscribeToSnapEvent(step, valveController);
                break;

            case InteractionStep.StepType.RemoveValve:
                SubscribeToRemoveEvent(step, valveController);
                break;
        }

        // Track this active step
        activeStepValves[step] = valveController;

        LogDebug($"🔧 Subscribed to AutoHands valve events for: {targetObject.name} (State: {valveController.CurrentState}-{valveController.CurrentSubstate})");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔧 Stopping AutoHands valve step: {step.stepName}");

        if (activeStepValves.ContainsKey(step))
        {
            var valveController = activeStepValves[step];

            // Unsubscribe from all events
            UnsubscribeFromAllEvents(valveController);

            // Remove from tracking
            activeStepValves.Remove(step);

            LogDebug($"🔧 Unsubscribed from AutoHands valve events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🔧 Cleaning up AutoHands valve step handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepValves.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        // Clear cache
        valveControllers.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all AutoHands valve controllers in the scene
    /// </summary>
    void CacheValveControllers()
    {
        LogDebug("🔧 Caching AutoHands valve controllers...");

        valveControllers.Clear();

        var valveControllerComponents = FindObjectsOfType<AutoHandsValveControllerV2>();
        foreach (var valveController in valveControllerComponents)
        {
            valveControllers[valveController.gameObject] = valveController;
            LogDebug($"🔧 Cached AutoHands valve controller: {valveController.name}");
        }

        LogInfo($"🔧 Cached {valveControllers.Count} AutoHands valve controllers");
    }

    /// <summary>
    /// Apply parameter overrides from InteractionStep to valve controller
    /// </summary>
    void ApplyStepParameterOverrides(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        // Note: InteractionStep has these fields (from TrainingSequence.cs):
        // - tightenThreshold
        // - loosenThreshold
        // - valveAngleTolerance
        // - rotationAxis
        // - rotationDampening

        // For now, AutoHandsValveControllerV2 uses profile-configured values
        // If step-level overrides are needed, we'd need to add methods to ValveController
        // to accept runtime parameter changes

        LogDebug($"🔧 Parameter overrides for step '{step.stepName}':");
        LogDebug($"   - Tighten Threshold: {step.tightenThreshold}° (from step)");
        LogDebug($"   - Loosen Threshold: {step.loosenThreshold}° (from step)");
        LogDebug($"   - Angle Tolerance: {step.valveAngleTolerance}° (from step)");

        // TODO: If dynamic parameter override is needed, add methods to AutoHandsValveControllerV2
        // like: valveController.SetTightenThreshold(step.tightenThreshold);
    }

    /// <summary>
    /// Subscribe to valve tighten event
    /// </summary>
    void SubscribeToTightenEvent(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        // Check if valve is in correct state for tightening
        if (valveController.CurrentState != ValveState.Locked)
        {
            LogWarning($"Valve {valveController.name} is not in Locked state (current: {valveController.CurrentState}). Waiting for snap...");
        }

        // Create delegate for this specific step
        System.Action tightenDelegate = () => OnValveTightened(step, valveController);
        tightenEventDelegates[valveController] = tightenDelegate;
        valveController.OnValveTightened += tightenDelegate;

        LogDebug($"🔧 Subscribed to OnValveTightened for {valveController.name}");
    }

    /// <summary>
    /// Subscribe to valve loosen event
    /// </summary>
    void SubscribeToLoosenEvent(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        // Check if valve is in correct state for loosening (should be Tight)
        if (valveController.CurrentSubstate != ValveSubstate.Tight)
        {
            LogWarning($"Valve {valveController.name} is not in Tight state (current: {valveController.CurrentState}-{valveController.CurrentSubstate}). Waiting for tightening...");
        }

        // Create delegate for this specific step
        System.Action loosenDelegate = () => OnValveLoosened(step, valveController);
        loosenEventDelegates[valveController] = loosenDelegate;
        valveController.OnValveLoosened += loosenDelegate;

        LogDebug($"🔧 Subscribed to OnValveLoosened for {valveController.name}");
    }

    /// <summary>
    /// Subscribe to valve snap event (InstallValve step)
    /// </summary>
    void SubscribeToSnapEvent(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        // Create delegate for snap event
        System.Action snapDelegate = () => OnValveSnapped(step, valveController);

        // Store as tighten delegate (reusing dictionary for snap events)
        tightenEventDelegates[valveController] = snapDelegate;
        valveController.OnValveSnapped += snapDelegate;

        LogDebug($"🔧 Subscribed to OnValveSnapped for {valveController.name}");
    }

    /// <summary>
    /// Subscribe to valve remove event (RemoveValve step)
    /// </summary>
    void SubscribeToRemoveEvent(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        // Create delegate for remove event
        System.Action removeDelegate = () => OnValveRemoved(step, valveController);

        // Store as loosen delegate (reusing dictionary for remove events)
        loosenEventDelegates[valveController] = removeDelegate;
        valveController.OnValveRemoved += removeDelegate;

        LogDebug($"🔧 Subscribed to OnValveRemoved for {valveController.name}");
    }

    /// <summary>
    /// Unsubscribe from all valve events for a controller
    /// </summary>
    void UnsubscribeFromAllEvents(AutoHandsValveControllerV2 valveController)
    {
        // Unsubscribe from tighten event
        if (tightenEventDelegates.ContainsKey(valveController))
        {
            valveController.OnValveTightened -= tightenEventDelegates[valveController];
            valveController.OnValveSnapped -= tightenEventDelegates[valveController];
            tightenEventDelegates.Remove(valveController);
        }

        // Unsubscribe from loosen event
        if (loosenEventDelegates.ContainsKey(valveController))
        {
            valveController.OnValveLoosened -= loosenEventDelegates[valveController];
            valveController.OnValveRemoved -= loosenEventDelegates[valveController];
            loosenEventDelegates.Remove(valveController);
        }
    }

    /// <summary>
    /// Handle valve tightened event
    /// </summary>
    void OnValveTightened(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve tightened! Completing step: {step.stepName}");
        CompleteStep(step, $"Valve {valveController.name} tightened to {valveController.CurrentRotation:F1}° with AutoHands");
    }

    /// <summary>
    /// Handle valve loosened event
    /// </summary>
    void OnValveLoosened(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve loosened! Completing step: {step.stepName}");
        CompleteStep(step, $"Valve {valveController.name} loosened (unlocked) with AutoHands");
    }

    /// <summary>
    /// Handle valve snapped event (InstallValve)
    /// </summary>
    void OnValveSnapped(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve snapped to socket! Completing step: {step.stepName}");
        CompleteStep(step, $"Valve {valveController.name} installed in socket with AutoHands");
    }

    /// <summary>
    /// Handle valve removed event (RemoveValve)
    /// </summary>
    void OnValveRemoved(InteractionStep step, AutoHandsValveControllerV2 valveController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve removed from socket! Completing step: {step.stepName}");
        CompleteStep(step, $"Valve {valveController.name} removed from socket with AutoHands");
    }
}

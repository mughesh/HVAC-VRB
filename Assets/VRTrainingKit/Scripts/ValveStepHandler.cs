// ValveStepHandler.cs
// Handles valve interaction steps with state-aware parameter application
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for valve-based interaction steps with proper state machine integration
/// Waits for valve stabilization before applying sequence parameters and subscribing to events
/// </summary>
public class ValveStepHandler : BaseStepHandler
{
    // Component cache for valve controllers
    private Dictionary<GameObject, ValveController> valveControllers = new Dictionary<GameObject, ValveController>();

    // Active step tracking with event delegates for proper cleanup
    private Dictionary<ValveController, System.Action> valveTightenedEventDelegates = new Dictionary<ValveController, System.Action>();
    private Dictionary<ValveController, System.Action> valveLoosenedEventDelegates = new Dictionary<ValveController, System.Action>();
    private Dictionary<InteractionStep, ValveController> activeStepValves = new Dictionary<InteractionStep, ValveController>();

    // Step monitoring coroutines
    private Dictionary<InteractionStep, Coroutine> stepMonitoringCoroutines = new Dictionary<InteractionStep, Coroutine>();

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
        LogInfo("🔧 ValveStepHandler initialized");

        // Refresh cache in case scene changed
        CacheValveControllers();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔧 Starting valve step: {step.stepName} [{step.type}]");

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

        // Track this active step
        activeStepValves[step] = valveController;

        // Start state-aware monitoring for this step
        var monitoringCoroutine = StartCoroutine(MonitorValveStepProgression(step, valveController));
        stepMonitoringCoroutines[step] = monitoringCoroutine;

        LogDebug($"🔧 Started valve step monitoring for: {targetObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔧 Stopping valve step: {step.stepName}");

        // Stop monitoring coroutine
        if (stepMonitoringCoroutines.ContainsKey(step))
        {
            if (stepMonitoringCoroutines[step] != null)
            {
                StopCoroutine(stepMonitoringCoroutines[step]);
            }
            stepMonitoringCoroutines.Remove(step);
        }

        // Clean up valve event subscriptions
        if (activeStepValves.ContainsKey(step))
        {
            var valveController = activeStepValves[step];
            UnsubscribeFromValveEvents(valveController);
            activeStepValves.Remove(step);
        }

        LogDebug($"🔧 Stopped valve step monitoring for: {step.stepName}");
    }

    public override void Cleanup()
    {
        LogDebug("🔧 Cleaning up valve step handler...");

        // Stop all monitoring coroutines
        foreach (var coroutine in stepMonitoringCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        stepMonitoringCoroutines.Clear();

        // Clean up all valve event subscriptions
        foreach (var valveController in valveTightenedEventDelegates.Keys)
        {
            UnsubscribeFromValveEvents(valveController);
        }

        valveTightenedEventDelegates.Clear();
        valveLoosenedEventDelegates.Clear();
        activeStepValves.Clear();
        valveControllers.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all valve controllers in the scene
    /// </summary>
    void CacheValveControllers()
    {
        LogDebug("🔧 Caching valve controllers...");

        valveControllers.Clear();

        var valveControllerComponents = FindObjectsOfType<ValveController>();
        foreach (var valveController in valveControllerComponents)
        {
            valveControllers[valveController.gameObject] = valveController;
            LogDebug($"🔧 Cached valve controller: {valveController.name}");
        }

        LogInfo($"🔧 Cached {valveControllers.Count} valve controllers");
    }

    /// <summary>
    /// Monitor valve step progression with state-aware parameter application
    /// </summary>
    IEnumerator MonitorValveStepProgression(InteractionStep step, ValveController valveController)
    {
        LogDebug($"🔧 MONITORING: Starting valve step monitoring for {step.stepName}");

        // Phase 1: Wait for valve to reach appropriate state for this step type
        yield return WaitForValveReadyState(step, valveController);

        // Phase 2: Apply sequence builder parameter overrides (if any)
        ApplyValveStepParameters(step, valveController);

        // Phase 3: Subscribe to appropriate valve events for step completion
        SubscribeToValveEvents(step, valveController);

        LogDebug($"🔧 MONITORING: Valve step setup complete for {step.stepName}");
    }

    /// <summary>
    /// Wait for valve to reach the appropriate state for the step type
    /// </summary>
    IEnumerator WaitForValveReadyState(InteractionStep step, ValveController valveController)
    {
        ValveState requiredState;
        ValveSubstate requiredSubstate;

        // Determine required state based on step type
        switch (step.type)
        {
            case InteractionStep.StepType.TightenValve:
            case InteractionStep.StepType.InstallValve:
                // Need valve to be in socket and loose (ready for tightening)
                requiredState = ValveState.Locked;
                requiredSubstate = ValveSubstate.Loose;
                break;

            case InteractionStep.StepType.LoosenValve:
                // Need valve to be tight (ready for loosening)
                requiredState = ValveState.Locked;
                requiredSubstate = ValveSubstate.Tight;
                break;

            case InteractionStep.StepType.RemoveValve:
                // Need valve to be loose (ready for removal)
                requiredState = ValveState.Locked;
                requiredSubstate = ValveSubstate.Loose;
                break;

            default:
                LogWarning($"🔧 Unknown valve step type: {step.type}");
                yield break;
        }

        LogDebug($"🔧 WAITING: Step {step.stepName} waiting for valve state: {requiredState}-{requiredSubstate}");

        float startTime = Time.time;
        float timeout = 30f; // Generous timeout for user interaction

        // Wait for required state
        while (valveController.CurrentState != requiredState || valveController.CurrentSubstate != requiredSubstate)
        {
            // Check for timeout (but don't fail - user might be slow)
            if (Time.time - startTime > timeout)
            {
                LogWarning($"🔧 WAITING: Valve {valveController.gameObject.name} taking longer than {timeout}s to reach required state for step {step.stepName}");
                LogWarning($"🔧 Current: {valveController.CurrentState}-{valveController.CurrentSubstate}, Required: {requiredState}-{requiredSubstate}");

                // Reset timeout for next check
                startTime = Time.time;
            }

            LogDebug($"🔧 WAITING: Current: {valveController.CurrentState}-{valveController.CurrentSubstate}, Required: {requiredState}-{requiredSubstate}");
            yield return new WaitForSeconds(0.5f); // Check every half second
        }

        LogDebug($"🔧 READY: Valve {valveController.gameObject.name} reached required state for step {step.stepName}");
    }

    /// <summary>
    /// Apply sequence builder parameter overrides to valve controller
    /// </summary>
    void ApplyValveStepParameters(InteractionStep step, ValveController valveController)
    {
        LogDebug($"🔧 PARAMS: Applying parameter overrides for step {step.stepName}");

        // Get current valve profile
        var currentProfile = GetValveProfile(valveController);
        if (currentProfile == null)
        {
            LogWarning($"🔧 PARAMS: No valve profile found for {valveController.gameObject.name}");
            return;
        }

        // Check if we need to apply any overrides
        bool needsOverride = false;

        if (step.rotationAxis != currentProfile.rotationAxis) needsOverride = true;
        if (IsTightenStep(step.type) && step.tightenThreshold != currentProfile.tightenThreshold) needsOverride = true;
        if (IsLoosenStep(step.type) && step.loosenThreshold != currentProfile.loosenThreshold) needsOverride = true;
        if (step.valveAngleTolerance != currentProfile.angleTolerance) needsOverride = true;
        if (step.rotationDampening != currentProfile.rotationDampening) needsOverride = true;

        if (!needsOverride)
        {
            LogDebug($"🔧 PARAMS: No parameter overrides needed for {step.stepName}");
            return;
        }

        // Create runtime profile with overrides
        var runtimeProfile = ScriptableObject.CreateInstance<ValveProfile>();

        // Copy base settings from original profile
        runtimeProfile.profileName = $"{currentProfile.profileName}_Runtime_{step.type}";
        runtimeProfile.rotationAxis = step.rotationAxis != currentProfile.rotationAxis ? step.rotationAxis : currentProfile.rotationAxis;

        // Apply only relevant threshold values based on step type
        if (IsTightenStep(step.type))
        {
            runtimeProfile.tightenThreshold = step.tightenThreshold != currentProfile.tightenThreshold ? step.tightenThreshold : currentProfile.tightenThreshold;
            runtimeProfile.loosenThreshold = currentProfile.loosenThreshold; // Keep profile default
        }
        else if (IsLoosenStep(step.type))
        {
            runtimeProfile.tightenThreshold = currentProfile.tightenThreshold; // Keep profile default
            runtimeProfile.loosenThreshold = step.loosenThreshold != currentProfile.loosenThreshold ? step.loosenThreshold : currentProfile.loosenThreshold;
        }
        else
        {
            // Install/Remove steps use both thresholds
            runtimeProfile.tightenThreshold = step.tightenThreshold != currentProfile.tightenThreshold ? step.tightenThreshold : currentProfile.tightenThreshold;
            runtimeProfile.loosenThreshold = step.loosenThreshold != currentProfile.loosenThreshold ? step.loosenThreshold : currentProfile.loosenThreshold;
        }

        runtimeProfile.angleTolerance = step.valveAngleTolerance != currentProfile.angleTolerance ? step.valveAngleTolerance : currentProfile.angleTolerance;
        runtimeProfile.rotationDampening = step.rotationDampening != currentProfile.rotationDampening ? step.rotationDampening : currentProfile.rotationDampening;

        // Copy other essential settings
        runtimeProfile.compatibleSocketTags = currentProfile.compatibleSocketTags;
        runtimeProfile.requireSpecificSockets = currentProfile.requireSpecificSockets;
        runtimeProfile.specificCompatibleSockets = currentProfile.specificCompatibleSockets;

        // Apply the modified profile
        valveController.Configure(runtimeProfile);

        LogInfo($"🔧 PARAMS: Applied parameter overrides to {valveController.gameObject.name}:");
        LogInfo($"🔧   - Rotation Axis: {runtimeProfile.rotationAxis}");
        LogInfo($"🔧   - Tighten Threshold: {runtimeProfile.tightenThreshold}°");
        LogInfo($"🔧   - Loosen Threshold: {runtimeProfile.loosenThreshold}°");
        LogInfo($"🔧   - Angle Tolerance: {runtimeProfile.angleTolerance}°");
    }

    /// <summary>
    /// Subscribe to appropriate valve events for step completion
    /// </summary>
    void SubscribeToValveEvents(InteractionStep step, ValveController valveController)
    {
        LogDebug($"🔧 EVENTS: Subscribing to valve events for step {step.stepName}");

        // Unsubscribe first to prevent duplicates
        UnsubscribeFromValveEvents(valveController);

        switch (step.type)
        {
            case InteractionStep.StepType.TightenValve:
            case InteractionStep.StepType.InstallValve:
                SubscribeToValveTightenEvents(step, valveController);
                break;

            case InteractionStep.StepType.LoosenValve:
            case InteractionStep.StepType.RemoveValve:
                SubscribeToValveLoosenEvents(step, valveController);
                break;
        }
    }

    /// <summary>
    /// Subscribe to valve tighten events
    /// </summary>
    void SubscribeToValveTightenEvents(InteractionStep step, ValveController valveController)
    {
        System.Action tightenDelegate = () => OnValveTightened(step);
        valveTightenedEventDelegates[valveController] = tightenDelegate;
        valveController.OnValveTightened += tightenDelegate;

        LogDebug($"🔧 EVENTS: Subscribed to valve tighten events for {step.stepName}");
    }

    /// <summary>
    /// Subscribe to valve loosen events
    /// </summary>
    void SubscribeToValveLoosenEvents(InteractionStep step, ValveController valveController)
    {
        System.Action loosenDelegate = () => OnValveLoosened(step);
        valveLoosenedEventDelegates[valveController] = loosenDelegate;
        valveController.OnValveLoosened += loosenDelegate;

        LogDebug($"🔧 EVENTS: Subscribed to valve loosen events for {step.stepName}");
    }

    /// <summary>
    /// Unsubscribe from all valve events for a controller
    /// </summary>
    void UnsubscribeFromValveEvents(ValveController valveController)
    {
        if (valveTightenedEventDelegates.ContainsKey(valveController))
        {
            valveController.OnValveTightened -= valveTightenedEventDelegates[valveController];
            valveTightenedEventDelegates.Remove(valveController);
        }

        if (valveLoosenedEventDelegates.ContainsKey(valveController))
        {
            valveController.OnValveLoosened -= valveLoosenedEventDelegates[valveController];
            valveLoosenedEventDelegates.Remove(valveController);
        }
    }

    /// <summary>
    /// Handle valve tightened event
    /// </summary>
    void OnValveTightened(InteractionStep step)
    {
        LogDebug($"🔧 EVENT: Valve tightened for step {step.stepName}");
        CompleteStep(step, "Valve tightened successfully");
    }

    /// <summary>
    /// Handle valve loosened event
    /// </summary>
    void OnValveLoosened(InteractionStep step)
    {
        LogDebug($"🔧 EVENT: Valve loosened for step {step.stepName}");
        CompleteStep(step, "Valve loosened successfully");
    }

    /// <summary>
    /// Get valve profile using reflection (helper method)
    /// </summary>
    ValveProfile GetValveProfile(ValveController valveController)
    {
        var profileField = typeof(ValveController).GetField("profile",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (profileField != null)
        {
            return (ValveProfile)profileField.GetValue(valveController);
        }

        return null;
    }

    /// <summary>
    /// Helper method to check if a step type requires tighten threshold
    /// </summary>
    bool IsTightenStep(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.TightenValve ||
               stepType == InteractionStep.StepType.InstallValve;
    }

    /// <summary>
    /// Helper method to check if a step type requires loosen threshold
    /// </summary>
    bool IsLoosenStep(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.LoosenValve ||
               stepType == InteractionStep.StepType.RemoveValve;
    }
}
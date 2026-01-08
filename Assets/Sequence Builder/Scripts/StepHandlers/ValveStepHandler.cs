// ScrewStepHandler.cs (formerly ValveStepHandler.cs)
// Handles screw interaction steps with state-aware parameter application
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for screw-based interaction steps using XRI framework with proper state machine integration
/// Waits for screw stabilization before applying sequence parameters and subscribing to events
/// </summary>
public class ScrewStepHandler : BaseXRIStepHandler
{
    // Component cache for screw controllers
    private Dictionary<GameObject, ScrewController> screwControllers = new Dictionary<GameObject, ScrewController>();

    // Active step tracking with event delegates for proper cleanup
    private Dictionary<ScrewController, System.Action> screwTightenedEventDelegates = new Dictionary<ScrewController, System.Action>();
    private Dictionary<ScrewController, System.Action> screwLoosenedEventDelegates = new Dictionary<ScrewController, System.Action>();
    private Dictionary<InteractionStep, ScrewController> activeStepScrews = new Dictionary<InteractionStep, ScrewController>();

    // Step monitoring coroutines
    private Dictionary<InteractionStep, Coroutine> stepMonitoringCoroutines = new Dictionary<InteractionStep, Coroutine>();

    void Awake()
    {
        CacheScrewControllers();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.TightenScrew ||
               stepType == InteractionStep.StepType.LoosenScrew ||
               stepType == InteractionStep.StepType.InstallScrew ||
               stepType == InteractionStep.StepType.RemoveScrew;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🔧 ScrewStepHandler initialized");

        // Refresh cache in case scene changed
        CacheScrewControllers();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔧 Starting screw step: {step.stepName} [{step.type}]");

        // Use controller's helper method to get object from registry (reliable!)
        var targetObject = controller.GetTargetObjectForStep(step);
        if (targetObject == null)
        {
            LogError($"Target object is null for screw step: {step.stepName}");
            return;
        }

        if (!screwControllers.ContainsKey(targetObject))
        {
            LogError($"No screw controller found for object: {targetObject.name} in step: {step.stepName}");
            return;
        }

        var screwController = screwControllers[targetObject];

        // Track this active step
        activeStepScrews[step] = screwController;

        // Start state-aware monitoring for this step
        var monitoringCoroutine = StartCoroutine(MonitorScrewStepProgression(step, screwController));
        stepMonitoringCoroutines[step] = monitoringCoroutine;

        LogDebug($"🔧 Started screw step monitoring for: {targetObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔧 Stopping screw step: {step.stepName}");

        // Stop monitoring coroutine
        if (stepMonitoringCoroutines.ContainsKey(step))
        {
            if (stepMonitoringCoroutines[step] != null)
            {
                StopCoroutine(stepMonitoringCoroutines[step]);
            }
            stepMonitoringCoroutines.Remove(step);
        }

        // Clean up screw event subscriptions
        if (activeStepScrews.ContainsKey(step))
        {
            var screwController = activeStepScrews[step];
            UnsubscribeFromScrewEvents(screwController);
            activeStepScrews.Remove(step);
        }

        LogDebug($"🔧 Stopped screw step monitoring for: {step.stepName}");
    }

    public override void Cleanup()
    {
        LogDebug("🔧 Cleaning up screw step handler...");

        // Stop all monitoring coroutines
        foreach (var coroutine in stepMonitoringCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        stepMonitoringCoroutines.Clear();

        // Clean up all screw event subscriptions
        foreach (var screwController in screwTightenedEventDelegates.Keys)
        {
            UnsubscribeFromScrewEvents(screwController);
        }

        screwTightenedEventDelegates.Clear();
        screwLoosenedEventDelegates.Clear();
        activeStepScrews.Clear();
        screwControllers.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all screw controllers in the scene
    /// </summary>
    void CacheScrewControllers()
    {
        LogDebug("🔧 Caching screw controllers...");

        screwControllers.Clear();

        var screwControllerComponents = FindObjectsOfType<ScrewController>();
        foreach (var screwController in screwControllerComponents)
        {
            screwControllers[screwController.gameObject] = screwController;
            LogDebug($"🔧 Cached screw controller: {screwController.name}");
        }

        LogInfo($"🔧 Cached {screwControllers.Count} screw controllers");
    }

    /// <summary>
    /// Monitor screw step progression with state-aware parameter application
    /// </summary>
    IEnumerator MonitorScrewStepProgression(InteractionStep step, ScrewController screwController)
    {
        LogDebug($"🔧 MONITORING: Starting screw step monitoring for {step.stepName}");

        // PHASE 1: Apply sequence builder parameter overrides EARLY (before state transitions)
        // This prevents Configure() from resetting screw state
        LogDebug($"🔧 EARLY PARAMS: Applying parameters while screw is still unlocked");
        ApplyScrewStepParameters(step, screwController);

        // PHASE 2: Wait for screw to reach appropriate state for this step type
        yield return WaitForScrewReadyState(step, screwController);

        // PHASE 3: Subscribe to appropriate screw events for step completion
        SubscribeToScrewEvents(step, screwController);

        LogDebug($"🔧 MONITORING: Valve step setup complete for {step.stepName}");
    }

    /// <summary>
    /// Wait for valve to reach the appropriate state for the step type
    /// </summary>
    IEnumerator WaitForScrewReadyState(InteractionStep step, ScrewController screwController)
    {
        ScrewState requiredState;
        ScrewSubstate requiredSubstate;

        // Determine required state based on step type
        switch (step.type)
        {
            case InteractionStep.StepType.TightenScrew:
            case InteractionStep.StepType.InstallScrew:
                // Need valve to be in socket and loose (ready for tightening)
                requiredState = ScrewState.Locked;
                requiredSubstate = ScrewSubstate.Loose;
                break;

            case InteractionStep.StepType.LoosenScrew:
                // Need valve to be tight (ready for loosening)
                requiredState = ScrewState.Locked;
                requiredSubstate = ScrewSubstate.Tight;
                break;

            case InteractionStep.StepType.RemoveScrew:
                // Need valve to be loose (ready for removal)
                requiredState = ScrewState.Locked;
                requiredSubstate = ScrewSubstate.Loose;
                break;

            default:
                LogWarning($"🔧 Unknown screw step type: {step.type}");
                yield break;
        }

        LogDebug($"🔧 WAITING: Step {step.stepName} waiting for screw state: {requiredState}-{requiredSubstate}");

        float startTime = Time.time;
        float lastLogTime = Time.time;
        float timeout = 30f; // Generous timeout for user interaction
        float logInterval = 3f; // Log every 3 seconds instead of every frame

        // Wait for required state
        while (screwController.CurrentState != requiredState || screwController.CurrentSubstate != requiredSubstate)
        {
            float currentTime = Time.time;

            // Check for timeout (but don't fail - user might be slow)
            if (currentTime - startTime > timeout)
            {
                LogWarning($"🔧 WAITING: Valve {screwController.gameObject.name} taking longer than {timeout}s to reach required state for step {step.stepName}");
                LogWarning($"🔧 Current: {screwController.CurrentState}-{screwController.CurrentSubstate}, Required: {requiredState}-{requiredSubstate}");

                // Reset timeout for next check
                startTime = currentTime;
                lastLogTime = currentTime; // Also reset log timer
            }

            // Only log every few seconds to reduce spam
            if (currentTime - lastLogTime > logInterval)
            {
                LogDebug($"🔧 WAITING: Current: {screwController.CurrentState}-{screwController.CurrentSubstate}, Required: {requiredState}-{requiredSubstate}");
                lastLogTime = currentTime;
            }

            yield return new WaitForSeconds(0.5f); // Check every half second
        }

        LogDebug($"🔧 READY: Valve {screwController.gameObject.name} reached required state for step {step.stepName}");
    }

    /// <summary>
    /// Apply sequence builder parameter overrides to valve controller
    /// </summary>
    void ApplyScrewStepParameters(InteractionStep step, ScrewController screwController)
    {
        LogDebug($"🔧 PARAMS: Applying parameter overrides for step {step.stepName}");

        // Get current screw profile
        var currentProfile = GetScrewProfile(screwController);
        if (currentProfile == null)
        {
            LogWarning($"🔧 PARAMS: No screw profile found for {screwController.gameObject.name}");
            return;
        }

        // Check if we need to apply any overrides - check ALL possible overrides regardless of step type
        bool needsOverride = false;

        if (step.rotationAxis != currentProfile.rotationAxis) needsOverride = true;
        if (step.tightenThreshold != currentProfile.tightenThreshold) needsOverride = true; // Always check tighten threshold
        if (step.loosenThreshold != currentProfile.loosenThreshold) needsOverride = true;   // Always check loosen threshold
        if (step.screwAngleTolerance != currentProfile.angleTolerance) needsOverride = true;
        if (step.rotationDampening != currentProfile.rotationDampening) needsOverride = true;

        LogDebug($"🔧 PARAMS: Override check - Step tighten: {step.tightenThreshold}, Profile tighten: {currentProfile.tightenThreshold}");

        if (!needsOverride)
        {
            LogDebug($"🔧 PARAMS: No parameter overrides needed for {step.stepName}");
            return;
        }

        // Create runtime profile with overrides
        var runtimeProfile = ScriptableObject.CreateInstance<ScrewProfile>();

        // Copy base settings from original profile
        runtimeProfile.profileName = $"{currentProfile.profileName}_Runtime_{step.type}";
        runtimeProfile.rotationAxis = step.rotationAxis != currentProfile.rotationAxis ? step.rotationAxis : currentProfile.rotationAxis;

        // Apply threshold values selectively based on step type to avoid cross-contamination
        // Tighten steps control tighten threshold, Loosen steps control loosen threshold

        // Debug the assignment logic
        LogDebug($"🔧 PARAMS: Step type: {step.type}");
        LogDebug($"🔧 PARAMS: Before assignment - Step tighten: {step.tightenThreshold}, Profile tighten: {currentProfile.tightenThreshold}");
        LogDebug($"🔧 PARAMS: Before assignment - Step loosen: {step.loosenThreshold}, Profile loosen: {currentProfile.loosenThreshold}");

        // Start with current profile values
        runtimeProfile.tightenThreshold = currentProfile.tightenThreshold;
        runtimeProfile.loosenThreshold = currentProfile.loosenThreshold;

        // Apply overrides based on step type to prevent cross-contamination
        if (IsTightenStep(step.type))
        {
            // Only override tighten threshold for tighten steps
            if (step.tightenThreshold != currentProfile.tightenThreshold)
            {
                runtimeProfile.tightenThreshold = step.tightenThreshold;
                LogDebug($"🔧 PARAMS: Applied TIGHTEN override: {step.tightenThreshold}°");
            }
        }

        if (IsLoosenStep(step.type))
        {
            // Only override loosen threshold for loosen steps
            if (step.loosenThreshold != currentProfile.loosenThreshold)
            {
                runtimeProfile.loosenThreshold = step.loosenThreshold;
                LogDebug($"🔧 PARAMS: Applied LOOSEN override: {step.loosenThreshold}°");
            }
        }

        LogDebug($"🔧 PARAMS: Final assignment - Runtime tighten: {runtimeProfile.tightenThreshold}, Runtime loosen: {runtimeProfile.loosenThreshold}");

        runtimeProfile.angleTolerance = step.screwAngleTolerance != currentProfile.angleTolerance ? step.screwAngleTolerance : currentProfile.angleTolerance;
        runtimeProfile.rotationDampening = step.rotationDampening != currentProfile.rotationDampening ? step.rotationDampening : currentProfile.rotationDampening;

        // Copy other essential settings
        runtimeProfile.compatibleSocketTags = currentProfile.compatibleSocketTags;
        runtimeProfile.requireSpecificSockets = currentProfile.requireSpecificSockets;
        runtimeProfile.specificCompatibleSockets = currentProfile.specificCompatibleSockets;

        // Apply the modified profile
        screwController.Configure(runtimeProfile);

        LogInfo($"🔧 PARAMS: Applied parameter overrides to {screwController.gameObject.name}:");
        LogInfo($"🔧   - Rotation Axis: {runtimeProfile.rotationAxis}");
        LogInfo($"🔧   - Tighten Threshold: {runtimeProfile.tightenThreshold}°");
        LogInfo($"🔧   - Loosen Threshold: {runtimeProfile.loosenThreshold}°");
        LogInfo($"🔧   - Angle Tolerance: {runtimeProfile.angleTolerance}°");
    }

    /// <summary>
    /// Subscribe to appropriate screw events for step completion
    /// </summary>
    void SubscribeToScrewEvents(InteractionStep step, ScrewController screwController)
    {
        LogDebug($"🔧 EVENTS: Subscribing to screw events for step {step.stepName}");

        // Unsubscribe first to prevent duplicates
        UnsubscribeFromScrewEvents(screwController);

        switch (step.type)
        {
            case InteractionStep.StepType.TightenScrew:
            case InteractionStep.StepType.InstallScrew:
                SubscribeToScrewTightenEvents(step, screwController);
                break;

            case InteractionStep.StepType.LoosenScrew:
            case InteractionStep.StepType.RemoveScrew:
                SubscribeToScrewLoosenEvents(step, screwController);
                break;
        }
    }

    /// <summary>
    /// Subscribe to screw tighten events
    /// </summary>
    void SubscribeToScrewTightenEvents(InteractionStep step, ScrewController screwController)
    {
        System.Action tightenDelegate = () => OnScrewTightened(step);
        screwTightenedEventDelegates[screwController] = tightenDelegate;
        screwController.OnScrewTightened += tightenDelegate;

        LogDebug($"🔧 EVENTS: Subscribed to screw tighten events for {step.stepName}");
    }

    /// <summary>
    /// Subscribe to screw loosen events
    /// </summary>
    void SubscribeToScrewLoosenEvents(InteractionStep step, ScrewController screwController)
    {
        System.Action loosenDelegate = () => OnScrewLoosened(step);
        screwLoosenedEventDelegates[screwController] = loosenDelegate;
        screwController.OnScrewLoosened += loosenDelegate;

        LogDebug($"🔧 EVENTS: Subscribed to screw loosen events for {step.stepName}");
    }

    /// <summary>
    /// Unsubscribe from all screw events for a controller
    /// </summary>
    void UnsubscribeFromScrewEvents(ScrewController screwController)
    {
        if (screwTightenedEventDelegates.ContainsKey(screwController))
        {
            screwController.OnScrewTightened -= screwTightenedEventDelegates[screwController];
            screwTightenedEventDelegates.Remove(screwController);
        }

        if (screwLoosenedEventDelegates.ContainsKey(screwController))
        {
            screwController.OnScrewLoosened -= screwLoosenedEventDelegates[screwController];
            screwLoosenedEventDelegates.Remove(screwController);
        }
    }

    /// <summary>
    /// Handle valve tightened event
    /// </summary>
    void OnScrewTightened(InteractionStep step)
    {
        LogDebug($"🔧 EVENT: Valve tightened for step {step.stepName}");
        CompleteStep(step, "Screw tightened successfully");
    }

    /// <summary>
    /// Handle valve loosened event
    /// </summary>
    void OnScrewLoosened(InteractionStep step)
    {
        LogDebug($"🔧 EVENT: Valve loosened for step {step.stepName}");
        CompleteStep(step, "Screw loosened successfully");
    }

    /// <summary>
    /// Get screw profile using reflection (helper method)
    /// </summary>
    ScrewProfile GetScrewProfile(ScrewController screwController)
    {
        var profileField = typeof(ScrewController).GetField("profile",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (profileField != null)
        {
            return (ScrewProfile)profileField.GetValue(screwController);
        }

        return null;
    }

    /// <summary>
    /// Helper method to check if a step type requires tighten threshold
    /// </summary>
    bool IsTightenStep(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.TightenScrew ||
               stepType == InteractionStep.StepType.InstallScrew;
    }

    /// <summary>
    /// Helper method to check if a step type requires loosen threshold
    /// </summary>
    bool IsLoosenStep(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.LoosenScrew ||
               stepType == InteractionStep.StepType.RemoveScrew;
    }
}
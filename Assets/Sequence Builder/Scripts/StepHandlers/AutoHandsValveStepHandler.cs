// AutoHandsScrewStepHandler.cs (formerly AutoHandsScrewStepHandler.cs)
// Handles screw interaction steps in training sequences using AutoHands framework
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for screw interaction steps (TightenScrew, LoosenScrew) using AutoHands framework
/// Manages AutoHandsScrewControllerV2 event subscriptions and state-based completion detection
/// </summary>
public class AutoHandsScrewStepHandler : BaseAutoHandsStepHandler
{
    // Component cache for AutoHands screw controllers
    private Dictionary<GameObject, AutoHandsScrewControllerV2> screwControllers = new Dictionary<GameObject, AutoHandsScrewControllerV2>();

    // Component cache for Grabbable components (for tool grab detection)
    private Dictionary<GameObject, Autohand.Grabbable> grabbableComponents = new Dictionary<GameObject, Autohand.Grabbable>();

    // Active step tracking with event delegates for proper cleanup
    private Dictionary<AutoHandsScrewControllerV2, System.Action> tightenEventDelegates = new Dictionary<AutoHandsScrewControllerV2, System.Action>();
    private Dictionary<AutoHandsScrewControllerV2, System.Action> loosenEventDelegates = new Dictionary<AutoHandsScrewControllerV2, System.Action>();
    private Dictionary<InteractionStep, AutoHandsScrewControllerV2> activeStepScrews = new Dictionary<InteractionStep, AutoHandsScrewControllerV2>();
    private Dictionary<InteractionStep, Autohand.Grabbable> activeStepGrabbables = new Dictionary<InteractionStep, Autohand.Grabbable>();

    void Awake()
    {
        CacheScrewControllers();
        CacheGrabbableComponents();
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
        LogInfo("🔧 AutoHandsScrewStepHandler initialized");

        // Refresh cache in case scene changed
        CacheScrewControllers();
        CacheGrabbableComponents();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔧 Starting AutoHands screw step: {step.stepName} (Type: {step.type})");

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

        // Apply parameter overrides from InteractionStep to screw controller
        ApplyStepParameterOverrides(step, screwController);

        // Subscribe to appropriate events based on step type
        switch (step.type)
        {
            case InteractionStep.StepType.TightenScrew:
                SubscribeToTightenEvent(step, screwController);
                break;

            case InteractionStep.StepType.LoosenScrew:
                SubscribeToLoosenEvent(step, screwController);
                break;

            case InteractionStep.StepType.InstallScrew:
                SubscribeToSnapEvent(step, screwController);
                break;

            case InteractionStep.StepType.RemoveScrew:
                SubscribeToRemoveEvent(step, screwController);
                break;
        }

        // Track this active step
        activeStepScrews[step] = screwController;

        // ALSO subscribe to grab events on the target object (tool) for arrow transitions
        if (targetObject != null && grabbableComponents.ContainsKey(targetObject))
        {
            var grabbableComponent = grabbableComponents[targetObject];
            grabbableComponent.OnGrabEvent += (hand, grabbable) => OnToolGrabbed(step, hand, grabbable);
            activeStepGrabbables[step] = grabbableComponent;
            LogDebug($"🔧 Subscribed to grab events on tool: {targetObject.name}");
        }

        LogDebug($"🔧 Subscribed to AutoHands valve events for: {targetObject.name} (State: {screwController.CurrentState}-{screwController.CurrentSubstate})");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔧 Stopping AutoHands screw step: {step.stepName}");

        if (activeStepScrews.ContainsKey(step))
        {
            var screwController = activeStepScrews[step];

            // Unsubscribe from all events
            UnsubscribeFromAllEvents(screwController);

            // Remove from tracking
            activeStepScrews.Remove(step);

            LogDebug($"🔧 Unsubscribed from AutoHands valve events for step: {step.stepName}");
        }

        // Unsubscribe from grab events
        if (activeStepGrabbables.ContainsKey(step))
        {
            var grabbableComponent = activeStepGrabbables[step];
            grabbableComponent.OnGrabEvent -= (hand, grabbable) => OnToolGrabbed(step, hand, grabbable);
            activeStepGrabbables.Remove(step);
            LogDebug($"🔧 Unsubscribed from grab events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🔧 Cleaning up AutoHands screw step handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepScrews.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        // Clear cache
        screwControllers.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all AutoHands screw controllers in the scene
    /// </summary>
    void CacheScrewControllers()
    {
        LogDebug("🔧 Caching AutoHands screw controllers...");

        screwControllers.Clear();

        var screwControllerComponents = FindObjectsOfType<AutoHandsScrewControllerV2>();
        foreach (var screwController in screwControllerComponents)
        {
            screwControllers[screwController.gameObject] = screwController;
            LogDebug($"🔧 Cached AutoHands screw controller: {screwController.name}");
        }

        LogInfo($"🔧 Cached {screwControllers.Count} AutoHands screw controllers");
    }

    /// <summary>
    /// Cache all AutoHands Grabbable components in the scene
    /// </summary>
    void CacheGrabbableComponents()
    {
        LogDebug("🔧 Caching AutoHands Grabbable components...");

        grabbableComponents.Clear();

        var grabbableObjects = FindObjectsOfType<Autohand.Grabbable>();
        foreach (var grabbable in grabbableObjects)
        {
            grabbableComponents[grabbable.gameObject] = grabbable;
            LogDebug($"🔧 Cached AutoHands Grabbable: {grabbable.name}");
        }

        LogInfo($"🔧 Cached {grabbableComponents.Count} AutoHands Grabbable components");
    }

    /// <summary>
    /// Handle grab event from AutoHands Grabbable component - for arrow transitions
    /// Event signature: OnGrabbed(Hand hand, Grabbable grab)
    /// </summary>
    void OnToolGrabbed(InteractionStep step, Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        var grabbedObject = grabbable.gameObject;
        // Use controller's helper method to get object from registry (reliable!)
        var expectedObject = controller.GetTargetObjectForStep(step);

        LogDebug($"🔧 Tool grabbed: {grabbedObject.name}, expected: {expectedObject?.name}");

        if (grabbedObject == expectedObject)
        {
            LogDebug($"🔧 Correct tool grabbed! Notifying controller for arrow transition");

            // Notify controller to trigger arrow transition (hide target, show destination)
            if (controller != null)
            {
                controller.NotifyObjectGrabbed(step);
            }
        }
    }

    /// <summary>
    /// Apply parameter overrides from InteractionStep to screw controller
    /// Creates runtime profile with step-specific parameters (profile values are fallback)
    /// </summary>
    void ApplyStepParameterOverrides(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        LogDebug($"🔧 PARAMS: Applying parameter overrides for step {step.stepName}");

        // Get current screw profile
        var currentProfile = GetScrewProfile(screwController);
        if (currentProfile == null)
        {
            LogWarning($"🔧 PARAMS: No screw profile found for {screwController.gameObject.name}");
            return;
        }

        // Check if we need to apply any overrides
        bool needsOverride = false;

        if (step.rotationAxis != currentProfile.rotationAxis) needsOverride = true;
        if (step.tightenThreshold != currentProfile.tightenThreshold) needsOverride = true;
        if (step.loosenThreshold != currentProfile.loosenThreshold) needsOverride = true;
        if (step.screwAngleTolerance != currentProfile.angleTolerance) needsOverride = true;
        if (step.rotationDampening != currentProfile.rotationDampening) needsOverride = true;

        LogDebug($"🔧 PARAMS: Override check - Step tighten: {step.tightenThreshold}, Profile tighten: {currentProfile.tightenThreshold}");
        LogDebug($"🔧 PARAMS: Override check - Step loosen: {step.loosenThreshold}, Profile loosen: {currentProfile.loosenThreshold}");

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

        runtimeProfile.angleTolerance = step.screwAngleTolerance != currentProfile.angleTolerance ? step.screwAngleTolerance : currentProfile.angleTolerance;
        runtimeProfile.rotationDampening = step.rotationDampening != currentProfile.rotationDampening ? step.rotationDampening : currentProfile.rotationDampening;

        // Copy other essential settings
        runtimeProfile.compatibleSocketTags = currentProfile.compatibleSocketTags;
        runtimeProfile.requireSpecificSockets = currentProfile.requireSpecificSockets;
        runtimeProfile.specificCompatibleSockets = currentProfile.specificCompatibleSockets;

        // Apply the modified profile to the controller
        screwController.Configure(runtimeProfile);

        // Update HingeJoint limits if valve is already snapped (for mid-sequence parameter changes)
        UpdateHingeJointLimits(screwController, runtimeProfile);

        LogInfo($"🔧 PARAMS: Applied parameter overrides to {screwController.gameObject.name}:");
        LogInfo($"🔧   - Rotation Axis: {runtimeProfile.rotationAxis}");
        LogInfo($"🔧   - Tighten Threshold: {runtimeProfile.tightenThreshold}°");
        LogInfo($"🔧   - Loosen Threshold: {runtimeProfile.loosenThreshold}°");
        LogInfo($"🔧   - Angle Tolerance: {runtimeProfile.angleTolerance}°");
    }

    /// <summary>
    /// Subscribe to valve tighten event
    /// </summary>
    void SubscribeToTightenEvent(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        // Check if valve is in correct state for tightening
        if (screwController.CurrentState != ScrewState.Locked)
        {
            LogWarning($"Screw {screwController.name} is not in Locked state (current: {screwController.CurrentState}). Waiting for snap...");
        }

        // Create delegate for this specific step
        System.Action tightenDelegate = () => OnScrewTightened(step, screwController);
        tightenEventDelegates[screwController] = tightenDelegate;
        screwController.OnScrewTightened += tightenDelegate;

        LogDebug($"🔧 Subscribed to OnScrewTightened for {screwController.name}");
    }

    /// <summary>
    /// Subscribe to valve loosen event
    /// </summary>
    void SubscribeToLoosenEvent(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        // Check if valve is in correct state for loosening (should be Tight)
        if (screwController.CurrentSubstate != ScrewSubstate.Tight)
        {
            LogWarning($"Screw {screwController.name} is not in Tight state (current: {screwController.CurrentState}-{screwController.CurrentSubstate}). Waiting for tightening...");
        }

        // Create delegate for this specific step
        System.Action loosenDelegate = () => OnScrewLoosened(step, screwController);
        loosenEventDelegates[screwController] = loosenDelegate;
        screwController.OnScrewLoosened += loosenDelegate;

        LogDebug($"🔧 Subscribed to OnScrewLoosened for {screwController.name}");
    }

    /// <summary>
    /// Subscribe to valve snap event (InstallScrew step)
    /// </summary>
    void SubscribeToSnapEvent(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        // Create delegate for snap event
        System.Action snapDelegate = () => OnScrewSnapped(step, screwController);

        // Store as tighten delegate (reusing dictionary for snap events)
        tightenEventDelegates[screwController] = snapDelegate;
        screwController.OnScrewSnapped += snapDelegate;

        LogDebug($"🔧 Subscribed to OnScrewSnapped for {screwController.name}");
    }

    /// <summary>
    /// Subscribe to valve remove event (RemoveScrew step)
    /// </summary>
    void SubscribeToRemoveEvent(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        // Create delegate for remove event
        System.Action removeDelegate = () => OnScrewRemoved(step, screwController);

        // Store as loosen delegate (reusing dictionary for remove events)
        loosenEventDelegates[screwController] = removeDelegate;
        screwController.OnScrewRemoved += removeDelegate;

        LogDebug($"🔧 Subscribed to OnScrewRemoved for {screwController.name}");
    }

    /// <summary>
    /// Unsubscribe from all valve events for a controller
    /// </summary>
    void UnsubscribeFromAllEvents(AutoHandsScrewControllerV2 screwController)
    {
        // Unsubscribe from tighten event
        if (tightenEventDelegates.ContainsKey(screwController))
        {
            screwController.OnScrewTightened -= tightenEventDelegates[screwController];
            screwController.OnScrewSnapped -= tightenEventDelegates[screwController];
            tightenEventDelegates.Remove(screwController);
        }

        // Unsubscribe from loosen event
        if (loosenEventDelegates.ContainsKey(screwController))
        {
            screwController.OnScrewLoosened -= loosenEventDelegates[screwController];
            screwController.OnScrewRemoved -= loosenEventDelegates[screwController];
            loosenEventDelegates.Remove(screwController);
        }
    }

    /// <summary>
    /// Handle valve tightened event
    /// </summary>
    void OnScrewTightened(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve tightened! Completing step: {step.stepName}");
        CompleteStep(step, $"Screw {screwController.name} tightened to {screwController.CurrentRotation:F1}° with AutoHands");
    }

    /// <summary>
    /// Handle valve loosened event
    /// </summary>
    void OnScrewLoosened(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve loosened! Completing step: {step.stepName}");
        CompleteStep(step, $"Screw {screwController.name} loosened (unlocked) with AutoHands");
    }

    /// <summary>
    /// Handle valve snapped event (InstallScrew)
    /// </summary>
    void OnScrewSnapped(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve snapped to socket! Completing step: {step.stepName}");
        CompleteStep(step, $"Screw {screwController.name} installed in socket with AutoHands");
    }

    /// <summary>
    /// Handle valve removed event (RemoveScrew)
    /// </summary>
    void OnScrewRemoved(InteractionStep step, AutoHandsScrewControllerV2 screwController)
    {
        if (step.isCompleted) return;

        LogDebug($"🔧 Valve removed from socket! Completing step: {step.stepName}");
        CompleteStep(step, $"Screw {screwController.name} removed from socket with AutoHands");
    }

    /// <summary>
    /// Get screw profile using reflection (helper method)
    /// </summary>
    ScrewProfile GetScrewProfile(AutoHandsScrewControllerV2 screwController)
    {
        var profileField = typeof(AutoHandsScrewControllerV2).GetField("profile",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (profileField != null)
        {
            return (ScrewProfile)profileField.GetValue(screwController);
        }

        return null;
    }

    /// <summary>
    /// Update HingeJoint limits if valve is already snapped (for mid-sequence parameter changes)
    /// Uses reflection to access private hingeJoint field
    /// </summary>
    void UpdateHingeJointLimits(AutoHandsScrewControllerV2 screwController, ScrewProfile profile)
    {
        // Use reflection to get private hingeJoint field
        var hingeJointField = typeof(AutoHandsScrewControllerV2).GetField("hingeJoint",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (hingeJointField != null)
        {
            HingeJoint hingeJoint = (HingeJoint)hingeJointField.GetValue(screwController);

            if (hingeJoint != null)
            {
                // Update limits based on new profile parameters
                JointLimits limits = hingeJoint.limits;
                limits.min = -profile.loosenThreshold;
                limits.max = profile.tightenThreshold;
                hingeJoint.limits = limits;

                LogDebug($"🔧 PARAMS: Updated HingeJoint limits - Min: {limits.min}°, Max: {limits.max}°");
            }
        }
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

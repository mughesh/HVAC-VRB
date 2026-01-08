// AutoHandsGrabStepHandler.cs
// Handles grab interaction steps in training sequences using AutoHands framework
using UnityEngine;
using Autohand;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for grab-based interaction steps using AutoHands framework
/// Manages Grabbable event subscriptions and step completion
/// Mirrors the structure of XRI GrabStepHandler but uses AutoHands components
/// </summary>
public class AutoHandsGrabStepHandler : BaseAutoHandsStepHandler
{
    // Component cache for grabbable components
    private Dictionary<GameObject, Grabbable> grabbableComponents = new Dictionary<GameObject, Grabbable>();

    // Active step tracking
    private Dictionary<InteractionStep, Grabbable> activeStepGrabs = new Dictionary<InteractionStep, Grabbable>();

    void Awake()
    {
        CacheGrabbableComponents();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.Grab;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🤏 AutoHandsGrabStepHandler initialized");

        // Refresh cache in case scene changed
        CacheGrabbableComponents();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🤏 Starting AutoHands grab step: {step.stepName}");

        // Use controller's helper method to get object from registry (reliable!)
        var targetObject = controller.GetTargetObjectForStep(step);
        if (targetObject == null)
        {
            LogError($"Target object is null for step: {step.stepName}");
            return;
        }

        if (!grabbableComponents.ContainsKey(targetObject))
        {
            LogError($"No AutoHands Grabbable found for object: {targetObject.name} in step: {step.stepName}");
            return;
        }

        var grabbableComponent = grabbableComponents[targetObject];

        // Subscribe to AutoHands grab events
        grabbableComponent.OnGrabEvent += (hand, grabbable) => OnObjectGrabbed(step, hand, grabbable);

        // Track this active step
        activeStepGrabs[step] = grabbableComponent;

        LogDebug($"🤏 Subscribed to AutoHands grab events for: {targetObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🤏 Stopping AutoHands grab step: {step.stepName}");

        if (activeStepGrabs.ContainsKey(step))
        {
            var grabbableComponent = activeStepGrabs[step];

            // Unsubscribe from events
            grabbableComponent.OnGrabEvent -= (hand, grabbable) => OnObjectGrabbed(step, hand, grabbable);

            // Remove from tracking
            activeStepGrabs.Remove(step);

            LogDebug($"🤏 Unsubscribed from AutoHands grab events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🤏 Cleaning up AutoHands grab step handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepGrabs.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        // Clear cache
        grabbableComponents.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all AutoHands Grabbable components in the scene
    /// </summary>
    void CacheGrabbableComponents()
    {
        LogDebug("🤏 Caching AutoHands Grabbable components...");

        grabbableComponents.Clear();

        var grabbableObjects = FindObjectsOfType<Grabbable>();
        foreach (var grabbable in grabbableObjects)
        {
            grabbableComponents[grabbable.gameObject] = grabbable;
            LogDebug($"🤏 Cached AutoHands grabbable: {grabbable.name}");
        }

        LogInfo($"🤏 Cached {grabbableComponents.Count} AutoHands Grabbable components");
    }

    /// <summary>
    /// Handle grab event from AutoHands Grabbable component
    /// Event signature: OnGrabbed(Hand hand, Grabbable grab)
    /// </summary>
    void OnObjectGrabbed(InteractionStep step, Hand hand, Grabbable grabbable)
    {
        if (step.isCompleted) return;

        var grabbedObject = grabbable.gameObject;
        // Use controller's helper method to get object from registry (reliable!)
        var expectedObject = controller.GetTargetObjectForStep(step);

        LogDebug($"🤏 AutoHands object grabbed: {grabbedObject.name}, expected: {expectedObject?.name}");
        LogDebug($"🤏 Grabbed by hand: {hand.name}");

        if (grabbedObject == expectedObject)
        {
            LogDebug($"🤏 AutoHands grab match! Completing step: {step.stepName}");
            CompleteStep(step, $"Grabbed {grabbedObject.name} with AutoHands");
        }
        else
        {
            LogDebug($"🤏 AutoHands grab mismatch - grabbed {grabbedObject.name} but expected {expectedObject?.name}");
        }
    }
}
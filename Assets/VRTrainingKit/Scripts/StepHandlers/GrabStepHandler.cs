// GrabStepHandler.cs
// Handles grab interaction steps in training sequences
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for grab-based interaction steps using XRI framework
/// Manages XRGrabInteractable event subscriptions and step completion
/// </summary>
public class GrabStepHandler : BaseXRIStepHandler
{
    // Component cache for grab interactables
    private Dictionary<GameObject, XRGrabInteractable> grabInteractables = new Dictionary<GameObject, XRGrabInteractable>();

    // Active step tracking
    private Dictionary<InteractionStep, XRGrabInteractable> activeStepGrabs = new Dictionary<InteractionStep, XRGrabInteractable>();

    void Awake()
    {
        CacheGrabInteractables();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.Grab;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🤏 GrabStepHandler initialized");

        // Refresh cache in case scene changed
        CacheGrabInteractables();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🤏 Starting grab step: {step.stepName}");

        var targetObject = step.targetObject.GameObject;
        if (targetObject == null)
        {
            LogError($"Target object is null for step: {step.stepName}");
            return;
        }

        if (!grabInteractables.ContainsKey(targetObject))
        {
            LogError($"No grab interactable found for object: {targetObject.name} in step: {step.stepName}");
            return;
        }

        var grabInteractable = grabInteractables[targetObject];

        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener((args) => OnObjectGrabbed(step, args));

        // Track this active step
        activeStepGrabs[step] = grabInteractable;

        LogDebug($"🤏 Subscribed to grab events for: {targetObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🤏 Stopping grab step: {step.stepName}");

        if (activeStepGrabs.ContainsKey(step))
        {
            var grabInteractable = activeStepGrabs[step];

            // Unsubscribe from events
            grabInteractable.selectEntered.RemoveAllListeners();

            // Remove from tracking
            activeStepGrabs.Remove(step);

            LogDebug($"🤏 Unsubscribed from grab events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🤏 Cleaning up grab step handler...");

        // Stop all active steps
        foreach (var step in activeStepGrabs.Keys)
        {
            StopStep(step);
        }

        // Clear cache
        grabInteractables.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all grab interactables in the scene
    /// </summary>
    void CacheGrabInteractables()
    {
        LogDebug("🤏 Caching grab interactables...");

        grabInteractables.Clear();

        var grabInteractableComponents = FindObjectsOfType<XRGrabInteractable>();
        foreach (var grabInteractable in grabInteractableComponents)
        {
            grabInteractables[grabInteractable.gameObject] = grabInteractable;
            LogDebug($"🤏 Cached grab interactable: {grabInteractable.name}");
        }

        LogInfo($"🤏 Cached {grabInteractables.Count} grab interactables");
    }

    /// <summary>
    /// Handle grab event from XRGrabInteractable
    /// </summary>
    void OnObjectGrabbed(InteractionStep step, SelectEnterEventArgs args)
    {
        if (step.isCompleted) return;

        var grabbedObject = args.interactableObject.transform.gameObject;
        var expectedObject = step.targetObject.GameObject;

        LogDebug($"🤏 Object grabbed: {grabbedObject.name}, expected: {expectedObject?.name}");

        if (grabbedObject == expectedObject)
        {
            LogDebug($"🤏 Grab match! Completing step: {step.stepName}");
            CompleteStep(step, $"Grabbed {grabbedObject.name}");
        }
        else
        {
            LogDebug($"🤏 Grab mismatch - grabbed {grabbedObject.name} but expected {expectedObject?.name}");
        }
    }
}
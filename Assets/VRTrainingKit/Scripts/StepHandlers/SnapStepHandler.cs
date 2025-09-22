// SnapStepHandler.cs
// Handles snap interaction steps in training sequences
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for snap-based interaction steps (GrabAndSnap)
/// Manages XRSocketInteractor event subscriptions and step completion
/// </summary>
public class SnapStepHandler : BaseStepHandler
{
    // Component cache for socket interactors
    private Dictionary<GameObject, XRSocketInteractor> socketInteractors = new Dictionary<GameObject, XRSocketInteractor>();

    // Active step tracking
    private Dictionary<InteractionStep, XRSocketInteractor> activeStepSockets = new Dictionary<InteractionStep, XRSocketInteractor>();

    void Awake()
    {
        CacheSocketInteractors();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.GrabAndSnap;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🔗 SnapStepHandler initialized");

        // Refresh cache in case scene changed
        CacheSocketInteractors();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔗 Starting snap step: {step.stepName}");

        var destinationObject = step.destination.GameObject;
        if (destinationObject == null)
        {
            LogError($"Destination object is null for step: {step.stepName}");
            return;
        }

        if (!socketInteractors.ContainsKey(destinationObject))
        {
            LogError($"No socket interactor found for object: {destinationObject.name} in step: {step.stepName}");
            return;
        }

        var socketInteractor = socketInteractors[destinationObject];

        // Subscribe to snap events
        socketInteractor.selectEntered.AddListener((args) => OnObjectSnapped(step, args));

        // Track this active step
        activeStepSockets[step] = socketInteractor;

        LogDebug($"🔗 Subscribed to snap events for socket: {destinationObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔗 Stopping snap step: {step.stepName}");

        if (activeStepSockets.ContainsKey(step))
        {
            var socketInteractor = activeStepSockets[step];

            // Unsubscribe from events
            socketInteractor.selectEntered.RemoveAllListeners();

            // Remove from tracking
            activeStepSockets.Remove(step);

            LogDebug($"🔗 Unsubscribed from snap events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🔗 Cleaning up snap step handler...");

        // Stop all active steps
        foreach (var step in activeStepSockets.Keys)
        {
            StopStep(step);
        }

        // Clear cache
        socketInteractors.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all socket interactors in the scene
    /// </summary>
    void CacheSocketInteractors()
    {
        LogDebug("🔗 Caching socket interactors...");

        socketInteractors.Clear();

        var socketInteractorComponents = FindObjectsOfType<XRSocketInteractor>();
        foreach (var socketInteractor in socketInteractorComponents)
        {
            socketInteractors[socketInteractor.gameObject] = socketInteractor;
            LogDebug($"🔗 Cached socket interactor: {socketInteractor.name}");
        }

        LogInfo($"🔗 Cached {socketInteractors.Count} socket interactors");
    }

    /// <summary>
    /// Handle snap event from XRSocketInteractor
    /// </summary>
    void OnObjectSnapped(InteractionStep step, SelectEnterEventArgs args)
    {
        if (step.isCompleted) return;

        var snappedObject = args.interactableObject.transform.gameObject;
        var expectedObject = step.targetObject.GameObject;
        var destinationSocket = args.interactorObject.transform.gameObject;
        var expectedDestination = step.destination.GameObject;

        LogDebug($"🔗 Object snapped: {snappedObject.name} to socket: {destinationSocket.name}");
        LogDebug($"🔗 Expected: {expectedObject?.name} to socket: {expectedDestination?.name}");

        if (snappedObject == expectedObject && destinationSocket == expectedDestination)
        {
            LogDebug($"🔗 Snap match! Completing step: {step.stepName}");
            CompleteStep(step, $"Snapped {snappedObject.name} to {destinationSocket.name}");
        }
        else
        {
            LogDebug($"🔗 Snap mismatch - incorrect object or socket");
        }
    }
}
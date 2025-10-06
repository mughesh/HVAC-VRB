// AutoHandsSnapStepHandler.cs
// Handles snap interaction steps in training sequences using AutoHands framework
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for snap-based interaction steps (GrabAndSnap) using AutoHands framework
/// Manages PlacePoint event subscriptions and step completion
/// Mirrors the structure of XRI SnapStepHandler but uses AutoHands PlacePoint components
/// Uses reflection for event handling to avoid dynamic keyword dependency
/// </summary>
public class AutoHandsSnapStepHandler : BaseAutoHandsStepHandler
{
    // Component cache for PlacePoint components (stored as Component to use reflection)
    private Dictionary<GameObject, Component> placePointComponents = new Dictionary<GameObject, Component>();

    // Active step tracking
    private Dictionary<InteractionStep, Component> activeStepPlacePoints = new Dictionary<InteractionStep, Component>();

    // Delegate tracking for proper unsubscription (lambdas can't be unsubscribed without keeping reference)
    private Dictionary<InteractionStep, System.Delegate> eventDelegates = new Dictionary<InteractionStep, System.Delegate>();

    void Awake()
    {
        CachePlacePointComponents();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.GrabAndSnap;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🔗 AutoHandsSnapStepHandler initialized");

        // Refresh cache in case scene changed
        CachePlacePointComponents();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🔗 Starting AutoHands snap step: {step.stepName}");

        var destinationObject = step.destination.GameObject;
        if (destinationObject == null)
        {
            LogError($"Destination object is null for step: {step.stepName}");
            return;
        }

        if (!placePointComponents.ContainsKey(destinationObject))
        {
            LogError($"No AutoHands PlacePoint found for object: {destinationObject.name} in step: {step.stepName}");
            return;
        }

        Component placePoint = placePointComponents[destinationObject];

        // Subscribe to PlacePoint.OnPlaceEvent using reflection
        // Event signature: PlacePointEvent(PlacePoint point, Grabbable grabbable)
        SubscribeToPlaceEvent(placePoint, step);

        // Track this active step
        activeStepPlacePoints[step] = placePoint;

        LogDebug($"🔗 Subscribed to AutoHands PlacePoint events for: {destinationObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🔗 Stopping AutoHands snap step: {step.stepName}");

        if (activeStepPlacePoints.ContainsKey(step))
        {
            Component placePoint = activeStepPlacePoints[step];

            // Unsubscribe from events using reflection
            UnsubscribeFromPlaceEvent(placePoint, step);

            // Remove from tracking
            activeStepPlacePoints.Remove(step);

            LogDebug($"🔗 Unsubscribed from AutoHands PlacePoint events for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🔗 Cleaning up AutoHands snap step handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepPlacePoints.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        // Clear caches
        placePointComponents.Clear();
        eventDelegates.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all AutoHands PlacePoint components in the scene
    /// </summary>
    void CachePlacePointComponents()
    {
        LogDebug("🔗 Caching AutoHands PlacePoint components...");

        placePointComponents.Clear();

        // Find all MonoBehaviours and filter by type name
        var allComponents = FindObjectsOfType<MonoBehaviour>();
        foreach (var component in allComponents)
        {
            if (component != null && component.GetType().Name == "PlacePoint")
            {
                placePointComponents[component.gameObject] = component;
                LogDebug($"🔗 Cached AutoHands PlacePoint: {component.name}");
            }
        }

        LogInfo($"🔗 Cached {placePointComponents.Count} AutoHands PlacePoint components");
    }

    /// <summary>
    /// Subscribe to PlacePoint OnPlaceEvent using reflection
    /// </summary>
    private void SubscribeToPlaceEvent(Component placePoint, InteractionStep step)
    {
        try
        {
            // Get the OnPlaceEvent field/property
            FieldInfo eventField = placePoint.GetType().GetField("OnPlaceEvent");
            if (eventField == null)
            {
                LogError($"Could not find OnPlaceEvent field on PlacePoint component");
                return;
            }

            // Get the delegate type for PlacePointEvent
            // PlacePointEvent signature: void(PlacePoint point, Grabbable grabbable)
            System.Type delegateType = eventField.FieldType;

            // Create a method wrapper that captures the step context
            System.Action<object, object> wrapper = (point, grabbable) =>
            {
                OnObjectPlaced(step, point as Component, grabbable as Component);
            };

            // Create delegate using reflection
            System.Delegate eventDelegate = System.Delegate.CreateDelegate(
                delegateType,
                wrapper.Target,
                wrapper.Method
            );

            // Add event handler using Combine
            System.Delegate currentDelegate = eventField.GetValue(placePoint) as System.Delegate;
            System.Delegate newDelegate = System.Delegate.Combine(currentDelegate, eventDelegate);
            eventField.SetValue(placePoint, newDelegate);

            // Store delegate reference for unsubscription
            eventDelegates[step] = eventDelegate;

            LogDebug($"✅ Subscribed to OnPlaceEvent using reflection");
        }
        catch (System.Exception ex)
        {
            LogError($"Failed to subscribe to PlacePoint events: {ex.Message}");
        }
    }

    /// <summary>
    /// Unsubscribe from PlacePoint OnPlaceEvent using reflection
    /// </summary>
    private void UnsubscribeFromPlaceEvent(Component placePoint, InteractionStep step)
    {
        try
        {
            if (!eventDelegates.ContainsKey(step))
            {
                LogDebug($"No delegate found for step, skipping unsubscribe");
                return;
            }

            FieldInfo eventField = placePoint.GetType().GetField("OnPlaceEvent");
            if (eventField != null)
            {
                System.Delegate currentDelegate = eventField.GetValue(placePoint) as System.Delegate;
                System.Delegate delegateToRemove = eventDelegates[step];
                System.Delegate newDelegate = System.Delegate.Remove(currentDelegate, delegateToRemove);
                eventField.SetValue(placePoint, newDelegate);

                eventDelegates.Remove(step);
                LogDebug($"✅ Unsubscribed from OnPlaceEvent using reflection");
            }
        }
        catch (System.Exception ex)
        {
            LogDebug($"Could not unsubscribe from PlacePoint events: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle placement event from AutoHands PlacePoint component
    /// Event signature: PlacePointEvent(PlacePoint point, Grabbable grabbable)
    /// </summary>
    void OnObjectPlaced(InteractionStep step, Component placePoint, Component grabbable)
    {
        if (step.isCompleted) return;

        if (placePoint == null || grabbable == null)
        {
            LogDebug($"🔗 Received null component in OnObjectPlaced");
            return;
        }

        var placedObject = grabbable.gameObject;
        var expectedObject = step.targetObject.GameObject;
        var destinationPlacePoint = placePoint.gameObject;
        var expectedDestination = step.destination.GameObject;

        LogDebug($"🔗 Object placed: {placedObject.name} to PlacePoint: {destinationPlacePoint.name}");
        LogDebug($"🔗 Expected: {expectedObject?.name} to PlacePoint: {expectedDestination?.name}");

        if (placedObject == expectedObject && destinationPlacePoint == expectedDestination)
        {
            LogDebug($"🔗 AutoHands snap match! Completing step: {step.stepName}");
            CompleteStep(step, $"Snapped {placedObject.name} to {destinationPlacePoint.name} with AutoHands");
        }
        else
        {
            LogDebug($"🔗 AutoHands snap mismatch - incorrect object or PlacePoint");
        }
    }
}

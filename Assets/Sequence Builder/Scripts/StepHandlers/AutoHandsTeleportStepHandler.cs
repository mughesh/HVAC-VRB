// AutoHandsTeleportStepHandler.cs
// Handles teleport interaction steps in training sequences using AutoHands framework
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for teleport-based interaction steps using AutoHands framework
/// Manages wrist button event subscriptions and teleport execution
/// Follows pattern from AutoHandsGrabStepHandler and AutoHandsSnapStepHandler
/// </summary>
public class AutoHandsTeleportStepHandler : BaseAutoHandsStepHandler
{
    // Component cache for teleport controllers
    private Dictionary<GameObject, TeleportController> teleportControllers = new Dictionary<GameObject, TeleportController>();

    // Component cache for wrist buttons
    private Dictionary<GameObject, WristUIButton> wristButtons = new Dictionary<GameObject, WristUIButton>();

    // Active step tracking
    private Dictionary<InteractionStep, TeleportController> activeStepTeleports = new Dictionary<InteractionStep, TeleportController>();
    private Dictionary<InteractionStep, WristUIButton> activeStepButtons = new Dictionary<InteractionStep, WristUIButton>();

    // Button event delegates for proper unsubscription
    private Dictionary<InteractionStep, UnityAction> buttonEventDelegates = new Dictionary<InteractionStep, UnityAction>();

    void Awake()
    {
        CacheTeleportControllers();
        CacheWristButtons();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.Teleport;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🚀 AutoHandsTeleportStepHandler initialized");

        // Refresh cache in case scene changed
        CacheTeleportControllers();
        CacheWristButtons();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🚀 Starting AutoHands teleport step: {step.stepName}");

        // Get teleport destination
        var destinationObject = step.teleportDestination?.GameObject;
        if (destinationObject == null)
        {
            LogError($"Teleport destination is null for step: {step.stepName}");
            return;
        }

        if (!teleportControllers.ContainsKey(destinationObject))
        {
            LogError($"No TeleportController found for object: {destinationObject.name} in step: {step.stepName}");
            return;
        }

        var teleportController = teleportControllers[destinationObject];

        // Get wrist button reference
        var buttonObject = step.wristButton?.GameObject;
        if (buttonObject == null)
        {
            LogError($"Wrist button reference is null for step: {step.stepName}");
            return;
        }

        if (!wristButtons.ContainsKey(buttonObject))
        {
            LogError($"No WristUIButton component found on: {buttonObject.name}");
            return;
        }

        var wristButton = wristButtons[buttonObject];

        // Create delegate that captures step context
        UnityAction buttonDelegate = () => OnTeleportButtonPressed(step, teleportController);

        // Subscribe to wrist button press event
        wristButton.OnButtonPressed.AddListener(buttonDelegate);

        // Track active step
        activeStepTeleports[step] = teleportController;
        activeStepButtons[step] = wristButton;
        buttonEventDelegates[step] = buttonDelegate;

        LogDebug($"🚀 Subscribed to wrist button press for: {buttonObject.name} → {destinationObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        LogDebug($"🚀 Stopping AutoHands teleport step: {step.stepName}");

        if (activeStepButtons.ContainsKey(step) && buttonEventDelegates.ContainsKey(step))
        {
            var wristButton = activeStepButtons[step];
            var buttonDelegate = buttonEventDelegates[step];

            // Unsubscribe from button event
            wristButton.OnButtonPressed.RemoveListener(buttonDelegate);

            // Remove from tracking
            activeStepTeleports.Remove(step);
            activeStepButtons.Remove(step);
            buttonEventDelegates.Remove(step);

            LogDebug($"🚀 Unsubscribed from wrist button press for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🚀 Cleaning up AutoHands teleport step handler...");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepTeleports.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        // Clear caches
        teleportControllers.Clear();
        wristButtons.Clear();
        buttonEventDelegates.Clear();

        base.Cleanup();
    }

    /// <summary>
    /// Cache all TeleportController components in the scene
    /// </summary>
    void CacheTeleportControllers()
    {
        LogDebug("🚀 Caching TeleportController components...");

        teleportControllers.Clear();

        var controllers = FindObjectsOfType<TeleportController>();
        foreach (var controller in controllers)
        {
            teleportControllers[controller.gameObject] = controller;
            LogDebug($"🚀 Cached TeleportController: {controller.name}");
        }

        LogInfo($"🚀 Cached {teleportControllers.Count} TeleportController components");
    }

    /// <summary>
    /// Cache all WristUIButton components in the scene
    /// </summary>
    void CacheWristButtons()
    {
        LogDebug("🚀 Caching WristUIButton components...");

        wristButtons.Clear();

        var buttons = FindObjectsOfType<WristUIButton>();
        foreach (var button in buttons)
        {
            wristButtons[button.gameObject] = button;
            LogDebug($"🚀 Cached WristUIButton: {button.name}");
        }

        LogInfo($"🚀 Cached {wristButtons.Count} WristUIButton components");
    }

    /// <summary>
    /// Handle wrist button press event for teleport execution
    /// </summary>
    void OnTeleportButtonPressed(InteractionStep step, TeleportController teleportController)
    {
        if (step.isCompleted)
        {
            LogDebug($"🚀 Step already completed, ignoring button press");
            return;
        }

        LogDebug($"🚀 Wrist button pressed! Executing teleport for step: {step.stepName}");

        // Execute teleport
        teleportController.ExecuteTeleport();

        // Complete step
        CompleteStep(step, $"Teleported to {teleportController.name}");

        LogDebug($"🚀 Teleport step completed: {step.stepName}");
    }
}

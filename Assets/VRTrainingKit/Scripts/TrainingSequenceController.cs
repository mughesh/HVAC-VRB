// TrainingSequenceController.cs
// Runtime controller for executing VR training sequences
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Runtime controller that executes training sequences by listening to XRI events
/// Handles step completion detection and progress tracking
/// </summary>
public class TrainingSequenceController : MonoBehaviour
{
    [Header("Training Sequence")]
    [Tooltip("The training sequence asset to execute")]
    public TrainingSequenceAsset trainingAsset;
    
    [Header("Debug Settings")]
    [Tooltip("Log detailed debug information")]
    public bool enableDebugLogging = true;
    
    [Tooltip("Show step completion messages")]
    public bool showStepCompletions = true;
    
    // Runtime state
    private TrainingProgram currentProgram;
    private List<InteractionStep> activeSteps = new List<InteractionStep>();
    private Dictionary<GameObject, XRGrabInteractable> grabInteractables = new Dictionary<GameObject, XRGrabInteractable>();
    private Dictionary<GameObject, XRSocketInteractor> socketInteractors = new Dictionary<GameObject, XRSocketInteractor>();
    private Dictionary<GameObject, KnobController> knobControllers = new Dictionary<GameObject, KnobController>();
    private Dictionary<GameObject, ValveController> valveControllers = new Dictionary<GameObject, ValveController>();
    
    // Store delegate references for proper cleanup
    private Dictionary<KnobController, System.Action<float>> knobEventDelegates = new Dictionary<KnobController, System.Action<float>>();
    private Dictionary<ValveController, System.Action> valveTightenedEventDelegates = new Dictionary<ValveController, System.Action>();
    private Dictionary<ValveController, System.Action> valveLoosenedEventDelegates = new Dictionary<ValveController, System.Action>();
    
    // Progress tracking
    private int currentModuleIndex = 0;
    private int currentTaskGroupIndex = 0;
    private bool sequenceComplete = false;
    
    // Events for UI integration (future phases)
    public System.Action<InteractionStep> OnStepCompleted;
    public System.Action<TaskGroup> OnTaskGroupCompleted;
    public System.Action<TrainingModule> OnModuleCompleted;
    public System.Action OnSequenceCompleted;
    
    void Start()
    {
        InitializeSequence();
    }
    
    void OnDestroy()
    {
        CleanupEventSubscriptions();
    }
    
    /// <summary>
    /// Initialize the training sequence execution
    /// </summary>
    void InitializeSequence()
    {
        if (trainingAsset == null)
        {
            LogError("No training asset assigned to TrainingSequenceController!");
            return;
        }
        
        currentProgram = trainingAsset.Program;
        if (currentProgram == null)
        {
            LogError("Training asset has no program data!");
            return;
        }
        
        LogInfo($"Starting training sequence: {currentProgram.programName}");
        
        // Find and cache all XRI components in the scene
        CacheXRIComponents();
        
        // Start with the first module and task group
        currentModuleIndex = 0;
        currentTaskGroupIndex = 0;
        
        // Begin execution
        StartCurrentTaskGroup();
    }
    
    /// <summary>
    /// Find and cache all XRI components for event subscription
    /// </summary>
    void CacheXRIComponents()
    {
        LogInfo("Caching XRI components in scene...");
        
        // Find all grab interactables
        var grabInteractables = FindObjectsOfType<XRGrabInteractable>();
        foreach (var grabInteractable in grabInteractables)
        {
            this.grabInteractables[grabInteractable.gameObject] = grabInteractable;
            LogDebug($"Cached grab interactable: {grabInteractable.name}");
        }
        
        // Find all socket interactors
        var socketInteractors = FindObjectsOfType<XRSocketInteractor>();
        foreach (var socketInteractor in socketInteractors)
        {
            this.socketInteractors[socketInteractor.gameObject] = socketInteractor;
            LogDebug($"Cached socket interactor: {socketInteractor.name}");
        }
        
        // Find all knob controllers
        var knobControllers = FindObjectsOfType<KnobController>();
        foreach (var knobController in knobControllers)
        {
            this.knobControllers[knobController.gameObject] = knobController;
            LogDebug($"Cached knob controller: {knobController.name}");
        }
        
        // Find all valve controllers
        var valveControllers = FindObjectsOfType<ValveController>();
        foreach (var valveController in valveControllers)
        {
            this.valveControllers[valveController.gameObject] = valveController;
            LogDebug($"Cached valve controller: {valveController.name}");
        }
        
        LogInfo($"Cached {this.grabInteractables.Count} grab interactables, {this.socketInteractors.Count} socket interactors, {this.knobControllers.Count} knob controllers, {this.valveControllers.Count} valve controllers");
        
        // Validate knob configurations
        ValidateKnobConfigurations();
        
        // Restore any missing profiles
        RestoreMissingProfiles();
    }
    
    /// <summary>
    /// Start executing the current task group
    /// </summary>
    void StartCurrentTaskGroup()
    {
        if (sequenceComplete) return;
        
        var currentModule = GetCurrentModule();
        var currentTaskGroup = GetCurrentTaskGroup();
        
        if (currentModule == null || currentTaskGroup == null)
        {
            CompleteSequence();
            return;
        }
        
        LogInfo($"Starting task group: {currentTaskGroup.groupName} (Module: {currentModule.moduleName})");
        
        // Clear previous active steps
        activeSteps.Clear();
        CleanupEventSubscriptions();
        
        // Add all steps from current task group to active steps
        foreach (var step in currentTaskGroup.steps)
        {
            if (step != null)
            {
                step.ResetCompletion(); // Reset completion state
                activeSteps.Add(step);
                LogDebug($"Added step to active list: {step.stepName} [{step.type}]");
            }
        }
        
        // Subscribe to relevant XRI events
        SubscribeToXRIEvents();
        
        // Check for any steps that are already complete or can be completed immediately
        CheckStepCompletions();
    }
    
    /// <summary>
    /// Subscribe to XRI events based on active steps
    /// </summary>
    void SubscribeToXRIEvents()
    {
        LogDebug("Subscribing to XRI events...");
        
        foreach (var step in activeSteps)
        {
            switch (step.type)
            {
                case InteractionStep.StepType.Grab:
                    SubscribeToGrabEvents(step);
                    break;
                    
                case InteractionStep.StepType.GrabAndSnap:
                    SubscribeToSnapEvents(step);
                    break;
                    
                case InteractionStep.StepType.TurnKnob:
                    SubscribeToKnobEvents(step);
                    break;
                    
                case InteractionStep.StepType.TightenValve:
                case InteractionStep.StepType.LoosenValve:
                case InteractionStep.StepType.InstallValve:
                case InteractionStep.StepType.RemoveValve:
                    SubscribeToValveEvents(step);
                    break;
                    
                case InteractionStep.StepType.WaitForCondition:
                    // Wait conditions are checked in CheckStepCompletions
                    break;
                    
                case InteractionStep.StepType.ShowInstruction:
                    // Instructions are handled separately (future phase)
                    CompleteStep(step, "Instruction shown");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Subscribe to grab events for a step
    /// </summary>
    void SubscribeToGrabEvents(InteractionStep step)
    {
        var targetObject = step.targetObject.GameObject;
        if (targetObject != null && grabInteractables.ContainsKey(targetObject))
        {
            var grabInteractable = grabInteractables[targetObject];
            grabInteractable.selectEntered.AddListener((args) => OnObjectGrabbed(step, args));
            LogDebug($"Subscribed to grab events for: {targetObject.name}");
        }
        else
        {
            LogWarning($"Could not find grab interactable for step: {step.stepName} (target: {step.targetObject.GameObjectName})");
        }
    }
    
    /// <summary>
    /// Subscribe to snap events for a step
    /// </summary>
    void SubscribeToSnapEvents(InteractionStep step)
    {
        var destinationObject = step.destination.GameObject;
        if (destinationObject != null && socketInteractors.ContainsKey(destinationObject))
        {
            var socketInteractor = socketInteractors[destinationObject];
            socketInteractor.selectEntered.AddListener((args) => OnObjectSnapped(step, args));
            LogDebug($"Subscribed to snap events for: {destinationObject.name}");
        }
        else
        {
            LogWarning($"Could not find socket interactor for step: {step.stepName} (destination: {step.destination.GameObjectName})");
        }
    }
    
    /// <summary>
    /// Subscribe to knob events for a step
    /// </summary>
    void SubscribeToKnobEvents(InteractionStep step)
    {
        var targetObject = step.targetObject.GameObject;
        if (targetObject != null && knobControllers.ContainsKey(targetObject))
        {
            var knobController = knobControllers[targetObject];
            
            // Create and store delegate reference for proper cleanup
            System.Action<float> angleDelegate = (angle) => OnKnobRotated(step, angle);
            knobEventDelegates[knobController] = angleDelegate;
            
            knobController.OnAngleChanged += angleDelegate;
            LogDebug($"Subscribed to knob events for: {targetObject.name} (Current angle: {knobController.CurrentAngle:F1}°)");
        }
        else
        {
            LogWarning($"Could not find knob controller for step: {step.stepName} (target: {step.targetObject.GameObjectName})");
        }
    }
    
    /// <summary>
    /// Subscribe to valve events for a step
    /// </summary>
    void SubscribeToValveEvents(InteractionStep step)
    {
        var targetObject = step.targetObject.GameObject;
        if (targetObject != null && valveControllers.ContainsKey(targetObject))
        {
            var valveController = valveControllers[targetObject];
            
            // Apply step-specific parameters if overrides are set
            ApplyValveStepParameters(valveController, step);
            
            // Subscribe to appropriate events based on step type
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
            
            LogDebug($"Subscribed to valve events for: {targetObject.name} (Step: {step.type})");
        }
        else
        {
            LogWarning($"Could not find valve controller for step: {step.stepName} (target: {step.targetObject.GameObjectName})");
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
        
        LogDebug($"🔗 SUBSCRIBED to valve tighten events: {valveController.name} for step: {step.stepName} (Type: {step.type})");
    }

    /// <summary>
    /// Subscribe to valve loosen events
    /// </summary>
    void SubscribeToValveLoosenEvents(InteractionStep step, ValveController valveController)
    {
        System.Action loosenDelegate = () => OnValveLoosened(step);
        valveLoosenedEventDelegates[valveController] = loosenDelegate;
        valveController.OnValveLoosened += loosenDelegate;
        
        LogDebug($"🔗 SUBSCRIBED to valve loosen events: {valveController.name} for step: {step.stepName} (Type: {step.type})");
    }

    /// <summary>
    /// Handle valve tightened event
    /// </summary>
    void OnValveTightened(InteractionStep step)
    {
        LogDebug($"🔧 VALVE TIGHTEN EVENT FIRED for step: {step.stepName} (Type: {step.type})");
        LogDebug($"✅ Completing tighten step: {step.stepName}");
        CompleteStep(step, "Valve tightened successfully");
    }

    /// <summary>
    /// Handle valve loosened event
    /// </summary>
    void OnValveLoosened(InteractionStep step)
    {
        LogDebug($"🔧 VALVE LOOSEN EVENT FIRED for step: {step.stepName} (Type: {step.type})");
        
        if (step.type == InteractionStep.StepType.LoosenValve || 
            step.type == InteractionStep.StepType.RemoveValve)
        {
            LogDebug($"✅ Completing loosen step: {step.stepName}");
            CompleteStep(step, "Valve loosened successfully");
        }
        else
        {
            LogDebug($"⚠️ Valve loosen event fired but step type is {step.type}, not completing");
        }
    }
    
    /// <summary>
    /// Apply sequence-level parameter overrides to valve controller
    /// </summary>
    void ApplyValveStepParameters(ValveController valveController, InteractionStep step)
    {
        // For now, we'll implement a basic version that creates runtime profile copies
        // This will be enhanced in Phase 4 with the full parameter override system
        
        // Get the current valve profile (we'll need to add this helper method)
        var profile = GetValveProfile(valveController);
        if (profile == null)
        {
            LogWarning($"No valve profile found for {valveController.gameObject.name}");
            return;
        }
        
        // Check if we need to apply any overrides
        bool needsOverride = false;
        
        if (step.rotationAxis != profile.rotationAxis) needsOverride = true;
        if (step.tightenThreshold != profile.tightenThreshold) needsOverride = true;
        if (step.loosenThreshold != profile.loosenThreshold) needsOverride = true;
        if (step.valveAngleTolerance != profile.angleTolerance) needsOverride = true;
        if (step.rotationDampening != profile.rotationDampening) needsOverride = true;
        
        if (needsOverride)
        {
            // Create a runtime copy of the profile (simplified version for now)
            var runtimeProfile = ScriptableObject.CreateInstance<ValveProfile>();
            
            // Copy base settings from original profile
            runtimeProfile.profileName = $"{profile.profileName}_Runtime";
            runtimeProfile.rotationAxis = step.rotationAxis != profile.rotationAxis ? step.rotationAxis : profile.rotationAxis;
            runtimeProfile.tightenThreshold = step.tightenThreshold != profile.tightenThreshold ? step.tightenThreshold : profile.tightenThreshold;
            runtimeProfile.loosenThreshold = step.loosenThreshold != profile.loosenThreshold ? step.loosenThreshold : profile.loosenThreshold;
            runtimeProfile.angleTolerance = step.valveAngleTolerance != profile.angleTolerance ? step.valveAngleTolerance : profile.angleTolerance;
            runtimeProfile.rotationDampening = step.rotationDampening != profile.rotationDampening ? step.rotationDampening : profile.rotationDampening;
            
            // Copy other essential settings
            runtimeProfile.compatibleSocketTags = profile.compatibleSocketTags;
            runtimeProfile.requireSpecificSockets = profile.requireSpecificSockets;
            runtimeProfile.specificCompatibleSockets = profile.specificCompatibleSockets;
            
            // Apply the modified profile
            valveController.Configure(runtimeProfile);
            
            LogDebug($"🔧 Applied parameter overrides to {valveController.gameObject.name}:");
            LogDebug($"   - Rotation Axis: {runtimeProfile.rotationAxis}");
            LogDebug($"   - Tighten Threshold: {runtimeProfile.tightenThreshold}°");
            LogDebug($"   - Loosen Threshold: {runtimeProfile.loosenThreshold}°");
            LogDebug($"   - Angle Tolerance: {runtimeProfile.angleTolerance}°");
        }
    }
    
    /// <summary>
    /// Get the valve profile from a valve controller (helper method)
    /// </summary>
    ValveProfile GetValveProfile(ValveController valveController)
    {
        // Use reflection to access the private profile field
        var profileField = typeof(ValveController).GetField("profile", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (profileField != null)
        {
            return (ValveProfile)profileField.GetValue(valveController);
        }
        
        return null;
    }
    
    /// <summary>
    /// Handle object grabbed event
    /// </summary>
    void OnObjectGrabbed(InteractionStep step, SelectEnterEventArgs args)
    {
        if (step.isCompleted) return;
        
        var grabbedObject = args.interactableObject.transform.gameObject;
        var expectedObject = step.targetObject.GameObject;
        
        if (grabbedObject == expectedObject)
        {
            CompleteStep(step, $"Grabbed {grabbedObject.name}");
        }
    }
    
    /// <summary>
    /// Handle object snapped event
    /// </summary>
    void OnObjectSnapped(InteractionStep step, SelectEnterEventArgs args)
    {
        if (step.isCompleted) return;
        
        var snappedObject = args.interactableObject.transform.gameObject;
        var expectedObject = step.targetObject.GameObject;
        var destinationSocket = args.interactorObject.transform.gameObject;
        var expectedDestination = step.destination.GameObject;
        
        if (snappedObject == expectedObject && destinationSocket == expectedDestination)
        {
            CompleteStep(step, $"Snapped {snappedObject.name} to {destinationSocket.name}");
        }
    }
    
    /// <summary>
    /// Handle knob rotated event
    /// </summary>
    void OnKnobRotated(InteractionStep step, float currentAngle)
    {
        if (step.isCompleted) return;
        
        float targetAngle = step.targetAngle;
        float tolerance = step.angleTolerance;
        
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
        
        // Enhanced debug logging
        LogDebug($"Knob rotation detected - {step.stepName}: " +
                $"Current: {currentAngle:F2}°, Target: {targetAngle:F2}°, " +
                $"Difference: {angleDifference:F2}°, Tolerance: {tolerance:F2}°");
        
        if (angleDifference <= tolerance)
        {
            CompleteStep(step, $"Knob rotated to {currentAngle:F1}° (target: {targetAngle}°, tolerance: ±{tolerance}°)");
        }
        else
        {
            // Show progress toward target
            float progress = Mathf.Max(0f, 1f - (angleDifference / (tolerance * 3f))); // 3x tolerance = 0% progress
            if (enableDebugLogging && progress > 0.1f)
            {
                LogDebug($"Knob progress: {(progress * 100f):F0}% toward target");
            }
        }
    }
    
    /// <summary>
    /// Complete a step and check for task group completion
    /// </summary>
    void CompleteStep(InteractionStep step, string reason)
    {
        if (step.isCompleted) return;
        
        step.isCompleted = true;
        OnStepCompleted?.Invoke(step);
        
        if (showStepCompletions)
        {
            LogInfo($"✓ Step completed: {step.stepName} - {reason}");
        }
        
        // Check if task group is complete
        CheckTaskGroupCompletion();
    }
    
    /// <summary>
    /// Check if any steps can be completed (wait conditions, etc.)
    /// </summary>
    void CheckStepCompletions()
    {
        foreach (var step in activeSteps.ToList()) // ToList to avoid modification during iteration
        {
            if (step.isCompleted) continue;
            
            if (step.type == InteractionStep.StepType.WaitForCondition)
            {
                CheckWaitCondition(step);
            }
        }
    }
    
    /// <summary>
    /// Check if a wait condition step can be completed
    /// </summary>
    void CheckWaitCondition(InteractionStep waitStep)
    {
        var currentTaskGroup = GetCurrentTaskGroup();
        if (currentTaskGroup == null) return;
        
        bool allConditionsMet = true;
        foreach (int stepIndex in waitStep.waitForSteps)
        {
            if (stepIndex < 0 || stepIndex >= currentTaskGroup.steps.Count)
            {
                LogWarning($"Invalid wait condition index {stepIndex} in step: {waitStep.stepName}");
                continue;
            }
            
            var requiredStep = currentTaskGroup.steps[stepIndex];
            if (!requiredStep.isCompleted)
            {
                allConditionsMet = false;
                break;
            }
        }
        
        if (allConditionsMet)
        {
            CompleteStep(waitStep, $"Wait conditions met ({waitStep.waitForSteps.Count} steps)");
        }
    }
    
    /// <summary>
    /// Check if the current task group is complete
    /// </summary>
    void CheckTaskGroupCompletion()
    {
        var currentTaskGroup = GetCurrentTaskGroup();
        if (currentTaskGroup == null) return;
        
        // Check if all required steps are complete
        bool allRequiredStepsComplete = true;
        int completedSteps = 0;
        int totalSteps = 0;
        
        foreach (var step in activeSteps)
        {
            totalSteps++;
            if (step.isCompleted)
            {
                completedSteps++;
            }
            else if (!step.isOptional)
            {
                allRequiredStepsComplete = false;
            }
        }
        
        LogDebug($"Task group progress: {completedSteps}/{totalSteps} steps completed");
        
        if (allRequiredStepsComplete)
        {
            CompleteTaskGroup();
        }
        else
        {
            // Check wait conditions for any remaining steps
            CheckStepCompletions();
        }
    }
    
    /// <summary>
    /// Complete the current task group and move to the next
    /// </summary>
    void CompleteTaskGroup()
    {
        var currentTaskGroup = GetCurrentTaskGroup();
        if (currentTaskGroup == null) return;
        
        LogInfo($"✓ Task group completed: {currentTaskGroup.groupName}");
        OnTaskGroupCompleted?.Invoke(currentTaskGroup);
        
        // Move to next task group
        currentTaskGroupIndex++;
        
        var currentModule = GetCurrentModule();
        if (currentTaskGroupIndex >= currentModule.taskGroups.Count)
        {
            // Current module is complete
            CompleteModule();
        }
        else
        {
            // Start next task group
            StartCurrentTaskGroup();
        }
    }
    
    /// <summary>
    /// Complete the current module and move to the next
    /// </summary>
    void CompleteModule()
    {
        var currentModule = GetCurrentModule();
        if (currentModule == null) return;
        
        LogInfo($"✓ Module completed: {currentModule.moduleName}");
        OnModuleCompleted?.Invoke(currentModule);
        
        // Move to next module
        currentModuleIndex++;
        currentTaskGroupIndex = 0;
        
        if (currentModuleIndex >= currentProgram.modules.Count)
        {
            // All modules complete - sequence finished
            CompleteSequence();
        }
        else
        {
            // Start next module
            StartCurrentTaskGroup();
        }
    }
    
    /// <summary>
    /// Complete the entire training sequence
    /// </summary>
    void CompleteSequence()
    {
        sequenceComplete = true;
        LogInfo($"🎉 Training sequence completed: {currentProgram.programName}");
        OnSequenceCompleted?.Invoke();
        
        CleanupEventSubscriptions();
    }
    
    /// <summary>
    /// Clean up all event subscriptions
    /// </summary>
    void CleanupEventSubscriptions()
    {
        LogDebug("Cleaning up event subscriptions...");
        
        // Cleanup grab interactables
        foreach (var kvp in grabInteractables)
        {
            if (kvp.Value != null)
            {
                kvp.Value.selectEntered.RemoveAllListeners();
            }
        }
        
        // Cleanup socket interactors
        foreach (var kvp in socketInteractors)
        {
            if (kvp.Value != null)
            {
                kvp.Value.selectEntered.RemoveAllListeners();
            }
        }
        
        // Cleanup knob controllers - FIXED VERSION
        foreach (var kvp in knobEventDelegates)
        {
            var knobController = kvp.Key;
            var angleDelegate = kvp.Value;
            
            if (knobController != null)
            {
                knobController.OnAngleChanged -= angleDelegate;
                LogDebug($"Unsubscribed from knob controller: {knobController.name}");
            }
        }
        knobEventDelegates.Clear();
        
        // Cleanup valve controllers
        foreach (var kvp in valveTightenedEventDelegates)
        {
            var valveController = kvp.Key;
            var tightenDelegate = kvp.Value;
            
            if (valveController != null)
            {
                valveController.OnValveTightened -= tightenDelegate;
                LogDebug($"Unsubscribed from valve tighten events: {valveController.name}");
            }
        }
        valveTightenedEventDelegates.Clear();
        
        foreach (var kvp in valveLoosenedEventDelegates)
        {
            var valveController = kvp.Key;
            var loosenDelegate = kvp.Value;
            
            if (valveController != null)
            {
                valveController.OnValveLoosened -= loosenDelegate;
                LogDebug($"Unsubscribed from valve loosen events: {valveController.name}");
            }
        }
        valveLoosenedEventDelegates.Clear();
        
        LogDebug("Event subscription cleanup complete");
    }
    
    /// <summary>
    /// Validate knob configurations for common setup issues
    /// </summary>
    void ValidateKnobConfigurations()
    {
        LogDebug("Validating knob configurations...");
        
        foreach (var kvp in knobControllers)
        {
            var knobObject = kvp.Key;
            var knobController = kvp.Value;
            
            // Check for required components
            var grabInteractable = knobObject.GetComponent<XRGrabInteractable>();
            var hingeJoint = knobObject.GetComponent<HingeJoint>();
            var rigidbody = knobObject.GetComponent<Rigidbody>();
            
            if (grabInteractable == null)
            {
                LogWarning($"Knob {knobObject.name} missing XRGrabInteractable component!");
                continue;
            }
            
            if (hingeJoint == null)
            {
                LogWarning($"Knob {knobObject.name} missing HingeJoint component!");
                continue;
            }
            
            if (rigidbody == null)
            {
                LogWarning($"Knob {knobObject.name} missing Rigidbody component!");
                continue;
            }
            
            // Check grab interactable settings
            if (grabInteractable.movementType != XRBaseInteractable.MovementType.VelocityTracking)
            {
                LogWarning($"Knob {knobObject.name}: movementType should be VelocityTracking for joint compatibility");
            }
            
            if (grabInteractable.trackPosition != false)
            {
                LogWarning($"Knob {knobObject.name}: trackPosition should be false for rotation-only knobs");
            }
            
            if (grabInteractable.trackRotation != true)
            {
                LogWarning($"Knob {knobObject.name}: trackRotation should be true for knob interaction");
            }
            
            // Check rigidbody settings
            if (rigidbody.isKinematic != false)
            {
                LogWarning($"Knob {knobObject.name}: Rigidbody.isKinematic should be false for HingeJoint physics");
            }
            
            // Check if there are knob steps that reference this object
            ValidateKnobSteps(knobObject, hingeJoint);
        }
        
        LogDebug("Knob configuration validation complete");
    }
    
    /// <summary>
    /// Validate knob steps against the actual knob configuration
    /// </summary>
    void ValidateKnobSteps(GameObject knobObject, HingeJoint hingeJoint)
    {
        if (currentProgram?.modules == null) return;
        
        foreach (var module in currentProgram.modules)
        {
            if (module.taskGroups == null) continue;
            
            foreach (var taskGroup in module.taskGroups)
            {
                if (taskGroup.steps == null) continue;
                
                foreach (var step in taskGroup.steps)
                {
                    if (step.type == InteractionStep.StepType.TurnKnob && 
                        step.targetObject.GameObject == knobObject)
                    {
                        // Check if target angle is within joint limits
                        if (hingeJoint.useLimits)
                        {
                            var limits = hingeJoint.limits;
                            if (step.targetAngle < limits.min || step.targetAngle > limits.max)
                            {
                                LogWarning($"Step '{step.stepName}': target angle {step.targetAngle}° is outside " +
                                          $"HingeJoint limits [{limits.min}° to {limits.max}°] for {knobObject.name}");
                            }
                        }
                        
                        // Check for reasonable tolerance
                        if (step.angleTolerance <= 0f)
                        {
                            LogWarning($"Step '{step.stepName}': angleTolerance should be > 0 (current: {step.angleTolerance})");
                        }
                        else if (step.angleTolerance > 45f)
                        {
                            LogWarning($"Step '{step.stepName}': angleTolerance {step.angleTolerance}° seems too large (> 45°)");
                        }
                        
                        LogDebug($"Validated knob step '{step.stepName}': target={step.targetAngle}°, tolerance=±{step.angleTolerance}°");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Restore missing profiles using stored editor preferences or default profiles
    /// </summary>
    void RestoreMissingProfiles()
    {
        LogDebug("Checking for objects with missing profiles...");
        
        int restoredCount = 0;
        
        foreach (var kvp in knobControllers)
        {
            var knobObject = kvp.Key;
            var knobController = kvp.Value;
            
            // Check if this knob controller is missing its profile
            if (IsProfileMissing(knobController, knobObject.name))
            {
                LogWarning($"Profile missing for {knobObject.name}! Attempting to restore...");
                
                if (RestoreKnobProfile(knobObject, knobController))
                {
                    restoredCount++;
                    LogInfo($"Successfully restored profile for {knobObject.name}");
                }
                else
                {
                    LogError($"Failed to restore profile for {knobObject.name} - knob may not work correctly");
                }
            }
        }
        
        if (restoredCount > 0)
        {
            LogInfo($"Restored profiles for {restoredCount} objects that were missing configurations");
        }
        else
        {
            LogDebug("All objects have valid profiles");
        }
    }
    
    /// <summary>
    /// Check if a KnobController is missing its profile
    /// </summary>
    bool IsProfileMissing(KnobController knobController, string objectName)
    {
        // Use reflection to safely check the profile field
        var profileField = typeof(KnobController).GetField("profile", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (profileField != null)
        {
            var profile = profileField.GetValue(knobController);
            return profile == null;
        }
        
        // If we can't check, assume it might be missing
        LogWarning($"Could not check profile status for {objectName} - reflection failed");
        return false;
    }
    
    /// <summary>
    /// Restore a knob profile using available methods
    /// </summary>
    bool RestoreKnobProfile(GameObject knobObject, KnobController knobController)
    {
        // Method 1: Try to find a default knob profile in Resources
        KnobProfile defaultProfile = Resources.Load<KnobProfile>("KnobProfile");
        if (defaultProfile != null)
        {
            try
            {
                LogDebug($"Applying default resource profile to {knobObject.name}");
                defaultProfile.ApplyToGameObject(knobObject);
                return true;
            }
            catch (System.Exception e)
            {
                LogError($"Failed to apply default profile to {knobObject.name}: {e.Message}");
            }
        }
        
        // Method 2: Scan for available knob profiles in project
        #if UNITY_EDITOR
        var profileGuids = UnityEditor.AssetDatabase.FindAssets("t:KnobProfile");
        if (profileGuids.Length > 0)
        {
            var profilePath = UnityEditor.AssetDatabase.GUIDToAssetPath(profileGuids[0]);
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<KnobProfile>(profilePath);
            if (profile != null)
            {
                try
                {
                    LogDebug($"Applying found project profile '{profile.profileName}' to {knobObject.name}");
                    profile.ApplyToGameObject(knobObject);
                    return true;
                }
                catch (System.Exception e)
                {
                    LogError($"Failed to apply project profile to {knobObject.name}: {e.Message}");
                }
            }
        }
        #endif
        
        LogWarning($"No suitable profile found for {knobObject.name}");
        return false;
    }
    
    /// <summary>
    /// Get the current module
    /// </summary>
    TrainingModule GetCurrentModule()
    {
        if (currentProgram?.modules == null || currentModuleIndex >= currentProgram.modules.Count)
            return null;
        return currentProgram.modules[currentModuleIndex];
    }
    
    /// <summary>
    /// Get the current task group
    /// </summary>
    TaskGroup GetCurrentTaskGroup()
    {
        var currentModule = GetCurrentModule();
        if (currentModule?.taskGroups == null || currentTaskGroupIndex >= currentModule.taskGroups.Count)
            return null;
        return currentModule.taskGroups[currentTaskGroupIndex];
    }
    
    /// <summary>
    /// Get current progress information
    /// </summary>
    public SequenceProgress GetProgress()
    {
        var progress = new SequenceProgress
        {
            currentModuleIndex = this.currentModuleIndex,
            totalModules = currentProgram?.modules?.Count ?? 0,
            currentTaskGroupIndex = this.currentTaskGroupIndex,
            isComplete = this.sequenceComplete
        };
        
        var currentModule = GetCurrentModule();
        if (currentModule != null)
        {
            progress.totalTaskGroups = currentModule.taskGroups?.Count ?? 0;
            progress.currentModuleName = currentModule.moduleName;
            
            var currentTaskGroup = GetCurrentTaskGroup();
            if (currentTaskGroup != null)
            {
                progress.currentTaskGroupName = currentTaskGroup.groupName;
                progress.completedSteps = activeSteps.Count(s => s.isCompleted);
                progress.totalSteps = activeSteps.Count;
            }
        }
        
        return progress;
    }
    
    // Logging methods
    void LogInfo(string message)
    {
        Debug.Log($"[TrainingSequence] {message}");
    }
    
    void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[TrainingSequence] {message}");
        }
    }
    
    void LogWarning(string message)
    {
        Debug.LogWarning($"[TrainingSequence] {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError($"[TrainingSequence] {message}");
    }
    
    /// <summary>
    /// Progress information for UI display
    /// </summary>
    [System.Serializable]
    public class SequenceProgress
    {
        public int currentModuleIndex;
        public int totalModules;
        public string currentModuleName;
        
        public int currentTaskGroupIndex;
        public int totalTaskGroups;
        public string currentTaskGroupName;
        
        public int completedSteps;
        public int totalSteps;
        
        public bool isComplete;
        
        public float GetOverallProgress()
        {
            if (totalModules == 0) return 0f;
            return (float)currentModuleIndex / totalModules;
        }
        
        public float GetCurrentTaskGroupProgress()
        {
            if (totalSteps == 0) return 0f;
            return (float)completedSteps / totalSteps;
        }
    }
}
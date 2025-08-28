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
        
        LogInfo($"Cached {this.grabInteractables.Count} grab interactables, {this.socketInteractors.Count} socket interactors, {this.knobControllers.Count} knob controllers");
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
            knobController.OnAngleChanged += (angle) => OnKnobRotated(step, angle);
            LogDebug($"Subscribed to knob events for: {targetObject.name}");
        }
        else
        {
            LogWarning($"Could not find knob controller for step: {step.stepName} (target: {step.targetObject.GameObjectName})");
        }
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
        
        if (angleDifference <= tolerance)
        {
            CompleteStep(step, $"Knob rotated to {currentAngle:F1}° (target: {targetAngle}°)");
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
        
        // Cleanup knob controllers
        foreach (var kvp in knobControllers)
        {
            if (kvp.Value != null)
            {
                // Remove all listeners using -= (events don't support direct null assignment)
                // Note: This requires tracking the specific delegates that were added
                // For proper cleanup, consider storing references to the added delegates
            }
        }
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
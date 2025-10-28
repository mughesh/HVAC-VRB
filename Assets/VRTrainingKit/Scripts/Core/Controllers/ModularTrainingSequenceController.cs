// ModularTrainingSequenceController.cs
// Modular runtime controller for executing VR training sequences
// Uses handler-based architecture for event isolation and extensibility
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Modular training sequence controller that delegates step handling to specialized handlers
/// Provides clean separation between sequence orchestration and interaction-specific logic
/// </summary>
public class ModularTrainingSequenceController : MonoBehaviour
{
    [Header("Training Sequence")]
    [Tooltip("The training sequence asset to execute")]
    public TrainingSequenceAsset trainingAsset;

    [Header("Debug Settings")]
    [Tooltip("Log detailed debug information")]
    public bool enableDebugLogging = true;

    [Tooltip("Show step completion messages")]
    public bool showStepCompletions = true;

    // Handler system
    private List<IStepHandler> stepHandlers = new List<IStepHandler>();
    private Dictionary<InteractionStep, IStepHandler> activeStepHandlers = new Dictionary<InteractionStep, IStepHandler>();

    // PHASE 1: Socket restriction manager (public for editor access in Phase 3)
    public SequenceFlowRestrictionManager restrictionManager;

    // Runtime state
    public TrainingProgram currentProgram;
    private List<InteractionStep> activeSteps = new List<InteractionStep>();
    private int currentModuleIndex = 0;
    private int currentTaskGroupIndex = 0;
    private bool sequenceComplete = false;

    // Events for UI integration
    public event Action<InteractionStep> OnStepCompleted;
    public event Action<TaskGroup> OnTaskGroupCompleted;
    public event Action<TrainingModule> OnModuleCompleted;
    public event Action OnSequenceCompleted;

    void Start()
    {
        // Initialize restriction manager for sequential flow enforcement
        restrictionManager = gameObject.AddComponent<SequenceFlowRestrictionManager>();

        InitializeSequence();
    }

    void OnDestroy()
    {
        // Cleanup restriction manager
        if (restrictionManager != null)
        {
            restrictionManager.Reset();
            Destroy(restrictionManager);
        }

        CleanupHandlers();
    }

    /// <summary>
    /// Initialize the training sequence execution
    /// </summary>
    void InitializeSequence()
    {
        if (trainingAsset == null)
        {
            LogError("No training asset assigned to ModularTrainingSequenceController!");
            return;
        }

        currentProgram = trainingAsset.Program;
        if (currentProgram == null)
        {
            LogError("Training asset has no program data!");
            return;
        }

        LogInfo($"🚀 Starting modular training sequence: {currentProgram.programName}");

        // Initialize handler system
        InitializeHandlers();

        // Start with the first module and task group
        currentModuleIndex = 0;
        currentTaskGroupIndex = 0;

        // Begin execution
        StartCurrentTaskGroup();
    }

    /// <summary>
    /// Initialize all step handlers based on current VR framework
    /// </summary>
    void InitializeHandlers()
    {
        LogInfo("🔧 Initializing step handlers...");

        // Clear any existing handlers
        CleanupHandlers();

        // Detect current framework
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        LogInfo($"🎯 Current VR Framework: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");

        // Auto-discover and initialize compatible handlers in scene
        var handlerComponents = FindObjectsOfType<MonoBehaviour>().OfType<IStepHandler>();
        var compatibleHandlers = 0;
        var incompatibleHandlers = 0;

        foreach (var handler in handlerComponents)
        {
            if (handler.SupportsFramework(currentFramework))
            {
                RegisterHandler(handler);
                compatibleHandlers++;
            }
            else
            {
                LogDebug($"🚫 Skipping incompatible handler: {handler.GetType().Name} (supports {GetSupportedFrameworks(handler)}, current: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)})");
                incompatibleHandlers++;
            }
        }

        // If no handlers found, create default ones
        if (stepHandlers.Count == 0)
        {
            LogWarning($"No compatible step handlers found for {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}. Creating default handlers...");
            CreateDefaultHandlers(currentFramework);
        }

        LogInfo($"🔧 Initialized {compatibleHandlers} compatible handlers ({incompatibleHandlers} skipped) for {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");
    }

    /// <summary>
    /// Register a step handler
    /// </summary>
    public void RegisterHandler(IStepHandler handler)
    {
        if (handler == null) return;

        stepHandlers.Add(handler);
        handler.Initialize(this);

        // Subscribe to handler events
        if (handler is BaseStepHandler baseHandler)
        {
            baseHandler.OnStepCompleted += OnHandlerStepCompleted;
        }

        LogDebug($"📝 Registered handler: {handler.GetType().Name}");
    }

    /// <summary>
    /// Create default handlers if none are found in scene
    /// </summary>
    void CreateDefaultHandlers(VRFramework framework)
    {
        switch (framework)
        {
            case VRFramework.XRI:
                CreateDefaultXRIHandlers();
                break;
            case VRFramework.AutoHands:
                CreateDefaultAutoHandsHandlers();
                break;
            default:
                LogWarning($"Cannot create default handlers for framework: {VRFrameworkDetector.GetFrameworkDisplayName(framework)}");
                break;
        }
    }

    /// <summary>
    /// Create default XRI handlers
    /// </summary>
    void CreateDefaultXRIHandlers()
    {
        LogInfo("🏗️ Creating default XRI handlers...");

        // Create XRI handler GameObjects
        var grabHandler = new GameObject("GrabStepHandler").AddComponent<GrabStepHandler>();
        var snapHandler = new GameObject("SnapStepHandler").AddComponent<SnapStepHandler>();
        var knobHandler = new GameObject("KnobStepHandler").AddComponent<KnobStepHandler>();
        var valveHandler = new GameObject("ValveStepHandler").AddComponent<ValveStepHandler>();

        // Set as children of this controller for organization
        grabHandler.transform.SetParent(transform);
        snapHandler.transform.SetParent(transform);
        knobHandler.transform.SetParent(transform);
        valveHandler.transform.SetParent(transform);

        // Register the handlers
        RegisterHandler(grabHandler);
        RegisterHandler(snapHandler);
        RegisterHandler(knobHandler);
        RegisterHandler(valveHandler);
    }

    /// <summary>
    /// Create default AutoHands handlers
    /// </summary>
    void CreateDefaultAutoHandsHandlers()
    {
        LogInfo("🏗️ Creating default AutoHands handlers...");

        // Create AutoHands handler GameObjects
        var grabHandler = new GameObject("AutoHandsGrabStepHandler").AddComponent<AutoHandsGrabStepHandler>();
        var snapHandler = new GameObject("AutoHandsSnapStepHandler").AddComponent<AutoHandsSnapStepHandler>();
        var knobHandler = new GameObject("AutoHandsKnobStepHandler").AddComponent<AutoHandsKnobStepHandler>();
        var valveHandler = new GameObject("AutoHandsValveStepHandler").AddComponent<AutoHandsValveStepHandler>();
        var waitForScriptConditionHandler = new GameObject("AutoHandsWaitForScriptConditionHandler").AddComponent<AutoHandsWaitForScriptConditionHandler>();

        // Set as children of this controller for organization
        grabHandler.transform.SetParent(transform);
        snapHandler.transform.SetParent(transform);
        knobHandler.transform.SetParent(transform);
        valveHandler.transform.SetParent(transform);
        waitForScriptConditionHandler.transform.SetParent(transform);

        // Register the handlers
        RegisterHandler(grabHandler);
        RegisterHandler(snapHandler);
        RegisterHandler(knobHandler);
        RegisterHandler(valveHandler);
        RegisterHandler(waitForScriptConditionHandler);
    }

    /// <summary>
    /// Get supported frameworks for a handler (for logging)
    /// </summary>
    string GetSupportedFrameworks(IStepHandler handler)
    {
        var supportedFrameworks = new List<string>();

        if (handler.SupportsFramework(VRFramework.XRI))
            supportedFrameworks.Add("XRI");
        if (handler.SupportsFramework(VRFramework.AutoHands))
            supportedFrameworks.Add("AutoHands");
        if (handler.SupportsFramework(VRFramework.None))
            supportedFrameworks.Add("None");

        return supportedFrameworks.Count > 0 ? string.Join(", ", supportedFrameworks) : "Unknown";
    }

    /// <summary>
    /// Handle step completion from handlers
    /// </summary>
    void OnHandlerStepCompleted(object sender, StepCompletionEventArgs args)
    {
        var step = args.step;
        var reason = args.reason;

        if (showStepCompletions)
        {
            LogInfo($"✅ Step completed: {step.stepName} - {reason}");
        }

        // PHASE 1: Notify restriction manager that this step completed
        var currentTaskGroup = GetCurrentTaskGroup();
        if (currentTaskGroup != null && currentTaskGroup.enforceSequentialFlow && restrictionManager != null)
        {
            restrictionManager.OnStepCompleted(step);
        }

        // Remove from active handlers
        if (activeStepHandlers.ContainsKey(step))
        {
            activeStepHandlers.Remove(step);
        }

        // Fire external event
        OnStepCompleted?.Invoke(step);

        // Check if task group is complete
        CheckTaskGroupCompletion();
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

        LogInfo($"📂 Starting task group: {currentTaskGroup.groupName} (Module: {currentModule.moduleName})");

        // Clear previous active steps
        StopAllActiveSteps();

        // PHASE 1: Initialize restriction manager if sequential flow is enabled
        if (currentTaskGroup.enforceSequentialFlow && restrictionManager != null)
        {
            restrictionManager.StartTaskGroup(currentTaskGroup);
            LogInfo($"🔒 Sequential flow enforcement ENABLED for this task group");
        }
        else
        {
            LogInfo($"🌐 Free exploration mode - all components available");
        }

        // Add all steps from current task group to active steps
        foreach (var step in currentTaskGroup.steps)
        {
            if (step != null)
            {
                step.ResetCompletion(); // Reset completion state
                activeSteps.Add(step);
                LogDebug($"📋 Added step to active list: {step.stepName} [{step.type}]");
            }
        }

        // Start handling all active steps
        StartActiveSteps();

        // Check for any steps that are already complete or can be completed immediately
        CheckStepCompletions();
    }

    /// <summary>
    /// Start handling all active steps by delegating to appropriate handlers
    /// </summary>
    void StartActiveSteps()
    {
        LogDebug("🎯 Starting active steps...");

        foreach (var step in activeSteps)
        {
            StartStep(step);
        }
    }

    /// <summary>
    /// Start handling a single step
    /// </summary>
    void StartStep(InteractionStep step)
    {
        // Find appropriate handler for this step
        var handler = FindHandlerForStep(step);
        if (handler == null)
        {
            LogWarning($"⚠️ No handler found for step: {step.stepName} [{step.type}]");
            return;
        }

        // PHASE 1: Notify restriction manager that this step is becoming active
        var currentTaskGroup = GetCurrentTaskGroup();
        if (currentTaskGroup != null && currentTaskGroup.enforceSequentialFlow && restrictionManager != null)
        {
            restrictionManager.OnStepBecameActive(step);
        }

        // Start handling the step
        activeStepHandlers[step] = handler;
        handler.StartStep(step);

        LogDebug($"🎯 Started step: {step.stepName} with handler: {handler.GetType().Name}");
    }

    /// <summary>
    /// Find the appropriate handler for a step
    /// </summary>
    IStepHandler FindHandlerForStep(InteractionStep step)
    {
        return stepHandlers.FirstOrDefault(h => h.CanHandle(step.type));
    }

    /// <summary>
    /// Stop all active steps
    /// </summary>
    void StopAllActiveSteps()
    {
        LogDebug("🛑 Stopping all active steps...");

        foreach (var kvp in activeStepHandlers.ToList())
        {
            var step = kvp.Key;
            var handler = kvp.Value;
            handler.StopStep(step);
        }

        activeStepHandlers.Clear();
        activeSteps.Clear();
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
            else if (step.type == InteractionStep.StepType.ShowInstruction)
            {
                // Instructions are auto-completed for now
                CompleteStep(step, "Instruction shown");
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
    /// Complete a step manually (for wait conditions and instructions)
    /// </summary>
    void CompleteStep(InteractionStep step, string reason)
    {
        if (step.isCompleted) return;

        step.isCompleted = true;
        OnStepCompleted?.Invoke(step);

        if (showStepCompletions)
        {
            LogInfo($"✅ Step completed: {step.stepName} - {reason}");
        }

        // Check if task group is complete
        CheckTaskGroupCompletion();
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

        LogDebug($"📊 Task group progress: {completedSteps}/{totalSteps} steps completed");

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

        LogInfo($"✅ Task group completed: {currentTaskGroup.groupName}");
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

        LogInfo($"✅ Module completed: {currentModule.moduleName}");
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

        StopAllActiveSteps();
    }

    /// <summary>
    /// Clean up all handlers
    /// </summary>
    void CleanupHandlers()
    {
        LogDebug("🧹 Cleaning up handlers...");

        foreach (var handler in stepHandlers)
        {
            if (handler is BaseStepHandler baseHandler)
            {
                baseHandler.OnStepCompleted -= OnHandlerStepCompleted;
            }
            handler.Cleanup();
        }

        stepHandlers.Clear();
        activeStepHandlers.Clear();
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
        Debug.Log($"[ModularTrainingSequence] {message}");
    }

    void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[ModularTrainingSequence] {message}");
        }
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[ModularTrainingSequence] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[ModularTrainingSequence] {message}");
    }

    /// <summary>
    /// Progress information for UI display (reused from original)
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
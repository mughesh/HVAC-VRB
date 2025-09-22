# VR Training Kit - Runtime Execution Flow

## Overview

The Runtime Execution System orchestrates the complete VR training experience through the `ModularTrainingSequenceController`. It manages the lifecycle of training sequences, delegates step handling to specialized handlers, tracks progress, and provides events for UI integration.

## Core Controller Architecture

### ModularTrainingSequenceController

The central orchestrator that manages sequence execution:

```csharp
public class ModularTrainingSequenceController : MonoBehaviour
{
    [Header("Training Sequence")]
    public TrainingSequenceAsset trainingAsset;

    [Header("Debug Settings")]
    public bool enableDebugLogging = true;
    public bool showStepCompletions = true;

    // Handler system
    private List<IStepHandler> stepHandlers = new List<IStepHandler>();
    private Dictionary<InteractionStep, IStepHandler> activeStepHandlers = new Dictionary<InteractionStep, IStepHandler>();

    // Runtime state
    private TrainingProgram currentProgram;
    private List<InteractionStep> activeSteps = new List<InteractionStep>();
    private int currentModuleIndex = 0;
    private int currentTaskGroupIndex = 0;
    private bool sequenceComplete = false;

    // Events for UI integration
    public event Action<InteractionStep> OnStepCompleted;
    public event Action<TaskGroup> OnTaskGroupCompleted;
    public event Action<TrainingModule> OnModuleCompleted;
    public event Action OnSequenceCompleted;
}
```

## Execution Lifecycle

### 1. **Initialization Phase**

```
Start() → InitializeSequence() → InitializeHandlers() → StartCurrentTaskGroup()
```

#### Sequence Initialization
```csharp
void InitializeSequence()
{
    // Validate training asset
    if (trainingAsset == null)
    {
        LogError("No training asset assigned!");
        return;
    }

    // Extract program data
    currentProgram = trainingAsset.Program;
    if (currentProgram == null)
    {
        LogError("Training asset has no program data!");
        return;
    }

    LogInfo($"🚀 Starting modular training sequence: {currentProgram.programName}");

    // Initialize handler system
    InitializeHandlers();

    // Start execution
    currentModuleIndex = 0;
    currentTaskGroupIndex = 0;
    StartCurrentTaskGroup();
}
```

#### Handler Discovery and Registration
```csharp
void InitializeHandlers()
{
    LogInfo("🔧 Initializing step handlers...");

    // Clear any existing handlers
    CleanupHandlers();

    // Auto-discover handlers in scene
    var handlerComponents = FindObjectsOfType<MonoBehaviour>().OfType<IStepHandler>();
    foreach (var handler in handlerComponents)
    {
        RegisterHandler(handler);
    }

    // Fallback: Create default handlers if none found
    if (stepHandlers.Count == 0)
    {
        CreateDefaultHandlers();
    }

    LogInfo($"🔧 Initialized {stepHandlers.Count} step handlers");
}
```

#### Handler Registration
```csharp
public void RegisterHandler(IStepHandler handler)
{
    stepHandlers.Add(handler);
    handler.Initialize(this);

    // Subscribe to handler events
    if (handler is BaseStepHandler baseHandler)
    {
        baseHandler.OnStepCompleted += OnHandlerStepCompleted;
    }

    LogDebug($"📝 Registered handler: {handler.GetType().Name}");
}
```

### 2. **Task Group Execution Phase**

```
StartCurrentTaskGroup() → StartActiveSteps() → CheckStepCompletions()
```

#### Task Group Startup
```csharp
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

    // Add all steps from current task group to active steps
    foreach (var step in currentTaskGroup.steps)
    {
        if (step != null)
        {
            step.ResetCompletion();
            activeSteps.Add(step);
        }
    }

    // Start handling all active steps
    StartActiveSteps();

    // Check for immediate completions
    CheckStepCompletions();
}
```

#### Step Distribution to Handlers
```csharp
void StartActiveSteps()
{
    LogDebug("🎯 Starting active steps...");

    foreach (var step in activeSteps)
    {
        StartStep(step);
    }
}

void StartStep(InteractionStep step)
{
    // Find appropriate handler for this step
    var handler = FindHandlerForStep(step);
    if (handler == null)
    {
        LogWarning($"⚠️ No handler found for step: {step.stepName} [{step.type}]");
        return;
    }

    // Start handling the step
    activeStepHandlers[step] = handler;
    handler.StartStep(step);

    LogDebug($"🎯 Started step: {step.stepName} with handler: {handler.GetType().Name}");
}

IStepHandler FindHandlerForStep(InteractionStep step)
{
    return stepHandlers.FirstOrDefault(h => h.CanHandle(step.type));
}
```

### 3. **Step Monitoring Phase**

```
Handler Events → OnHandlerStepCompleted → CheckTaskGroupCompletion → Progress Tracking
```

#### Handler Event Processing
```csharp
void OnHandlerStepCompleted(object sender, StepCompletionEventArgs args)
{
    var step = args.step;
    var reason = args.reason;

    if (showStepCompletions)
    {
        LogInfo($"✅ Step completed: {step.stepName} - {reason}");
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
```

#### Built-in Step Completion Logic
```csharp
void CheckStepCompletions()
{
    foreach (var step in activeSteps.ToList())
    {
        if (step.isCompleted) continue;

        if (step.type == InteractionStep.StepType.WaitForCondition)
        {
            CheckWaitCondition(step);
        }
        else if (step.type == InteractionStep.StepType.ShowInstruction)
        {
            // Instructions are auto-completed
            CompleteStep(step, "Instruction shown");
        }
    }
}
```

#### Wait Condition Processing
```csharp
void CheckWaitCondition(InteractionStep waitStep)
{
    var currentTaskGroup = GetCurrentTaskGroup();
    if (currentTaskGroup == null) return;

    bool allConditionsMet = true;
    foreach (int stepIndex in waitStep.waitForSteps)
    {
        if (stepIndex < 0 || stepIndex >= currentTaskGroup.steps.Count)
        {
            LogWarning($"Invalid wait condition index {stepIndex}");
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
```

### 4. **Progress Tracking Phase**

```
CheckTaskGroupCompletion → CompleteTaskGroup → CompleteModule → CompleteSequence
```

#### Task Group Completion
```csharp
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
```

#### Hierarchical Progression
```csharp
void CompleteTaskGroup()
{
    var currentTaskGroup = GetCurrentTaskGroup();
    LogInfo($"✅ Task group completed: {currentTaskGroup.groupName}");
    OnTaskGroupCompleted?.Invoke(currentTaskGroup);

    // Move to next task group
    currentTaskGroupIndex++;

    var currentModule = GetCurrentModule();
    if (currentTaskGroupIndex >= currentModule.taskGroups.Count)
    {
        CompleteModule(); // Current module is complete
    }
    else
    {
        StartCurrentTaskGroup(); // Start next task group
    }
}

void CompleteModule()
{
    var currentModule = GetCurrentModule();
    LogInfo($"✅ Module completed: {currentModule.moduleName}");
    OnModuleCompleted?.Invoke(currentModule);

    // Move to next module
    currentModuleIndex++;
    currentTaskGroupIndex = 0;

    if (currentModuleIndex >= currentProgram.modules.Count)
    {
        CompleteSequence(); // All modules complete
    }
    else
    {
        StartCurrentTaskGroup(); // Start next module
    }
}

void CompleteSequence()
{
    sequenceComplete = true;
    LogInfo($"🎉 Training sequence completed: {currentProgram.programName}");
    OnSequenceCompleted?.Invoke();

    StopAllActiveSteps();
}
```

### 5. **Cleanup Phase**

```csharp
void OnDestroy()
{
    CleanupHandlers();
}

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
```

## State Management

### Runtime State Variables

```csharp
// Current position in hierarchy
private int currentModuleIndex = 0;
private int currentTaskGroupIndex = 0;

// Active execution state
private List<InteractionStep> activeSteps = new List<InteractionStep>();
private Dictionary<InteractionStep, IStepHandler> activeStepHandlers = new Dictionary<InteractionStep, IStepHandler>();

// Completion tracking
private bool sequenceComplete = false;
```

### Progress Information

```csharp
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
```

### Progress Calculation

```csharp
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
```

## Event System

### Event Types

The controller provides events for different completion levels:

```csharp
// Step-level events
public event Action<InteractionStep> OnStepCompleted;

// Task group-level events
public event Action<TaskGroup> OnTaskGroupCompleted;

// Module-level events
public event Action<TrainingModule> OnModuleCompleted;

// Sequence-level events
public event Action OnSequenceCompleted;
```

### Event Usage Patterns

#### UI Integration
```csharp
public class TrainingUI : MonoBehaviour
{
    void Start()
    {
        var controller = FindObjectOfType<ModularTrainingSequenceController>();

        controller.OnStepCompleted += (step) => {
            UpdateStepUI(step);
            PlayCompletionFeedback();
        };

        controller.OnTaskGroupCompleted += (taskGroup) => {
            ShowTaskGroupSummary(taskGroup);
            UpdateProgressBar();
        };

        controller.OnSequenceCompleted += () => {
            ShowCompletionCertificate();
            EnableReplayOption();
        };
    }
}
```

#### Analytics Integration
```csharp
public class TrainingAnalytics : MonoBehaviour
{
    void Start()
    {
        var controller = FindObjectOfType<ModularTrainingSequenceController>();

        controller.OnStepCompleted += (step) => {
            LogStepCompletion(step.stepName, Time.time);
        };

        controller.OnModuleCompleted += (module) => {
            LogModuleCompletion(module.moduleName, GetModuleDuration());
        };
    }
}
```

## Handler Integration

### Handler Discovery Process

```csharp
// 1. Auto-discovery of existing handlers
var handlerComponents = FindObjectsOfType<MonoBehaviour>().OfType<IStepHandler>();

// 2. Manual registration
controller.RegisterHandler(new CustomStepHandler());

// 3. Fallback creation (future feature)
if (stepHandlers.Count == 0)
{
    CreateDefaultHandlers();
}
```

### Handler Communication Flow

```
Controller → Handler.StartStep(step)
Handler → Handler.OnStepCompleted event
Controller → OnHandlerStepCompleted()
Controller → CheckTaskGroupCompletion()
Controller → Progress/Event system
```

### Handler Event Pattern

```csharp
// Handler completes step
protected void CompleteStep(InteractionStep step, string reason)
{
    step.isCompleted = true;
    OnStepCompleted?.Invoke(this, new StepCompletionEventArgs(step, reason));
}

// Controller receives event
void OnHandlerStepCompleted(object sender, StepCompletionEventArgs args)
{
    // Update tracking
    activeStepHandlers.Remove(args.step);

    // Fire external events
    OnStepCompleted?.Invoke(args.step);

    // Check progression
    CheckTaskGroupCompletion();
}
```

## Error Handling and Recovery

### Validation at Runtime

```csharp
void InitializeSequence()
{
    if (trainingAsset == null)
    {
        LogError("No training asset assigned!");
        return;
    }

    var validation = trainingAsset.ValidateProgram();
    if (validation.HasErrors)
    {
        LogError($"Training sequence has {validation.errors.Count} errors!");
        foreach (var error in validation.errors)
        {
            LogError($"  - {error}");
        }
        return;
    }
}
```

### Missing Handler Handling

```csharp
void StartStep(InteractionStep step)
{
    var handler = FindHandlerForStep(step);
    if (handler == null)
    {
        LogWarning($"⚠️ No handler found for step: {step.stepName} [{step.type}]");

        // Mark as complete to prevent blocking
        if (step.isOptional)
        {
            CompleteStep(step, "No handler available (optional step)");
        }
        return;
    }

    // Continue with handler...
}
```

### GameObject Reference Resolution

```csharp
// Handled automatically by GameObjectReference system
GameObject target = step.targetObject; // May trigger GameObject.Find() fallback
if (target == null)
{
    LogWarning($"Could not resolve target object for step: {step.stepName}");
    // Graceful degradation or error handling
}
```

## Performance Considerations

### Handler Caching

All handlers cache their target components at initialization:

```csharp
void CacheGrabInteractables()
{
    grabInteractables.Clear();
    var components = FindObjectsOfType<XRGrabInteractable>();
    foreach (var component in components)
    {
        grabInteractables[component.gameObject] = component;
    }
}
```

### Efficient Progress Tracking

```csharp
// Only recalculate when needed
void CheckTaskGroupCompletion()
{
    // Quick early exit
    if (sequenceComplete) return;

    // Efficient counting
    bool allRequiredStepsComplete = true;
    foreach (var step in activeSteps)
    {
        if (!step.isCompleted && !step.isOptional)
        {
            allRequiredStepsComplete = false;
            break; // Early exit
        }
    }
}
```

### Memory Management

```csharp
void StopAllActiveSteps()
{
    // Clean up handler references
    foreach (var kvp in activeStepHandlers.ToList())
    {
        kvp.Value.StopStep(kvp.Key);
    }

    // Clear collections
    activeStepHandlers.Clear();
    activeSteps.Clear();
}
```

## Debugging and Monitoring

### Comprehensive Logging

```csharp
// Configurable debug levels
public bool enableDebugLogging = true;
public bool showStepCompletions = true;

void LogDebug(string message)
{
    if (enableDebugLogging)
    {
        Debug.Log($"[ModularTrainingSequence] {message}");
    }
}

// Emoji-coded log messages for visual identification
LogInfo("🚀 Starting sequence...");
LogDebug("🎯 Starting active steps...");
LogInfo("✅ Step completed...");
LogInfo("🎉 Training sequence completed...");
```

### Runtime State Inspection

```csharp
// Public methods for external monitoring
public SequenceProgress GetProgress() { /* ... */ }
public TrainingModule GetCurrentModule() { /* ... */ }
public TaskGroup GetCurrentTaskGroup() { /* ... */ }

// Editor debugging support
[Header("Runtime Debug Info")]
[SerializeField] private int debugCurrentModule;
[SerializeField] private int debugCurrentTaskGroup;
[SerializeField] private int debugActiveSteps;

void Update()
{
    if (enableDebugLogging)
    {
        debugCurrentModule = currentModuleIndex;
        debugCurrentTaskGroup = currentTaskGroupIndex;
        debugActiveSteps = activeSteps.Count;
    }
}
```

## Integration Patterns

### UI System Integration

```csharp
public class TrainingUIManager : MonoBehaviour
{
    public void ConnectToController()
    {
        var controller = FindObjectOfType<ModularTrainingSequenceController>();

        // Progress tracking
        InvokeRepeating(nameof(UpdateProgressUI), 0f, 0.5f);

        // Event subscriptions
        controller.OnStepCompleted += OnStepCompleted;
        controller.OnTaskGroupCompleted += OnTaskGroupCompleted;
    }

    void UpdateProgressUI()
    {
        var progress = controller.GetProgress();
        progressBar.fillAmount = progress.GetOverallProgress();
        statusText.text = $"{progress.currentModuleName} - {progress.currentTaskGroupName}";
    }
}
```

### Save System Integration

```csharp
public class TrainingSaveSystem : MonoBehaviour
{
    void SaveProgress()
    {
        var progress = controller.GetProgress();
        var saveData = new TrainingSaveData
        {
            moduleIndex = progress.currentModuleIndex,
            taskGroupIndex = progress.currentTaskGroupIndex,
            completedSteps = GetCompletedStepIds(),
            timestamp = System.DateTime.Now
        };

        SaveDataToFile(saveData);
    }
}
```

The Runtime Execution System provides a robust, event-driven foundation for complex VR training scenarios while maintaining clean separation of concerns and extensive debugging capabilities.
# VR Training Kit - Training Sequence System

## Overview

The Training Sequence System provides a hierarchical data structure for creating complex VR training scenarios. It supports multi-level organization, step dependencies, validation, and ScriptableObject serialization for persistent storage.

## Hierarchical Structure

### Four-Level Hierarchy

```
TrainingProgram (Root)
├── TrainingModule (Book/Volume)
│   ├── TaskGroup (Chapter)
│   │   ├── InteractionStep (Individual Action)
│   │   ├── InteractionStep
│   │   └── ...
│   ├── TaskGroup
│   └── ...
├── TrainingModule
└── ...
```

### Hierarchy Examples

**HVAC Training Program**:
- **Program**: "HVAC System Training"
  - **Module**: "Leak Testing"
    - **Task Group**: "Initial Setup"
      - **Step**: "Remove liquid valve cap"
      - **Step**: "Place cap on table"
    - **Task Group**: "Pressure Testing"
      - **Step**: "Connect pressure gauge"
      - **Step**: "Tighten valve to 45°"

## Core Data Classes

### 1. **TrainingProgram** (Root Level)

```csharp
[System.Serializable]
public class TrainingProgram
{
    [Header("Program Information")]
    public string programName = "New Training Program";
    [TextArea(3, 5)]
    public string description = "";

    [Header("Modules")]
    public List<TrainingModule> modules = new List<TrainingModule>();

    [Header("UI State")]
    public bool isExpanded = true; // For editor tree view
}
```

**Purpose**: Top-level container for complete training curricula
**Example**: "HVAC Training", "Automotive Repair", "Medical Procedures"

### 2. **TrainingModule** (Volume Level)

```csharp
[System.Serializable]
public class TrainingModule
{
    [Header("Module Information")]
    public string moduleName = "New Module";
    [TextArea(3, 5)]
    public string description = "";

    [Header("Task Groups")]
    public List<TaskGroup> taskGroups = new List<TaskGroup>();

    [Header("UI State")]
    public bool isExpanded = true;
}
```

**Purpose**: Major training sections with distinct learning objectives
**Example**: "Leak Testing", "Valve Maintenance", "System Diagnostics"

### 3. **TaskGroup** (Chapter Level)

```csharp
[System.Serializable]
public class TaskGroup
{
    [Header("Group Information")]
    public string groupName = "New Task Group";
    [TextArea(3, 5)]
    public string description = "";

    [Header("Interaction Steps")]
    public List<InteractionStep> steps = new List<InteractionStep>();

    [Header("UI State")]
    public bool isExpanded = true;
}
```

**Purpose**: Related steps that form a cohesive procedure
**Example**: "Initial Valve Connections", "Pressure Testing", "System Cleanup"

### 4. **InteractionStep** (Action Level)

```csharp
[System.Serializable]
public class InteractionStep
{
    public enum StepType
    {
        Grab,              // Pick up object
        GrabAndSnap,       // Pick up and place in snap point
        TurnKnob,          // Rotate knob to specific angle
        WaitForCondition,  // Wait for previous steps
        ShowInstruction,   // Display instruction to user

        // Valve operation step types
        TightenValve,      // Forward flow: grab → snap → tighten
        LoosenValve,       // Reverse flow: loosen → remove
        InstallValve,      // Complete forward flow (grab → snap → tighten)
        RemoveValve        // Complete reverse flow (loosen → remove)
    }

    [Header("Step Information")]
    public string stepName = "New Step";
    public StepType type = StepType.GrabAndSnap;

    [Header("Target Objects")]
    public GameObjectReference targetObject = new GameObjectReference();
    public GameObjectReference destination = new GameObjectReference();

    [Header("Execution Settings")]
    public bool allowParallel = false;
    public bool isOptional = false;

    [Header("Runtime State")]
    public bool isCompleted = false;
}
```

## Step Types and Configuration

### 1. **Basic Interaction Steps**

#### Grab Step
**Purpose**: Simple object pickup
**Required**: `targetObject`
**Handler**: `GrabStepHandler`

```csharp
var grabStep = new InteractionStep("Pick up wrench", InteractionStep.StepType.Grab)
{
    targetObject = wrenchReference,
    hint = "Pick up the wrench from the table"
};
```

#### GrabAndSnap Step
**Purpose**: Pick up object and place in specific location
**Required**: `targetObject`, `destination`
**Handler**: `SnapStepHandler`

```csharp
var snapStep = new InteractionStep("Place wrench in holder", InteractionStep.StepType.GrabAndSnap)
{
    targetObject = wrenchReference,
    destination = wrenchHolderReference,
    hint = "Place the wrench in the tool holder"
};
```

#### TurnKnob Step
**Purpose**: Rotate object to specific angle
**Required**: `targetObject`, `targetAngle`, `angleTolerance`
**Handler**: `KnobStepHandler`

```csharp
var knobStep = new InteractionStep("Adjust pressure", InteractionStep.StepType.TurnKnob)
{
    targetObject = pressureKnobReference,
    targetAngle = 45f,
    angleTolerance = 5f,
    hint = "Turn the pressure knob to 45 degrees"
};
```

### 2. **Valve Operation Steps**

#### TightenValve Step
**Purpose**: Rotate valve in tightening direction
**Required**: `targetObject`, `targetSocket`, `tightenThreshold`, `valveAngleTolerance`
**Handler**: `ValveStepHandler`

```csharp
var tightenStep = new InteractionStep("Tighten valve", InteractionStep.StepType.TightenValve)
{
    targetObject = valveReference,
    targetSocket = socketReference,
    tightenThreshold = 50f,
    valveAngleTolerance = 5f,
    rotationAxis = Vector3.up,
    hint = "Tighten the valve clockwise until secure"
};
```

#### LoosenValve Step
**Purpose**: Rotate valve in loosening direction
**Required**: `targetObject`, `targetSocket`, `loosenThreshold`, `valveAngleTolerance`
**Handler**: `ValveStepHandler`

```csharp
var loosenStep = new InteractionStep("Loosen valve", InteractionStep.StepType.LoosenValve)
{
    targetObject = valveReference,
    targetSocket = socketReference,
    loosenThreshold = 90f,
    valveAngleTolerance = 5f,
    hint = "Loosen the valve counterclockwise"
};
```

#### InstallValve Step
**Purpose**: Complete valve installation (grab → snap → tighten)
**Required**: `targetObject`, `targetSocket`, `tightenThreshold`
**Handler**: `ValveStepHandler`

#### RemoveValve Step
**Purpose**: Complete valve removal (loosen → remove)
**Required**: `targetObject`, `targetSocket`, `loosenThreshold`
**Handler**: `ValveStepHandler`

### 3. **Control Flow Steps**

#### WaitForCondition Step
**Purpose**: Wait for other steps to complete
**Required**: `waitForSteps` (list of step indices)
**Handler**: Built-in logic in `ModularTrainingSequenceController`

```csharp
var waitStep = new InteractionStep("Wait for setup", InteractionStep.StepType.WaitForCondition)
{
    waitForSteps = new List<int> { 0, 1, 2 }, // Wait for steps 0, 1, and 2
    hint = "Complete the initial setup steps first"
};
```

#### ShowInstruction Step
**Purpose**: Display instruction to user
**Required**: `hint` (instruction text)
**Handler**: Built-in logic in `ModularTrainingSequenceController`

```csharp
var instructionStep = new InteractionStep("Safety reminder", InteractionStep.StepType.ShowInstruction)
{
    hint = "Always wear safety goggles when working with pressurized systems",
    isOptional = true
};
```

## Advanced Step Configuration

### Parallel Execution

```csharp
// These steps can be completed in any order
var step1 = new InteractionStep("Remove cap A", InteractionStep.StepType.Grab)
{
    allowParallel = true
};

var step2 = new InteractionStep("Remove cap B", InteractionStep.StepType.Grab)
{
    allowParallel = true
};
```

### Optional Steps

```csharp
var optionalStep = new InteractionStep("Clean workspace", InteractionStep.StepType.ShowInstruction)
{
    isOptional = true,
    hint = "Clean your workspace for better organization"
};
```

### Step Dependencies

```csharp
// Step 3 waits for steps 0 and 1 to complete
var dependentStep = new InteractionStep("Final assembly", InteractionStep.StepType.WaitForCondition)
{
    waitForSteps = new List<int> { 0, 1 }
};
```

## Validation System

### Step-Level Validation

Each step type has specific validation requirements:

```csharp
public bool IsValid()
{
    switch (type)
    {
        case StepType.Grab:
            return targetObject != null && targetObject.IsValid;

        case StepType.GrabAndSnap:
            return targetObject != null && targetObject.IsValid &&
                   destination != null && destination.IsValid;

        case StepType.TurnKnob:
            return targetObject != null && targetObject.IsValid;

        case StepType.TightenValve:
        case StepType.LoosenValve:
        case StepType.InstallValve:
        case StepType.RemoveValve:
            return targetObject != null && targetObject.IsValid &&
                   targetSocket != null && targetSocket.IsValid;

        case StepType.WaitForCondition:
            return waitForSteps.Count > 0;

        case StepType.ShowInstruction:
            return !string.IsNullOrEmpty(hint);
    }
}
```

### Validation Error Messages

```csharp
public string GetValidationMessage()
{
    if (IsValid()) return "Step is valid";

    switch (type)
    {
        case StepType.Grab:
            return "Missing or invalid target object";

        case StepType.GrabAndSnap:
            if (targetObject == null || !targetObject.IsValid)
                return "Missing or invalid target object";
            if (destination == null || !destination.IsValid)
                return "Missing or invalid destination";
            break;

        case StepType.WaitForCondition:
            return "No steps specified to wait for";

        case StepType.ShowInstruction:
            return "Missing instruction text";
    }

    return "Unknown validation error";
}
```

### Comprehensive Asset Validation

`TrainingSequenceAsset` provides full program validation:

```csharp
public class ValidationResult
{
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();

    public bool HasErrors => errors.Count > 0;
    public bool IsValid => !HasErrors;

    public void AddError(string message) => errors.Add(message);
    public void AddWarning(string message) => warnings.Add(message);
}

public ValidationResult ValidateProgram()
{
    var result = new ValidationResult();

    // Validate program structure
    if (program == null)
        result.AddError("No training program assigned");

    // Validate modules, task groups, and steps
    foreach (var module in program.modules)
    {
        foreach (var group in module.taskGroups)
        {
            foreach (var step in group.steps)
            {
                if (!step.IsValid())
                    result.AddError($"Step '{step.stepName}': {step.GetValidationMessage()}");
            }
        }
    }

    return result;
}
```

## Factory Pattern for Sequence Creation

### TrainingSequenceFactory

Provides template and utility methods for creating common sequence patterns:

```csharp
public static class TrainingSequenceFactory
{
    /// <summary>
    /// Creates a basic HVAC leak testing program as a template
    /// </summary>
    public static TrainingProgram CreateHVACLeakTestingProgram()
    {
        var program = new TrainingProgram("HVAC Training",
            "Comprehensive HVAC system training program");

        // Create Leak Testing Module
        var leakTestingModule = new TrainingModule("Leak Testing",
            "Learn to perform AC system leak testing procedures");

        // Initial Setup Task Group
        var initialSetup = new TaskGroup("Initial Setup",
            "Remove valve caps and prepare tools");

        initialSetup.steps.Add(new InteractionStep("Remove liquid valve cap",
            InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Remove the cap from the liquid valve and place it on the table"
        });

        initialSetup.steps.Add(new InteractionStep("Remove gas valve cap",
            InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Remove the cap from the gas valve and place it on the table"
        });

        leakTestingModule.taskGroups.Add(initialSetup);
        program.modules.Add(leakTestingModule);

        return program;
    }

    /// <summary>
    /// Creates an empty program with basic structure for quick start
    /// </summary>
    public static TrainingProgram CreateEmptyProgram(string name = "New Training Program")
    {
        var program = new TrainingProgram(name);

        var module = new TrainingModule("New Module");
        var taskGroup = new TaskGroup("New Task Group");
        taskGroup.steps.Add(new InteractionStep("New Step",
            InteractionStep.StepType.GrabAndSnap));

        module.taskGroups.Add(taskGroup);
        program.modules.Add(module);

        return program;
    }
}
```

### Custom Sequence Creation

```csharp
// Manual sequence creation
var program = new TrainingProgram("Custom Training", "My custom training program");

var module = new TrainingModule("Setup", "Initial setup procedures");
var taskGroup = new TaskGroup("Tool Preparation", "Prepare required tools");

// Add steps
taskGroup.steps.Add(new InteractionStep("Get wrench", InteractionStep.StepType.Grab)
{
    targetObject = wrenchReference,
    hint = "Pick up the adjustable wrench"
});

taskGroup.steps.Add(new InteractionStep("Place wrench", InteractionStep.StepType.GrabAndSnap)
{
    targetObject = wrenchReference,
    destination = workbenchReference,
    hint = "Place wrench on workbench"
});

// Build hierarchy
module.taskGroups.Add(taskGroup);
program.modules.Add(module);
```

## ScriptableObject Integration

### TrainingSequenceAsset

Provides Unity asset integration for persistent storage:

```csharp
[CreateAssetMenu(fileName = "TrainingSequence", menuName = "VR Training/Training Sequence Asset")]
public class TrainingSequenceAsset : ScriptableObject
{
    [SerializeField]
    private TrainingProgram program;

    public TrainingProgram Program
    {
        get { return program; }
        set
        {
            program = value;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }

    public ValidationResult ValidateProgram() { /* ... */ }
}
```

### Asset Creation Workflow

1. **Create Asset**: Right-click → Create → VR Training → Training Sequence Asset
2. **Configure Program**: Use editor GUI or programmatic setup
3. **Validate**: Check for errors and warnings
4. **Save**: Unity asset system handles serialization

### Asset Loading

```csharp
// Load specific asset
var asset = Resources.Load<TrainingSequenceAsset>("MyTrainingSequence");
var program = asset.Program;

// Load all training assets
var allAssets = Resources.LoadAll<TrainingSequenceAsset>("");
```

## Runtime State Management

### Completion Tracking

```csharp
public class InteractionStep
{
    [Header("Runtime State")]
    public bool isCompleted = false;

    public void ResetCompletion()
    {
        isCompleted = false;
    }
}
```

### Progress Calculation

```csharp
public class SequenceProgress
{
    public int currentModuleIndex;
    public int totalModules;
    public int currentTaskGroupIndex;
    public int totalTaskGroups;
    public int completedSteps;
    public int totalSteps;

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

## Best Practices

### 1. **Sequence Design**

- **Logical Grouping**: Group related steps into task groups
- **Clear Naming**: Use descriptive names for all levels
- **Progressive Complexity**: Start simple, build complexity
- **Validation First**: Ensure all references are valid

### 2. **Step Dependencies**

- **Minimize Dependencies**: Reduce complexity when possible
- **Use Wait Conditions**: For sequential requirements
- **Enable Parallel Execution**: For independent steps
- **Mark Optional Steps**: For non-critical actions

### 3. **Error Handling**

- **Validate Early**: Check references before runtime
- **Provide Clear Messages**: Detailed validation feedback
- **Handle Missing Objects**: Graceful degradation
- **Test Thoroughly**: Validate complete sequences

### 4. **Performance Considerations**

- **Lazy Loading**: Load sequences when needed
- **Reference Caching**: Cache GameObject lookups
- **Memory Management**: Clean up completed sequences
- **Asset Organization**: Organize assets logically

## Integration with Other Systems

### Handler System Integration

```csharp
// Handlers check step types they can handle
public bool CanHandle(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.TightenValve ||
           stepType == InteractionStep.StepType.LoosenValve;
}
```

### Profile System Integration

```csharp
// Steps reference objects configured with profiles
var step = new InteractionStep("Turn valve", InteractionStep.StepType.TightenValve)
{
    targetObject = valveReference, // Configured with ValveProfile
    targetSocket = socketReference  // Configured with SnapProfile
};
```

### Editor Integration

The sequence system integrates with Unity's editor through custom property drawers, inspector windows, and visual editing tools for creating and managing complex training scenarios.

This hierarchical system provides the foundation for creating scalable, maintainable VR training experiences with clear organization and robust validation.
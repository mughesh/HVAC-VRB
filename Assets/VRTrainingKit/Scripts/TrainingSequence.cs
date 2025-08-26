// TrainingSequence.cs
// Core data structures for hierarchical VR training sequences
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Root level training program containing multiple modules
/// Like a book series (e.g., "HVAC Training")
/// </summary>
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
    
    public TrainingProgram()
    {
        modules = new List<TrainingModule>();
    }
    
    public TrainingProgram(string name, string desc = "")
    {
        programName = name;
        description = desc;
        modules = new List<TrainingModule>();
    }
}

/// <summary>
/// Training module within a program
/// Like a volume/book (e.g., "Leak Testing")
/// </summary>
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
    public bool isExpanded = true; // For editor tree view
    
    public TrainingModule()
    {
        taskGroups = new List<TaskGroup>();
    }
    
    public TrainingModule(string name, string desc = "")
    {
        moduleName = name;
        description = desc;
        taskGroups = new List<TaskGroup>();
    }
}

/// <summary>
/// Group of related interaction steps
/// Like a chapter (e.g., "Initial Valve Connections")
/// </summary>
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
    public bool isExpanded = true; // For editor tree view
    
    public TaskGroup()
    {
        steps = new List<InteractionStep>();
    }
    
    public TaskGroup(string name, string desc = "")
    {
        groupName = name;
        description = desc;
        steps = new List<InteractionStep>();
    }
}

/// <summary>
/// Individual interaction step within a task group
/// Like a subheading (e.g., "Remove liquid valve cap")
/// </summary>
[System.Serializable]
public class InteractionStep
{
    /// <summary>
    /// Types of interaction steps supported
    /// </summary>
    public enum StepType
    {
        Grab,              // Pick up object
        GrabAndSnap,       // Pick up and place in snap point
        TurnKnob,          // Rotate knob to specific angle
        WaitForCondition,  // Wait for previous steps
        ShowInstruction    // Display instruction to user
    }
    
    [Header("Step Information")]
    public string stepName = "New Step";
    public StepType type = StepType.GrabAndSnap;
    
    [Header("Target Objects")]
    [Tooltip("The object to interact with")]
    public GameObject targetObject;
    
    [Tooltip("For GrabAndSnap: The snap point where object should be placed")]
    public GameObject destination;
    
    [Header("Knob Settings")]
    [Tooltip("For TurnKnob: Target angle in degrees")]
    public float targetAngle = 0f;
    
    [Tooltip("Degrees of error allowed for knob completion")]
    public float angleTolerance = 5f;
    
    [Header("Execution Settings")]
    [Tooltip("Can be completed in any order with other parallel steps")]
    public bool allowParallel = false;
    
    [Tooltip("Step is not required for completion")]
    public bool isOptional = false;
    
    [Header("Wait Conditions")]
    [Tooltip("For WaitForCondition: Indices of steps in current group to wait for")]
    public List<int> waitForSteps = new List<int>();
    
    [Header("Instructions")]
    [TextArea(2, 4)]
    [Tooltip("Hint text shown to user")]
    public string hint = "";
    
    [Header("Runtime State")]
    [Tooltip("Completion state - managed by runtime controller")]
    public bool isCompleted = false;
    
    public InteractionStep()
    {
        waitForSteps = new List<int>();
    }
    
    public InteractionStep(string name, StepType stepType)
    {
        stepName = name;
        type = stepType;
        waitForSteps = new List<int>();
    }
    
    /// <summary>
    /// Validates if this step has all required references for its type
    /// </summary>
    public bool IsValid()
    {
        switch (type)
        {
            case StepType.Grab:
                return targetObject != null;
                
            case StepType.GrabAndSnap:
                return targetObject != null && destination != null;
                
            case StepType.TurnKnob:
                return targetObject != null; // targetAngle and tolerance have defaults
                
            case StepType.WaitForCondition:
                return waitForSteps.Count > 0;
                
            case StepType.ShowInstruction:
                return !string.IsNullOrEmpty(hint);
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Gets a user-friendly description of what this step requires
    /// </summary>
    public string GetValidationMessage()
    {
        if (IsValid()) return "Step is valid";
        
        switch (type)
        {
            case StepType.Grab:
                return "Missing target object";
                
            case StepType.GrabAndSnap:
                if (targetObject == null && destination == null)
                    return "Missing target object and destination";
                if (targetObject == null)
                    return "Missing target object";
                if (destination == null)
                    return "Missing destination";
                break;
                
            case StepType.TurnKnob:
                return "Missing target object (knob)";
                
            case StepType.WaitForCondition:
                return "No steps specified to wait for";
                
            case StepType.ShowInstruction:
                return "Missing instruction text";
        }
        
        return "Unknown validation error";
    }
    
    /// <summary>
    /// Resets the completion state of this step
    /// </summary>
    public void ResetCompletion()
    {
        isCompleted = false;
    }
}

/// <summary>
/// Utility class for creating common training sequence patterns
/// </summary>
public static class TrainingSequenceFactory
{
    /// <summary>
    /// Creates a basic HVAC leak testing program as a template
    /// </summary>
    public static TrainingProgram CreateHVACLeakTestingProgram()
    {
        var program = new TrainingProgram("HVAC Training", "Comprehensive HVAC system training program");
        
        // Create Leak Testing Module
        var leakTestingModule = new TrainingModule("Leak Testing", "Learn to perform AC system leak testing procedures");
        
        // Initial Setup Task Group
        var initialSetup = new TaskGroup("Initial Setup", "Remove valve caps and prepare tools");
        initialSetup.steps.Add(new InteractionStep("Remove liquid valve cap", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Remove the cap from the liquid valve and place it on the table",
            allowParallel = true
        });
        initialSetup.steps.Add(new InteractionStep("Remove gas valve cap", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Remove the cap from the gas valve and place it on the table",
            allowParallel = true
        });
        initialSetup.steps.Add(new InteractionStep("Place allen key on liquid valve", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Connect the allen key to the liquid valve",
            allowParallel = true
        });
        initialSetup.steps.Add(new InteractionStep("Place allen key on gas valve", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Connect the allen key to the gas valve",
            allowParallel = true
        });
        
        // Hose Connections Task Group
        var hoseConnections = new TaskGroup("Hose Connections", "Connect manifold hoses to the system");
        hoseConnections.steps.Add(new InteractionStep("Yellow hose to nitrogen gauge", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Connect the male end of the yellow hose to the nitrogen gauge",
            allowParallel = true
        });
        hoseConnections.steps.Add(new InteractionStep("Yellow hose to manifold", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Connect the female end of the yellow hose to the manifold",
            allowParallel = true
        });
        hoseConnections.steps.Add(new InteractionStep("Blue hose to suction valve", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Connect the male end of the blue hose to the suction valve",
            allowParallel = true
        });
        hoseConnections.steps.Add(new InteractionStep("Blue hose to manifold", InteractionStep.StepType.GrabAndSnap)
        {
            hint = "Connect the female end of the blue hose to the manifold",
            allowParallel = true
        });
        
        // Valve Operations Task Group
        var valveOperations = new TaskGroup("Valve Operations", "Operate system valves for leak testing");
        valveOperations.steps.Add(new InteractionStep("Wait for connections", InteractionStep.StepType.WaitForCondition)
        {
            hint = "All hose connections must be complete before valve operations",
            waitForSteps = new List<int> { 0, 1, 2, 3 } // Reference to hose connection steps
        });
        valveOperations.steps.Add(new InteractionStep("Turn nitrogen valve", InteractionStep.StepType.TurnKnob)
        {
            hint = "Turn the nitrogen valve 45 degrees clockwise",
            targetAngle = 45f,
            angleTolerance = 5f
        });
        valveOperations.steps.Add(new InteractionStep("Turn manifold valve", InteractionStep.StepType.TurnKnob)
        {
            hint = "Turn the manifold valve 90 degrees clockwise",
            targetAngle = 90f,
            angleTolerance = 5f
        });
        
        // Add task groups to module
        leakTestingModule.taskGroups.Add(initialSetup);
        leakTestingModule.taskGroups.Add(hoseConnections);
        leakTestingModule.taskGroups.Add(valveOperations);
        
        // Add module to program
        program.modules.Add(leakTestingModule);
        
        return program;
    }
    
    /// <summary>
    /// Creates an empty program with basic structure for quick start
    /// </summary>
    public static TrainingProgram CreateEmptyProgram(string name = "New Training Program")
    {
        var program = new TrainingProgram(name);
        
        // Add one empty module and task group to get started
        var module = new TrainingModule("New Module");
        var taskGroup = new TaskGroup("New Task Group");
        taskGroup.steps.Add(new InteractionStep("New Step", InteractionStep.StepType.GrabAndSnap));
        
        module.taskGroups.Add(taskGroup);
        program.modules.Add(module);
        
        return program;
    }
}
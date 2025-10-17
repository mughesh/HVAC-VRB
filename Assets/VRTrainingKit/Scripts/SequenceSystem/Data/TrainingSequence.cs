// TrainingSequence.cs
// Core data structures for hierarchical VR training sequences
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Safe GameObject reference that works in both runtime and asset files
/// Uses Unity's instance ID system for reliable object references that survive name changes
/// </summary>
[System.Serializable]
public class GameObjectReference
{
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private int _instanceID = 0;
    [SerializeField] private string _gameObjectName = "";
    [SerializeField] private string _scenePath = "";
    [SerializeField] private bool _isValid = false;

    /// <summary>
    /// The referenced GameObject (null if not found or invalid)
    /// </summary>
    public GameObject GameObject
    {
        get
        {
            // Try to return the direct reference first
            if (_gameObject != null)
            {
                // Verify the reference is still valid (not destroyed)
                try
                {
                    var name = _gameObject.name; // This will throw if destroyed
                    return _gameObject;
                }
                catch
                {
                    _gameObject = null;
                }
            }

            // Try to find by instance ID if we have one
            if (_instanceID != 0)
            {
                #if UNITY_EDITOR
                var found = UnityEditor.EditorUtility.InstanceIDToObject(_instanceID) as GameObject;
                #else
                var found = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(obj => obj.GetInstanceID() == _instanceID);
                #endif

                if (found != null)
                {
                    _gameObject = found;
                    _gameObjectName = found.name; // Update name cache
                    _isValid = true;
                    return found;
                }
            }

            // Fallback: try to find by name only as last resort (for backward compatibility)
            if (!string.IsNullOrEmpty(_gameObjectName))
            {
                var found = GameObject.Find(_gameObjectName);
                if (found != null)
                {
                    _gameObject = found;
                    _instanceID = found.GetInstanceID(); // Cache the instance ID
                    _isValid = true;
                    return found;
                }
            }

            _isValid = false;
            return null;
        }
        set
        {
            _gameObject = value;
            _instanceID = value != null ? value.GetInstanceID() : 0;
            _gameObjectName = value != null ? value.name : "";
            _scenePath = value != null && value.scene.IsValid() ? value.scene.path : "";
            _isValid = value != null;
        }
    }
    
    /// <summary>
    /// Whether this reference is valid and points to an existing GameObject
    /// </summary>
    public bool IsValid => GameObject != null;
    
    /// <summary>
    /// Name of the referenced GameObject (even if the reference is broken)
    /// </summary>
    public string GameObjectName => _gameObjectName;
    
    /// <summary>
    /// Scene path where the GameObject was found (for debugging)
    /// </summary>
    public string ScenePath => _scenePath;
    
    public GameObjectReference()
    {
        _gameObject = null;
        _gameObjectName = "";
        _scenePath = "";
        _isValid = false;
    }
    
    public GameObjectReference(GameObject gameObject)
    {
        GameObject = gameObject;
    }
    
    /// <summary>
    /// Implicit conversion from GameObject
    /// </summary>
    public static implicit operator GameObjectReference(GameObject gameObject)
    {
        return new GameObjectReference(gameObject);
    }
    
    /// <summary>
    /// Implicit conversion to GameObject
    /// </summary>
    public static implicit operator GameObject(GameObjectReference reference)
    {
        return reference?.GameObject;
    }
    
    /// <summary>
    /// Refreshes the reference, useful when objects may have been renamed or moved
    /// </summary>
    public void RefreshReference()
    {
        // Force a lookup through the property getter
        var current = GameObject;
        if (current != null)
        {
            // Update cached name in case it changed
            _gameObjectName = current.name;
        }
    }

    /// <summary>
    /// Returns display name for editor UI
    /// </summary>
    public override string ToString()
    {
        var obj = GameObject; // Use the property to get the latest reference
        if (obj != null)
            return obj.name;
        if (!string.IsNullOrEmpty(_gameObjectName))
            return $"{_gameObjectName} (Missing)";
        return "None";
    }
}

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

    [Header("Execution Control")]
    [Tooltip("Task Group Level Socket Restrictions: Enables all sockets in current task group, disables sockets in other task groups.")]
    public bool enforceSequentialFlow = false;

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
    [Tooltip("The object to interact with")]
    public GameObjectReference targetObject = new GameObjectReference();
    
    [Tooltip("For GrabAndSnap: The snap point where object should be placed")]
    public GameObjectReference destination = new GameObjectReference();
    
    [Header("Knob Settings")]
    [Tooltip("For TurnKnob: Target angle in degrees")]
    public float targetAngle = 0f;
    
    [Tooltip("Degrees of error allowed for knob completion")]
    public float angleTolerance = 5f;
    
    [Header("Valve Settings")]
    [Tooltip("For valve operations: Rotation axis (X=1,0,0 Y=0,1,0 Z=0,0,1)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("For TightenValve/InstallValve: Degrees of rotation required to tighten")]
    [Range(10f, 360f)]
    public float tightenThreshold = 50f;

    [Tooltip("For LoosenValve/RemoveValve: Degrees of reverse rotation required to loosen")]  
    [Range(10f, 360f)]
    public float loosenThreshold = 90f;

    [Tooltip("For valve operations: Angle completion tolerance")]
    [Range(1f, 15f)]
    public float valveAngleTolerance = 5f;

    [Tooltip("Socket for valve operations (TightenValve, LoosenValve)")]
    public GameObjectReference targetSocket = new GameObjectReference();

    [Header("Valve Advanced Settings")]
    [Tooltip("Rotation dampening/friction override (0 = use profile default)")]
    [Range(0f, 10f)]
    public float rotationDampening = 0f; // 0 means use profile default
    
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
                return targetObject != null && targetObject.IsValid;
                
            case StepType.GrabAndSnap:
                return targetObject != null && targetObject.IsValid && 
                       destination != null && destination.IsValid;
                
            case StepType.TurnKnob:
                return targetObject != null && targetObject.IsValid; // targetAngle and tolerance have defaults
                
            case StepType.TightenValve:
            case StepType.LoosenValve:
                return targetObject != null && targetObject.IsValid && 
                       targetSocket != null && targetSocket.IsValid;
                       
            case StepType.InstallValve:
            case StepType.RemoveValve:
                return targetObject != null && targetObject.IsValid && 
                       targetSocket != null && targetSocket.IsValid;
                
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
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target object";
                break;
                
            case StepType.GrabAndSnap:
                if ((targetObject == null || !targetObject.IsValid) && (destination == null || !destination.IsValid))
                    return "Missing target object and destination";
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target object";
                if (destination == null || !destination.IsValid)
                    return "Missing or invalid destination";
                break;
                
            case StepType.TurnKnob:
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target object (knob)";
                break;
                
            case StepType.TightenValve:
                if ((targetObject == null || !targetObject.IsValid) && (targetSocket == null || !targetSocket.IsValid))
                    return "Missing valve object and socket";
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target valve object";
                if (targetSocket == null || !targetSocket.IsValid)
                    return "Missing or invalid target socket";
                break;
                
            case StepType.LoosenValve:
                if ((targetObject == null || !targetObject.IsValid) && (targetSocket == null || !targetSocket.IsValid))
                    return "Missing valve object and socket";
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target valve object";
                if (targetSocket == null || !targetSocket.IsValid)
                    return "Missing or invalid target socket";
                break;
                
            case StepType.InstallValve:
                if ((targetObject == null || !targetObject.IsValid) && (targetSocket == null || !targetSocket.IsValid))
                    return "Missing valve object and socket for complete installation";
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target valve object";
                if (targetSocket == null || !targetSocket.IsValid)
                    return "Missing or invalid target socket";
                break;
                
            case StepType.RemoveValve:
                if ((targetObject == null || !targetObject.IsValid) && (targetSocket == null || !targetSocket.IsValid))
                    return "Missing valve object and socket for complete removal";
                if (targetObject == null || !targetObject.IsValid)
                    return "Missing or invalid target valve object";
                if (targetSocket == null || !targetSocket.IsValid)
                    return "Missing or invalid target socket";
                break;
                
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
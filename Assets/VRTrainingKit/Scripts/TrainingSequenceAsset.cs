// TrainingSequenceAsset.cs
// ScriptableObject for saving and loading training sequence programs
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// ScriptableObject asset for storing and managing training sequences
/// Integrates with Unity's asset system for save/load functionality
/// </summary>
[CreateAssetMenu(fileName = "TrainingSequence", menuName = "VR Training/Training Sequence Asset", order = 4)]
public class TrainingSequenceAsset : ScriptableObject
{
    [Header("Training Program Data")]
    [SerializeField]
    private TrainingProgram program;
    
    [Header("Asset Information")]
    [TextArea(2, 3)]
    public string assetDescription = "Training sequence asset created with VR Training Kit";
    
    /// <summary>
    /// Gets the training program stored in this asset
    /// </summary>
    public TrainingProgram Program
    {
        get { return program; }
        set
        {
            program = value;

            // Mark asset as dirty when program is changed
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
    
    /// <summary>
    /// Initialize the asset with default values
    /// </summary>
    private void OnEnable()
    {
        // Initialize empty program if none exists
        if (program == null)
        {
            program = new TrainingProgram();
        }
    }
    
    /// <summary>
    /// Validates the training program for common issues
    /// </summary>
    public ValidationResult ValidateProgram()
    {
        var result = new ValidationResult();
        
        if (program == null)
        {
            result.AddError("No training program assigned to this asset");
            return result;
        }
        
        if (string.IsNullOrEmpty(program.programName))
        {
            result.AddWarning("Program name is empty");
        }
        
        if (program.modules == null || program.modules.Count == 0)
        {
            result.AddWarning("Program has no modules");
            return result;
        }
        
        // Validate each module
        for (int moduleIndex = 0; moduleIndex < program.modules.Count; moduleIndex++)
        {
            var module = program.modules[moduleIndex];
            
            if (string.IsNullOrEmpty(module.moduleName))
            {
                result.AddWarning($"Module {moduleIndex} has no name");
            }
            
            if (module.taskGroups == null || module.taskGroups.Count == 0)
            {
                result.AddWarning($"Module '{module.moduleName}' has no task groups");
                continue;
            }
            
            // Validate each task group
            for (int groupIndex = 0; groupIndex < module.taskGroups.Count; groupIndex++)
            {
                var group = module.taskGroups[groupIndex];
                
                if (string.IsNullOrEmpty(group.groupName))
                {
                    result.AddWarning($"Task group {groupIndex} in module '{module.moduleName}' has no name");
                }
                
                if (group.steps == null || group.steps.Count == 0)
                {
                    result.AddWarning($"Task group '{group.groupName}' has no steps");
                    continue;
                }
                
                // Validate each step
                for (int stepIndex = 0; stepIndex < group.steps.Count; stepIndex++)
                {
                    var step = group.steps[stepIndex];
                    
                    if (string.IsNullOrEmpty(step.stepName))
                    {
                        result.AddWarning($"Step {stepIndex} in group '{group.groupName}' has no name");
                    }
                    
                    if (!step.IsValid())
                    {
                        result.AddError($"Step '{step.stepName}' in group '{group.groupName}': {step.GetValidationMessage()}");
                    }
                    
                    // Check for broken GameObject references
                    if ((step.targetObject == null || !step.targetObject.IsValid) && 
                        step.type != InteractionStep.StepType.WaitForCondition && 
                        step.type != InteractionStep.StepType.ShowInstruction)
                    {
                        string objName = step.targetObject?.GameObjectName ?? "null";
                        result.AddError($"Step '{step.stepName}' has missing/invalid target object: {objName}");
                    }
                    
                    if (step.type == InteractionStep.StepType.GrabAndSnap && 
                        (step.destination == null || !step.destination.IsValid))
                    {
                        string destName = step.destination?.GameObjectName ?? "null";
                        result.AddError($"Step '{step.stepName}' has missing/invalid destination: {destName}");
                    }
                    
                    // Validate wait conditions
                    if (step.type == InteractionStep.StepType.WaitForCondition)
                    {
                        foreach (int waitIndex in step.waitForSteps)
                        {
                            if (waitIndex < 0 || waitIndex >= group.steps.Count)
                            {
                                result.AddError($"Step '{step.stepName}' has invalid wait condition index: {waitIndex}");
                            }
                        }
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Creates a deep copy of this asset's program
    /// </summary>
    public TrainingProgram GetProgramCopy()
    {
        if (program == null) return null;
        
        // Use Unity's JsonUtility for deep copy
        string json = JsonUtility.ToJson(program);
        return JsonUtility.FromJson<TrainingProgram>(json);
    }
    
    /// <summary>
    /// Replaces the current program with a new one
    /// </summary>
    public void SetProgram(TrainingProgram newProgram)
    {
        Program = newProgram;
        
        // Mark asset as dirty for saving
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Gets summary statistics about this training program
    /// </summary>
    public ProgramStats GetStats()
    {
        var stats = new ProgramStats();
        
        if (program == null || program.modules == null)
            return stats;
        
        stats.moduleCount = program.modules.Count;
        
        foreach (var module in program.modules)
        {
            if (module.taskGroups == null) continue;
            
            stats.taskGroupCount += module.taskGroups.Count;
            
            foreach (var group in module.taskGroups)
            {
                if (group.steps == null) continue;
                
                stats.totalSteps += group.steps.Count;
                
                foreach (var step in group.steps)
                {
                    if (step.allowParallel)
                        stats.parallelSteps++;
                    if (step.isOptional)
                        stats.optionalSteps++;
                    
                    // Count by type
                    switch (step.type)
                    {
                        case InteractionStep.StepType.Grab:
                            stats.grabSteps++;
                            break;
                        case InteractionStep.StepType.GrabAndSnap:
                            stats.grabAndSnapSteps++;
                            break;
                        case InteractionStep.StepType.TurnKnob:
                            stats.knobSteps++;
                            break;
                        case InteractionStep.StepType.WaitForCondition:
                            stats.waitSteps++;
                            break;
                        case InteractionStep.StepType.ShowInstruction:
                            stats.instructionSteps++;
                            break;
                    }
                }
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// Validation result containing errors and warnings
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public System.Collections.Generic.List<string> errors = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
        
        public bool HasErrors => errors.Count > 0;
        public bool HasWarnings => warnings.Count > 0;
        public bool IsValid => !HasErrors;
        
        public void AddError(string message) => errors.Add(message);
        public void AddWarning(string message) => warnings.Add(message);
        
        public void Clear()
        {
            errors.Clear();
            warnings.Clear();
        }
    }
    
    /// <summary>
    /// Statistics about a training program
    /// </summary>
    [System.Serializable]
    public class ProgramStats
    {
        public int moduleCount;
        public int taskGroupCount;
        public int totalSteps;
        public int parallelSteps;
        public int optionalSteps;
        
        // Steps by type
        public int grabSteps;
        public int grabAndSnapSteps;
        public int knobSteps;
        public int waitSteps;
        public int instructionSteps;
        
        public override string ToString()
        {
            return $"Modules: {moduleCount}, Groups: {taskGroupCount}, Steps: {totalSteps} " +
                   $"(Grab: {grabSteps}, Snap: {grabAndSnapSteps}, Knob: {knobSteps}, " +
                   $"Wait: {waitSteps}, Instruction: {instructionSteps})";
        }
    }
}

/// <summary>
/// Utility class for managing training sequence assets
/// </summary>
public static class TrainingSequenceAssetManager
{
    /// <summary>
    /// Creates a new training sequence asset with the HVAC template
    /// </summary>
    public static TrainingSequenceAsset CreateHVACTemplateAsset()
    {
        var asset = ScriptableObject.CreateInstance<TrainingSequenceAsset>();
        asset.name = "HVAC_LeakTesting_Template";
        asset.assetDescription = "Template for HVAC leak testing procedures";
        asset.SetProgram(TrainingSequenceFactory.CreateHVACLeakTestingProgram());
        
        return asset;
    }
    
    /// <summary>
    /// Creates a new empty training sequence asset
    /// </summary>
    public static TrainingSequenceAsset CreateEmptyAsset(string assetName = "New Training Sequence")
    {
        var asset = ScriptableObject.CreateInstance<TrainingSequenceAsset>();
        asset.name = assetName;
        asset.assetDescription = "Custom training sequence";
        asset.SetProgram(TrainingSequenceFactory.CreateEmptyProgram());
        
        return asset;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Saves an asset to the VR Training Kit sequences folder
    /// </summary>
    public static void SaveAssetToSequencesFolder(TrainingSequenceAsset asset, string fileName = null)
    {
        if (fileName == null)
            fileName = asset.name + ".asset";
        
        // Ensure sequences folder exists
        string folderPath = "Assets/VRTrainingKit/Sequences";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/VRTrainingKit"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "VRTrainingKit");
            }
            UnityEditor.AssetDatabase.CreateFolder("Assets/VRTrainingKit", "Sequences");
        }
        
        string assetPath = $"{folderPath}/{fileName}";
        UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        
        // Focus the created asset
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = asset;
    }
    
    /// <summary>
    /// Loads all training sequence assets from the project
    /// </summary>
    public static TrainingSequenceAsset[] LoadAllSequenceAssets()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TrainingSequenceAsset");
        var assets = new TrainingSequenceAsset[guids.Length];
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<TrainingSequenceAsset>(path);
        }
        
        return assets;
    }
    #endif
}
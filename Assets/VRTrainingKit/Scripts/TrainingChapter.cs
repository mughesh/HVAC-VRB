using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A chapter in a training sequence containing multiple related steps
/// </summary>
[CreateAssetMenu(fileName = "TrainingChapter", menuName = "VR Training/Training Chapter")]
public class TrainingChapter : ScriptableObject
{
    [Header("Chapter Information")]
    [Tooltip("Name of this chapter (e.g., 'Hose Connection Setup')")]
    public string chapterName = "New Chapter";
    
    [Tooltip("Description of what this chapter teaches")]
    [TextArea(2, 3)]
    public string description = "Description of this training chapter";
    
    [Header("Chapter Behavior")]
    [Tooltip("Must steps be completed in order, or can they be done in any sequence?")]
    public ChapterType chapterType = ChapterType.Sequential;
    
    [Tooltip("Automatically advance to next chapter when all steps are complete")]
    public bool autoAdvance = true;
    
    [Tooltip("Show visual hints for current step")]
    public bool showHints = true;
    
    [Header("Steps")]
    [Tooltip("All steps in this chapter")]
    public List<SequenceStep> steps = new List<SequenceStep>();
    
    [Header("Chapter Status")]
    [ReadOnly]
    public bool isActive = false;
    
    [ReadOnly]
    public bool isCompleted = false;
    
    [ReadOnly]
    public int currentStepIndex = 0;
    
    public enum ChapterType
    {
        Sequential,     // Steps must be completed in order
        Parallel,       // Steps can be completed in any order  
        Optional        // Steps are optional, chapter completes when any are done
    }
    
    // Events
    public System.Action<TrainingChapter> OnChapterStarted;
    public System.Action<TrainingChapter> OnChapterCompleted;
    public System.Action<SequenceStep, int> OnStepCompleted;
    
    /// <summary>
    /// Get the currently active step (for sequential chapters)
    /// </summary>
    public SequenceStep CurrentStep
    {
        get
        {
            if (chapterType == ChapterType.Sequential && currentStepIndex < steps.Count)
                return steps[currentStepIndex];
            return null;
        }
    }
    
    /// <summary>
    /// Get all steps that should currently be active
    /// </summary>
    public List<SequenceStep> ActiveSteps
    {
        get
        {
            var activeSteps = new List<SequenceStep>();
            
            switch (chapterType)
            {
                case ChapterType.Sequential:
                    // Only current step is active
                    if (CurrentStep != null && !CurrentStep.isCompleted)
                        activeSteps.Add(CurrentStep);
                    break;
                    
                case ChapterType.Parallel:
                    // All incomplete steps are active
                    activeSteps.AddRange(steps.Where(step => !step.isCompleted));
                    break;
                    
                case ChapterType.Optional:
                    // All incomplete steps are active
                    activeSteps.AddRange(steps.Where(step => !step.isCompleted));
                    break;
            }
            
            return activeSteps;
        }
    }
    
    /// <summary>
    /// Start this chapter
    /// </summary>
    public void StartChapter()
    {
        isActive = true;
        isCompleted = false;
        currentStepIndex = 0;
        
        // Reset all steps
        foreach (var step in steps)
        {
            step.ResetStep();
        }
        
        Debug.Log($"[Training] Started chapter: {chapterName}");
        OnChapterStarted?.Invoke(this);
    }
    
    /// <summary>
    /// Update the completion status of all steps and the chapter
    /// </summary>
    public void UpdateProgress()
    {
        if (!isActive || isCompleted) return;
        
        // Update all step completions
        foreach (var step in steps)
        {
            bool wasCompleted = step.isCompleted;
            step.UpdateCompletion();
            
            // Fire step completion event
            if (!wasCompleted && step.isCompleted)
            {
                int stepIndex = steps.IndexOf(step);
                OnStepCompleted?.Invoke(step, stepIndex);
                Debug.Log($"[Training] Step completed: {step.stepName} in {chapterName}");
                
                // For sequential chapters, advance to next step
                if (chapterType == ChapterType.Sequential)
                {
                    currentStepIndex = Mathf.Min(currentStepIndex + 1, steps.Count);
                }
            }
        }
        
        // Check if chapter is complete
        CheckChapterCompletion();
    }
    
    private void CheckChapterCompletion()
    {
        bool chapterComplete = false;
        
        switch (chapterType)
        {
            case ChapterType.Sequential:
            case ChapterType.Parallel:
                // All steps must be completed
                chapterComplete = steps.All(step => step.isCompleted);
                break;
                
            case ChapterType.Optional:
                // At least one step must be completed
                chapterComplete = steps.Any(step => step.isCompleted);
                break;
        }
        
        if (chapterComplete && !isCompleted)
        {
            CompleteChapter();
        }
    }
    
    private void CompleteChapter()
    {
        isCompleted = true;
        isActive = false;
        
        Debug.Log($"[Training] Chapter completed: {chapterName}");
        OnChapterCompleted?.Invoke(this);
    }
    
    /// <summary>
    /// Get completion percentage (0-1)
    /// </summary>
    public float GetCompletionPercentage()
    {
        if (steps.Count == 0) return 1f;
        
        int completedSteps = steps.Count(step => step.isCompleted);
        return (float)completedSteps / steps.Count;
    }
    
    /// <summary>
    /// Get objects that should be unlocked/available for interaction
    /// </summary>
    public List<GameObject> GetUnlockedObjects()
    {
        var unlockedObjects = new List<GameObject>();
        
        foreach (var step in ActiveSteps)
        {
            if (step.requiredObject != null)
                unlockedObjects.Add(step.requiredObject);
            if (step.secondaryObject != null)
                unlockedObjects.Add(step.secondaryObject);
        }
        
        return unlockedObjects.Distinct().ToList();
    }
    
    /// <summary>
    /// Check if a specific object should be locked based on chapter state
    /// </summary>
    public bool IsObjectLocked(GameObject obj)
    {
        if (!isActive) return true;
        
        // Object is unlocked if it's part of any active step
        var unlockedObjects = GetUnlockedObjects();
        return !unlockedObjects.Contains(obj);
    }
    
    /// <summary>
    /// Get current instruction text for UI display
    /// </summary>
    public string GetCurrentInstruction()
    {
        if (!isActive || isCompleted)
            return "Chapter completed!";
            
        switch (chapterType)
        {
            case ChapterType.Sequential:
                return CurrentStep?.instruction ?? "All steps completed!";
                
            case ChapterType.Parallel:
                var incompleteSteps = steps.Where(s => !s.isCompleted).ToList();
                if (incompleteSteps.Count == 0)
                    return "All steps completed!";
                else if (incompleteSteps.Count == 1)
                    return incompleteSteps[0].instruction;
                else
                    return $"Complete {incompleteSteps.Count} remaining steps in any order";
                    
            case ChapterType.Optional:
                var optionalSteps = steps.Where(s => !s.isCompleted).ToList();
                if (optionalSteps.Count == 0)
                    return "Chapter completed!";
                else
                    return $"Complete any of the {optionalSteps.Count} available steps";
                    
            default:
                return "";
        }
    }
    
    /// <summary>
    /// Reset chapter to initial state
    /// </summary>
    public void ResetChapter()
    {
        isActive = false;
        isCompleted = false;
        currentStepIndex = 0;
        
        foreach (var step in steps)
        {
            step.ResetStep();
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Validate this chapter in editor
    /// </summary>
    public List<string> ValidateChapter()
    {
        var issues = new List<string>();
        
        if (string.IsNullOrEmpty(chapterName))
            issues.Add("Chapter name is empty");
            
        if (steps.Count == 0)
            issues.Add("Chapter has no steps");
            
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step == null)
            {
                issues.Add($"Step {i} is null");
                continue;
            }
            
            if (string.IsNullOrEmpty(step.stepName))
                issues.Add($"Step {i} has no name");
                
            if (step.requiredObject == null)
                issues.Add($"Step '{step.stepName}' has no required object");
                
            if (step.requirementType == SequenceStep.RequirementType.MustBeSnapped && 
                step.secondaryObject == null)
                issues.Add($"Step '{step.stepName}' requires snapping but has no snap point");
        }
        
        return issues;
    }
    #endif
}
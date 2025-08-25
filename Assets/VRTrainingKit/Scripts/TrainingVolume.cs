using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Complete training procedure containing multiple chapters
/// Top-level ScriptableObject for training sequences
/// </summary>
[CreateAssetMenu(fileName = "TrainingVolume", menuName = "VR Training/Training Volume")]
public class TrainingVolume : ScriptableObject
{
    [Header("Volume Information")]
    [Tooltip("Name of this training procedure (e.g., 'AC Leak Testing Procedure')")]
    public string volumeName = "New Training Volume";
    
    [Tooltip("Detailed description of what this training covers")]
    [TextArea(3, 5)]
    public string description = "Complete training procedure description";
    
    [Tooltip("Industry or domain this training applies to")]
    public string category = "HVAC";
    
    [Tooltip("Estimated completion time in minutes")]
    public int estimatedDurationMinutes = 30;
    
    [Header("Volume Settings")]
    [Tooltip("Allow trainees to make mistakes and learn from them")]
    public bool allowMistakes = true;
    
    [Tooltip("Show visual hints and guidance")]
    public bool showHints = true;
    
    [Tooltip("Automatically advance between chapters")]
    public bool autoAdvanceChapters = true;
    
    [Tooltip("Reset volume when trainee makes critical mistakes")]
    public bool resetOnCriticalError = false;
    
    [Header("Chapters")]
    [Tooltip("All chapters in this training volume, in order")]
    public List<TrainingChapter> chapters = new List<TrainingChapter>();
    
    [Header("Volume Status")]
    [ReadOnly]
    public bool isActive = false;
    
    [ReadOnly]
    public bool isCompleted = false;
    
    [ReadOnly]
    public int currentChapterIndex = 0;
    
    [ReadOnly]
    public float startTime = 0f;
    
    // Events
    public System.Action<TrainingVolume> OnVolumeStarted;
    public System.Action<TrainingVolume> OnVolumeCompleted;
    public System.Action<TrainingChapter, int> OnChapterChanged;
    public System.Action<string> OnMistakeMade;
    
    /// <summary>
    /// Currently active chapter
    /// </summary>
    public TrainingChapter CurrentChapter
    {
        get
        {
            if (currentChapterIndex >= 0 && currentChapterIndex < chapters.Count)
                return chapters[currentChapterIndex];
            return null;
        }
    }
    
    /// <summary>
    /// Get all objects that should currently be unlocked
    /// </summary>
    public List<GameObject> GetCurrentlyUnlockedObjects()
    {
        if (CurrentChapter != null && CurrentChapter.isActive)
            return CurrentChapter.GetUnlockedObjects();
        return new List<GameObject>();
    }
    
    /// <summary>
    /// Start the training volume
    /// </summary>
    public void StartVolume()
    {
        if (chapters.Count == 0)
        {
            Debug.LogError($"Cannot start volume '{volumeName}' - no chapters defined");
            return;
        }
        
        isActive = true;
        isCompleted = false;
        currentChapterIndex = 0;
        startTime = Time.time;
        
        // Subscribe to chapter events
        foreach (var chapter in chapters)
        {
            chapter.OnChapterCompleted += OnChapterCompleted;
        }
        
        // Start first chapter
        StartCurrentChapter();
        
        Debug.Log($"[Training] Started volume: {volumeName}");
        OnVolumeStarted?.Invoke(this);
    }
    
    private void StartCurrentChapter()
    {
        if (CurrentChapter != null)
        {
            CurrentChapter.StartChapter();
            OnChapterChanged?.Invoke(CurrentChapter, currentChapterIndex);
            Debug.Log($"[Training] Started chapter {currentChapterIndex + 1}/{chapters.Count}: {CurrentChapter.chapterName}");
        }
    }
    
    private void OnChapterCompleted(TrainingChapter completedChapter)
    {
        Debug.Log($"[Training] Chapter completed: {completedChapter.chapterName}");
        
        // Move to next chapter if auto-advance is enabled
        if (autoAdvanceChapters && currentChapterIndex < chapters.Count - 1)
        {
            currentChapterIndex++;
            StartCurrentChapter();
        }
        else if (currentChapterIndex >= chapters.Count - 1)
        {
            // All chapters completed
            CompleteVolume();
        }
    }
    
    private void CompleteVolume()
    {
        isCompleted = true;
        isActive = false;
        
        float completionTime = Time.time - startTime;
        Debug.Log($"[Training] Volume completed: {volumeName} in {completionTime:F1} seconds");
        
        // Unsubscribe from events
        foreach (var chapter in chapters)
        {
            chapter.OnChapterCompleted -= OnChapterCompleted;
        }
        
        OnVolumeCompleted?.Invoke(this);
    }
    
    /// <summary>
    /// Update progress for the current chapter
    /// Call this regularly (e.g., from SequenceController)
    /// </summary>
    public void UpdateProgress()
    {
        if (!isActive || isCompleted) return;
        
        CurrentChapter?.UpdateProgress();
    }
    
    /// <summary>
    /// Check if a specific object should be locked
    /// </summary>
    public bool IsObjectLocked(GameObject obj)
    {
        if (!isActive) return true;
        
        return CurrentChapter?.IsObjectLocked(obj) ?? true;
    }
    
    /// <summary>
    /// Handle when a mistake is made
    /// </summary>
    public void OnMistakeMadeInternal(string mistakeDescription)
    {
        Debug.LogWarning($"[Training] Mistake made: {mistakeDescription}");
        OnMistakeMade?.Invoke(mistakeDescription);
        
        if (resetOnCriticalError && mistakeDescription.Contains("CRITICAL"))
        {
            ResetVolume();
        }
    }
    
    /// <summary>
    /// Get current instruction for UI display
    /// </summary>
    public string GetCurrentInstruction()
    {
        if (!isActive)
            return "Training not started";
            
        if (isCompleted)
            return "Training completed!";
            
        return CurrentChapter?.GetCurrentInstruction() ?? "No active chapter";
    }
    
    /// <summary>
    /// Get overall completion percentage (0-1)
    /// </summary>
    public float GetOverallCompletionPercentage()
    {
        if (chapters.Count == 0) return 1f;
        
        float totalCompletion = 0f;
        foreach (var chapter in chapters)
        {
            totalCompletion += chapter.GetCompletionPercentage();
        }
        
        return totalCompletion / chapters.Count;
    }
    
    /// <summary>
    /// Get current chapter progress info for UI
    /// </summary>
    public string GetProgressText()
    {
        if (!isActive) return "Not started";
        if (isCompleted) return "Completed";
        
        float chapterProgress = CurrentChapter?.GetCompletionPercentage() ?? 0f;
        return $"Chapter {currentChapterIndex + 1}/{chapters.Count}: {chapterProgress:P0} complete";
    }
    
    /// <summary>
    /// Skip to a specific chapter (for testing/debugging)
    /// </summary>
    public void SkipToChapter(int chapterIndex)
    {
        if (chapterIndex >= 0 && chapterIndex < chapters.Count)
        {
            // End current chapter
            if (CurrentChapter != null && CurrentChapter.isActive)
            {
                CurrentChapter.ResetChapter();
            }
            
            currentChapterIndex = chapterIndex;
            StartCurrentChapter();
            
            Debug.Log($"[Training] Skipped to chapter {chapterIndex + 1}: {CurrentChapter.chapterName}");
        }
    }
    
    /// <summary>
    /// Reset the entire volume to starting state
    /// </summary>
    public void ResetVolume()
    {
        isActive = false;
        isCompleted = false;
        currentChapterIndex = 0;
        startTime = 0f;
        
        // Reset all chapters
        foreach (var chapter in chapters)
        {
            chapter.ResetChapter();
            chapter.OnChapterCompleted -= OnChapterCompleted;
        }
        
        Debug.Log($"[Training] Volume reset: {volumeName}");
    }
    
    /// <summary>
    /// Get summary of volume statistics
    /// </summary>
    public TrainingStatistics GetStatistics()
    {
        var stats = new TrainingStatistics();
        stats.volumeName = volumeName;
        stats.totalChapters = chapters.Count;
        stats.completedChapters = chapters.Count(c => c.isCompleted);
        stats.overallCompletion = GetOverallCompletionPercentage();
        stats.estimatedDuration = estimatedDurationMinutes;
        
        if (isActive)
            stats.currentSessionDuration = (Time.time - startTime) / 60f; // in minutes
            
        return stats;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Validate this volume for common issues
    /// </summary>
    public List<string> ValidateVolume()
    {
        var issues = new List<string>();
        
        if (string.IsNullOrEmpty(volumeName))
            issues.Add("Volume name is empty");
            
        if (chapters.Count == 0)
            issues.Add("Volume has no chapters");
            
        for (int i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            if (chapter == null)
            {
                issues.Add($"Chapter {i} is null");
                continue;
            }
            
            var chapterIssues = chapter.ValidateChapter();
            foreach (var issue in chapterIssues)
            {
                issues.Add($"Chapter '{chapter.chapterName}': {issue}");
            }
        }
        
        if (estimatedDurationMinutes <= 0)
            issues.Add("Estimated duration should be greater than 0");
            
        return issues;
    }
    
    /// <summary>
    /// Create a test volume with sample AC leak testing chapters
    /// </summary>
    [ContextMenu("Create Sample AC Volume")]
    public void CreateSampleACVolume()
    {
        volumeName = "AC Leak Testing Procedure";
        description = "Complete HVAC leak testing procedure including hose connections, pressure testing, and leak detection.";
        category = "HVAC";
        estimatedDurationMinutes = 15;
        allowMistakes = true;
        showHints = true;
        
        // Note: This would need actual chapter assets to work
        // chapters.Add(sample chapters here);
        
        Debug.Log("Sample AC volume template created. Add chapter assets to complete setup.");
    }
    #endif
}

/// <summary>
/// Statistics and progress data for a training volume
/// </summary>
[System.Serializable]
public class TrainingStatistics
{
    public string volumeName;
    public int totalChapters;
    public int completedChapters;
    public float overallCompletion;
    public int estimatedDuration; // minutes
    public float currentSessionDuration; // minutes
    public System.DateTime lastAccessed;
}
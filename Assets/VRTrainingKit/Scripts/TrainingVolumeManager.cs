using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages training volume execution and integrates with existing XRI events
/// Bridge between the new TrainingVolume system and existing interaction events
/// </summary>
public class TrainingVolumeManager : MonoBehaviour
{
    [Header("Training Configuration")]
    [Tooltip("The training volume to execute")]
    public TrainingVolume currentVolume;
    
    [Tooltip("Start training automatically when scene loads")]
    public bool autoStartTraining = false;
    
    [Tooltip("Show debug information in console")]
    public bool enableDebugLogging = true;
    
    [Header("Integration")]
    [Tooltip("Legacy sequence controller (will be replaced by volume system)")]
    public SequenceController legacySequenceController;
    
    [Header("Runtime Status")]
    [ReadOnly]
    public bool isTrainingActive = false;
    
    [ReadOnly]
    public string currentChapterName = "";
    
    [ReadOnly]
    public string currentInstruction = "";
    
    [ReadOnly]
    public float completionPercentage = 0f;
    
    // Cached references for performance
    private Dictionary<GameObject, KnobController> knobControllers = new Dictionary<GameObject, KnobController>();
    private Dictionary<GameObject, SnapValidator> snapValidators = new Dictionary<GameObject, SnapValidator>();
    
    // Events
    public System.Action<TrainingVolume> OnTrainingStarted;
    public System.Action<TrainingVolume> OnTrainingCompleted;
    public System.Action<TrainingChapter> OnChapterChanged;
    public System.Action<string> OnInstructionChanged;
    
    private void Start()
    {
        InitializeSystem();
        
        if (autoStartTraining && currentVolume != null)
        {
            StartTraining();
        }
    }
    
    private void Update()
    {
        if (isTrainingActive && currentVolume != null)
        {
            // Update volume progress
            currentVolume.UpdateProgress();
            
            // Update UI info
            UpdateRuntimeStatus();
        }
    }
    
    /// <summary>
    /// Initialize the training system and connect to existing XRI events
    /// </summary>
    private void InitializeSystem()
    {
        DebugLog("Initializing Training Volume Manager...");
        
        // Cache all knob controllers and connect to their events
        var allKnobControllers = FindObjectsOfType<KnobController>();
        foreach (var knob in allKnobControllers)
        {
            knobControllers[knob.gameObject] = knob;
            
            // Subscribe to knob events
            knob.OnAngleChanged += (angle) => OnKnobInteraction(knob.gameObject, angle);
            
            DebugLog($"Connected to knob: {knob.name}");
        }
        
        // Cache all snap validators and connect to their events
        var allSnapValidators = FindObjectsOfType<SnapValidator>();
        foreach (var validator in allSnapValidators)
        {
            snapValidators[validator.gameObject] = validator;
            DebugLog($"Found snap validator: {validator.name}");
        }
        
        // Subscribe to sequence controller events if it exists (for backward compatibility)
        if (legacySequenceController != null)
        {
            legacySequenceController.OnActionWarning += OnLegacyActionWarning;
            DebugLog("Connected to legacy sequence controller events");
        }
        
        DebugLog($"Training system initialized. Found {knobControllers.Count} knobs, {snapValidators.Count} snap points");
    }
    
    /// <summary>
    /// Start the training volume
    /// </summary>
    public void StartTraining()
    {
        if (currentVolume == null)
        {
            Debug.LogError("[Training] Cannot start training - no volume assigned!");
            return;
        }
        
        DebugLog($"Starting training volume: {currentVolume.volumeName}");
        
        // Subscribe to volume events
        currentVolume.OnVolumeStarted += OnVolumeStarted;
        currentVolume.OnVolumeCompleted += OnVolumeCompleted;
        currentVolume.OnChapterChanged += OnVolumeChapterChanged;
        
        // Start the volume
        currentVolume.StartVolume();
        isTrainingActive = true;
        
        OnTrainingStarted?.Invoke(currentVolume);
    }
    
    /// <summary>
    /// Stop the current training
    /// </summary>
    public void StopTraining()
    {
        if (currentVolume != null)
        {
            currentVolume.ResetVolume();
            
            // Unsubscribe from events
            currentVolume.OnVolumeStarted -= OnVolumeStarted;
            currentVolume.OnVolumeCompleted -= OnVolumeCompleted;
            currentVolume.OnChapterChanged -= OnVolumeChapterChanged;
        }
        
        isTrainingActive = false;
        DebugLog("Training stopped");
    }
    
    /// <summary>
    /// Handle knob interaction events from existing KnobController
    /// </summary>
    private void OnKnobInteraction(GameObject knob, float angle)
    {
        if (!isTrainingActive || currentVolume == null) return;
        
        DebugLog($"Knob interaction: {knob.name} -> {angle:F1}°");
        
        // Check if this knob interaction should be blocked
        if (currentVolume.IsObjectLocked(knob))
        {
            DebugLog($"WARNING: Knob {knob.name} is locked in current training state!");
            currentVolume.OnMistakeMadeInternal($"Attempted to use {knob.name} before required prerequisites");
        }
    }
    
    /// <summary>
    /// Handle snap events (called by SnapValidator)
    /// </summary>
    public void OnSnapEvent(GameObject snapPoint, GameObject snappedObject, bool isSnapped)
    {
        if (!isTrainingActive || currentVolume == null) return;
        
        string action = isSnapped ? "snapped" : "unsnapped";
        DebugLog($"Snap event: {snappedObject.name} {action} to {snapPoint.name}");
        
        // Check if this snap should be blocked
        if (isSnapped && currentVolume.IsObjectLocked(snappedObject))
        {
            DebugLog($"WARNING: Object {snappedObject.name} should not be snapped yet!");
            currentVolume.OnMistakeMadeInternal($"Connected {snappedObject.name} out of sequence");
        }
    }
    
    /// <summary>
    /// Update runtime status information
    /// </summary>
    private void UpdateRuntimeStatus()
    {
        if (currentVolume == null) return;
        
        // Update completion percentage
        completionPercentage = currentVolume.GetOverallCompletionPercentage();
        
        // Update current chapter info
        var currentChapter = currentVolume.CurrentChapter;
        currentChapterName = currentChapter?.chapterName ?? "No active chapter";
        
        // Update current instruction
        string newInstruction = currentVolume.GetCurrentInstruction();
        if (newInstruction != currentInstruction)
        {
            currentInstruction = newInstruction;
            OnInstructionChanged?.Invoke(currentInstruction);
        }
    }
    
    /// <summary>
    /// Called when volume starts
    /// </summary>
    private void OnVolumeStarted(TrainingVolume volume)
    {
        DebugLog($"Training volume started: {volume.volumeName}");
        
        // Disable legacy sequence controller if it exists
        if (legacySequenceController != null)
        {
            legacySequenceController.enabled = false;
            DebugLog("Disabled legacy sequence controller");
        }
    }
    
    /// <summary>
    /// Called when volume completes
    /// </summary>
    private void OnVolumeCompleted(TrainingVolume volume)
    {
        DebugLog($"Training volume completed: {volume.volumeName}");
        isTrainingActive = false;
        
        OnTrainingCompleted?.Invoke(volume);
        
        // Show completion stats
        var stats = volume.GetStatistics();
        DebugLog($"Training Stats - Chapters: {stats.completedChapters}/{stats.totalChapters}, Time: {stats.currentSessionDuration:F1}min");
    }
    
    /// <summary>
    /// Called when chapter changes
    /// </summary>
    private void OnVolumeChapterChanged(TrainingChapter chapter, int chapterIndex)
    {
        DebugLog($"Chapter changed: {chapter.chapterName} (Chapter {chapterIndex + 1})");
        OnChapterChanged?.Invoke(chapter);
        
        // Update visual feedback for newly unlocked objects
        UpdateObjectVisualFeedback();
    }
    
    /// <summary>
    /// Update visual feedback on all interactable objects
    /// </summary>
    private void UpdateObjectVisualFeedback()
    {
        if (currentVolume == null) return;
        
        var unlockedObjects = currentVolume.GetCurrentlyUnlockedObjects();
        
        // Update all sequence validators
        var validators = FindObjectsOfType<SequenceValidator>();
        foreach (var validator in validators)
        {
            // This would trigger the validator to re-check its state
            validator.SendMessage("CheckSequenceRequirements", SendMessageOptions.DontRequireReceiver);
        }
        
        DebugLog($"Updated visual feedback for {unlockedObjects.Count} unlocked objects");
    }
    
    /// <summary>
    /// Handle legacy sequence controller warnings (backward compatibility)
    /// </summary>
    private void OnLegacyActionWarning(string warning)
    {
        DebugLog($"Legacy system warning: {warning}");
    }
    
    /// <summary>
    /// Get current training statistics
    /// </summary>
    public TrainingStatistics GetCurrentStats()
    {
        return currentVolume?.GetStatistics() ?? new TrainingStatistics();
    }
    
    /// <summary>
    /// Skip to specific chapter (for testing/debugging)
    /// </summary>
    public void SkipToChapter(int chapterIndex)
    {
        if (currentVolume != null && isTrainingActive)
        {
            currentVolume.SkipToChapter(chapterIndex);
            DebugLog($"Skipped to chapter {chapterIndex}");
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[TrainingManager] {message}");
        }
    }
    
    /// <summary>
    /// GUI for testing and debugging
    /// </summary>
    private void OnGUI()
    {
        if (!isTrainingActive || currentVolume == null) return;
        
        // Simple debug UI
        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 150));
        GUILayout.Box("Training Progress");
        
        GUILayout.Label($"Volume: {currentVolume.volumeName}");
        GUILayout.Label($"Chapter: {currentChapterName}");
        GUILayout.Label($"Progress: {completionPercentage:P0}");
        GUILayout.Label($"Instruction: {currentInstruction}", GUI.skin.label);
        
        if (GUILayout.Button("Stop Training"))
        {
            StopTraining();
        }
        
        GUILayout.EndArea();
    }
}


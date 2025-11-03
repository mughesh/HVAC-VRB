using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scene-based registry for arrow references that actually persists!
/// This MonoBehaviour lives in the scene and CAN save scene GameObject references.
/// This solves the ScriptableObject limitation where scene refs are stripped on save.
/// </summary>
public class SequenceArrowRegistry : MonoBehaviour
{
    [System.Serializable]
    public class ArrowMapping
    {
        [Tooltip("Full path: ModuleName/TaskGroupName/StepName")]
        public string stepPath;

        [Header("Target Object Reference")]
        [Tooltip("The target object for this step (e.g., valve, hose)")]
        public GameObject targetObject;

        [Tooltip("Destination object for GrabAndSnap steps")]
        public GameObject destinationObject;

        [Header("Arrow References")]
        [Tooltip("Arrow to show when step starts (points to target object)")]
        public GameObject targetArrow;

        [Tooltip("Arrow to show after grab (points to destination)")]
        public GameObject destinationArrow;

        [Header("Arrow Behavior")]
        [Tooltip("Hide target arrow after grabbing object")]
        public bool hideTargetArrowAfterGrab = true;

        [Tooltip("Show destination arrow after grabbing object")]
        public bool showDestinationAfterGrab = true;
    }

    [Header("Sequence Reference")]
    [Tooltip("The Training Sequence this registry is for")]
    public TrainingSequenceAsset sequenceAsset;

    [Header("Arrow Mappings")]
    [Tooltip("Maps step paths to arrow GameObjects - THIS IS WHERE ARROWS ARE STORED")]
    public List<ArrowMapping> arrowMappings = new List<ArrowMapping>();

    [Header("Auto-Sync Settings")]
    [Tooltip("Automatically sync from sequence asset when entering play mode")]
    public bool autoSyncOnPlayMode = true;

    [Tooltip("Tag to search for arrow GameObjects (e.g., 'Arrow')")]
    public string arrowTag = "Arrow";

    // Singleton pattern for easy access
    private static SequenceArrowRegistry _instance;
    public static SequenceArrowRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SequenceArrowRegistry>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Set singleton
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"Multiple SequenceArrowRegistry instances found! Using the first one.");
        }

#if UNITY_EDITOR
        // Auto-sync when entering play mode
        if (autoSyncOnPlayMode && !Application.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("🔄 Auto-syncing arrow registry before play mode...");
            SyncFromSequenceAsset();
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Optional: Auto-sync when inspector values change
        // Uncomment if you want automatic sync on any change
        // if (autoSyncOnPlayMode && sequenceAsset != null)
        // {
        //     SyncFromSequenceAsset();
        // }
    }
#endif

    /// <summary>
    /// Gets arrow mapping for a specific step
    /// </summary>
    public ArrowMapping GetMapping(string moduleName, string taskGroupName, string stepName)
    {
        string stepPath = $"{moduleName}/{taskGroupName}/{stepName}";
        return arrowMappings.FirstOrDefault(m => m.stepPath == stepPath);
    }

    /// <summary>
    /// Gets arrow mapping by direct path
    /// </summary>
    public ArrowMapping GetMappingByPath(string stepPath)
    {
        return arrowMappings.FirstOrDefault(m => m.stepPath == stepPath);
    }

    /// <summary>
    /// Gets target object for a step
    /// </summary>
    public GameObject GetTargetObject(string moduleName, string taskGroupName, string stepName)
    {
        var mapping = GetMapping(moduleName, taskGroupName, stepName);
        return mapping?.targetObject;
    }

    /// <summary>
    /// Gets destination object for a step
    /// </summary>
    public GameObject GetDestinationObject(string moduleName, string taskGroupName, string stepName)
    {
        var mapping = GetMapping(moduleName, taskGroupName, stepName);
        return mapping?.destinationObject;
    }

    /// <summary>
    /// Gets target arrow for a step
    /// </summary>
    public GameObject GetTargetArrow(string moduleName, string taskGroupName, string stepName)
    {
        var mapping = GetMapping(moduleName, taskGroupName, stepName);
        return mapping?.targetArrow;
    }

    /// <summary>
    /// Gets destination arrow for a step
    /// </summary>
    public GameObject GetDestinationArrow(string moduleName, string taskGroupName, string stepName)
    {
        var mapping = GetMapping(moduleName, taskGroupName, stepName);
        return mapping?.destinationArrow;
    }

    /// <summary>
    /// Checks if target arrow should be hidden after grab
    /// </summary>
    public bool ShouldHideTargetAfterGrab(string moduleName, string taskGroupName, string stepName)
    {
        var mapping = GetMapping(moduleName, taskGroupName, stepName);
        return mapping?.hideTargetArrowAfterGrab ?? true;
    }

    /// <summary>
    /// Checks if destination arrow should be shown after grab
    /// </summary>
    public bool ShouldShowDestinationAfterGrab(string moduleName, string taskGroupName, string stepName)
    {
        var mapping = GetMapping(moduleName, taskGroupName, stepName);
        return mapping?.showDestinationAfterGrab ?? true;
    }

    /// <summary>
    /// Adds or updates a mapping
    /// </summary>
    public void SetMapping(string moduleName, string taskGroupName, string stepName,
        GameObject targetArrow, GameObject destinationArrow,
        bool hideTargetAfterGrab = true, bool showDestAfterGrab = true)
    {
        string stepPath = $"{moduleName}/{taskGroupName}/{stepName}";

        var existing = arrowMappings.FirstOrDefault(m => m.stepPath == stepPath);
        if (existing != null)
        {
            // Update existing
            existing.targetArrow = targetArrow;
            existing.destinationArrow = destinationArrow;
            existing.hideTargetArrowAfterGrab = hideTargetAfterGrab;
            existing.showDestinationAfterGrab = showDestAfterGrab;
        }
        else
        {
            // Add new
            arrowMappings.Add(new ArrowMapping
            {
                stepPath = stepPath,
                targetArrow = targetArrow,
                destinationArrow = destinationArrow,
                hideTargetArrowAfterGrab = hideTargetAfterGrab,
                showDestinationAfterGrab = showDestAfterGrab
            });
        }
    }

    /// <summary>
    /// Removes a mapping
    /// </summary>
    public void RemoveMapping(string moduleName, string taskGroupName, string stepName)
    {
        string stepPath = $"{moduleName}/{taskGroupName}/{stepName}";
        arrowMappings.RemoveAll(m => m.stepPath == stepPath);
    }

    /// <summary>
    /// Clears all mappings
    /// </summary>
    public void ClearAllMappings()
    {
        arrowMappings.Clear();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Generates step paths from the linked sequence asset
    /// </summary>
    public List<string> GenerateStepPaths()
    {
        var paths = new List<string>();

        if (sequenceAsset == null || sequenceAsset.Program == null)
        {
            Debug.LogWarning("No sequence asset linked to generate paths from!");
            return paths;
        }

        foreach (var module in sequenceAsset.Program.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    string stepPath = $"{module.moduleName}/{taskGroup.groupName}/{step.stepName}";
                    paths.Add(stepPath);
                }
            }
        }

        return paths;
    }

    /// <summary>
    /// AUTOMATIC SYNC: Reads all GameObject references from sequence asset and stores them in registry.
    /// This is the KEY METHOD that solves the persistence problem!
    /// Call this after editing references in the Sequence Builder UI.
    /// </summary>
    public int SyncFromSequenceAsset()
    {
        if (sequenceAsset == null || sequenceAsset.Program == null)
        {
            Debug.LogError("Cannot sync: No sequence asset assigned!");
            return 0;
        }

        int synced = 0;
        int nullRefs = 0;
        int totalSteps = 0;

        Debug.Log("🔄 Starting sync from sequence asset...");

        foreach (var module in sequenceAsset.Program.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    totalSteps++;
                    string stepPath = $"{module.moduleName}/{taskGroup.groupName}/{step.stepName}";

                    // Find or create mapping
                    var mapping = arrowMappings.FirstOrDefault(m => m.stepPath == stepPath);
                    if (mapping == null)
                    {
                        mapping = new ArrowMapping { stepPath = stepPath };
                        arrowMappings.Add(mapping);
                    }

                    // Sync target object (read from GameObjectReference)
                    GameObject targetObj = step.targetObject?.GameObject;
                    if (targetObj != null)
                    {
                        mapping.targetObject = targetObj;
                        synced++;
                        Debug.Log($"  ✓ Synced target object: {targetObj.name} → {stepPath}");
                    }
                    else if (step.targetObject != null && !string.IsNullOrEmpty(step.targetObject.GameObjectName))
                    {
                        nullRefs++;
                        Debug.LogWarning($"  ⚠ Could not resolve target object '{step.targetObject.GameObjectName}' for {stepPath}");
                    }

                    // Sync destination object
                    GameObject destObj = step.destination?.GameObject;
                    if (destObj != null)
                    {
                        mapping.destinationObject = destObj;
                        synced++;
                        Debug.Log($"  ✓ Synced destination: {destObj.name} → {stepPath}");
                    }
                    else if (step.destination != null && !string.IsNullOrEmpty(step.destination.GameObjectName))
                    {
                        nullRefs++;
                        Debug.LogWarning($"  ⚠ Could not resolve destination '{step.destination.GameObjectName}' for {stepPath}");
                    }

                    // Sync target arrow
                    GameObject targetArrowObj = step.targetArrow?.GameObject;
                    if (targetArrowObj != null)
                    {
                        mapping.targetArrow = targetArrowObj;
                        synced++;
                        Debug.Log($"  ✓ Synced target arrow: {targetArrowObj.name} → {stepPath}");
                    }
                    else if (step.targetArrow != null && !string.IsNullOrEmpty(step.targetArrow.GameObjectName))
                    {
                        nullRefs++;
                        Debug.LogWarning($"  ⚠ Could not resolve target arrow '{step.targetArrow.GameObjectName}' for {stepPath}");
                    }

                    // Sync destination arrow
                    GameObject destArrowObj = step.destinationArrow?.GameObject;
                    if (destArrowObj != null)
                    {
                        mapping.destinationArrow = destArrowObj;
                        synced++;
                        Debug.Log($"  ✓ Synced destination arrow: {destArrowObj.name} → {stepPath}");
                    }
                    else if (step.destinationArrow != null && !string.IsNullOrEmpty(step.destinationArrow.GameObjectName))
                    {
                        nullRefs++;
                        Debug.LogWarning($"  ⚠ Could not resolve destination arrow '{step.destinationArrow.GameObjectName}' for {stepPath}");
                    }

                    // Sync arrow behavior settings
                    mapping.hideTargetArrowAfterGrab = step.hideTargetArrowAfterGrab;
                    mapping.showDestinationAfterGrab = step.showDestinationAfterGrab;
                }
            }
        }

        string summary = $"✅ Sync complete!\n" +
                        $"   Total steps: {totalSteps}\n" +
                        $"   References synced: {synced}\n" +
                        $"   Unresolved references: {nullRefs}";

        if (nullRefs > 0)
        {
            summary += "\n\n⚠ WARNING: Some references could not be resolved.\n" +
                      "This usually means:\n" +
                      "1. The references were never set in the Sequence Builder, OR\n" +
                      "2. Unity has already cleared them (editor was restarted)\n\n" +
                      "Solution: Assign the references in the Sequence Builder UI, then click Sync immediately.";
        }

        Debug.Log(summary);
        return synced;
    }

    /// <summary>
    /// Validates sync status - shows which references are set vs missing
    /// </summary>
    public string GetSyncStatus()
    {
        if (sequenceAsset == null || sequenceAsset.Program == null)
        {
            return "No sequence asset assigned";
        }

        int totalSteps = 0;
        int stepsWithAllRefs = 0;
        int stepsWithPartialRefs = 0;
        int stepsWithNoRefs = 0;

        foreach (var module in sequenceAsset.Program.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    totalSteps++;
                    string stepPath = $"{module.moduleName}/{taskGroup.groupName}/{step.stepName}";
                    var mapping = arrowMappings.FirstOrDefault(m => m.stepPath == stepPath);

                    if (mapping == null)
                    {
                        stepsWithNoRefs++;
                        continue;
                    }

                    int refCount = 0;
                    if (mapping.targetObject != null) refCount++;
                    if (mapping.destinationObject != null) refCount++;
                    if (mapping.targetArrow != null) refCount++;
                    if (mapping.destinationArrow != null) refCount++;

                    if (refCount == 0)
                        stepsWithNoRefs++;
                    else if (refCount >= 2) // At least target object + arrow
                        stepsWithAllRefs++;
                    else
                        stepsWithPartialRefs++;
                }
            }
        }

        return $"Total Steps: {totalSteps}\n" +
               $"✓ Fully synced: {stepsWithAllRefs}\n" +
               $"⚠ Partially synced: {stepsWithPartialRefs}\n" +
               $"✗ Not synced: {stepsWithNoRefs}";
    }

    /// <summary>
    /// Auto-populates mappings from sequence asset (creates entries with null arrows)
    /// </summary>
    public void GenerateMappingsFromSequence()
    {
        if (sequenceAsset == null || sequenceAsset.Program == null)
        {
            Debug.LogWarning("No sequence asset linked!");
            return;
        }

        int added = 0;

        foreach (var module in sequenceAsset.Program.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    string stepPath = $"{module.moduleName}/{taskGroup.groupName}/{step.stepName}";

                    // Only add if not already present
                    if (!arrowMappings.Any(m => m.stepPath == stepPath))
                    {
                        arrowMappings.Add(new ArrowMapping
                        {
                            stepPath = stepPath,
                            targetArrow = null,
                            destinationArrow = null,
                            hideTargetArrowAfterGrab = step.hideTargetArrowAfterGrab,
                            showDestinationAfterGrab = step.showDestinationAfterGrab
                        });
                        added++;
                    }
                }
            }
        }

        Debug.Log($"Generated {added} new arrow mapping entries from sequence asset.");
    }

    /// <summary>
    /// Cleans up mappings that don't exist in the sequence anymore
    /// </summary>
    public void CleanupStaleMapping()
    {
        var validPaths = GenerateStepPaths();
        int removed = arrowMappings.RemoveAll(m => !validPaths.Contains(m.stepPath));

        if (removed > 0)
        {
            Debug.Log($"Removed {removed} stale arrow mappings.");
        }
    }
#endif
}

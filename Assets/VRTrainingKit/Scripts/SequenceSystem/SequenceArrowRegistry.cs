using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        [Tooltip("Arrow to show when step starts (points to target object)")]
        public GameObject targetArrow;

        [Tooltip("Arrow to show after grab (points to destination)")]
        public GameObject destinationArrow;

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

    [Header("Auto-Scan Settings")]
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
    }

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

// VRTrainingWindowState.cs
// Centralized state container for VR Training Kit editor window
// Part of Phase 1: Infrastructure refactoring

#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized state container for VRInteractionSetupWindow.
/// This class extracts all state fields from the main window to enable sharing across multiple UI classes.
///
/// Usage:
/// - Create instance in VRInteractionSetupWindow.OnEnable()
/// - Pass to tab drawer classes for read/write access
/// - State persists for lifetime of editor window instance
/// </summary>
public class VRTrainingWindowState
{
    // ==========================================
    // Tab Navigation
    // ==========================================

    /// <summary>
    /// Available tabs in the editor window
    /// </summary>
    public enum Tab
    {
        Setup,
        Configure,
        Sequence,
        RuntimeMonitor,
        Validate
    }

    /// <summary>
    /// Currently active tab
    /// </summary>
    public Tab CurrentTab { get; set; } = Tab.Setup;

    // ==========================================
    // Setup Tab State
    // ==========================================

    /// <summary>
    /// Results from scene scanning
    /// </summary>
    public InteractionSetupService.SceneAnalysis SceneAnalysis { get; set; }

    /// <summary>
    /// Scroll position for setup tab
    /// </summary>
    public Vector2 SetupScrollPos { get; set; }

    // ==========================================
    // Configure Tab State - Profile Selection
    // ==========================================

    /// <summary>
    /// Currently selected grab profile
    /// </summary>
    public InteractionProfile SelectedGrabProfile { get; set; }

    /// <summary>
    /// Currently selected knob profile
    /// </summary>
    public InteractionProfile SelectedKnobProfile { get; set; }

    /// <summary>
    /// Currently selected snap profile
    /// </summary>
    public InteractionProfile SelectedSnapProfile { get; set; }

    /// <summary>
    /// Currently selected tool profile
    /// </summary>
    public InteractionProfile SelectedToolProfile { get; set; }

    /// <summary>
    /// Currently selected valve profile
    /// </summary>
    public InteractionProfile SelectedValveProfile { get; set; }

    /// <summary>
    /// Currently selected turn profile
    /// </summary>
    public InteractionProfile SelectedTurnProfile { get; set; }

    /// <summary>
    /// Scroll position for configure tab
    /// </summary>
    public Vector2 ConfigScrollPos { get; set; }

    // ==========================================
    // Configure Tab State - Profile Caches
    // ==========================================

    /// <summary>
    /// Cached list of available grab profiles
    /// </summary>
    public List<InteractionProfile> CachedGrabProfiles { get; set; } = new List<InteractionProfile>();

    /// <summary>
    /// Cached list of available knob profiles
    /// </summary>
    public List<InteractionProfile> CachedKnobProfiles { get; set; } = new List<InteractionProfile>();

    /// <summary>
    /// Cached list of available snap profiles
    /// </summary>
    public List<InteractionProfile> CachedSnapProfiles { get; set; } = new List<InteractionProfile>();

    /// <summary>
    /// Cached list of available tool profiles
    /// </summary>
    public List<InteractionProfile> CachedToolProfiles { get; set; } = new List<InteractionProfile>();

    /// <summary>
    /// Cached list of available valve profiles
    /// </summary>
    public List<InteractionProfile> CachedValveProfiles { get; set; } = new List<InteractionProfile>();

    /// <summary>
    /// Cached list of available turn profiles
    /// </summary>
    public List<InteractionProfile> CachedTurnProfiles { get; set; } = new List<InteractionProfile>();

    // ==========================================
    // Sequence Tab State - Legacy System
    // ==========================================

    /// <summary>
    /// Reference to legacy sequence controller (if present in scene)
    /// </summary>
    public LegacySequenceController LegacySequenceController { get; set; }

    /// <summary>
    /// Scroll position for legacy sequence tab
    /// </summary>
    public Vector2 SequenceScrollPos { get; set; }

    /// <summary>
    /// Whether to show sequence help section
    /// </summary>
    public bool ShowSequenceHelp { get; set; } = false;

    // ==========================================
    // Sequence Tab State - Hierarchical System
    // ==========================================

    /// <summary>
    /// Currently loaded training sequence asset
    /// </summary>
    public TrainingSequenceAsset CurrentTrainingAsset { get; set; }

    /// <summary>
    /// Current training program being edited
    /// </summary>
    public TrainingProgram CurrentProgram { get; set; }

    /// <summary>
    /// Scroll position for training sequence view
    /// </summary>
    public Vector2 TrainingSequenceScrollPos { get; set; }

    /// <summary>
    /// Array of available training sequence assets
    /// </summary>
    public TrainingSequenceAsset[] AvailableAssets { get; set; }

    /// <summary>
    /// Index of currently selected asset in dropdown
    /// </summary>
    public int SelectedAssetIndex { get; set; } = 0;

    /// <summary>
    /// Whether assets have been loaded
    /// </summary>
    public bool AssetsLoaded { get; set; } = false;

    // ==========================================
    // Sequence Tab State - Two-Panel Editor
    // ==========================================

    /// <summary>
    /// Scroll position for tree view panel (left side)
    /// </summary>
    public Vector2 TreeViewScrollPos { get; set; }

    /// <summary>
    /// Scroll position for details panel (right side)
    /// </summary>
    public Vector2 DetailsPanelScrollPos { get; set; }

    /// <summary>
    /// Currently selected item in hierarchy (TrainingModule, TaskGroup, or InteractionStep)
    /// </summary>
    public object SelectedHierarchyItem { get; set; }

    /// <summary>
    /// Type of selected item: "module", "taskgroup", "step", "program"
    /// </summary>
    public string SelectedItemType { get; set; }

    /// <summary>
    /// Whether to show the add menu
    /// </summary>
    public bool ShowAddMenu { get; set; } = false;

    /// <summary>
    /// Position of splitter between tree view and details panel (0-1)
    /// Default: 0.4 (40% tree view, 60% details)
    /// </summary>
    public float SplitterPosition { get; set; } = 0.4f;

    // ==========================================
    // Validate Tab State
    // ==========================================

    /// <summary>
    /// List of validation issues found
    /// </summary>
    public List<string> ValidationIssues { get; set; } = new List<string>();

    /// <summary>
    /// Scroll position for validate tab
    /// </summary>
    public Vector2 ValidateScrollPos { get; set; }

    // ==========================================
    // Helper Methods
    // ==========================================

    /// <summary>
    /// Clears all cached profile lists
    /// </summary>
    public void ClearProfileCaches()
    {
        CachedGrabProfiles?.Clear();
        CachedKnobProfiles?.Clear();
        CachedSnapProfiles?.Clear();
        CachedToolProfiles?.Clear();
        CachedValveProfiles?.Clear();
        CachedTurnProfiles?.Clear();
    }

    /// <summary>
    /// Resets sequence selection state
    /// </summary>
    public void ClearSequenceSelection()
    {
        SelectedHierarchyItem = null;
        SelectedItemType = null;
    }

    /// <summary>
    /// Resets all scroll positions to zero
    /// </summary>
    public void ResetScrollPositions()
    {
        SetupScrollPos = Vector2.zero;
        ConfigScrollPos = Vector2.zero;
        SequenceScrollPos = Vector2.zero;
        TrainingSequenceScrollPos = Vector2.zero;
        TreeViewScrollPos = Vector2.zero;
        DetailsPanelScrollPos = Vector2.zero;
        ValidateScrollPos = Vector2.zero;
    }
}
#endif

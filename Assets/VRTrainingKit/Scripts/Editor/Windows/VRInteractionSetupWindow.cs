// VRInteractionSetupWindow.cs
// Main editor window for VR Training Kit
// Refactored: All tab content extracted to dedicated drawer classes

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Main editor window for VR Training Kit setup and configuration.
/// Coordinates between extracted tab drawers for modular functionality.
/// </summary>
public class VRInteractionSetupWindow : EditorWindow, ISequenceTreeViewCallbacks
{
    // Window tabs
    private enum Tab
    {
        Setup,
        Configure,
        Sequence,
        RuntimeMonitor,
        Validate
    }

    private Tab currentTab = Tab.Setup;

    // Core managers
    private ProfileCacheManager _profileCacheManager;

    // Tab drawers (Phase 6 extraction)
    private SetupTabDrawer _setupTabDrawer;
    private ConfigureTabDrawer _configureTabDrawer;
    private SequenceTabDrawer _sequenceTabDrawer;
    private ValidateTabDrawer _validateTabDrawer;
    private RuntimeMonitorTabDrawer _runtimeMonitorTabDrawer;

    // Sequence tab components (Phase 4 & 5)
    private SequenceTreeView _sequenceTreeView;
    private SequencePropertiesPanel _sequencePropertiesPanel;

    // Legacy sequence system (kept for backward compatibility)
    private LegacySequenceController sequenceController;

    [MenuItem("Sequence Builder/Setup Assistant")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRInteractionSetupWindow>("Sequence Builder");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        // Initialize profile cache manager
        _profileCacheManager = new ProfileCacheManager();

        // Initialize sequence components (Phase 4 & 5)
        _sequenceTreeView = new SequenceTreeView(this);
        _sequencePropertiesPanel = new SequencePropertiesPanel(OnAutoSave);

        // Initialize tab drawers (Phase 6)
        _setupTabDrawer = new SetupTabDrawer(
            _profileCacheManager,
            GetSelectedProfile,
            OnSceneAnalysisChanged
        );

        _configureTabDrawer = new ConfigureTabDrawer(_profileCacheManager);

        _sequenceTabDrawer = new SequenceTabDrawer(
            _sequenceTreeView,
            _sequencePropertiesPanel,
            Repaint
        );

        _validateTabDrawer = new ValidateTabDrawer();
        _runtimeMonitorTabDrawer = new RuntimeMonitorTabDrawer();

        // Load default profiles
        LoadDefaultProfiles();

        // Cache available profiles for performance
        _profileCacheManager.RefreshAllCaches();

        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // Try to restore scene analysis if we had one before
        if (_setupTabDrawer.SceneAnalysis == null && EditorPrefs.HasKey("VRTrainingKit_LastSceneAnalysisValid"))
        {
            bool wasValid = EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid", false);
            if (wasValid)
            {
                Debug.Log("[VRInteractionSetupWindow] Restoring scene analysis after play mode transition");
                _setupTabDrawer.SceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from play mode state changes
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        // Save that we had a valid scene analysis
        if (_setupTabDrawer?.SceneAnalysis != null)
        {
            EditorPrefs.SetBool("VRTrainingKit_LastSceneAnalysisValid", true);
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Debug.Log("[VRInteractionSetupWindow] Entered play mode - refreshing scene analysis");
            if (_setupTabDrawer?.SceneAnalysis != null)
            {
                _setupTabDrawer.SceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("[VRInteractionSetupWindow] Entered edit mode - refreshing scene analysis and training assets");

            // Refresh scene analysis
            if (EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid", false))
            {
                _setupTabDrawer.SceneAnalysis = InteractionSetupService.ScanScene();
            }

            // Refresh training asset references after play mode
            _sequenceTabDrawer?.RefreshTrainingAssetReferences();
        }
    }

    private void OnGUI()
    {
        // Handle keyboard shortcuts
        HandleKeyboardShortcuts();

        // Tab selection
        DrawTabBar();

        EditorGUILayout.Space(10);

        // Draw current tab
        switch (currentTab)
        {
            case Tab.Setup:
                _setupTabDrawer?.Draw();
                break;
            case Tab.Configure:
                _configureTabDrawer?.Draw();
                break;
            case Tab.Sequence:
                _sequenceTabDrawer?.Draw(position.width);
                break;
            case Tab.RuntimeMonitor:
                _runtimeMonitorTabDrawer?.Draw();
                // Auto-refresh when in Play Mode
                if (EditorApplication.isPlaying)
                {
                    Repaint();
                }
                break;
            case Tab.Validate:
                _validateTabDrawer?.Draw();
                break;
        }
    }

    /// <summary>
    /// Draw the tab selection bar
    /// </summary>
    private void DrawTabBar()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Toggle(currentTab == Tab.Setup, "Setup", "Button"))
            currentTab = Tab.Setup;
        if (GUILayout.Toggle(currentTab == Tab.Configure, "Configure", "Button"))
            currentTab = Tab.Configure;
        if (GUILayout.Toggle(currentTab == Tab.Sequence, "Sequence", "Button"))
            currentTab = Tab.Sequence;

        // Runtime Monitor tab - only show if enabled
        if (RuntimeMonitorTabDrawer.IsEnabled())
        {
            if (GUILayout.Toggle(currentTab == Tab.RuntimeMonitor, "Runtime Monitor", "Button"))
                currentTab = Tab.RuntimeMonitor;
        }
        else
        {
            // If Runtime Monitor was selected but settings are now disabled, switch to Setup tab
            if (currentTab == Tab.RuntimeMonitor)
                currentTab = Tab.Setup;
        }

        if (GUILayout.Toggle(currentTab == Tab.Validate, "Validate", "Button"))
            currentTab = Tab.Validate;

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Handle keyboard shortcuts
    /// </summary>
    private void HandleKeyboardShortcuts()
    {
        Event current = Event.current;

        // Ctrl+S or Cmd+S to force save
        if (current.type == EventType.KeyDown &&
            ((current.control && current.keyCode == KeyCode.S) ||
             (current.command && current.keyCode == KeyCode.S)))
        {
            _sequenceTabDrawer?.ForceSaveAllAssets();
            current.Use();
        }

        // Ctrl+R or Cmd+R to refresh references
        if (current.type == EventType.KeyDown &&
            ((current.control && current.keyCode == KeyCode.R) ||
             (current.command && current.keyCode == KeyCode.R)))
        {
            _sequenceTabDrawer?.RefreshAllObjectReferences();
            current.Use();
        }
    }

    /// <summary>
    /// Load default profiles based on detected framework
    /// </summary>
    private void LoadDefaultProfiles()
    {
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        Debug.Log($"[VRInteractionSetupWindow] Loading profiles for framework: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");

        if (currentFramework == VRFramework.AutoHands)
        {
            LoadAutoHandsProfiles();
            Debug.Log("[VRInteractionSetupWindow] AutoHands framework detected - Loading AutoHands profiles");
        }
        else
        {
            LoadXRIProfiles();
            if (currentFramework == VRFramework.None)
            {
                Debug.LogWarning("[VRInteractionSetupWindow] No VR framework detected - Loading XRI profiles as fallback");
            }
        }
    }

    /// <summary>
    /// Load XRI profiles from Resources and Assets
    /// </summary>
    private void LoadXRIProfiles()
    {
        SetSelectedProfile(ProfileCacheManager.ProfileType.Grab,
            Resources.Load<InteractionProfile>("XRI/DefaultGrabProfile") ?? Resources.Load<InteractionProfile>("DefaultGrabProfile") ?? FindFirstProfile("GrabProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Knob,
            Resources.Load<InteractionProfile>("XRI/DefaultKnobProfile") ?? Resources.Load<InteractionProfile>("DefaultKnobProfile") ?? FindFirstProfile("KnobProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Snap,
            Resources.Load<InteractionProfile>("XRI/DefaultSnapProfile") ?? Resources.Load<InteractionProfile>("DefaultSnapProfile") ?? FindFirstProfile("SnapProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Tool,
            Resources.Load<InteractionProfile>("XRI/DefaultToolProfile") ?? Resources.Load<InteractionProfile>("DefaultToolProfile") ?? FindFirstProfile("ToolProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Valve,
            Resources.Load<InteractionProfile>("XRI/DefaultValveProfile") ?? Resources.Load<InteractionProfile>("DefaultValveProfile") ?? FindFirstProfile("ValveProfile"));
    }

    /// <summary>
    /// Load AutoHands profiles from Resources and Assets
    /// </summary>
    private void LoadAutoHandsProfiles()
    {
        SetSelectedProfile(ProfileCacheManager.ProfileType.Grab,
            Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsGrabProfile") ?? FindFirstProfile("AutoHandsGrabProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Knob,
            Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsKnobProfile") ?? FindFirstProfile("AutoHandsKnobProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Snap,
            Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsSnapProfile") ?? FindFirstProfile("AutoHandsSnapProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Tool,
            Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsToolProfile") ?? FindFirstProfile("AutoHandsToolProfile"));

        SetSelectedProfile(ProfileCacheManager.ProfileType.Valve,
            Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsValveProfile") ?? FindFirstProfile("AutoHandsValveProfile"));
    }

    /// <summary>
    /// Find the first profile of a given type in the project
    /// </summary>
    private InteractionProfile FindFirstProfile(string typeName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeName}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
        }
        return null;
    }

    /// <summary>
    /// Get selected profile for a type (used by SetupTabDrawer)
    /// </summary>
    private InteractionProfile GetSelectedProfile(ProfileCacheManager.ProfileType profileType)
    {
        return _configureTabDrawer?.GetSelectedProfile(profileType);
    }

    /// <summary>
    /// Set selected profile for a type
    /// </summary>
    private void SetSelectedProfile(ProfileCacheManager.ProfileType profileType, InteractionProfile profile)
    {
        _configureTabDrawer?.SetSelectedProfile(profileType, profile);
    }

    /// <summary>
    /// Called when scene analysis changes
    /// </summary>
    private void OnSceneAnalysisChanged()
    {
        Repaint();
    }

    // ==========================================
    // ISequenceTreeViewCallbacks Implementation
    // ==========================================

    public void OnItemSelected(object item, string itemType)
    {
        Repaint();
    }

    public void OnAutoSave()
    {
        _sequenceTabDrawer?.AutoSaveCurrentAsset();
    }

    public void OnRequestRepaint()
    {
        Repaint();
    }
}
#endif

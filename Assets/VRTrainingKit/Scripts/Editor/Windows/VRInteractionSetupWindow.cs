// VRInteractionSetupWindow.cs
// This file should be placed in an "Editor" folder in your Unity project

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


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
    
    // Setup tab
    private InteractionSetupService.SceneAnalysis sceneAnalysis;
    private Vector2 setupScrollPos;
    
    // Profile caching - Now uses centralized ProfileCacheManager
    private ProfileCacheManager _profileCacheManager;

    // Configure tab - Now uses extracted ConfigureTabDrawer
    private ConfigureTabDrawer _configureTabDrawer;

    // Sequence tab - Now uses extracted SequenceTreeView (Phase 4)
    private SequenceTreeView _sequenceTreeView;

    // Property accessors for backward compatibility - delegate to ConfigureTabDrawer
    private InteractionProfile selectedGrabProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Grab);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Grab, value);
    }
    private InteractionProfile selectedKnobProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Knob);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Knob, value);
    }
    private InteractionProfile selectedSnapProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Snap);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Snap, value);
    }
    private InteractionProfile selectedToolProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Tool);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Tool, value);
    }
    private InteractionProfile selectedValveProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Valve);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Valve, value);
    }
    private InteractionProfile selectedTurnProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Turn);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Turn, value);
    }
    private InteractionProfile selectedTeleportProfile
    {
        get => _configureTabDrawer?.GetSelectedProfile(ProfileCacheManager.ProfileType.Teleport);
        set => _configureTabDrawer?.SetSelectedProfile(ProfileCacheManager.ProfileType.Teleport, value);
    }
    private Vector2 configScrollPos
    {
        get => _configureTabDrawer?.ScrollPosition ?? Vector2.zero;
        set { if (_configureTabDrawer != null) _configureTabDrawer.ScrollPosition = value; }
    }

    // Accessors for backward compatibility during refactoring
    private List<InteractionProfile> cachedGrabProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Grab) ?? new List<InteractionProfile>();
    private List<InteractionProfile> cachedKnobProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Knob) ?? new List<InteractionProfile>();
    private List<InteractionProfile> cachedSnapProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Snap) ?? new List<InteractionProfile>();
    private List<InteractionProfile> cachedToolProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Tool) ?? new List<InteractionProfile>();
    private List<InteractionProfile> cachedValveProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Valve) ?? new List<InteractionProfile>();
    private List<InteractionProfile> cachedTurnProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Turn) ?? new List<InteractionProfile>();
    private List<InteractionProfile> cachedTeleportProfiles => _profileCacheManager?.GetCachedProfiles(ProfileCacheManager.ProfileType.Teleport) ?? new List<InteractionProfile>();
    
    // Sequence tab - Legacy state-based system
    private LegacySequenceController sequenceController;
    private Vector2 sequenceScrollPos;
    private bool showSequenceHelp = false;
    
    // Training Sequence tab - New hierarchical system
    private TrainingSequenceAsset currentTrainingAsset;
    private TrainingProgram currentProgram;
    private Vector2 trainingSequenceScrollPos;
    private TrainingSequenceAsset[] availableAssets;
    private int selectedAssetIndex = 0;
    private bool assetsLoaded = false;
    
    // Phase 5: Two-panel editing system
    private Vector2 detailsPanelScrollPos;
    private float splitterPosition = 0.4f; // 40% tree view, 60% details panel

    // Selection state - now delegates to SequenceTreeView (Phase 4)
    private object selectedHierarchyItem
    {
        get => _sequenceTreeView?.SelectedItem;
        set { if (_sequenceTreeView != null) _sequenceTreeView.SelectedItem = value; }
    }
    private string selectedItemType
    {
        get => _sequenceTreeView?.SelectedItemType;
        set { if (_sequenceTreeView != null) _sequenceTreeView.SelectedItemType = value; }
    }
    private Vector2 treeViewScrollPos
    {
        get => _sequenceTreeView?.ScrollPosition ?? Vector2.zero;
        set { if (_sequenceTreeView != null) _sequenceTreeView.ScrollPosition = value; }
    }
    
    // Validate tab
    private List<string> validationIssues = new List<string>();
    private Vector2 validateScrollPos;
    
    // Styling
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle successStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;
    
    [MenuItem("VR Training/Setup Assistant")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRInteractionSetupWindow>("VR Training Setup");
        window.minSize = new Vector2(400, 500);
    }
    
    private void OnEnable()
    {
        InitializeStyles();

        // Initialize profile cache manager
        _profileCacheManager = new ProfileCacheManager();

        // Initialize Configure tab drawer
        _configureTabDrawer = new ConfigureTabDrawer(_profileCacheManager);

        // Initialize Sequence tree view
        _sequenceTreeView = new SequenceTreeView(this);

        LoadDefaultProfiles();

        // Cache available profiles for performance
        RefreshProfileCaches();

        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        
        // Try to restore scene analysis if we had one before
        if (sceneAnalysis == null && EditorPrefs.HasKey("VRTrainingKit_LastSceneAnalysisValid"))
        {
            bool wasValid = EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid", false);
            if (wasValid)
            {
                Debug.Log("[VRInteractionSetupWindow] Restoring scene analysis after play mode transition");
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from play mode state changes
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        
        // Save that we had a valid scene analysis
        if (sceneAnalysis != null)
        {
            EditorPrefs.SetBool("VRTrainingKit_LastSceneAnalysisValid", true);
        }
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Debug.Log("[VRInteractionSetupWindow] Entered play mode - refreshing scene analysis");
            if (sceneAnalysis != null)
            {
                // Refresh the analysis in play mode to show current state
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("[VRInteractionSetupWindow] Entered edit mode - refreshing scene analysis and training assets");

            // Refresh scene analysis
            if (EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid", false))
            {
                sceneAnalysis = InteractionSetupService.ScanScene();
            }

            // CRITICAL FIX: Refresh training asset references after play mode
            RefreshTrainingAssetReferences();
        }
    }

    /// <summary>
    /// Refreshes training asset references after play mode or asset changes
    /// This fixes the issue where step value changes don't reflect in real-time after play mode
    /// </summary>
    private void RefreshTrainingAssetReferences()
    {
        if (currentTrainingAsset != null)
        {
            // Store current selection
            string currentAssetName = currentTrainingAsset.name;

            // Reload all available assets
            LoadAvailableTrainingAssets();

            // Try to restore the previously selected asset
            if (availableAssets != null)
            {
                for (int i = 0; i < availableAssets.Length; i++)
                {
                    if (availableAssets[i] != null && availableAssets[i].name == currentAssetName)
                    {
                        selectedAssetIndex = i;
                        LoadTrainingAsset(availableAssets[i]);
                        Debug.Log($"[VRInteractionSetupWindow] Restored training asset: {currentAssetName}");
                        return;
                    }
                }
            }

            Debug.LogWarning($"[VRInteractionSetupWindow] Could not restore training asset: {currentAssetName}");
        }
    }
    
    private void InitializeStyles()
    {
        headerStyle = new GUIStyle()
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 5, 5)
        };
        
        subHeaderStyle = new GUIStyle()
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 3, 3)
        };
        
        successStyle = new GUIStyle()
        {
            normal = { textColor = Color.green },
            padding = new RectOffset(5, 5, 2, 2)
        };
        
        warningStyle = new GUIStyle()
        {
            normal = { textColor = Color.yellow },
            padding = new RectOffset(5, 5, 2, 2)
        };
        
        errorStyle = new GUIStyle()
        {
            normal = { textColor = Color.red },
            padding = new RectOffset(5, 5, 2, 2)
        };
    }
    
    private void LoadDefaultProfiles()
    {
        // Detect current framework and load appropriate profiles
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();

        Debug.Log($"[VRInteractionSetupWindow] Loading profiles for framework: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");

        // For Phase 1, always load XRI profiles since AutoHands profiles are placeholders
        // In Phase 2, this will be updated to actually switch between profile types
        LoadXRIProfiles();

        // Log framework-specific message
        if (currentFramework == VRFramework.AutoHands)
        {
            Debug.Log("[VRInteractionSetupWindow] AutoHands framework detected - Loading XRI profiles as placeholders until Phase 2 implementation");
        }
        else if (currentFramework == VRFramework.None)
        {
            Debug.LogWarning("[VRInteractionSetupWindow] No VR framework detected - Loading XRI profiles as fallback");
        }
    }

    /// <summary>
    /// Load framework-appropriate profiles from Resources and Assets
    /// </summary>
    private void LoadXRIProfiles()
    {
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();

        // Load framework-appropriate profiles
        if (currentFramework == VRFramework.AutoHands)
        {
            LoadAutoHandsProfiles();
        }
        else
        {
            LoadXRIProfilesInternal();
        }
    }

    /// <summary>
    /// Load XRI profiles from Resources and Assets
    /// </summary>
    private void LoadXRIProfilesInternal()
    {
        // Try to load default profiles from Resources (framework-specific paths first, then fallback)
        selectedGrabProfile = Resources.Load<InteractionProfile>("XRI/DefaultGrabProfile") ?? Resources.Load<InteractionProfile>("DefaultGrabProfile");
        selectedKnobProfile = Resources.Load<InteractionProfile>("XRI/DefaultKnobProfile") ?? Resources.Load<InteractionProfile>("DefaultKnobProfile");
        selectedSnapProfile = Resources.Load<InteractionProfile>("XRI/DefaultSnapProfile") ?? Resources.Load<InteractionProfile>("DefaultSnapProfile");
        selectedToolProfile = Resources.Load<InteractionProfile>("XRI/DefaultToolProfile") ?? Resources.Load<InteractionProfile>("DefaultToolProfile");
        selectedValveProfile = Resources.Load<InteractionProfile>("XRI/DefaultValveProfile") ?? Resources.Load<InteractionProfile>("DefaultValveProfile");

        // If not found in Resources, search in Assets for XRI profiles
        if (selectedGrabProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:GrabProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedGrabProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedKnobProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:KnobProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedKnobProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedSnapProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SnapProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedSnapProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedToolProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ToolProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedToolProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedValveProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ValveProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedValveProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }
    }

    /// <summary>
    /// Load AutoHands profiles from Resources and Assets
    /// </summary>
    private void LoadAutoHandsProfiles()
    {
        // Try to load default profiles from Resources (framework-specific paths first)
        selectedGrabProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsGrabProfile");
        selectedKnobProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsKnobProfile");
        selectedSnapProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsSnapProfile");
        selectedToolProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsToolProfile");
        selectedValveProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsValveProfile");

        // If not found in Resources, search in Assets for AutoHands profiles
        if (selectedGrabProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsGrabProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedGrabProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedKnobProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsKnobProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedKnobProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedSnapProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsSnapProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedSnapProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedToolProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsToolProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedToolProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedValveProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsValveProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedValveProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }
    }

    /// <summary>
    /// Refresh cached profile lists for performance - called once on enable and when profiles change
    /// </summary>
    private void RefreshProfileCaches()
    {
        // Profile caching now handled by ProfileCacheManager
        _profileCacheManager?.RefreshAllCaches();
    }


    /// <summary>
    /// Handles keyboard shortcuts for the editor window
    /// </summary>
    private void HandleKeyboardShortcuts()
    {
        Event current = Event.current;

        // Ctrl+S or Cmd+S to force save
        if (current.type == EventType.KeyDown &&
            ((current.control && current.keyCode == KeyCode.S) ||
             (current.command && current.keyCode == KeyCode.S)))
        {
            ForceSaveAllAssets();
            current.Use(); // Consume the event so Unity doesn't process it
        }

        // Ctrl+R or Cmd+R to refresh references (useful after renaming objects)
        if (current.type == EventType.KeyDown &&
            ((current.control && current.keyCode == KeyCode.R) ||
             (current.command && current.keyCode == KeyCode.R)))
        {
            RefreshAllObjectReferences();
            current.Use();
        }
    }

    /// <summary>
    /// Refreshes all GameObject references in the current training program
    /// Useful when objects have been renamed or moved
    /// </summary>
    private void RefreshAllObjectReferences()
    {
        if (currentProgram == null) return;

        int refreshCount = 0;

        foreach (var module in currentProgram.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    if (step.targetObject != null)
                    {
                        step.targetObject.RefreshReference();
                        refreshCount++;
                    }
                    if (step.destination != null)
                    {
                        step.destination.RefreshReference();
                        refreshCount++;
                    }
                    if (step.targetSocket != null)
                    {
                        step.targetSocket.RefreshReference();
                        refreshCount++;
                    }
                }
            }
        }

        if (refreshCount > 0)
        {
            AutoSaveCurrentAsset();
            Debug.Log($"[VRTrainingKit] Refreshed {refreshCount} object references. Use Ctrl+R to refresh again if needed.");
        }
        else
        {
            Debug.Log("[VRTrainingKit] No object references found to refresh.");
        }
    }

    private void OnGUI()
    {
        // Handle keyboard shortcuts
        HandleKeyboardShortcuts();

        // Tab selection
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.Setup, "Setup", "Button"))
            currentTab = Tab.Setup;
        if (GUILayout.Toggle(currentTab == Tab.Configure, "Configure", "Button"))
            currentTab = Tab.Configure;
        if (GUILayout.Toggle(currentTab == Tab.Sequence, "Sequence", "Button"))
            currentTab = Tab.Sequence;

        // Runtime Monitor tab - only show if RuntimeMonitorSettings exists and is enabled
        if (IsRuntimeMonitorTabEnabled())
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
        
        EditorGUILayout.Space(10);
        
        // Draw current tab
        switch (currentTab)
        {
            case Tab.Setup:
                DrawSetupTab();
                break;
            case Tab.Configure:
                DrawConfigureTab();
                break;
            case Tab.Sequence:
                DrawSequenceTab();
                break;
            case Tab.RuntimeMonitor:
                DrawRuntimeMonitorTab();
                break;
            case Tab.Validate:
                DrawValidateTab();
                break;
        }
    }
    
    private void DrawSetupTab()
    {
        EditorGUILayout.LabelField("Scene Setup", headerStyle);
        EditorGUILayout.Space(5);

        // Framework Status Section
        DrawFrameworkStatus();
        EditorGUILayout.Space(10);

        // Scan button
        if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
        {
            sceneAnalysis = InteractionSetupService.ScanScene();
        }
        
        EditorGUILayout.Space(10);
        
        // Show analysis results
        if (sceneAnalysis != null)
        {
            setupScrollPos = EditorGUILayout.BeginScrollView(setupScrollPos);
            
            // Grab objects
            DrawObjectGroup("Grab Objects", sceneAnalysis.grabObjects, "grab", selectedGrabProfile);
            EditorGUILayout.Space(10);
            
            // Knob objects
            DrawObjectGroup("Knob Objects", sceneAnalysis.knobObjects, "knob", selectedKnobProfile);
            EditorGUILayout.Space(10);
            
            // Snap points
            DrawObjectGroup("Snap Points", sceneAnalysis.snapObjects, "snap", selectedSnapProfile);
            EditorGUILayout.Space(10);
            
            // Tool objects
            DrawObjectGroup("Tool Objects", sceneAnalysis.toolObjects, "tool", selectedToolProfile);
            EditorGUILayout.Space(10);
            
            // Valve objects
            DrawObjectGroup("Valve Objects", sceneAnalysis.valveObjects, "valve", selectedValveProfile);
            EditorGUILayout.Space(10);

            // Turn objects
            DrawObjectGroup("Turn Objects", sceneAnalysis.turnObjects, "turn", selectedTurnProfile);
            EditorGUILayout.Space(10);

            // Teleport points
            DrawObjectGroup("🚀 Teleport Points", sceneAnalysis.teleportObjects, "teleportPoint", selectedTeleportProfile);

            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // Quick setup buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Apply All Components", GUILayout.Height(35)))
            {
                ApplyAllComponents();
            }
            
            if (GUILayout.Button("Clean All", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Clean Components", 
                    "This will remove all VR interaction components from tagged objects. Continue?", 
                    "Yes", "Cancel"))
                {
                    InteractionSetupService.CleanupComponents();
                    sceneAnalysis = InteractionSetupService.ScanScene();
                }
            }
            
            if (GUILayout.Button("Edit Layers", GUILayout.Height(35)))
            {
                InteractionLayerManager.OpenInteractionLayerSettings();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Click 'Scan Scene' to analyze tagged objects in your scene.", MessageType.Info);
        }
    }
    
    private void DrawObjectGroup(string title, List<GameObject> objects, string tag, InteractionProfile profile)
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{title} ({objects.Count} found)", subHeaderStyle);
        
        if (objects.Count > 0)
        {
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                Selection.objects = objects.ToArray();
            }
            
            if (GUILayout.Button("Configure", GUILayout.Width(80)))
            {
                if (profile != null)
                {
                    InteractionSetupService.ApplyComponentsToObjects(objects, profile);
                    EditorUtility.DisplayDialog("Configuration Complete", 
                        $"Applied {profile.profileName} to {objects.Count} objects", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("No Profile", 
                        $"Please select a profile for {tag} objects in the Configure tab", "OK");
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // List objects with layer mask control
        if (objects.Count > 0 && objects.Count <= 20) // Increased limit for layer editing
        {
            EditorGUI.indentLevel++;
            foreach (var obj in objects)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Check if configured (framework-aware)
                bool isConfigured = false;
                XRBaseInteractable interactable = null;
                XRSocketInteractor socketInteractor = null;

                // Detect current framework
                var currentFramework = VRFrameworkDetector.DetectCurrentFramework();

                if (currentFramework == VRFramework.XRI)
                {
                    // XRI validation
                    if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve" || tag == "turn")
                    {
                        interactable = obj.GetComponent<XRGrabInteractable>();
                        isConfigured = interactable != null;
                    }
                    else if (tag == "snap")
                    {
                        socketInteractor = obj.GetComponent<XRSocketInteractor>();
                        isConfigured = socketInteractor != null;
                    }
                }
                else if (currentFramework == VRFramework.AutoHands)
                {
                    // AutoHands validation - check for Grabbable component
                    if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve" || tag == "turn")
                    {
                        var grabbable = obj.GetComponent<Autohand.Grabbable>();
                        isConfigured = grabbable != null;
                    }
                    else if (tag == "snap")
                    {
                        // AutoHands PlacePoint validation using reflection
                        var components = obj.GetComponents<MonoBehaviour>();
                        foreach (var component in components)
                        {
                            if (component != null && component.GetType().Name == "PlacePoint")
                            {
                                isConfigured = true;
                                break;
                            }
                        }
                    }
                    else if (tag == "teleportPoint")
                    {
                        // TeleportController validation
                        var teleportController = obj.GetComponent<TeleportController>();
                        isConfigured = teleportController != null;
                    }
                }
                
                string statusIcon = isConfigured ? "✓" : "○";
                GUIStyle statusStyle = isConfigured ? successStyle : GUI.skin.label;
                
                // Object name
                EditorGUILayout.LabelField($"{statusIcon} {obj.name}", statusStyle, GUILayout.Width(200));
                
                // Layer mask dropdown (only if configured and XRI framework)
                if (isConfigured)
                {
                    if (currentFramework == VRFramework.XRI && (interactable != null || socketInteractor != null))
                    {
                        EditorGUI.BeginChangeCheck();

                        LayerMask currentMask = 0;
                        if (interactable != null)
                            currentMask = interactable.interactionLayers.value;
                        else if (socketInteractor != null)
                            currentMask = socketInteractor.interactionLayers.value;

                        // Create a dropdown for interaction layers
                        LayerMask newMask = DrawInteractionLayerMask(currentMask, GUILayout.Width(150));

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(obj, "Change Interaction Layer");
                            if (interactable != null)
                            {
                                var layers = interactable.interactionLayers;
                                layers.value = newMask;
                                interactable.interactionLayers = layers;
                            }
                            else if (socketInteractor != null)
                            {
                                var layers = socketInteractor.interactionLayers;
                                layers.value = newMask;
                                socketInteractor.interactionLayers = layers;
                            }
                            EditorUtility.SetDirty(obj);
                        }
                    }
                    else if (currentFramework == VRFramework.AutoHands)
                    {
                        // AutoHands doesn't use XRI interaction layers
                        EditorGUILayout.LabelField("✓ Configured (AutoHands)", EditorStyles.miniLabel, GUILayout.Width(150));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Default", EditorStyles.miniLabel, GUILayout.Width(150));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Configure first", EditorStyles.miniLabel, GUILayout.Width(150));
                }
                
                // Configure button (individual)
                if (GUILayout.Button("Configure", GUILayout.Width(70)))
                {
                    if (profile != null)
                    {
                        InteractionSetupService.ApplyComponentsToObjects(new List<GameObject> { obj }, profile);
                        EditorUtility.DisplayDialog("Configuration Complete", 
                            $"Applied {profile.profileName} to {obj.name}", "OK");
                        
                        // Refresh analysis after configuration
                        sceneAnalysis = InteractionSetupService.ScanScene();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Profile", 
                            $"Please select a profile for {tag} objects in the Configure tab", "OK");
                    }
                }
                
                // Select button
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
        else if (objects.Count > 20)
        {
            EditorGUILayout.LabelField($"  (Too many to list - use 'Select All' to view)", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // Helper method to draw interaction layer mask dropdown
    private LayerMask DrawInteractionLayerMask(LayerMask mask, params GUILayoutOption[] options)
    {
        return InteractionLayerManager.DrawLayerMaskDropdown(mask, options);
    }
    private object GetInteractionLayerSettings()
    {
        // This would need to access Unity's XRI InteractionLayerSettings
        // For now, returning null as placeholder
        return null;
    }
    
    private void DrawConfigureTab()
    {
        // Delegate to ConfigureTabDrawer (Phase 3 extraction)
        _configureTabDrawer?.Draw();
    }
    
    private void DrawSequenceTab()
    {
        EditorGUILayout.LabelField("Training Sequence Builder", headerStyle);
        EditorGUILayout.Space(5);
        
        // Load available assets if needed
        if (!assetsLoaded)
        {
            LoadAvailableTrainingAssets();
        }
        
        // Asset selection bar
        DrawAssetSelectionBar();
        
        EditorGUILayout.Space(10);
        
        // Debug information
        if (currentTrainingAsset == null)
        {
            EditorGUILayout.HelpBox("currentTrainingAsset is null. Try selecting an asset from the dropdown above.", MessageType.Warning);
            return;
        }
        
        if (currentProgram == null)
        {
            EditorGUILayout.HelpBox($"currentProgram is null for asset: {currentTrainingAsset.name}. The asset may be corrupted.", MessageType.Error);
            return;
        }
        
        // Main content area with two-panel layout
        DrawTwoPanelLayout();
    }
    
    /// <summary>
    /// Draw the main two-panel editing interface
    /// </summary>
    private void DrawTwoPanelLayout()
    {
        // Use horizontal layout for simplicity
        EditorGUILayout.BeginHorizontal();

        // Tree view panel (left) - 40% width
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * splitterPosition));
        // Delegate to SequenceTreeView (Phase 4 extraction)
        _sequenceTreeView?.Draw(currentProgram);
        EditorGUILayout.EndVertical();

        // Details panel (right) - 60% width
        EditorGUILayout.BeginVertical();
        DrawDetailsContent();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // NOTE: DrawTreeViewContent moved to SequenceTreeView (Phase 4)
    
    /// <summary>
    /// Draw the details content (right side)
    /// </summary>
    private void DrawDetailsContent()
    {
        EditorGUILayout.BeginVertical("box");

        try
        {
            // Header
            EditorGUILayout.LabelField("Properties", subHeaderStyle);
            EditorGUILayout.Space(5);

            // Content based on selection
            detailsPanelScrollPos = EditorGUILayout.BeginScrollView(detailsPanelScrollPos, GUILayout.ExpandHeight(true));

            try
            {
                if (selectedHierarchyItem == null)
                {
                    EditorGUILayout.HelpBox("Select an item from the hierarchy to edit its properties.", MessageType.Info);
                }
                else
                {
                    DrawSelectedItemProperties();
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing properties: {e.Message}", MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
    }
    

    // NOTE: DrawProgramTreeItem, DrawModuleTreeItem, DrawTaskGroupTreeItem, DrawStepTreeItem,
    // GetStepTypeIcon, SelectItem moved to SequenceTreeView (Phase 4)

    /// <summary>
    /// Draw properties for the currently selected item
    /// </summary>
    private void DrawSelectedItemProperties()
    {
        EditorGUI.BeginChangeCheck();
        
        switch (selectedItemType)
        {
            case "program":
                DrawProgramProperties();
                break;
            case "module":
                DrawModuleProperties();
                break;
            case "taskgroup":
                DrawTaskGroupProperties();
                break;
            case "step":
                DrawStepProperties();
                break;
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            // Auto-save when changes are made
            AutoSaveCurrentAsset();
        }
    }
    
    /// <summary>
    /// Draw program properties
    /// </summary>
    private void DrawProgramProperties()
    {
        var program = (TrainingProgram)selectedHierarchyItem;
        
        EditorGUILayout.LabelField("Program Settings", EditorStyles.boldLabel);
        
        program.programName = EditorGUILayout.TextField("Program Name", program.programName);
        
        EditorGUILayout.LabelField("Description");
        program.description = EditorGUILayout.TextArea(program.description, GUILayout.Height(60));
        
        EditorGUILayout.Space(10);
        
        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        int moduleCount = program.modules?.Count ?? 0;
        int totalSteps = 0;
        int totalTaskGroups = 0;
        
        if (program.modules != null)
        {
            foreach (var module in program.modules)
            {
                if (module.taskGroups != null)
                {
                    totalTaskGroups += module.taskGroups.Count;
                    foreach (var taskGroup in module.taskGroups)
                    {
                        if (taskGroup.steps != null)
                            totalSteps += taskGroup.steps.Count;
                    }
                }
            }
        }
        
        EditorGUILayout.LabelField($"Modules: {moduleCount}");
        EditorGUILayout.LabelField($"Task Groups: {totalTaskGroups}");
        EditorGUILayout.LabelField($"Total Steps: {totalSteps}");
    }
    
    /// <summary>
    /// Draw module properties
    /// </summary>
    private void DrawModuleProperties()
    {
        var module = (TrainingModule)selectedHierarchyItem;
        
        EditorGUILayout.LabelField("Module Settings", EditorStyles.boldLabel);
        
        module.moduleName = EditorGUILayout.TextField("Module Name", module.moduleName);
        
        EditorGUILayout.LabelField("Description");
        module.description = EditorGUILayout.TextArea(module.description, GUILayout.Height(60));
        
        EditorGUILayout.Space(10);
        
        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        int taskGroupCount = module.taskGroups?.Count ?? 0;
        int stepCount = 0;
        
        if (module.taskGroups != null)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                if (taskGroup.steps != null)
                    stepCount += taskGroup.steps.Count;
            }
        }
        
        EditorGUILayout.LabelField($"Task Groups: {taskGroupCount}");
        EditorGUILayout.LabelField($"Total Steps: {stepCount}");
    }
    
    /// <summary>
    /// Draw task group properties
    /// </summary>
    private void DrawTaskGroupProperties()
    {
        var taskGroup = (TaskGroup)selectedHierarchyItem;
        
        EditorGUILayout.LabelField("Task Group Settings", EditorStyles.boldLabel);
        
        taskGroup.groupName = EditorGUILayout.TextField("Group Name", taskGroup.groupName);
        
        EditorGUILayout.LabelField("Description");
        taskGroup.description = EditorGUILayout.TextArea(taskGroup.description, GUILayout.Height(60));

        EditorGUILayout.Space(10);

        // PHASE 1: Sequential Flow Control
        EditorGUILayout.LabelField("Sequential Flow Control", EditorStyles.boldLabel);

        taskGroup.enforceSequentialFlow = EditorGUILayout.Toggle(
            new GUIContent("Enforce Sequential Flow",
                "Task group level socket restrictions. Current task group sockets enabled, others disabled."),
            taskGroup.enforceSequentialFlow
        );

        if (taskGroup.enforceSequentialFlow)
        {
            EditorGUILayout.HelpBox(
                "🔒 Task Group Socket Restrictions\n\n" +
                "• All sockets in CURRENT task group are enabled\n" +
                "• All sockets in OTHER task groups are disabled\n" +
                "• Steps within task group can be done in any order\n" +
                "• Prevents placing objects in wrong task group sockets\n" +
                "• Grabbable objects remain active (no grab restrictions)\n" +
                "• Check console for [SequenceFlowRestriction] logs",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "🌐 Free Exploration Mode\n\n" +
                "All sockets and grabbable objects are always enabled.",
                MessageType.None
            );
        }

        EditorGUILayout.Space(10);

        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        int stepCount = taskGroup.steps?.Count ?? 0;
        int validSteps = 0;
        
        if (taskGroup.steps != null)
        {
            foreach (var step in taskGroup.steps)
            {
                if (step.IsValid())
                    validSteps++;
            }
        }
        
        EditorGUILayout.LabelField($"Steps: {stepCount}");
        EditorGUILayout.LabelField($"Valid Steps: {validSteps}");
        EditorGUILayout.LabelField($"Invalid Steps: {stepCount - validSteps}");
    }
    
    /// <summary>
    /// Draw step properties
    /// </summary>
    private void DrawStepProperties()
    {
        var step = (InteractionStep)selectedHierarchyItem;
        
        EditorGUILayout.LabelField("Step Settings", EditorStyles.boldLabel);
        
        // Basic properties
        step.stepName = EditorGUILayout.TextField("Step Name", step.stepName);
        step.type = (InteractionStep.StepType)EditorGUILayout.EnumPopup("Type", step.type);
        
        EditorGUILayout.Space(10);
        
        // Target objects based on type
        if (step.type == InteractionStep.StepType.Grab ||
            step.type == InteractionStep.StepType.GrabAndSnap ||
            step.type == InteractionStep.StepType.TurnKnob ||
            step.type == InteractionStep.StepType.WaitForScriptCondition)
        {
            EditorGUILayout.LabelField("Target Objects", EditorStyles.boldLabel);
            
            // Custom GameObject field using our drawer system
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Object", GUILayout.Width(100));
            
            GameObject currentTarget = step.targetObject.GameObject;
            GameObject newTarget = (GameObject)EditorGUILayout.ObjectField(currentTarget, typeof(GameObject), true);
            
            if (newTarget != currentTarget)
            {
                step.targetObject.GameObject = newTarget;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (step.type == InteractionStep.StepType.GrabAndSnap)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Destination", GUILayout.Width(100));
                
                GameObject currentDest = step.destination.GameObject;
                GameObject newDest = (GameObject)EditorGUILayout.ObjectField(currentDest, typeof(GameObject), true);
                
                if (newDest != currentDest)
                {
                    step.destination.GameObject = newDest;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        // WaitForScriptCondition-specific info
        if (step.type == InteractionStep.StepType.WaitForScriptCondition)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Target Object must have a component that implements ISequenceCondition interface.\n\n" +
                "Available condition types:\n" +
                "• DummyCondition (for testing)\n" +
                "• ButtonPressCondition\n" +
                "• Custom conditions (inherit from BaseSequenceCondition)",
                MessageType.Info);
        }

        // Knob-specific settings
        if (step.type == InteractionStep.StepType.TurnKnob)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Knob Settings", EditorStyles.boldLabel);
            step.targetAngle = EditorGUILayout.FloatField("Target Angle", step.targetAngle);
            step.angleTolerance = EditorGUILayout.FloatField("Angle Tolerance", step.angleTolerance);

            // Rotation direction dropdown
            EditorGUILayout.Space(3);
            step.knobRotationType = (InteractionStep.KnobRotationType)EditorGUILayout.EnumPopup(
                new GUIContent("Rotation Direction", "Required rotation direction based on HingeJoint limits"),
                step.knobRotationType
            );

            // Help text for rotation direction
            string directionHelp = step.knobRotationType switch
            {
                InteractionStep.KnobRotationType.OpenToMax => "Opening: Rotate toward max limit (increasing angle)",
                InteractionStep.KnobRotationType.CloseToMin => "Closing: Rotate toward min limit (decreasing angle)",
                InteractionStep.KnobRotationType.Any => "Any direction is acceptable",
                _ => ""
            };
            if (!string.IsNullOrEmpty(directionHelp))
            {
                EditorGUILayout.HelpBox(directionHelp, MessageType.Info);
            }
        }
        
        // Valve-specific settings
        if (step.type == InteractionStep.StepType.TightenValve ||
            step.type == InteractionStep.StepType.LoosenValve ||
            step.type == InteractionStep.StepType.InstallValve ||
            step.type == InteractionStep.StepType.RemoveValve)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Valve Settings", EditorStyles.boldLabel);
            
            // Target Object field (valve)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Object", GUILayout.Width(100));
            step.targetObject.GameObject = (GameObject)EditorGUILayout.ObjectField(
                step.targetObject.GameObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            // Target Socket field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Socket", GUILayout.Width(100));
            step.targetSocket.GameObject = (GameObject)EditorGUILayout.ObjectField(
                step.targetSocket.GameObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            // Rotation axis selection
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Rotation Axis");
            EditorGUILayout.BeginHorizontal();
            
            bool isXAxis = step.rotationAxis == Vector3.right;
            bool isYAxis = step.rotationAxis == Vector3.up;
            bool isZAxis = step.rotationAxis == Vector3.forward;
            
            if (GUILayout.Toggle(isXAxis, "X-Axis") && !isXAxis) step.rotationAxis = Vector3.right;
            if (GUILayout.Toggle(isYAxis, "Y-Axis") && !isYAxis) step.rotationAxis = Vector3.up;
            if (GUILayout.Toggle(isZAxis, "Z-Axis") && !isZAxis) step.rotationAxis = Vector3.forward;
            
            EditorGUILayout.EndHorizontal();
            
            // Threshold settings based on step type
            if (step.type == InteractionStep.StepType.TightenValve || 
                step.type == InteractionStep.StepType.InstallValve)
            {
                EditorGUILayout.Space(3);
                step.tightenThreshold = EditorGUILayout.Slider("Tighten Degrees", step.tightenThreshold, 10f, 360f);
            }
            
            if (step.type == InteractionStep.StepType.LoosenValve ||
                step.type == InteractionStep.StepType.RemoveValve)
            {
                EditorGUILayout.Space(3);  
                step.loosenThreshold = EditorGUILayout.Slider("Loosen Degrees", step.loosenThreshold, 10f, 360f);
            }
            
            if (step.type == InteractionStep.StepType.InstallValve ||
                step.type == InteractionStep.StepType.RemoveValve)
            {
                // Complete operations show both thresholds
                EditorGUILayout.Space(3);
                step.tightenThreshold = EditorGUILayout.Slider("Tighten Degrees", step.tightenThreshold, 10f, 360f);
                step.loosenThreshold = EditorGUILayout.Slider("Loosen Degrees", step.loosenThreshold, 10f, 360f);
            }
            
            // Common settings
            EditorGUILayout.Space(3);
            step.valveAngleTolerance = EditorGUILayout.Slider("Angle Tolerance", step.valveAngleTolerance, 1f, 15f);
            
            // Advanced settings
            EditorGUILayout.Space(3);
            step.rotationDampening = EditorGUILayout.Slider("Rotation Dampening", step.rotationDampening, 0f, 10f);
            if (step.rotationDampening == 0f)
            {
                EditorGUILayout.HelpBox("Set to 0 to use profile default", MessageType.Info);
            }
        }

        // Teleport-specific settings
        if (step.type == InteractionStep.StepType.Teleport)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🚀 Teleport Settings", EditorStyles.boldLabel);

            // Wrist Button field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist Button", GUILayout.Width(100));
            GameObject currentButton = step.wristButton.GameObject;
            GameObject newButton = (GameObject)EditorGUILayout.ObjectField(currentButton, typeof(GameObject), true);
            if (newButton != currentButton)
            {
                step.wristButton.GameObject = newButton;
            }
            EditorGUILayout.EndHorizontal();

            // Validate WristUIButton component
            if (step.wristButton.GameObject != null)
            {
                var wristUIButton = step.wristButton.GameObject.GetComponent<WristUIButton>();
                if (wristUIButton == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Selected GameObject does not have WristUIButton component!", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ Valid WristUIButton component found", MessageType.Info);
                }
            }

            EditorGUILayout.Space(3);

            // Teleport Destination field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Destination", GUILayout.Width(100));
            GameObject currentDest = step.teleportDestination.GameObject;
            GameObject newDest = (GameObject)EditorGUILayout.ObjectField(currentDest, typeof(GameObject), true);
            if (newDest != currentDest)
            {
                step.teleportDestination.GameObject = newDest;
            }
            EditorGUILayout.EndHorizontal();

            // Validate TeleportController component
            if (step.teleportDestination.GameObject != null)
            {
                var teleportController = step.teleportDestination.GameObject.GetComponent<TeleportController>();
                if (teleportController == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Selected GameObject does not have TeleportController component!", MessageType.Warning);
                }
                else
                {
                    // Show TeleportController info
                    string info = $"✅ TeleportController found\n" +
                                 $"Recentering: {(teleportController.enableRecentering ? "Enabled" : "Disabled")}\n" +
                                 $"Preview: {(teleportController.showDestinationPreview ? "Visible" : "Hidden")}";
                    if (teleportController.autoHandPlayerReference == null)
                    {
                        info += "\n⚠️ AutoHandPlayer reference not set on controller!";
                    }
                    EditorGUILayout.HelpBox(info, teleportController.autoHandPlayerReference == null ? MessageType.Warning : MessageType.Info);
                }

                // Check for teleportPoint tag
                if (!step.teleportDestination.GameObject.CompareTag("teleportPoint"))
                {
                    EditorGUILayout.HelpBox("⚠️ Destination should be tagged as 'teleportPoint' for consistency", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(3);

            // Recentering settings
            EditorGUILayout.LabelField("Recentering Settings", EditorStyles.boldLabel);
            step.enableRecentering = EditorGUILayout.Toggle("Enable Recentering", step.enableRecentering);

            if (step.enableRecentering)
            {
                step.recenteringDelay = EditorGUILayout.Slider("Recentering Delay", step.recenteringDelay, 0f, 2f);
                EditorGUILayout.HelpBox("XR tracking origin will recenter after teleport", MessageType.Info);
            }
        }

        // Execution settings
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Execution Settings", EditorStyles.boldLabel);
        step.allowParallel = EditorGUILayout.Toggle("Allow Parallel", step.allowParallel);
        step.isOptional = EditorGUILayout.Toggle("Is Optional", step.isOptional);
        
        // Hint
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Instruction");
        step.hint = EditorGUILayout.TextArea(step.hint, GUILayout.Height(40));

        // Guidance Arrows
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Guidance Arrows", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Place arrow GameObjects independently in scene, then reference them here. Arrows will show/hide automatically based on step progress.", MessageType.Info);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Target Arrow", EditorStyles.miniBoldLabel);
        step.targetArrow.GameObject = (GameObject)EditorGUILayout.ObjectField(
            "Arrow GameObject",
            step.targetArrow.GameObject,
            typeof(GameObject),
            true
        );

        if (step.targetArrow.GameObject != null)
        {
            EditorGUI.indentLevel++;
            step.hideTargetArrowAfterGrab = EditorGUILayout.Toggle("Hide After Grab", step.hideTargetArrowAfterGrab);
            EditorGUI.indentLevel--;

            // Validate arrow has GuidanceArrow component
            if (step.targetArrow.GameObject.GetComponent<GuidanceArrow>() == null)
            {
                EditorGUILayout.HelpBox("Warning: Arrow GameObject needs GuidanceArrow component!", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Destination Arrow (Optional)", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("For GrabAndSnap/Valve steps - shows after object is grabbed", EditorStyles.miniLabel);
        step.destinationArrow.GameObject = (GameObject)EditorGUILayout.ObjectField(
            "Arrow GameObject",
            step.destinationArrow.GameObject,
            typeof(GameObject),
            true
        );

        if (step.destinationArrow.GameObject != null)
        {
            EditorGUI.indentLevel++;
            step.showDestinationAfterGrab = EditorGUILayout.Toggle("Show After Grab", step.showDestinationAfterGrab);
            EditorGUI.indentLevel--;

            // Validate arrow has GuidanceArrow component
            if (step.destinationArrow.GameObject.GetComponent<GuidanceArrow>() == null)
            {
                EditorGUILayout.HelpBox("Warning: Arrow GameObject needs GuidanceArrow component!", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();

        // Validation
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        
        bool isValid = step.IsValid();
        string validationMessage = step.GetValidationMessage();
        
        GUIStyle validationStyle = isValid ? successStyle : errorStyle;
        EditorGUILayout.LabelField($"Status: {validationMessage}", validationStyle);
        
        if (!isValid)
        {
            EditorGUILayout.HelpBox("This step has validation errors. Check the target objects and settings above.", MessageType.Warning);
        }
    }
    
    // NOTE: ShowAddMenu, ShowAddTaskGroupMenu, ShowAddStepMenu,
    // AddNewModule, AddNewTaskGroup, AddNewStep,
    // DeleteModule, DeleteTaskGroup, DeleteStep moved to SequenceTreeView (Phase 4)

    private void LoadAvailableTrainingAssets()
    {
        #if UNITY_EDITOR
        availableAssets = TrainingSequenceAssetManager.LoadAllSequenceAssets();
        assetsLoaded = true;
        
        // Auto-select first asset if available
        if (availableAssets.Length > 0 && currentTrainingAsset == null)
        {
            selectedAssetIndex = 0;
            LoadTrainingAsset(availableAssets[0]);
        }
        #endif
    }
    
    private void DrawAssetSelectionBar()
    {
        EditorGUILayout.BeginHorizontal();
        
        // Asset dropdown
        if (availableAssets != null && availableAssets.Length > 0)
        {
            EditorGUILayout.LabelField("Program:", GUILayout.Width(60));
            
            string[] assetNames = new string[availableAssets.Length];
            for (int i = 0; i < availableAssets.Length; i++)
            {
                assetNames[i] = availableAssets[i] != null ? availableAssets[i].name : "Missing Asset";
            }
            
            EditorGUI.BeginChangeCheck();
            selectedAssetIndex = EditorGUILayout.Popup(selectedAssetIndex, assetNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedAssetIndex >= 0 && selectedAssetIndex < availableAssets.Length)
                {
                    LoadTrainingAsset(availableAssets[selectedAssetIndex]);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No training sequence assets found in project");
        }
        
        // Control buttons
        if (GUILayout.Button("New", GUILayout.Width(50)))
        {
            CreateNewTrainingAsset();
        }
        
        if (GUILayout.Button("Load", GUILayout.Width(50)))
        {
            LoadAvailableTrainingAssets();
        }
        
        if (GUILayout.Button("Save", GUILayout.Width(50)) && currentTrainingAsset != null)
        {
            SaveCurrentAsset();
        }

        // Info about keyboard shortcuts
        if (currentTrainingAsset != null)
        {
            GUILayout.Label("💡 Ctrl+S: Force Save | Ctrl+R: Refresh References", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void LoadTrainingAsset(TrainingSequenceAsset asset)
    {
        currentTrainingAsset = asset;
        currentProgram = asset?.Program;
        
        if (currentTrainingAsset != null)
        {
            Debug.Log($"Loaded training asset: {currentTrainingAsset.name}");
            var stats = currentTrainingAsset.GetStats();
            Debug.Log($"Asset stats: {stats}");
        }
    }
    
    private void CreateNewTrainingAsset()
    {
        #if UNITY_EDITOR
        // Show options for template or empty
        if (EditorUtility.DisplayDialog("Create New Training Sequence", 
            "Would you like to start with the HVAC template or create an empty sequence?", 
            "HVAC Template", "Empty Sequence"))
        {
            var asset = TrainingSequenceAssetManager.CreateHVACTemplateAsset();
            TrainingSequenceAssetManager.SaveAssetToSequencesFolder(asset);
        }
        else
        {
            var asset = TrainingSequenceAssetManager.CreateEmptyAsset();
            TrainingSequenceAssetManager.SaveAssetToSequencesFolder(asset);
        }
        
        // Reload assets list
        LoadAvailableTrainingAssets();
        #endif
    }
    
    private void SaveCurrentAsset()
    {
        if (currentTrainingAsset != null)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentTrainingAsset);
            UnityEditor.AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Save Complete", $"Saved {currentTrainingAsset.name}", "OK");
            #endif
        }
    }

    /// <summary>
    /// Auto-saves the current asset without showing dialog
    /// Called automatically when data changes
    /// </summary>
    private void AutoSaveCurrentAsset()
    {
        if (currentTrainingAsset != null)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentTrainingAsset);
            // Note: Don't call SaveAssets() here as it can cause performance issues
            // Unity will save automatically when needed (scene save, project save, etc.)
            Debug.Log($"[VRTrainingKit] Auto-saved training asset: {currentTrainingAsset.name}");
            #endif
        }
    }

    /// <summary>
    /// Force saves all assets (equivalent to Ctrl+S)
    /// </summary>
    private void ForceSaveAllAssets()
    {
        if (currentTrainingAsset != null)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentTrainingAsset);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[VRTrainingKit] Force saved all assets including: {currentTrainingAsset.name}");
            #endif
        }
    }

    private void DrawRuntimeMonitorTab()
    {
        EditorGUILayout.LabelField("Runtime Monitor", headerStyle);
        EditorGUILayout.Space(5);

        // Play Mode check
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("⏸️ Runtime Monitor is only available in Play Mode.\n\nEnter Play Mode to view real-time sequence execution status.", MessageType.Info);
            return;
        }

        // Find the active controller in the scene
        var controller = FindObjectOfType<ModularTrainingSequenceController>();
        if (controller == null)
        {
            EditorGUILayout.HelpBox("❌ No ModularTrainingSequenceController found in scene.\n\nMake sure your scene has a GameObject with the ModularTrainingSequenceController component.", MessageType.Warning);
            return;
        }

        // Begin scrollable area
        EditorGUILayout.BeginVertical();

        var progress = controller.GetProgress();

        // ===== PROGRAM OVERVIEW SECTION =====
        EditorGUILayout.LabelField("Program Overview", subHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (controller.currentProgram != null)
        {
            EditorGUILayout.LabelField($"📋 Program: {controller.currentProgram.programName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Show all modules and their task groups
            for (int moduleIdx = 0; moduleIdx < controller.currentProgram.modules.Count; moduleIdx++)
            {
                var module = controller.currentProgram.modules[moduleIdx];
                bool isCurrentModule = moduleIdx == progress.currentModuleIndex;
                bool isCompletedModule = moduleIdx < progress.currentModuleIndex;

                // Module header
                string moduleIcon = isCompletedModule ? "✅" : (isCurrentModule ? "🟢" : "⏸️");
                GUIStyle moduleStyle = new GUIStyle(EditorStyles.label);
                if (isCurrentModule)
                {
                    moduleStyle.fontStyle = FontStyle.Bold;
                    moduleStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
                }
                else if (isCompletedModule)
                {
                    moduleStyle.normal.textColor = new Color(0.3f, 0.7f, 0.3f);
                }
                else
                {
                    moduleStyle.normal.textColor = Color.gray;
                }

                EditorGUILayout.LabelField($"{moduleIcon} Module: {module.moduleName}", moduleStyle);

                // Show task groups for current or adjacent modules only (for cleaner UI)
                if (moduleIdx >= progress.currentModuleIndex - 1 && moduleIdx <= progress.currentModuleIndex + 1)
                {
                    for (int tgIdx = 0; tgIdx < module.taskGroups.Count; tgIdx++)
                    {
                        var taskGroup = module.taskGroups[tgIdx];
                        bool isCurrentTaskGroup = isCurrentModule && tgIdx == progress.currentTaskGroupIndex;
                        bool isCompletedTaskGroup = isCurrentModule ? (tgIdx < progress.currentTaskGroupIndex) : isCompletedModule;
                        bool isUpcomingTaskGroup = !isCurrentTaskGroup && !isCompletedTaskGroup;

                        string tgIcon = isCompletedTaskGroup ? "✅" : (isCurrentTaskGroup ? "🟢" : "⏸️");
                        GUIStyle tgStyle = new GUIStyle(EditorStyles.label);

                        if (isCurrentTaskGroup)
                        {
                            tgStyle.fontStyle = FontStyle.Bold;
                            tgStyle.normal.textColor = new Color(0.2f, 0.6f, 1f);
                        }
                        else if (isCompletedTaskGroup)
                        {
                            tgStyle.normal.textColor = new Color(0.4f, 0.7f, 0.4f);
                        }
                        else
                        {
                            tgStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                        }

                        string stepInfo = "";
                        if (isCurrentTaskGroup)
                        {
                            stepInfo = $" ({progress.completedSteps}/{progress.totalSteps} steps)";
                        }

                        EditorGUILayout.LabelField($"   {tgIcon} {taskGroup.groupName}{stepInfo}", tgStyle);
                    }
                }
                else
                {
                    // Show count only for distant modules
                    EditorGUILayout.LabelField($"   ... {module.taskGroups.Count} task groups", EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(3);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No program loaded", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ===== SEQUENCE STATUS SECTION =====
        EditorGUILayout.LabelField("Current Status", subHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"Current Module: {progress.currentModuleName ?? "None"}");
        EditorGUILayout.LabelField($"Current Task Group: {progress.currentTaskGroupName ?? "None"}");

        // Check if sequential flow is enabled
        var currentTaskGroup = controller.currentProgram?.modules?[progress.currentModuleIndex]?.taskGroups?[progress.currentTaskGroupIndex];
        bool sequentialFlowEnabled = currentTaskGroup != null && currentTaskGroup.enforceSequentialFlow;

        EditorGUILayout.LabelField($"Sequential Flow: {(sequentialFlowEnabled ? "✅ Enabled" : "❌ Disabled")}");

        // Progress bar
        float progressPercent = progress.totalSteps > 0 ? (float)progress.completedSteps / progress.totalSteps : 0f;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progressPercent, $"{progress.completedSteps}/{progress.totalSteps} steps ({progressPercent:P0})");

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ===== STEP PROGRESS SECTION =====
        EditorGUILayout.LabelField("Step Progress", subHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (currentTaskGroup != null && currentTaskGroup.steps != null && currentTaskGroup.steps.Count > 0)
        {
            foreach (var step in currentTaskGroup.steps)
            {
                string icon = GetStepIcon(step);
                string status = GetStepStatus(step);
                GUIStyle stepStyle = GetStepStyle(step);

                EditorGUILayout.LabelField($"{icon} {step.stepName} ({status})", stepStyle);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No steps in current task group", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ===== SOCKET STATES SECTION =====
        EditorGUILayout.LabelField("Socket States", subHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (controller.restrictionManager != null && sequentialFlowEnabled)
        {
            var socketStates = controller.restrictionManager.GetSocketStates();

            if (socketStates != null && socketStates.Count > 0)
            {
                // Group by state for better readability
                var enabledSockets = socketStates.Where(s => s.isEnabled).ToList();
                var disabledSockets = socketStates.Where(s => !s.isEnabled).ToList();

                if (enabledSockets.Count > 0)
                {
                    EditorGUILayout.LabelField($"✅ Enabled Sockets ({enabledSockets.Count})", EditorStyles.boldLabel);
                    foreach (var socket in enabledSockets)
                    {
                        string occupiedLabel = socket.isOccupied ? " [Occupied]" : "";
                        EditorGUILayout.LabelField($"   • {socket.socketName}{occupiedLabel} - {socket.disabledReason}");
                    }
                    EditorGUILayout.Space(5);
                }

                if (disabledSockets.Count > 0)
                {
                    EditorGUILayout.LabelField($"❌ Disabled Sockets ({disabledSockets.Count})", EditorStyles.boldLabel);
                    foreach (var socket in disabledSockets)
                    {
                        EditorGUILayout.LabelField($"   • {socket.socketName} - {socket.disabledReason}", EditorStyles.helpBox);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No socket components found in scene", EditorStyles.centeredGreyMiniLabel);
            }
        }
        else if (!sequentialFlowEnabled)
        {
            EditorGUILayout.LabelField("⚠️ Sequential flow is not enabled for current task group", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Socket restrictions are not active - all sockets are available", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Restriction manager not initialized", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ===== UTILITY BUTTONS SECTION =====
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔄 Refresh", GUILayout.Height(30)))
        {
            Repaint(); // Force window repaint
        }

        GUI.enabled = controller.restrictionManager != null && sequentialFlowEnabled;
        if (GUILayout.Button("🔓 Enable All Sockets", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Enable All Sockets",
                "This will temporarily enable all sockets, bypassing sequence restrictions.\n\nThis is for debugging purposes only and will be reset when the task group changes.\n\nContinue?",
                "Yes", "Cancel"))
            {
                controller.restrictionManager.Reset();
                Debug.Log("[RuntimeMonitor] All sockets re-enabled for debugging");
            }
        }

        if (GUILayout.Button("🔄 Reset Sequence", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Reset Sequence",
                "This will stop Play Mode and reload the scene.\n\nAny unsaved changes will be lost.\n\nContinue?",
                "Yes", "Cancel"))
            {
                EditorApplication.isPlaying = false;
            }
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // Auto-refresh when in Play Mode
        if (EditorApplication.isPlaying)
        {
            Repaint();
        }
    }

    // Helper methods for DrawRuntimeMonitorTab
    private string GetStepIcon(InteractionStep step)
    {
        if (step.isCompleted) return "✅";

        // Check if step is currently active
        // We consider a step active if it's not completed
        return step.isCompleted ? "✅" : "🟢";
    }

    private string GetStepStatus(InteractionStep step)
    {
        if (step.isCompleted) return "Completed";
        return "Active - In Progress";
    }

    private GUIStyle GetStepStyle(InteractionStep step)
    {
        if (step.isCompleted)
        {
            var completedStyle = new GUIStyle(EditorStyles.label);
            completedStyle.normal.textColor = new Color(0.3f, 0.7f, 0.3f); // Green
            return completedStyle;
        }
        else
        {
            var activeStyle = new GUIStyle(EditorStyles.label);
            activeStyle.normal.textColor = new Color(0.2f, 0.6f, 1f); // Blue
            activeStyle.fontStyle = FontStyle.Bold;
            return activeStyle;
        }
    }

    private void DrawValidateTab()
    {
        EditorGUILayout.LabelField("Setup Validation", headerStyle);
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            validationIssues = InteractionSetupService.ValidateSetup();
        }
        
        EditorGUILayout.Space(10);
        
        if (validationIssues.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {validationIssues.Count} issues:", warningStyle);
            
            validateScrollPos = EditorGUILayout.BeginScrollView(validateScrollPos);
            
            foreach (var issue in validationIssues)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
                EditorGUILayout.LabelField(issue, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        else if (validationIssues != null)
        {
            EditorGUILayout.LabelField("✓ All checks passed!", successStyle);
        }
    }
    
    private void ApplyAllComponents()
    {
        if (sceneAnalysis == null) return;
        
        int appliedCount = 0;
        
        if (selectedGrabProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.grabObjects, selectedGrabProfile);
            appliedCount += sceneAnalysis.grabObjects.Count;
        }
        
        if (selectedKnobProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.knobObjects, selectedKnobProfile);
            appliedCount += sceneAnalysis.knobObjects.Count;
        }
        
        if (selectedSnapProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.snapObjects, selectedSnapProfile);
            appliedCount += sceneAnalysis.snapObjects.Count;
        }
        
        if (selectedToolProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.toolObjects, selectedToolProfile);
            appliedCount += sceneAnalysis.toolObjects.Count;
        }
        
        if (selectedValveProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.valveObjects, selectedValveProfile);
            appliedCount += sceneAnalysis.valveObjects.Count;
        }
        
        EditorUtility.DisplayDialog("Setup Complete", 
            $"Successfully configured {appliedCount} objects", "OK");
        
        // Refresh the scene analysis
        sceneAnalysis = InteractionSetupService.ScanScene();
    }
    
    // NOTE: CreateNewProfile<T> and CreateDefaultProfiles moved to ConfigureTabDrawer (Phase 3)

    private void SetupACLeakTestingSequence()
    {
        if (sequenceController == null) return;
        
        // Clear existing state groups
        sequenceController.stateGroups.Clear();
        
        // Create Initial Setup state
        var initialState = new LegacySequenceController.StateGroup();
        initialState.groupName = "Initial Setup";
        initialState.isActive = true;
        initialState.allowedActions = new List<string> { "Hose_Male_Connector", "Hose_Female_Connector" };
        initialState.lockedActions = new List<string> { "Nitrogen_Cylinder_Valve" };
        
        // Create System Ready state
        var systemReadyState = new LegacySequenceController.StateGroup();
        systemReadyState.groupName = "System Ready";
        systemReadyState.isActive = false;
        systemReadyState.allowedActions = new List<string> { "Nitrogen_Cylinder_Valve", "Gauge_Adjustment_Knob" };
        
        // Add conditions for System Ready (all hoses connected)
        var allConnectedCondition = new LegacySequenceController.StateGroup.Condition();
        allConnectedCondition.type = LegacySequenceController.StateGroup.Condition.ConditionType.AllObjectsSnapped;
        systemReadyState.activationConditions.Add(allConnectedCondition);
        
        // Add states to controller
        sequenceController.stateGroups.Add(initialState);
        sequenceController.stateGroups.Add(systemReadyState);
        
        // Mark the controller as dirty so changes are saved
        EditorUtility.SetDirty(sequenceController);
        
        EditorUtility.DisplayDialog("Sequence Setup Complete",
            "AC Leak Testing sequence has been configured.\n\n" +
            "Next steps:\n" +
            "1. Add SequenceValidator components to the nitrogen cylinder valve\n" +
            "2. Set Required State Group to 'System Ready'",
            "OK");
    }

    /// <summary>
    /// Draws framework status information in the Setup tab
    /// </summary>
    private void DrawFrameworkStatus()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("🔧 VR Framework Status", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        // Detect current framework
        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var frameworkDisplayName = VRFrameworkDetector.GetFrameworkDisplayName(detectedFramework);
        var isFrameworkValid = VRFrameworkDetector.ValidateFrameworkSetup();

        // Framework detection display
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Detected Framework:", GUILayout.Width(140));

        // Framework status with color coding
        var originalColor = GUI.color;
        if (detectedFramework == VRFramework.None)
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("❌ " + frameworkDisplayName, EditorStyles.boldLabel);
        }
        else if (isFrameworkValid)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("✅ " + frameworkDisplayName, EditorStyles.boldLabel);
        }
        else
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("⚠️ " + frameworkDisplayName, EditorStyles.boldLabel);
        }
        GUI.color = originalColor;
        EditorGUILayout.EndHorizontal();

        // Framework Manager status (if available)
        var frameworkManager = VRFrameworkManager.Instance;
        if (frameworkManager != null)
        {
            var activeFramework = frameworkManager.GetActiveFramework();
            var hasMismatch = frameworkManager.HasFrameworkMismatch();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Framework:", GUILayout.Width(140));

            if (hasMismatch)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ " + VRFrameworkDetector.GetFrameworkDisplayName(activeFramework) + " (Mismatch)", EditorStyles.boldLabel);
                GUI.color = originalColor;
            }
            else
            {
                EditorGUILayout.LabelField(VRFrameworkDetector.GetFrameworkDisplayName(activeFramework));
            }
            EditorGUILayout.EndHorizontal();

            // Show mismatch warning
            if (hasMismatch)
            {
                EditorGUILayout.HelpBox(frameworkManager.GetFrameworkMismatchInfo(), MessageType.Warning);
            }
        }

        // Framework validation issues
        if (detectedFramework != VRFramework.None && !isFrameworkValid)
        {
            EditorGUILayout.HelpBox($"Framework setup issues detected. Use VR Training > Framework Validator for details.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    // NOTE: GetProfileFrameworkType moved to ConfigureTabDrawer (Phase 3)

    /// <summary>
    /// Profile type validation helper methods
    /// </summary>
    // Profile validation - Now delegates to ProfileCacheManager
    private bool IsGrabProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Grab) ?? false;

    private bool IsKnobProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Knob) ?? false;

    private bool IsSnapProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Snap) ?? false;

    private bool IsToolProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Tool) ?? false;

    private bool IsValveProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Valve) ?? false;

    private bool IsTurnProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Turn) ?? false;

    private bool IsTeleportProfile(InteractionProfile profile) =>
        _profileCacheManager?.IsValidProfile(profile, ProfileCacheManager.ProfileType.Teleport) ?? false;

    // NOTE: DrawConfigureFrameworkNotice moved to ConfigureTabDrawer (Phase 3)

    /// <summary>
    /// Check if Runtime Monitor tab should be visible
    /// Requires RuntimeMonitorSettings component in scene with showRuntimeMonitorTab enabled
    /// </summary>
    private bool IsRuntimeMonitorTabEnabled()
    {
        // Check if RuntimeMonitorSettings exists in scene and is enabled
        return RuntimeMonitorSettings.IsRuntimeMonitorEnabled();
    }

    // ==========================================
    // ISequenceTreeViewCallbacks Implementation (Phase 4)
    // ==========================================

    /// <summary>
    /// Called when an item is selected in the tree view
    /// </summary>
    public void OnItemSelected(object item, string itemType)
    {
        // Selection is now managed by SequenceTreeView via property accessors
        // This callback is for any additional actions needed on selection
        Repaint();
    }

    /// <summary>
    /// Called when the tree view needs to auto-save
    /// </summary>
    public void OnAutoSave()
    {
        AutoSaveCurrentAsset();
    }

    /// <summary>
    /// Called to request a repaint
    /// </summary>
    public void OnRequestRepaint()
    {
        Repaint();
    }
}
#endif
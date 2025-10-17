// VRInteractionSetupWindow.cs
// This file should be placed in an "Editor" folder in your Unity project

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class VRInteractionSetupWindow : EditorWindow
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
    
    // Configure tab - Support both XRI and AutoHands profiles
    private InteractionProfile selectedGrabProfile;
    private InteractionProfile selectedKnobProfile;
    private InteractionProfile selectedSnapProfile;
    private InteractionProfile selectedToolProfile;
    private InteractionProfile selectedValveProfile;
    private Vector2 configScrollPos;

    // Cache available profiles to avoid performance issues
    private List<InteractionProfile> cachedGrabProfiles;
    private List<InteractionProfile> cachedKnobProfiles;
    private List<InteractionProfile> cachedSnapProfiles;
    private List<InteractionProfile> cachedToolProfiles;
    private List<InteractionProfile> cachedValveProfiles;
    
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
    private Vector2 treeViewScrollPos;
    private Vector2 detailsPanelScrollPos;
    private object selectedHierarchyItem; // Can be TrainingModule, TaskGroup, or InteractionStep
    private string selectedItemType; // "module", "taskgroup", "step", "program"
    private bool showAddMenu = false;
    private float splitterPosition = 0.4f; // 40% tree view, 60% details panel
    
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
        RefreshGrabProfileCache();
        RefreshKnobProfileCache();
        RefreshSnapProfileCache();
        RefreshToolProfileCache();
        RefreshValveProfileCache();
    }

    private void RefreshGrabProfileCache()
    {
        cachedGrabProfiles = new List<InteractionProfile>();

        // Find XRI GrabProfile
        string[] xriGrabGuids = AssetDatabase.FindAssets("t:GrabProfile");
        foreach (string guid in xriGrabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsGrabProfile(profile))
            {
                cachedGrabProfiles.Add(profile);
            }
        }

        // Find AutoHands GrabProfile
        string[] autoHandsGrabGuids = AssetDatabase.FindAssets("t:AutoHandsGrabProfile");
        foreach (string guid in autoHandsGrabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsGrabProfile(profile))
            {
                cachedGrabProfiles.Add(profile);
            }
        }
    }

    private void RefreshKnobProfileCache()
    {
        cachedKnobProfiles = new List<InteractionProfile>();

        string[] xriKnobGuids = AssetDatabase.FindAssets("t:KnobProfile");
        foreach (string guid in xriKnobGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsKnobProfile(profile))
            {
                cachedKnobProfiles.Add(profile);
            }
        }

        string[] autoHandsKnobGuids = AssetDatabase.FindAssets("t:AutoHandsKnobProfile");
        foreach (string guid in autoHandsKnobGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsKnobProfile(profile))
            {
                cachedKnobProfiles.Add(profile);
            }
        }
    }

    private void RefreshSnapProfileCache()
    {
        cachedSnapProfiles = new List<InteractionProfile>();

        string[] xriSnapGuids = AssetDatabase.FindAssets("t:SnapProfile");
        foreach (string guid in xriSnapGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsSnapProfile(profile))
            {
                cachedSnapProfiles.Add(profile);
            }
        }

        string[] autoHandsSnapGuids = AssetDatabase.FindAssets("t:AutoHandsSnapProfile");
        foreach (string guid in autoHandsSnapGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsSnapProfile(profile))
            {
                cachedSnapProfiles.Add(profile);
            }
        }
    }

    private void RefreshToolProfileCache()
    {
        cachedToolProfiles = new List<InteractionProfile>();

        string[] xriToolGuids = AssetDatabase.FindAssets("t:ToolProfile");
        foreach (string guid in xriToolGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsToolProfile(profile))
            {
                cachedToolProfiles.Add(profile);
            }
        }

        string[] autoHandsToolGuids = AssetDatabase.FindAssets("t:AutoHandsToolProfile");
        foreach (string guid in autoHandsToolGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsToolProfile(profile))
            {
                cachedToolProfiles.Add(profile);
            }
        }
    }

    private void RefreshValveProfileCache()
    {
        cachedValveProfiles = new List<InteractionProfile>();

        string[] xriValveGuids = AssetDatabase.FindAssets("t:ValveProfile");
        foreach (string guid in xriValveGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsValveProfile(profile))
            {
                cachedValveProfiles.Add(profile);
            }
        }

        string[] autoHandsValveGuids = AssetDatabase.FindAssets("t:AutoHandsValveProfile");
        foreach (string guid in autoHandsValveGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsValveProfile(profile))
            {
                cachedValveProfiles.Add(profile);
            }
        }
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
        if (GUILayout.Toggle(currentTab == Tab.RuntimeMonitor, "Runtime Monitor", "Button"))
            currentTab = Tab.RuntimeMonitor;
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
                    if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve")
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
                    if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve")
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
        EditorGUILayout.LabelField("Profile Configuration", headerStyle);
        EditorGUILayout.Space(5);

        // Framework compatibility notice
        DrawConfigureFrameworkNotice();
        EditorGUILayout.Space(5);

        configScrollPos = EditorGUILayout.BeginScrollView(configScrollPos);
        
        // Grab Profile
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Grab Profile", subHeaderStyle);

        // Framework-aware ObjectField - accepts both XRI and AutoHands grab profiles
        var grabbableProfileTemp = EditorGUILayout.ObjectField(
            "Profile Asset", selectedGrabProfile, typeof(InteractionProfile), false) as InteractionProfile;

        // Validate that it's a grab-type profile
        if (grabbableProfileTemp != null && IsGrabProfile(grabbableProfileTemp))
        {
            selectedGrabProfile = grabbableProfileTemp; // No cast needed - keep as InteractionProfile
        }
        else if (grabbableProfileTemp != null && !IsGrabProfile(grabbableProfileTemp))
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{grabbableProfileTemp.name}' is not a grab-type profile.", "OK");
        }
        
        if (selectedGrabProfile == null)
        {
            // Use cached profiles for performance - no more per-frame AssetDatabase queries!
            if (cachedGrabProfiles != null && cachedGrabProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedGrabProfiles)
                {
                    if (profile != null) // Null check in case profile was deleted
                    {
                        EditorGUILayout.BeginHorizontal();
                        // Show framework type for clarity
                        string frameworkType = profile.GetType().Name.Contains("AutoHands") ? "[AutoHands]" : "[XRI]";
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedGrabProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No grab profiles found. Create one below.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Grab Profile"))
            {
                CreateNewProfile<GrabProfile>("GrabProfile");
                RefreshGrabProfileCache(); // Refresh cache after creating new profile
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                RefreshGrabProfileCache(); // Manual refresh button
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedGrabProfile;
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Knob Profile
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Knob Profile", subHeaderStyle);
        var knobProfileTemp = EditorGUILayout.ObjectField(
            "Profile Asset", selectedKnobProfile, typeof(InteractionProfile), false) as InteractionProfile;

        // Framework-aware ObjectField - accepts both XRI and AutoHands knob profiles
        if (knobProfileTemp != null && IsKnobProfile(knobProfileTemp))
        {
            selectedKnobProfile = knobProfileTemp;
        }
        else if (knobProfileTemp != null)
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{knobProfileTemp.name}' is not a knob-type profile.", "OK");
        }
        
        if (selectedKnobProfile == null)
        {
            // Use cached profiles for performance
            if (cachedKnobProfiles != null && cachedKnobProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedKnobProfiles)
                {
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string frameworkType = profile.GetType().Name.Contains("AutoHands") ? "[AutoHands]" : "[XRI]";
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedKnobProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No knob profiles found. Create one below.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Knob Profile"))
            {
                CreateNewProfile<KnobProfile>("KnobProfile");
                RefreshKnobProfileCache();
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                RefreshKnobProfileCache();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedKnobProfile;
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Snap Profile
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Snap Profile", subHeaderStyle);
        var snapProfileTemp = EditorGUILayout.ObjectField(
            "Profile Asset", selectedSnapProfile, typeof(InteractionProfile), false) as InteractionProfile;

        // Framework-aware ObjectField - accepts both XRI and AutoHands snap profiles
        if (snapProfileTemp != null && IsSnapProfile(snapProfileTemp))
        {
            selectedSnapProfile = snapProfileTemp;
        }
        else if (snapProfileTemp != null)
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{snapProfileTemp.name}' is not a snap-type profile.", "OK");
        }
        
        if (selectedSnapProfile == null)
        {
            // Use cached profiles for performance
            if (cachedSnapProfiles != null && cachedSnapProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedSnapProfiles)
                {
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string frameworkType = profile.GetType().Name.Contains("AutoHands") ? "[AutoHands]" : "[XRI]";
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedSnapProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No snap profiles found. Create one below.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Snap Profile"))
            {
                CreateNewProfile<SnapProfile>("SnapProfile");
                RefreshSnapProfileCache();
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                RefreshSnapProfileCache();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedSnapProfile;
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        // Tool Profile
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tool Profile", subHeaderStyle);
        var toolProfileTemp = EditorGUILayout.ObjectField(
            "Profile Asset", selectedToolProfile, typeof(InteractionProfile), false) as InteractionProfile;

        // Framework-aware ObjectField - accepts both XRI and AutoHands tool profiles
        if (toolProfileTemp != null && IsToolProfile(toolProfileTemp))
        {
            selectedToolProfile = toolProfileTemp;
        }
        else if (toolProfileTemp != null)
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{toolProfileTemp.name}' is not a tool-type profile.", "OK");
        }
        
        if (selectedToolProfile == null)
        {
            // Use cached profiles for performance
            if (cachedToolProfiles != null && cachedToolProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedToolProfiles)
                {
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string frameworkType = profile.GetType().Name.Contains("AutoHands") ? "[AutoHands]" : "[XRI]";
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedToolProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No tool profiles found. Create one below.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Tool Profile"))
            {
                CreateNewProfile<ToolProfile>("ToolProfile");
                RefreshToolProfileCache();
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                RefreshToolProfileCache();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedToolProfile;
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
        
        // Valve Profile
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Valve Profile", subHeaderStyle);
        var valveProfileTemp = EditorGUILayout.ObjectField(
            "Profile Asset", selectedValveProfile, typeof(InteractionProfile), false) as InteractionProfile;

        // Framework-aware ObjectField - accepts both XRI and AutoHands valve profiles
        if (valveProfileTemp != null && IsValveProfile(valveProfileTemp))
        {
            selectedValveProfile = valveProfileTemp;
        }
        else if (valveProfileTemp != null)
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{valveProfileTemp.name}' is not a valve-type profile.", "OK");
        }
        
        if (selectedValveProfile == null)
        {
            // Use cached profiles for performance
            if (cachedValveProfiles != null && cachedValveProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedValveProfiles)
                {
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string frameworkType = profile.GetType().Name.Contains("AutoHands") ? "[AutoHands]" : "[XRI]";
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedValveProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No valve profiles found. Create one below.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Valve Profile"))
            {
                CreateNewProfile<ValveProfile>("ValveProfile");
                RefreshValveProfileCache();
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                RefreshValveProfileCache();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedValveProfile;
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // Create default profiles button
        if (GUILayout.Button("Create All Default Profiles", GUILayout.Height(30)))
        {
            CreateDefaultProfiles();
        }
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
        DrawTreeViewContent();
        EditorGUILayout.EndVertical();
        
        // Details panel (right) - 60% width  
        EditorGUILayout.BeginVertical();
        DrawDetailsContent();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Draw the tree view content (left side)
    /// </summary>
    private void DrawTreeViewContent()
    {
        EditorGUILayout.BeginVertical("box");
        
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Hierarchy", subHeaderStyle);
        
        // Add menu button
        if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(20)))
        {
            ShowAddMenu();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
        
        // Tree view with scrolling
        treeViewScrollPos = EditorGUILayout.BeginScrollView(treeViewScrollPos, GUILayout.ExpandHeight(true));
        
        // Draw program header
        DrawProgramTreeItem();
        
        // Draw modules
        if (currentProgram != null && currentProgram.modules != null)
        {
            for (int moduleIndex = 0; moduleIndex < currentProgram.modules.Count; moduleIndex++)
            {
                DrawModuleTreeItem(currentProgram.modules[moduleIndex], moduleIndex);
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Draw the details content (right side)
    /// </summary>
    private void DrawDetailsContent()
    {
        EditorGUILayout.BeginVertical("box");
        
        // Header
        EditorGUILayout.LabelField("Properties", subHeaderStyle);
        EditorGUILayout.Space(5);
        
        // Content based on selection
        detailsPanelScrollPos = EditorGUILayout.BeginScrollView(detailsPanelScrollPos, GUILayout.ExpandHeight(true));
        
        if (selectedHierarchyItem == null)
        {
            EditorGUILayout.HelpBox("Select an item from the hierarchy to edit its properties.", MessageType.Info);
        }
        else
        {
            DrawSelectedItemProperties();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    
    /// <summary>
    /// Draw program-level tree item
    /// </summary>
    private void DrawProgramTreeItem()
    {
        EditorGUILayout.BeginHorizontal();
        
        // Selection highlighting
        Color backgroundColor = selectedItemType == "program" ? Color.blue * 0.3f : Color.clear;
        if (backgroundColor != Color.clear)
        {
            GUI.backgroundColor = backgroundColor;
        }
        
        // Foldout and name
        currentProgram.isExpanded = EditorGUILayout.Foldout(currentProgram.isExpanded, 
            $"📋 {currentProgram.programName}", true);
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Handle selection
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            SelectItem(currentProgram, "program");
            Event.current.Use();
        }
    }
    
    /// <summary>
    /// Draw module tree item
    /// </summary>
    private void DrawModuleTreeItem(TrainingModule module, int moduleIndex)
    {
        if (!currentProgram.isExpanded) return;
        
        EditorGUI.indentLevel++;
        
        EditorGUILayout.BeginHorizontal();
        
        // Selection highlighting
        Color backgroundColor = (selectedItemType == "module" && selectedHierarchyItem == module) ? Color.blue * 0.3f : Color.clear;
        if (backgroundColor != Color.clear)
        {
            GUI.backgroundColor = backgroundColor;
        }
        
        // Foldout and name
        module.isExpanded = EditorGUILayout.Foldout(module.isExpanded, 
            $"📚 {module.moduleName}", true);
        
        // Actions
        if (GUILayout.Button("➕", GUILayout.Width(25)))
        {
            ShowAddTaskGroupMenu(module);
        }
        if (GUILayout.Button("❌", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Module", $"Delete module '{module.moduleName}'?", "Delete", "Cancel"))
            {
                DeleteModule(moduleIndex);
            }
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Handle selection
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            SelectItem(module, "module");
            Event.current.Use();
        }
        
        // Draw task groups
        if (module.isExpanded && module.taskGroups != null)
        {
            for (int groupIndex = 0; groupIndex < module.taskGroups.Count; groupIndex++)
            {
                DrawTaskGroupTreeItem(module.taskGroups[groupIndex], module, groupIndex);
            }
        }
        
        EditorGUI.indentLevel--;
    }
    
    /// <summary>
    /// Draw task group tree item
    /// </summary>
    private void DrawTaskGroupTreeItem(TaskGroup taskGroup, TrainingModule parentModule, int groupIndex)
    {
        EditorGUI.indentLevel++;
        
        EditorGUILayout.BeginHorizontal();
        
        // Selection highlighting
        Color backgroundColor = (selectedItemType == "taskgroup" && selectedHierarchyItem == taskGroup) ? Color.blue * 0.3f : Color.clear;
        if (backgroundColor != Color.clear)
        {
            GUI.backgroundColor = backgroundColor;
        }
        
        // Foldout and name
        taskGroup.isExpanded = EditorGUILayout.Foldout(taskGroup.isExpanded, 
            $"📁 {taskGroup.groupName}", true);
        
        // Actions
        if (GUILayout.Button("➕", GUILayout.Width(25)))
        {
            ShowAddStepMenu(taskGroup);
        }
        if (GUILayout.Button("❌", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Task Group", $"Delete task group '{taskGroup.groupName}'?", "Delete", "Cancel"))
            {
                DeleteTaskGroup(parentModule, groupIndex);
            }
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Handle selection
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            SelectItem(taskGroup, "taskgroup");
            Event.current.Use();
        }
        
        // Draw steps
        if (taskGroup.isExpanded && taskGroup.steps != null)
        {
            for (int stepIndex = 0; stepIndex < taskGroup.steps.Count; stepIndex++)
            {
                DrawStepTreeItem(taskGroup.steps[stepIndex], taskGroup, stepIndex);
            }
        }
        
        EditorGUI.indentLevel--;
    }
    
    /// <summary>
    /// Draw step tree item
    /// </summary>
    private void DrawStepTreeItem(InteractionStep step, TaskGroup parentTaskGroup, int stepIndex)
    {
        EditorGUI.indentLevel++;
        
        EditorGUILayout.BeginHorizontal();
        
        // Selection highlighting
        Color backgroundColor = (selectedItemType == "step" && selectedHierarchyItem == step) ? Color.blue * 0.3f : Color.clear;
        if (backgroundColor != Color.clear)
        {
            GUI.backgroundColor = backgroundColor;
        }
        
        // Status icon
        string statusIcon = step.IsValid() ? "✅" : "⚠️";
        string typeIcon = GetStepTypeIcon(step.type);
        
        // Name and type
        EditorGUILayout.LabelField($"{statusIcon} {typeIcon} {step.stepName}", GUILayout.ExpandWidth(true));
        
        // Actions
        if (GUILayout.Button("❌", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Step", $"Delete step '{step.stepName}'?", "Delete", "Cancel"))
            {
                DeleteStep(parentTaskGroup, stepIndex);
            }
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // Handle selection
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            SelectItem(step, "step");
            Event.current.Use();
        }
        
        EditorGUI.indentLevel--;
    }
    
    /// <summary>
    /// Get icon for step type
    /// </summary>
    private string GetStepTypeIcon(InteractionStep.StepType stepType)
    {
        switch (stepType)
        {
            case InteractionStep.StepType.Grab: return "✋";
            case InteractionStep.StepType.GrabAndSnap: return "🔗";
            case InteractionStep.StepType.TurnKnob: return "🔄";
            case InteractionStep.StepType.WaitForCondition: return "⏳";
            case InteractionStep.StepType.ShowInstruction: return "💬";
            default: return "❓";
        }
    }
    
    /// <summary>
    /// Select a hierarchy item
    /// </summary>
    private void SelectItem(object item, string itemType)
    {
        selectedHierarchyItem = item;
        selectedItemType = itemType;
        Repaint();
    }
    
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
            step.type == InteractionStep.StepType.TurnKnob)
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
        
        // Knob-specific settings
        if (step.type == InteractionStep.StepType.TurnKnob)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Knob Settings", EditorStyles.boldLabel);
            step.targetAngle = EditorGUILayout.FloatField("Target Angle", step.targetAngle);
            step.angleTolerance = EditorGUILayout.FloatField("Angle Tolerance", step.angleTolerance);
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
        
        // Execution settings
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Execution Settings", EditorStyles.boldLabel);
        step.allowParallel = EditorGUILayout.Toggle("Allow Parallel", step.allowParallel);
        step.isOptional = EditorGUILayout.Toggle("Is Optional", step.isOptional);
        
        // Hint
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Instruction");
        step.hint = EditorGUILayout.TextArea(step.hint, GUILayout.Height(40));
        
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
    
    /// <summary>
    /// Show add menu for top-level items
    /// </summary>
    private void ShowAddMenu()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add Module"), false, () => AddNewModule());
        menu.ShowAsContext();
    }
    
    /// <summary>
    /// Show add menu for task groups
    /// </summary>
    private void ShowAddTaskGroupMenu(TrainingModule module)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add Task Group"), false, () => AddNewTaskGroup(module));
        menu.ShowAsContext();
    }
    
    /// <summary>
    /// Show add menu for steps
    /// </summary>
    private void ShowAddStepMenu(TaskGroup taskGroup)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Grab Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.Grab));
        menu.AddItem(new GUIContent("Grab and Snap Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.GrabAndSnap));
        menu.AddItem(new GUIContent("Turn Knob Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.TurnKnob));
        
        // Valve operation steps
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Valve Operations/Tighten Valve"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.TightenValve));
        menu.AddItem(new GUIContent("Valve Operations/Loosen Valve"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.LoosenValve));
        menu.AddItem(new GUIContent("Valve Operations/Install Valve (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.InstallValve));
        menu.AddItem(new GUIContent("Valve Operations/Remove Valve (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.RemoveValve));
        
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Wait Condition Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.WaitForCondition));
        menu.AddItem(new GUIContent("Show Instruction Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.ShowInstruction));
        menu.ShowAsContext();
    }
    
    /// <summary>
    /// Add a new module
    /// </summary>
    private void AddNewModule()
    {
        if (currentProgram.modules == null)
            currentProgram.modules = new List<TrainingModule>();
        
        var newModule = new TrainingModule("New Module", "Module description");
        currentProgram.modules.Add(newModule);
        
        // Auto-select the new module
        SelectItem(newModule, "module");

        AutoSaveCurrentAsset();
    }
    
    /// <summary>
    /// Add a new task group
    /// </summary>
    private void AddNewTaskGroup(TrainingModule module)
    {
        if (module.taskGroups == null)
            module.taskGroups = new List<TaskGroup>();
        
        var newTaskGroup = new TaskGroup("New Task Group", "Task group description");
        module.taskGroups.Add(newTaskGroup);
        
        // Auto-select the new task group
        SelectItem(newTaskGroup, "taskgroup");

        AutoSaveCurrentAsset();
    }
    
    /// <summary>
    /// Add a new step
    /// </summary>
    private void AddNewStep(TaskGroup taskGroup, InteractionStep.StepType stepType)
    {
        if (taskGroup.steps == null)
            taskGroup.steps = new List<InteractionStep>();
        
        var newStep = new InteractionStep("New Step", stepType);
        newStep.hint = "Step instruction goes here";
        taskGroup.steps.Add(newStep);
        
        // Auto-select the new step
        SelectItem(newStep, "step");

        AutoSaveCurrentAsset();
    }
    
    /// <summary>
    /// Delete a module
    /// </summary>
    private void DeleteModule(int moduleIndex)
    {
        if (currentProgram.modules != null && moduleIndex >= 0 && moduleIndex < currentProgram.modules.Count)
        {
            currentProgram.modules.RemoveAt(moduleIndex);
            selectedHierarchyItem = null;
            selectedItemType = null;
            AutoSaveCurrentAsset();
        }
    }
    
    /// <summary>
    /// Delete a task group
    /// </summary>
    private void DeleteTaskGroup(TrainingModule module, int groupIndex)
    {
        if (module.taskGroups != null && groupIndex >= 0 && groupIndex < module.taskGroups.Count)
        {
            module.taskGroups.RemoveAt(groupIndex);
            selectedHierarchyItem = null;
            selectedItemType = null;
            AutoSaveCurrentAsset();
        }
    }
    
    /// <summary>
    /// Delete a step
    /// </summary>
    private void DeleteStep(TaskGroup taskGroup, int stepIndex)
    {
        if (taskGroup.steps != null && stepIndex >= 0 && stepIndex < taskGroup.steps.Count)
        {
            taskGroup.steps.RemoveAt(stepIndex);
            selectedHierarchyItem = null;
            selectedItemType = null;
            AutoSaveCurrentAsset();
        }
    }
    
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
    
    private void CreateNewProfile<T>(string defaultName) where T : InteractionProfile
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Profile", 
            defaultName, 
            "asset", 
            "Save the interaction profile asset");
        
        if (!string.IsNullOrEmpty(path))
        {
            T profile = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            
            if (typeof(T) == typeof(GrabProfile))
                selectedGrabProfile = profile as GrabProfile;
            else if (typeof(T) == typeof(KnobProfile))
                selectedKnobProfile = profile as KnobProfile;
            else if (typeof(T) == typeof(SnapProfile))
                selectedSnapProfile = profile as SnapProfile;
            
            Selection.activeObject = profile;
        }
    }
    
    private void CreateDefaultProfiles()
    {
        string folderPath = "Assets/VRTrainingKit/Resources";
        
        // Create folders if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/VRTrainingKit"))
        {
            AssetDatabase.CreateFolder("Assets", "VRTrainingKit");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/VRTrainingKit", "Resources");
        }
        
        // Create Grab Profile
        if (selectedGrabProfile == null)
        {
            GrabProfile grabProfile = ScriptableObject.CreateInstance<GrabProfile>();
            grabProfile.profileName = "Default Grab";
            grabProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabProfile.trackPosition = true;
            grabProfile.trackRotation = true;
            grabProfile.throwOnDetach = true;
            
            AssetDatabase.CreateAsset(grabProfile, $"{folderPath}/DefaultGrabProfile.asset");
            selectedGrabProfile = grabProfile;
        }
        
        // Create Knob Profile
        if (selectedKnobProfile == null)
        {
            KnobProfile knobProfile = ScriptableObject.CreateInstance<KnobProfile>();
            knobProfile.profileName = "Default Knob";
            knobProfile.rotationAxis = KnobProfile.RotationAxis.Y;
            knobProfile.useLimits = true;
            knobProfile.minAngle = -90f;
            knobProfile.maxAngle = 180f;  // Based on your screenshot
            knobProfile.useSpring = true;
            knobProfile.springValue = 0f;
            knobProfile.damper = 1f;
            knobProfile.targetPosition = 0f;
            knobProfile.bounceMinVelocity = 0.2f;
            knobProfile.contactDistance = 0f;
            knobProfile.useHapticFeedback = true;
            knobProfile.colliderType = ColliderType.Box;
            
            AssetDatabase.CreateAsset(knobProfile, $"{folderPath}/DefaultKnobProfile.asset");
            selectedKnobProfile = knobProfile;
        }
        
        // Create Snap Profile
        if (selectedSnapProfile == null)
        {
            SnapProfile snapProfile = ScriptableObject.CreateInstance<SnapProfile>();
            snapProfile.profileName = "Default Snap";
            snapProfile.socketRadius = 0.1f;
            snapProfile.socketActive = true;
            snapProfile.showInteractableHoverMeshes = true;
            
            AssetDatabase.CreateAsset(snapProfile, $"{folderPath}/DefaultSnapProfile.asset");
            selectedSnapProfile = snapProfile;
        }
        
        // Create Tool Profile
        if (selectedToolProfile == null)
        {
            ToolProfile toolProfile = ScriptableObject.CreateInstance<ToolProfile>();
            toolProfile.profileName = "Default Tool";
            toolProfile.rotationAxis = Vector3.up;
            toolProfile.tightenAngle = 90f;
            toolProfile.loosenAngle = 90f;
            toolProfile.tightenThreshold = 90f;
            toolProfile.loosenThreshold = 45f;
            toolProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            toolProfile.trackPosition = true;
            toolProfile.trackRotation = true;
            
            AssetDatabase.CreateAsset(toolProfile, $"{folderPath}/DefaultToolProfile.asset");
            selectedToolProfile = toolProfile;
        }
        
        // Create Valve Profile
        if (selectedValveProfile == null)
        {
            ValveProfile valveProfile = ScriptableObject.CreateInstance<ValveProfile>();
            valveProfile.profileName = "Default Valve";

            // Configurable defaults - these can be changed per step in sequence builder
            valveProfile.rotationAxis = Vector3.up;        // Y-axis rotation (most common)
            valveProfile.tightenThreshold = 180f;          // More realistic full turn
            valveProfile.loosenThreshold = 180f;           // Symmetric loosening
            valveProfile.angleTolerance = 10f;             // More forgiving tolerance

            // Socket compatibility
            valveProfile.compatibleSocketTags = new string[] { "valve_socket" };

            // XR Interaction settings
            valveProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            valveProfile.trackPosition = true;
            valveProfile.trackRotation = true;

            // Physics settings for better valve feel
            valveProfile.rotationDampening = 3f;           // Moderate dampening
            valveProfile.dampeningSpeed = 8f;              // Responsive dampening

            AssetDatabase.CreateAsset(valveProfile, $"{folderPath}/DefaultValveProfile.asset");
            selectedValveProfile = valveProfile;
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Profiles Created", 
            "Default profiles have been created in Assets/VRTrainingKit/Resources", "OK");
    }
    
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

    /// <summary>
    /// Profile type validation helper methods
    /// </summary>
    private bool IsGrabProfile(InteractionProfile profile)
    {
        return profile is GrabProfile ||
               (profile != null && profile.GetType().Name.Contains("Grab"));
    }

    private bool IsKnobProfile(InteractionProfile profile)
    {
        return profile is KnobProfile ||
               (profile != null && profile.GetType().Name.Contains("Knob"));
    }

    private bool IsSnapProfile(InteractionProfile profile)
    {
        return profile is SnapProfile ||
               (profile != null && profile.GetType().Name.Contains("Snap"));
    }

    private bool IsToolProfile(InteractionProfile profile)
    {
        return profile is ToolProfile ||
               (profile != null && profile.GetType().Name.Contains("Tool"));
    }

    private bool IsValveProfile(InteractionProfile profile)
    {
        return profile is ValveProfile ||
               (profile != null && profile.GetType().Name.Contains("Valve"));
    }

    /// <summary>
    /// Draws framework compatibility notice in the Configure tab
    /// </summary>
    private void DrawConfigureFrameworkNotice()
    {
        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var frameworkDisplayName = VRFrameworkDetector.GetFrameworkDisplayName(detectedFramework);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var originalColor = GUI.color;

        switch (detectedFramework)
        {
            case VRFramework.XRI:
                EditorGUILayout.LabelField("✅ XRI Framework Detected", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Current profiles are compatible with your XRI setup.", EditorStyles.wordWrappedLabel);
                break;

            case VRFramework.AutoHands:
                GUI.color = new Color(1f, 0.8f, 0f); // Orange
                EditorGUILayout.LabelField("⚠️ AutoHands Framework Detected", EditorStyles.boldLabel);
                GUI.color = originalColor;

                EditorGUILayout.LabelField(
                    "Current profiles are XRI-based. AutoHands profiles will be available in Phase 2.",
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Use VR Training > Framework Validator for detailed framework analysis.", MessageType.Info);
                break;

            case VRFramework.None:
                GUI.color = new Color(1f, 0.5f, 0.5f); // Light red
                EditorGUILayout.LabelField("❌ No VR Framework Detected", EditorStyles.boldLabel);
                GUI.color = originalColor;

                EditorGUILayout.LabelField(
                    "No VR framework found in scene. Add an XR Origin (XRI) or AutoHandPlayer (AutoHands) to use profiles.",
                    EditorStyles.wordWrappedLabel);
                break;

            default:
                EditorGUILayout.LabelField($"Framework: {frameworkDisplayName}", EditorStyles.boldLabel);
                break;
        }

        EditorGUILayout.EndVertical();
    }
}
#endif
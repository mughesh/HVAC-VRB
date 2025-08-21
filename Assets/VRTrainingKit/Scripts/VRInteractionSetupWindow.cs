// VRInteractionSetupWindow.cs
// This file should be placed in an "Editor" folder in your Unity project

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
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
        Validate
    }
    
    private Tab currentTab = Tab.Setup;
    
    // Setup tab
    private InteractionSetupService.SceneAnalysis sceneAnalysis;
    private Vector2 setupScrollPos;
    
    // Configure tab
    private GrabProfile selectedGrabProfile;
    private KnobProfile selectedKnobProfile;
    private SnapProfile selectedSnapProfile;
    private Vector2 configScrollPos;
    
    // Sequence tab
    private SequenceController sequenceController;
    private Vector2 sequenceScrollPos;
    private bool showSequenceHelp = false;
    
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
        // Try to load default profiles from Resources
        selectedGrabProfile = Resources.Load<GrabProfile>("DefaultGrabProfile");
        selectedKnobProfile = Resources.Load<KnobProfile>("DefaultKnobProfile");
        selectedSnapProfile = Resources.Load<SnapProfile>("DefaultSnapProfile");
        
        // If not found in Resources, search in Assets
        if (selectedGrabProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:GrabProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedGrabProfile = AssetDatabase.LoadAssetAtPath<GrabProfile>(path);
            }
        }
        
        if (selectedKnobProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:KnobProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedKnobProfile = AssetDatabase.LoadAssetAtPath<KnobProfile>(path);
            }
        }
        
        if (selectedSnapProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SnapProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedSnapProfile = AssetDatabase.LoadAssetAtPath<SnapProfile>(path);
            }
        }
    }
    
    private void OnGUI()
    {
        // Tab selection
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == Tab.Setup, "Setup", "Button"))
            currentTab = Tab.Setup;
        if (GUILayout.Toggle(currentTab == Tab.Configure, "Configure", "Button"))
            currentTab = Tab.Configure;
        if (GUILayout.Toggle(currentTab == Tab.Sequence, "Sequence", "Button"))
            currentTab = Tab.Sequence;
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
            case Tab.Validate:
                DrawValidateTab();
                break;
        }
    }
    
    private void DrawSetupTab()
    {
        EditorGUILayout.LabelField("Scene Setup", headerStyle);
        EditorGUILayout.Space(5);
        
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
                
                // Check if configured
                bool isConfigured = false;
                XRBaseInteractable interactable = null;
                XRSocketInteractor socketInteractor = null;
                
                if (tag == "grab" || tag == "knob")
                {
                    interactable = obj.GetComponent<XRGrabInteractable>();
                    isConfigured = interactable != null;
                }
                else if (tag == "snap")
                {
                    socketInteractor = obj.GetComponent<XRSocketInteractor>();
                    isConfigured = socketInteractor != null;
                }
                
                string statusIcon = isConfigured ? "✓" : "○";
                GUIStyle statusStyle = isConfigured ? successStyle : GUI.skin.label;
                
                // Object name
                EditorGUILayout.LabelField($"{statusIcon} {obj.name}", statusStyle, GUILayout.Width(200));
                
                // Layer mask dropdown (only if configured)
                if (isConfigured)
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
                else
                {
                    EditorGUILayout.LabelField("Configure first", EditorStyles.miniLabel, GUILayout.Width(150));
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
        
        configScrollPos = EditorGUILayout.BeginScrollView(configScrollPos);
        
        // Grab Profile
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Grab Profile", subHeaderStyle);
        selectedGrabProfile = (GrabProfile)EditorGUILayout.ObjectField(
            "Profile Asset", selectedGrabProfile, typeof(GrabProfile), false);
        
        if (selectedGrabProfile == null)
        {
            // Show list of available profiles
            string[] grabProfileGuids = AssetDatabase.FindAssets("t:GrabProfile");
            if (grabProfileGuids.Length > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (string guid in grabProfileGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GrabProfile profile = AssetDatabase.LoadAssetAtPath<GrabProfile>(path);
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("  • " + profile.name, EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedGrabProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            if (GUILayout.Button("Create New Grab Profile"))
            {
                CreateNewProfile<GrabProfile>("GrabProfile");
            }
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
        selectedKnobProfile = (KnobProfile)EditorGUILayout.ObjectField(
            "Profile Asset", selectedKnobProfile, typeof(KnobProfile), false);
        
        if (selectedKnobProfile == null)
        {
            // Show list of available profiles
            string[] knobProfileGuids = AssetDatabase.FindAssets("t:KnobProfile");
            if (knobProfileGuids.Length > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (string guid in knobProfileGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    KnobProfile profile = AssetDatabase.LoadAssetAtPath<KnobProfile>(path);
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("  • " + profile.name, EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedKnobProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            if (GUILayout.Button("Create New Knob Profile"))
            {
                CreateNewProfile<KnobProfile>("KnobProfile");
            }
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
        selectedSnapProfile = (SnapProfile)EditorGUILayout.ObjectField(
            "Profile Asset", selectedSnapProfile, typeof(SnapProfile), false);
        
        if (selectedSnapProfile == null)
        {
            // Show list of available profiles
            string[] snapProfileGuids = AssetDatabase.FindAssets("t:SnapProfile");
            if (snapProfileGuids.Length > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (string guid in snapProfileGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    SnapProfile profile = AssetDatabase.LoadAssetAtPath<SnapProfile>(path);
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("  • " + profile.name, EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedSnapProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            if (GUILayout.Button("Create New Snap Profile"))
            {
                CreateNewProfile<SnapProfile>("SnapProfile");
            }
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedSnapProfile;
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
        EditorGUILayout.LabelField("Sequence Controller", headerStyle);
        EditorGUILayout.Space(5);
        
        // Find or create sequence controller
        sequenceController = FindObjectOfType<SequenceController>();
        
        if (sequenceController == null)
        {
            EditorGUILayout.HelpBox("No Sequence Controller found in scene.", MessageType.Warning);
            
            if (GUILayout.Button("Create Sequence Controller", GUILayout.Height(30)))
            {
                GameObject controllerObj = new GameObject("SequenceController");
                sequenceController = controllerObj.AddComponent<SequenceController>();
                Selection.activeGameObject = controllerObj;
            }
        }
        else
        {
            sequenceScrollPos = EditorGUILayout.BeginScrollView(sequenceScrollPos);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Controller found: " + sequenceController.name);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = sequenceController.gameObject;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Show help
            showSequenceHelp = EditorGUILayout.Foldout(showSequenceHelp, "How to Setup Sequences");
            if (showSequenceHelp)
            {
                EditorGUILayout.HelpBox(
                    "1. Select the Sequence Controller in the Hierarchy\n" +
                    "2. In the Inspector, add State Groups\n" +
                    "3. For each State Group, define:\n" +
                    "   - Activation Conditions (what triggers this state)\n" +
                    "   - Allowed/Locked Actions\n" +
                    "4. Add SequenceValidator components to objects that should be locked\n" +
                    "5. Set the Required State Group on each SequenceValidator",
                    MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Quick setup for AC scene
            EditorGUILayout.LabelField("Quick Setup Templates", subHeaderStyle);
            
            if (GUILayout.Button("Setup AC Leak Testing Sequence"))
            {
                SetupACLeakTestingSequence();
            }
            
            EditorGUILayout.EndScrollView();
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
        var initialState = new SequenceController.StateGroup();
        initialState.groupName = "Initial Setup";
        initialState.isActive = true;
        initialState.allowedActions = new List<string> { "Hose_Male_Connector", "Hose_Female_Connector" };
        initialState.lockedActions = new List<string> { "Nitrogen_Cylinder_Valve" };
        
        // Create System Ready state
        var systemReadyState = new SequenceController.StateGroup();
        systemReadyState.groupName = "System Ready";
        systemReadyState.isActive = false;
        systemReadyState.allowedActions = new List<string> { "Nitrogen_Cylinder_Valve", "Gauge_Adjustment_Knob" };
        
        // Add conditions for System Ready (all hoses connected)
        var allConnectedCondition = new SequenceController.StateGroup.Condition();
        allConnectedCondition.type = SequenceController.StateGroup.Condition.ConditionType.AllObjectsSnapped;
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
}
#endif
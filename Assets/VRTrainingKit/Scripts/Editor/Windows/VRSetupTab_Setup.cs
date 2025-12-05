// VRSetupTab_Setup.cs
// Setup tab for VR Training Setup window - Scene object scanning and configuration

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Setup tab - Handles scene object scanning and interaction component configuration
/// </summary>
public class VRSetupTab_Setup : VRSetupTabBase
{
    private InteractionSetupService.SceneAnalysis sceneAnalysis;
    private Vector2 setupScrollPos;
    
    public VRSetupTab_Setup(VRInteractionSetupWindow window, VRProfileManager profileManager) : base(window, profileManager)
    {
    }
    
    public override void OnEnable()
    {
        // Try to restore scene analysis if we had one before
        if (sceneAnalysis == null && EditorPrefs.HasKey("VRTrainingKit_LastSceneAnalysisValid"))
        {
            bool wasValid = EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid", false);
            if (wasValid)
            {
                Debug.Log("[VRSetupTab_Setup] Restoring scene analysis after play mode transition");
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
    }
    
    public override void OnDisable()
    {
        // Save that we had a valid scene analysis
        if (sceneAnalysis != null)
        {
            EditorPrefs.SetBool("VRTrainingKit_LastSceneAnalysisValid", true);
        }
    }
    
    public override void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Debug.Log("[VRSetupTab_Setup] Entered play mode - refreshing scene analysis");
            if (sceneAnalysis != null)
            {
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("[VRSetupTab_Setup] Entered edit mode - refreshing scene analysis");
            if (EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid", false))
            {
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
        }
    }
    
    public override void DrawTab()
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
            DrawObjectGroup("Grab Objects", sceneAnalysis.grabObjects, "grab", profileManager.selectedGrabProfile);
            EditorGUILayout.Space(10);
            
            // Knob objects
            DrawObjectGroup("Knob Objects", sceneAnalysis.knobObjects, "knob", profileManager.selectedKnobProfile);
            EditorGUILayout.Space(10);
            
            // Snap points
            DrawObjectGroup("Snap Points", sceneAnalysis.snapObjects, "snap", profileManager.selectedSnapProfile);
            EditorGUILayout.Space(10);
            
            // Tool objects
            DrawObjectGroup("Tool Objects", sceneAnalysis.toolObjects, "tool", profileManager.selectedToolProfile);
            EditorGUILayout.Space(10);
            
            // Valve objects
            DrawObjectGroup("Valve Objects", sceneAnalysis.valveObjects, "valve", profileManager.selectedValveProfile);
            EditorGUILayout.Space(10);

            // Turn objects
            DrawObjectGroup("Turn Objects", sceneAnalysis.turnObjects, "turn", profileManager.selectedTurnProfile);
            EditorGUILayout.Space(10);

            // Teleport points
            DrawObjectGroup("🚀 Teleport Points", sceneAnalysis.teleportObjects, "teleportPoint", profileManager.selectedTeleportProfile);

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
    
    private void DrawFrameworkStatus()
    {
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Current VR Framework", subHeaderStyle);
        
        string frameworkName = currentFramework == VRFramework.XRI ? "XR Interaction Toolkit" : 
                              currentFramework == VRFramework.AutoHands ? "AutoHands" : "None Detected";
        
        GUIStyle statusStyle = currentFramework != VRFramework.None ? successStyle : errorStyle;
        EditorGUILayout.LabelField($"Status: {frameworkName}", statusStyle);
        
        if (currentFramework == VRFramework.None)
        {
            EditorGUILayout.HelpBox("⚠️ No VR framework detected. Please install XR Interaction Toolkit or AutoHands.", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
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
        if (objects.Count > 0 && objects.Count <= 20)
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
    
    private LayerMask DrawInteractionLayerMask(LayerMask mask, params GUILayoutOption[] options)
    {
        return InteractionLayerManager.DrawLayerMaskDropdown(mask, options);
    }
    
    private void ApplyAllComponents()
    {
        if (sceneAnalysis == null)
        {
            EditorUtility.DisplayDialog("No Analysis", "Please scan the scene first", "OK");
            return;
        }
        
        int totalApplied = 0;
        
        if (profileManager.selectedGrabProfile != null && sceneAnalysis.grabObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.grabObjects, profileManager.selectedGrabProfile);
            totalApplied += sceneAnalysis.grabObjects.Count;
        }
        
        if (profileManager.selectedKnobProfile != null && sceneAnalysis.knobObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.knobObjects, profileManager.selectedKnobProfile);
            totalApplied += sceneAnalysis.knobObjects.Count;
        }
        
        if (profileManager.selectedSnapProfile != null && sceneAnalysis.snapObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.snapObjects, profileManager.selectedSnapProfile);
            totalApplied += sceneAnalysis.snapObjects.Count;
        }
        
        if (profileManager.selectedToolProfile != null && sceneAnalysis.toolObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.toolObjects, profileManager.selectedToolProfile);
            totalApplied += sceneAnalysis.toolObjects.Count;
        }
        
        if (profileManager.selectedValveProfile != null && sceneAnalysis.valveObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.valveObjects, profileManager.selectedValveProfile);
            totalApplied += sceneAnalysis.valveObjects.Count;
        }
        
        if (profileManager.selectedTurnProfile != null && sceneAnalysis.turnObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.turnObjects, profileManager.selectedTurnProfile);
            totalApplied += sceneAnalysis.turnObjects.Count;
        }
        
        if (profileManager.selectedTeleportProfile != null && sceneAnalysis.teleportObjects.Count > 0)
        {
            InteractionSetupService.ApplyComponentsToObjects(sceneAnalysis.teleportObjects, profileManager.selectedTeleportProfile);
            totalApplied += sceneAnalysis.teleportObjects.Count;
        }
        
        EditorUtility.DisplayDialog("Configuration Complete", 
            $"Applied components to {totalApplied} objects", "OK");
        
        // Refresh analysis
        sceneAnalysis = InteractionSetupService.ScanScene();
    }
}

#endif

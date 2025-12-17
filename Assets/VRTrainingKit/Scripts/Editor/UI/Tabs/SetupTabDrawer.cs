// SetupTabDrawer.cs
// Extracted Setup tab drawer for VRInteractionSetupWindow
// Part of Phase 6: Final tab extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Draws the Setup tab content for VRInteractionSetupWindow.
/// Handles scene scanning, object configuration, and interaction layer management.
/// </summary>
public class SetupTabDrawer
{
    // Dependencies
    private readonly ProfileCacheManager _profileCacheManager;
    private readonly System.Func<ProfileCacheManager.ProfileType, InteractionProfile> _getSelectedProfile;
    private readonly System.Action _onSceneAnalysisChanged;

    // State
    private InteractionSetupService.SceneAnalysis _sceneAnalysis;
    private Vector2 _scrollPosition;

    public SetupTabDrawer(
        ProfileCacheManager profileCacheManager,
        System.Func<ProfileCacheManager.ProfileType, InteractionProfile> getSelectedProfile,
        System.Action onSceneAnalysisChanged)
    {
        _profileCacheManager = profileCacheManager;
        _getSelectedProfile = getSelectedProfile;
        _onSceneAnalysisChanged = onSceneAnalysisChanged;
    }

    /// <summary>
    /// Gets or sets the current scene analysis
    /// </summary>
    public InteractionSetupService.SceneAnalysis SceneAnalysis
    {
        get => _sceneAnalysis;
        set => _sceneAnalysis = value;
    }

    /// <summary>
    /// Gets or sets the scroll position
    /// </summary>
    public Vector2 ScrollPosition
    {
        get => _scrollPosition;
        set => _scrollPosition = value;
    }

    /// <summary>
    /// Draw the Setup tab content
    /// </summary>
    public void Draw()
    {
        EditorGUILayout.LabelField("Scene Setup", VRTrainingEditorStyles.HeaderStyle);
        EditorGUILayout.Space(5);

        // Framework Status Section
        DrawFrameworkStatus();
        EditorGUILayout.Space(10);

        // Scan button
        if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
        {
            _sceneAnalysis = InteractionSetupService.ScanScene();
            _onSceneAnalysisChanged?.Invoke();
        }

        EditorGUILayout.Space(10);

        // Show analysis results
        if (_sceneAnalysis != null)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Grab objects
            DrawObjectGroup("Grab Objects", _sceneAnalysis.grabObjects, "grab", ProfileCacheManager.ProfileType.Grab);
            EditorGUILayout.Space(10);

            // Knob objects
            DrawObjectGroup("Knob Objects", _sceneAnalysis.knobObjects, "knob", ProfileCacheManager.ProfileType.Knob);
            EditorGUILayout.Space(10);

            // Snap points
            DrawObjectGroup("Snap Points", _sceneAnalysis.snapObjects, "snap", ProfileCacheManager.ProfileType.Snap);
            EditorGUILayout.Space(10);

            // Tool objects
            DrawObjectGroup("Tool Objects", _sceneAnalysis.toolObjects, "tool", ProfileCacheManager.ProfileType.Tool);
            EditorGUILayout.Space(10);

            // Valve objects
            DrawObjectGroup("Screw Objects", _sceneAnalysis.screwObjects, "valve", ProfileCacheManager.ProfileType.Valve);
            EditorGUILayout.Space(10);

            // Turn objects
            DrawObjectGroup("Turn Objects", _sceneAnalysis.turnObjects, "turn", ProfileCacheManager.ProfileType.Turn);
            EditorGUILayout.Space(10);

            // Teleport points
            DrawObjectGroup("Teleport Points", _sceneAnalysis.teleportObjects, "teleportPoint", ProfileCacheManager.ProfileType.Teleport);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Quick setup buttons
            DrawQuickSetupButtons();
        }
        else
        {
            EditorGUILayout.HelpBox("Click 'Scan Scene' to analyze tagged objects in your scene.", MessageType.Info);
        }
    }

    /// <summary>
    /// Draw quick setup buttons
    /// </summary>
    private void DrawQuickSetupButtons()
    {
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
                _sceneAnalysis = InteractionSetupService.ScanScene();
                _onSceneAnalysisChanged?.Invoke();
            }
        }

        if (GUILayout.Button("Edit Layers", GUILayout.Height(35)))
        {
            InteractionLayerManager.OpenInteractionLayerSettings();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draw an object group section
    /// </summary>
    private void DrawObjectGroup(string title, List<GameObject> objects, string tag, ProfileCacheManager.ProfileType profileType)
    {
        var profile = _getSelectedProfile?.Invoke(profileType);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{title} ({objects.Count} found)", VRTrainingEditorStyles.SubHeaderStyle);

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
                DrawObjectRow(obj, tag, profile);
            }
            EditorGUI.indentLevel--;
        }
        else if (objects.Count > 20)
        {
            EditorGUILayout.LabelField($"  (Too many to list - use 'Select All' to view)", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw a single object row in the object group
    /// </summary>
    private void DrawObjectRow(GameObject obj, string tag, InteractionProfile profile)
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

        string statusIcon = isConfigured ? "\u2713" : "\u25CB";
        GUIStyle statusStyle = isConfigured ? VRTrainingEditorStyles.SuccessStyle : GUI.skin.label;

        // Object name
        EditorGUILayout.LabelField($"{statusIcon} {obj.name}", statusStyle, GUILayout.Width(200));

        // Layer mask dropdown (only if configured and XRI framework)
        DrawLayerMaskControl(obj, isConfigured, currentFramework, interactable, socketInteractor);

        // Configure button (individual)
        if (GUILayout.Button("Configure", GUILayout.Width(70)))
        {
            if (profile != null)
            {
                InteractionSetupService.ApplyComponentsToObjects(new List<GameObject> { obj }, profile);
                EditorUtility.DisplayDialog("Configuration Complete",
                    $"Applied {profile.profileName} to {obj.name}", "OK");

                // Refresh analysis after configuration
                _sceneAnalysis = InteractionSetupService.ScanScene();
                _onSceneAnalysisChanged?.Invoke();
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

    /// <summary>
    /// Draw layer mask control for an object
    /// </summary>
    private void DrawLayerMaskControl(GameObject obj, bool isConfigured, VRFramework currentFramework,
        XRBaseInteractable interactable, XRSocketInteractor socketInteractor)
    {
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
                LayerMask newMask = InteractionLayerManager.DrawLayerMaskDropdown(currentMask, GUILayout.Width(150));

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
                EditorGUILayout.LabelField("\u2713 Configured (AutoHands)", EditorStyles.miniLabel, GUILayout.Width(150));
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
    }

    /// <summary>
    /// Draws framework status information
    /// </summary>
    private void DrawFrameworkStatus()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("\uD83D\uDD27 VR Framework Status", EditorStyles.boldLabel);
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
            EditorGUILayout.LabelField("\u274C " + frameworkDisplayName, EditorStyles.boldLabel);
        }
        else if (isFrameworkValid)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("\u2705 " + frameworkDisplayName, EditorStyles.boldLabel);
        }
        else
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("\u26A0\uFE0F " + frameworkDisplayName, EditorStyles.boldLabel);
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
                EditorGUILayout.LabelField("\u26A0\uFE0F " + VRFrameworkDetector.GetFrameworkDisplayName(activeFramework) + " (Mismatch)", EditorStyles.boldLabel);
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
    /// Apply all components to tagged objects
    /// </summary>
    private void ApplyAllComponents()
    {
        if (_sceneAnalysis == null) return;

        int appliedCount = 0;

        var grabProfile = _getSelectedProfile?.Invoke(ProfileCacheManager.ProfileType.Grab);
        var knobProfile = _getSelectedProfile?.Invoke(ProfileCacheManager.ProfileType.Knob);
        var snapProfile = _getSelectedProfile?.Invoke(ProfileCacheManager.ProfileType.Snap);
        var toolProfile = _getSelectedProfile?.Invoke(ProfileCacheManager.ProfileType.Tool);
        var valveProfile = _getSelectedProfile?.Invoke(ProfileCacheManager.ProfileType.Valve);

        if (grabProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(_sceneAnalysis.grabObjects, grabProfile);
            appliedCount += _sceneAnalysis.grabObjects.Count;
        }

        if (knobProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(_sceneAnalysis.knobObjects, knobProfile);
            appliedCount += _sceneAnalysis.knobObjects.Count;
        }

        if (snapProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(_sceneAnalysis.snapObjects, snapProfile);
            appliedCount += _sceneAnalysis.snapObjects.Count;
        }

        if (toolProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(_sceneAnalysis.toolObjects, toolProfile);
            appliedCount += _sceneAnalysis.toolObjects.Count;
        }

        if (valveProfile != null)
        {
            InteractionSetupService.ApplyComponentsToObjects(_sceneAnalysis.screwObjects, valveProfile);
            appliedCount += _sceneAnalysis.screwObjects.Count;
        }

        EditorUtility.DisplayDialog("Setup Complete",
            $"Successfully configured {appliedCount} objects", "OK");

        // Refresh the scene analysis
        _sceneAnalysis = InteractionSetupService.ScanScene();
        _onSceneAnalysisChanged?.Invoke();
    }
}
#endif

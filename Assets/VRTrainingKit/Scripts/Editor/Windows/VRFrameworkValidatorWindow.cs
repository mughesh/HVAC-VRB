// VRFrameworkValidatorWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window for validating and managing VR framework settings
/// Located under VR Training menu as requested
/// </summary>
public class VRFrameworkValidatorWindow : EditorWindow
{
    private VRFrameworkManager frameworkManager;
    private FrameworkValidationResult lastValidation;
    private Vector2 scrollPosition;
    private bool autoRefresh = true;
    private double lastRefreshTime;

    // GUI Styles
    private GUIStyle headerStyle;
    private GUIStyle statusStyle;
    private GUIStyle errorStyle;
    private GUIStyle warningStyle;
    private GUIStyle successStyle;

    [MenuItem("Sequence Builder/Framework Validator")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRFrameworkValidatorWindow>("Framework Validator");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshValidation();
    }

    private void OnGUI()
    {
        InitializeStyles();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawFrameworkManager();
        EditorGUILayout.Space(10);

        DrawFrameworkDetection();
        EditorGUILayout.Space(10);

        DrawValidationResults();
        EditorGUILayout.Space(10);

        DrawControls();

        EditorGUILayout.EndScrollView();

        // Auto-refresh every 2 seconds if enabled
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > 2.0)
        {
            RefreshValidation();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }
    }

    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        if (errorStyle == null)
        {
            errorStyle = new GUIStyle(EditorStyles.helpBox);
            errorStyle.normal.textColor = Color.red;
        }

        if (warningStyle == null)
        {
            warningStyle = new GUIStyle(EditorStyles.helpBox);
            warningStyle.normal.textColor = new Color(1f, 0.6f, 0f); // Orange
        }

        if (successStyle == null)
        {
            successStyle = new GUIStyle(EditorStyles.helpBox);
            successStyle.normal.textColor = Color.green;
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("VR Framework Validator", headerStyle);
        EditorGUILayout.LabelField("Validate and manage VR framework settings", EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawFrameworkManager()
    {
        EditorGUILayout.LabelField("Framework Manager Settings", EditorStyles.boldLabel);

        // Try to get framework manager instance
        if (frameworkManager == null)
        {
            frameworkManager = VRFrameworkManager.Instance;
        }

        if (frameworkManager != null)
        {
            EditorGUI.BeginChangeCheck();

            // Framework Manager Settings
            EditorGUILayout.BeginVertical(statusStyle);

            frameworkManager.autoDetect = EditorGUILayout.Toggle("Auto Detect Framework", frameworkManager.autoDetect);

            if (!frameworkManager.autoDetect)
            {
                frameworkManager.preferredFramework = (VRFramework)EditorGUILayout.EnumPopup("Preferred Framework", frameworkManager.preferredFramework);
            }

            frameworkManager.forceFramework = EditorGUILayout.Toggle("Force Framework", frameworkManager.forceFramework);

            if (frameworkManager.forceFramework)
            {
                frameworkManager.forcedFramework = (VRFramework)EditorGUILayout.EnumPopup("Forced Framework", frameworkManager.forcedFramework);
            }

            frameworkManager.showFrameworkMismatchWarnings = EditorGUILayout.Toggle("Show Mismatch Warnings", frameworkManager.showFrameworkMismatchWarnings);
            frameworkManager.validateOnSceneLoad = EditorGUILayout.Toggle("Validate On Scene Load", frameworkManager.validateOnSceneLoad);

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(frameworkManager);
                RefreshValidation();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No VRFrameworkManager found. Create one via:\nAssets > Create > Sequence Builder > Framework Manager", MessageType.Warning);

            if (GUILayout.Button("Create Framework Manager"))
            {
                CreateFrameworkManager();
            }
        }
    }

    private void DrawFrameworkDetection()
    {
        EditorGUILayout.LabelField("Framework Detection", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(statusStyle);

        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var activeFramework = frameworkManager?.GetActiveFramework() ?? VRFramework.None;

        EditorGUILayout.LabelField("Detected Framework:", VRFrameworkDetector.GetFrameworkDisplayName(detectedFramework));
        EditorGUILayout.LabelField("Active Framework:", VRFrameworkDetector.GetFrameworkDisplayName(activeFramework));

        // Framework info
        var frameworkInfo = VRFrameworkDetector.GetFrameworkInfo();
        EditorGUILayout.LabelField("Details:");
        EditorGUILayout.TextArea(frameworkInfo, GUILayout.Height(60));

        EditorGUILayout.EndVertical();
    }

    private void DrawValidationResults()
    {
        EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

        if (lastValidation != null)
        {
            // Overall status
            if (lastValidation.isValid && !lastValidation.HasIssues)
            {
                EditorGUILayout.LabelField("✅ Framework setup is valid", successStyle);
            }
            else if (lastValidation.HasIssues)
            {
                EditorGUILayout.LabelField("⚠️ Framework has issues", warningStyle);
            }
            else
            {
                EditorGUILayout.LabelField("❌ Framework setup is invalid", errorStyle);
            }

            // Show errors
            if (lastValidation.errors.Count > 0)
            {
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                foreach (var error in lastValidation.errors)
                {
                    EditorGUILayout.LabelField($"• {error}", errorStyle);
                }
            }

            // Show warnings
            if (lastValidation.warnings.Count > 0)
            {
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                foreach (var warning in lastValidation.warnings)
                {
                    EditorGUILayout.LabelField($"• {warning}", warningStyle);
                }
            }

            // Framework mismatch info
            if (lastValidation.hasMismatch)
            {
                EditorGUILayout.Space(5);
                var mismatchInfo = frameworkManager?.GetFrameworkMismatchInfo();
                if (!string.IsNullOrEmpty(mismatchInfo))
                {
                    EditorGUILayout.HelpBox(mismatchInfo, MessageType.Warning);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No validation results available", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawControls()
    {
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh Validation"))
        {
            RefreshValidation();
        }

        if (GUILayout.Button("Test Framework Detection"))
        {
           // VRFrameworkDetectorTest.TestFrameworkDetection();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);

        if (GUILayout.Button("Open VR Training Setup"))
        {
            VRInteractionSetupWindow.ShowWindow();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Help section
        EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Framework Validator helps you:\n" +
            "• Detect which VR framework is active in your scene\n" +
            "• Validate framework setup and components\n" +
            "• Manage framework preferences\n" +
            "• Identify framework mismatches",
            MessageType.Info);
    }

    private void RefreshValidation()
    {
        if (frameworkManager != null)
        {
            lastValidation = frameworkManager.ValidateCurrentSetup();
        }
        Repaint();
    }

    private void CreateFrameworkManager()
    {
        var manager = CreateInstance<VRFrameworkManager>();

        // Create Resources folder if it doesn't exist
        var resourcesPath = "Assets/VRTrainingKit/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets/VRTrainingKit", "Resources");
        }

        var assetPath = $"{resourcesPath}/VRFrameworkManager.asset";
        AssetDatabase.CreateAsset(manager, assetPath);
        AssetDatabase.SaveAssets();

        frameworkManager = manager;

        Debug.Log($"[VRFrameworkValidatorWindow] Created VRFrameworkManager at {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = manager;
    }
}
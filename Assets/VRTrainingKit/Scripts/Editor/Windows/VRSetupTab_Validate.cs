// VRSetupTab_Validate.cs
// Validate tab for VR Training Setup window - Validation and troubleshooting

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Validate tab - Handles setup validation and troubleshooting
/// </summary>
public class VRSetupTab_Validate : VRSetupTabBase
{
    private List<string> validationIssues = new List<string>();
    private Vector2 validateScrollPos;
    
    public VRSetupTab_Validate(VRInteractionSetupWindow window) : base(window)
    {
    }
    
    public override void DrawTab()
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
        else if (validationIssues != null && validationIssues.Count == 0)
        {
            EditorGUILayout.LabelField("✓ All checks passed!", successStyle);
        }
    }
}

#endif

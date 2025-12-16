// ValidateTabDrawer.cs
// Extracted Validate tab drawer for VRInteractionSetupWindow
// Part of Phase 6: Final tab extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Draws the Validate tab content for VRInteractionSetupWindow.
/// Handles setup validation and issue reporting.
/// </summary>
public class ValidateTabDrawer
{
    // State
    private List<string> _validationIssues = new List<string>();
    private Vector2 _scrollPosition;

    /// <summary>
    /// Gets or sets the validation issues list
    /// </summary>
    public List<string> ValidationIssues
    {
        get => _validationIssues;
        set => _validationIssues = value ?? new List<string>();
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
    /// Draw the Validate tab content
    /// </summary>
    public void Draw()
    {
        EditorGUILayout.LabelField("Setup Validation", VRTrainingEditorStyles.HeaderStyle);
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            _validationIssues = InteractionSetupService.ValidateSetup();
        }

        EditorGUILayout.Space(10);

        if (_validationIssues.Count > 0)
        {
            EditorGUILayout.LabelField("Found " + _validationIssues.Count + " issues:", VRTrainingEditorStyles.WarningStyle);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var issue in _validationIssues)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField("\u26A0", GUILayout.Width(20));
                EditorGUILayout.LabelField(issue, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        else if (_validationIssues != null)
        {
            EditorGUILayout.LabelField("\u2713 All checks passed!", VRTrainingEditorStyles.SuccessStyle);
        }
    }
}
#endif

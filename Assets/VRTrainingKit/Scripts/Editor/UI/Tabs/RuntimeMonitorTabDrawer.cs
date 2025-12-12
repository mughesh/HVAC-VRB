// RuntimeMonitorTabDrawer.cs
// Extracted Runtime Monitor tab drawer for VRInteractionSetupWindow
// Part of Phase 6: Final tab extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Draws the Runtime Monitor tab content for VRInteractionSetupWindow.
/// Provides real-time monitoring of training sequence execution during play mode.
/// </summary>
public class RuntimeMonitorTabDrawer
{
    // Scroll position for the entire tab
    private Vector2 _scrollPosition;

    /// <summary>
    /// Check if Runtime Monitor tab should be visible
    /// </summary>
    public static bool IsEnabled()
    {
        return RuntimeMonitorSettings.IsRuntimeMonitorEnabled();
    }

    /// <summary>
    /// Draw the Runtime Monitor tab content
    /// </summary>
    public void Draw()
    {
        EditorGUILayout.LabelField("Runtime Monitor", VRTrainingEditorStyles.HeaderStyle);
        EditorGUILayout.Space(5);

        // Play Mode check
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("\u23F8\uFE0F Runtime Monitor is only available in Play Mode.\n\nEnter Play Mode to view real-time sequence execution status.", MessageType.Info);
            return;
        }

        // Find the active controller in the scene
        var controller = Object.FindObjectOfType<ModularTrainingSequenceController>();
        if (controller == null)
        {
            EditorGUILayout.HelpBox("\u274C No ModularTrainingSequenceController found in scene.\n\nMake sure your scene has a GameObject with the ModularTrainingSequenceController component.", MessageType.Warning);
            return;
        }

        // Begin scrollable area for the entire content
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        var progress = controller.GetProgress();

        // Program Overview Section
        DrawProgramOverview(controller, progress);
        EditorGUILayout.Space(10);

        // Sequence Status Section
        DrawSequenceStatus(controller, progress);
        EditorGUILayout.Space(10);

        // Step Progress Section
        DrawStepProgress(controller, progress);
        EditorGUILayout.Space(10);

        // Socket States Section
        DrawSocketStates(controller, progress);
        EditorGUILayout.Space(10);

        // Utility Buttons Section
        DrawUtilityButtons(controller, progress);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draw program overview section
    /// </summary>
    private void DrawProgramOverview(ModularTrainingSequenceController controller, ModularTrainingSequenceController.SequenceProgress progress)
    {
        EditorGUILayout.LabelField("Program Overview", VRTrainingEditorStyles.SubHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (controller.currentProgram != null)
        {
            EditorGUILayout.LabelField("\uD83D\uDCCB Program: " + controller.currentProgram.programName, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Show all modules and their task groups
            for (int moduleIdx = 0; moduleIdx < controller.currentProgram.modules.Count; moduleIdx++)
            {
                var module = controller.currentProgram.modules[moduleIdx];
                bool isCurrentModule = moduleIdx == progress.currentModuleIndex;
                bool isCompletedModule = moduleIdx < progress.currentModuleIndex;

                // Module header
                string moduleIcon = isCompletedModule ? "\u2705" : (isCurrentModule ? "\uD83D\uDFE2" : "\u23F8\uFE0F");
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

                EditorGUILayout.LabelField(moduleIcon + " Module: " + module.moduleName, moduleStyle);

                // Show task groups for current or adjacent modules only (for cleaner UI)
                if (moduleIdx >= progress.currentModuleIndex - 1 && moduleIdx <= progress.currentModuleIndex + 1)
                {
                    for (int tgIdx = 0; tgIdx < module.taskGroups.Count; tgIdx++)
                    {
                        var taskGroup = module.taskGroups[tgIdx];
                        bool isCurrentTaskGroup = isCurrentModule && tgIdx == progress.currentTaskGroupIndex;
                        bool isCompletedTaskGroup = isCurrentModule ? (tgIdx < progress.currentTaskGroupIndex) : isCompletedModule;

                        string tgIcon = isCompletedTaskGroup ? "\u2705" : (isCurrentTaskGroup ? "\uD83D\uDFE2" : "\u23F8\uFE0F");
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
                            stepInfo = " (" + progress.completedSteps + "/" + progress.totalSteps + " steps)";
                        }

                        EditorGUILayout.LabelField("   " + tgIcon + " " + taskGroup.groupName + stepInfo, tgStyle);
                    }
                }
                else
                {
                    // Show count only for distant modules
                    EditorGUILayout.LabelField("   ... " + module.taskGroups.Count + " task groups", EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(3);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No program loaded", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw sequence status section
    /// </summary>
    private void DrawSequenceStatus(ModularTrainingSequenceController controller, ModularTrainingSequenceController.SequenceProgress progress)
    {
        EditorGUILayout.LabelField("Current Status", VRTrainingEditorStyles.SubHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Current Module: " + (progress.currentModuleName ?? "None"));
        EditorGUILayout.LabelField("Current Task Group: " + (progress.currentTaskGroupName ?? "None"));

        // Check if sequential flow is enabled
        var currentTaskGroup = controller.currentProgram?.modules?[progress.currentModuleIndex]?.taskGroups?[progress.currentTaskGroupIndex];
        bool sequentialFlowEnabled = currentTaskGroup != null && currentTaskGroup.enforceSequentialFlow;

        EditorGUILayout.LabelField("Sequential Flow: " + (sequentialFlowEnabled ? "\u2705 Enabled" : "\u274C Disabled"));

        // Progress bar
        float progressPercent = progress.totalSteps > 0 ? (float)progress.completedSteps / progress.totalSteps : 0f;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progressPercent, progress.completedSteps + "/" + progress.totalSteps + " steps (" + progressPercent.ToString("P0") + ")");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw step progress section
    /// </summary>
    private void DrawStepProgress(ModularTrainingSequenceController controller, ModularTrainingSequenceController.SequenceProgress progress)
    {
        EditorGUILayout.LabelField("Step Progress", VRTrainingEditorStyles.SubHeaderStyle);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var currentTaskGroup = controller.currentProgram?.modules?[progress.currentModuleIndex]?.taskGroups?[progress.currentTaskGroupIndex];

        if (currentTaskGroup != null && currentTaskGroup.steps != null && currentTaskGroup.steps.Count > 0)
        {
            foreach (var step in currentTaskGroup.steps)
            {
                string icon = GetStepIcon(step);
                string status = GetStepStatus(step);
                GUIStyle stepStyle = GetStepStyle(step);

                EditorGUILayout.LabelField(icon + " " + step.stepName + " (" + status + ")", stepStyle);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No steps in current task group", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw socket states section
    /// </summary>
    private void DrawSocketStates(ModularTrainingSequenceController controller, ModularTrainingSequenceController.SequenceProgress progress)
    {
        var currentTaskGroup = controller.currentProgram?.modules?[progress.currentModuleIndex]?.taskGroups?[progress.currentTaskGroupIndex];
        bool sequentialFlowEnabled = currentTaskGroup != null && currentTaskGroup.enforceSequentialFlow;

        EditorGUILayout.LabelField("Socket States", VRTrainingEditorStyles.SubHeaderStyle);
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
                    EditorGUILayout.LabelField("\u2705 Enabled Sockets (" + enabledSockets.Count + ")", EditorStyles.boldLabel);
                    foreach (var socket in enabledSockets)
                    {
                        string occupiedLabel = socket.isOccupied ? " [Occupied]" : "";
                        EditorGUILayout.LabelField("   \u2022 " + socket.socketName + occupiedLabel + " - " + socket.disabledReason);
                    }
                    EditorGUILayout.Space(5);
                }

                if (disabledSockets.Count > 0)
                {
                    EditorGUILayout.LabelField("\u274C Disabled Sockets (" + disabledSockets.Count + ")", EditorStyles.boldLabel);
                    foreach (var socket in disabledSockets)
                    {
                        EditorGUILayout.LabelField("   \u2022 " + socket.socketName + " - " + socket.disabledReason, EditorStyles.helpBox);
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
            EditorGUILayout.LabelField("\u26A0\uFE0F Sequential flow is not enabled for current task group", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Socket restrictions are not active - all sockets are available", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Restriction manager not initialized", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw utility buttons section
    /// </summary>
    private void DrawUtilityButtons(ModularTrainingSequenceController controller, ModularTrainingSequenceController.SequenceProgress progress)
    {
        var currentTaskGroup = controller.currentProgram?.modules?[progress.currentModuleIndex]?.taskGroups?[progress.currentTaskGroupIndex];
        bool sequentialFlowEnabled = currentTaskGroup != null && currentTaskGroup.enforceSequentialFlow;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("\uD83D\uDD04 Refresh", GUILayout.Height(30)))
        {
            // Force window repaint handled by caller
        }

        GUI.enabled = controller.restrictionManager != null && sequentialFlowEnabled;
        if (GUILayout.Button("\uD83D\uDD13 Enable All Sockets", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Enable All Sockets",
                "This will temporarily enable all sockets, bypassing sequence restrictions.\n\nThis is for debugging purposes only and will be reset when the task group changes.\n\nContinue?",
                "Yes", "Cancel"))
            {
                controller.restrictionManager.Reset();
                Debug.Log("[RuntimeMonitor] All sockets re-enabled for debugging");
            }
        }

        if (GUILayout.Button("\uD83D\uDD04 Reset Sequence", GUILayout.Height(30)))
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
    }

    // Helper methods
    private string GetStepIcon(InteractionStep step)
    {
        return step.isCompleted ? "\u2705" : "\uD83D\uDFE2";
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
}
#endif

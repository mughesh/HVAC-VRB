// SequenceTreeView.cs
// Extracted tree view panel for Sequence tab
// Part of Phase 4: Sequence Tree View extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Draws the tree view (hierarchy) panel for the Sequence tab.
/// Handles display of TrainingProgram -> TrainingModule -> TaskGroup -> InteractionStep hierarchy.
/// </summary>
public class SequenceTreeView
{
    // Callback interface for communicating with parent window
    private readonly ISequenceTreeViewCallbacks _callbacks;

    // Scroll position
    private Vector2 _scrollPosition;

    // Selection state
    private object _selectedItem;
    private string _selectedItemType;

    public SequenceTreeView(ISequenceTreeViewCallbacks callbacks)
    {
        _callbacks = callbacks;
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
    /// Gets or sets the selected item
    /// </summary>
    public object SelectedItem
    {
        get => _selectedItem;
        set => _selectedItem = value;
    }

    /// <summary>
    /// Gets or sets the selected item type
    /// </summary>
    public string SelectedItemType
    {
        get => _selectedItemType;
        set => _selectedItemType = value;
    }

    /// <summary>
    /// Draw the tree view content
    /// </summary>
    public void Draw(TrainingProgram program)
    {
        EditorGUILayout.BeginVertical("box");

        try
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hierarchy", VRTrainingEditorStyles.SubHeaderStyle);

            // Add menu button
            if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(20)))
            {
                ShowAddMenu(program);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // Tree view with scrolling
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            try
            {
                // Draw program header
                DrawProgramTreeItem(program);

                // Draw modules
                if (program != null && program.modules != null)
                {
                    for (int moduleIndex = 0; moduleIndex < program.modules.Count; moduleIndex++)
                    {
                        DrawModuleTreeItem(program.modules[moduleIndex], program, moduleIndex);
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing tree: {e.Message}", MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Draw program-level tree item
    /// </summary>
    private void DrawProgramTreeItem(TrainingProgram program)
    {
        if (program == null) return;

        EditorGUILayout.BeginHorizontal();

        // Selection highlighting
        Color backgroundColor = _selectedItemType == "program" ? Color.blue * 0.3f : Color.clear;
        if (backgroundColor != Color.clear)
        {
            GUI.backgroundColor = backgroundColor;
        }

        // Foldout and name
        program.isExpanded = EditorGUILayout.Foldout(program.isExpanded,
            $"📋 {program.programName}", true);

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // Handle selection
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            SelectItem(program, "program");
            Event.current.Use();
        }
    }

    /// <summary>
    /// Draw module tree item
    /// </summary>
    private void DrawModuleTreeItem(TrainingModule module, TrainingProgram program, int moduleIndex)
    {
        if (!program.isExpanded) return;

        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal();

        // Selection highlighting
        Color backgroundColor = (_selectedItemType == "module" && _selectedItem == module) ? Color.blue * 0.3f : Color.clear;
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
                DeleteModule(program, moduleIndex);
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
        Color backgroundColor = (_selectedItemType == "taskgroup" && _selectedItem == taskGroup) ? Color.blue * 0.3f : Color.clear;
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
        Color backgroundColor = (_selectedItemType == "step" && _selectedItem == step) ? Color.blue * 0.3f : Color.clear;
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
            case InteractionStep.StepType.TightenValve: return "🔧";
            case InteractionStep.StepType.LoosenValve: return "🔓";
            case InteractionStep.StepType.InstallValve: return "🔩";
            case InteractionStep.StepType.RemoveValve: return "🔧";
            case InteractionStep.StepType.WaitForScriptCondition: return "⚙️";
            case InteractionStep.StepType.Teleport: return "🚀";
            default: return "❓";
        }
    }

    /// <summary>
    /// Select a hierarchy item
    /// </summary>
    private void SelectItem(object item, string itemType)
    {
        _selectedItem = item;
        _selectedItemType = itemType;
        _callbacks?.OnItemSelected(item, itemType);
        _callbacks?.OnRequestRepaint();
    }

    // ==========================================
    // Add Menus
    // ==========================================

    /// <summary>
    /// Show add menu for top-level items
    /// </summary>
    private void ShowAddMenu(TrainingProgram program)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add Module"), false, () => AddNewModule(program));
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
        menu.AddItem(new GUIContent("🚀 Teleport Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.Teleport));

        // Valve operation steps
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Valve Operations/Tighten Valve"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.TightenValve));
        menu.AddItem(new GUIContent("Valve Operations/Loosen Valve"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.LoosenValve));
        menu.AddItem(new GUIContent("Valve Operations/Install Valve (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.InstallValve));
        menu.AddItem(new GUIContent("Valve Operations/Remove Valve (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.RemoveValve));

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Wait Condition Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.WaitForCondition));
        menu.AddItem(new GUIContent("Wait For Script Condition"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.WaitForScriptCondition));
        menu.AddItem(new GUIContent("Show Instruction Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.ShowInstruction));
        menu.ShowAsContext();
    }

    // ==========================================
    // Add Operations
    // ==========================================

    /// <summary>
    /// Add a new module
    /// </summary>
    private void AddNewModule(TrainingProgram program)
    {
        if (program.modules == null)
            program.modules = new List<TrainingModule>();

        var newModule = new TrainingModule("New Module", "Module description");
        program.modules.Add(newModule);

        // Auto-select the new module
        SelectItem(newModule, "module");

        _callbacks?.OnAutoSave();
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

        _callbacks?.OnAutoSave();
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

        _callbacks?.OnAutoSave();
    }

    // ==========================================
    // Delete Operations
    // ==========================================

    /// <summary>
    /// Delete a module
    /// </summary>
    private void DeleteModule(TrainingProgram program, int moduleIndex)
    {
        if (program.modules != null && moduleIndex >= 0 && moduleIndex < program.modules.Count)
        {
            program.modules.RemoveAt(moduleIndex);
            _selectedItem = null;
            _selectedItemType = null;
            _callbacks?.OnItemSelected(null, null);
            _callbacks?.OnAutoSave();
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
            _selectedItem = null;
            _selectedItemType = null;
            _callbacks?.OnItemSelected(null, null);
            _callbacks?.OnAutoSave();
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
            _selectedItem = null;
            _selectedItemType = null;
            _callbacks?.OnItemSelected(null, null);
            _callbacks?.OnAutoSave();
        }
    }

    /// <summary>
    /// Clear selection
    /// </summary>
    public void ClearSelection()
    {
        _selectedItem = null;
        _selectedItemType = null;
    }
}
#endif

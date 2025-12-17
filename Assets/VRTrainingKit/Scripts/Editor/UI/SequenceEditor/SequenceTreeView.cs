// SequenceTreeView.cs
// Extracted tree view panel for Sequence tab
// Part of Phase 4: Sequence Tree View extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
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

    // Reorderable list caches (for drag-and-drop reordering)
    private Dictionary<TaskGroup, ReorderableList> _stepReorderableLists = new Dictionary<TaskGroup, ReorderableList>();
    private Dictionary<TrainingModule, ReorderableList> _taskGroupReorderableLists = new Dictionary<TrainingModule, ReorderableList>();

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
    /// Clears reorderable list caches (call when loading a new asset)
    /// </summary>
    public void ClearReorderableListCaches()
    {
        _stepReorderableLists.Clear();
        _taskGroupReorderableLists.Clear();
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

        // Draw task groups with ReorderableList (for smooth dragging)
        if (module.isExpanded && module.taskGroups != null)
        {
            EditorGUI.indentLevel++;

            var taskGroupList = GetOrCreateTaskGroupReorderableList(module);
            if (taskGroupList != null)
            {
                taskGroupList.DoLayoutList();

                // Draw expanded children AFTER all task group headers
                // (This creates smooth drag for TaskGroups while maintaining hierarchy)
                for (int i = 0; i < module.taskGroups.Count; i++)
                {
                    var taskGroup = module.taskGroups[i];
                    if (taskGroup.isExpanded && taskGroup.steps != null)
                    {
                        EditorGUI.indentLevel++;
                        var stepList = GetOrCreateStepReorderableList(taskGroup);
                        if (stepList != null)
                            stepList.DoLayoutList();
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUI.indentLevel--;
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

        // Draw steps with reorderable list
        if (taskGroup.isExpanded && taskGroup.steps != null)
        {
            EditorGUI.indentLevel++;
            var stepList = GetOrCreateStepReorderableList(taskGroup);
            if (stepList != null)
            {
                stepList.DoLayoutList();
            }
            EditorGUI.indentLevel--;
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
            case InteractionStep.StepType.TightenScrew: return "🔩";
            case InteractionStep.StepType.LoosenScrew: return "🔩";
            case InteractionStep.StepType.InstallScrew: return "🔩";
            case InteractionStep.StepType.RemoveScrew: return "🔩";
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
        menu.AddItem(new GUIContent("Screw Operations/Tighten Screw"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.TightenScrew));
        menu.AddItem(new GUIContent("Screw Operations/Loosen Screw"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.LoosenScrew));
        menu.AddItem(new GUIContent("Screw Operations/Install Screw (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.InstallScrew));
        menu.AddItem(new GUIContent("Screw Operations/Remove Screw (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.RemoveScrew));

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
    /// Get or create reorderable list for a task group's steps
    /// </summary>
    private ReorderableList GetOrCreateStepReorderableList(TaskGroup taskGroup)
    {
        if (taskGroup?.steps == null) return null;

        if (!_stepReorderableLists.TryGetValue(taskGroup, out var list) || list == null)
        {
            list = new ReorderableList(
                taskGroup.steps,
                typeof(InteractionStep),
                true,   // draggable
                false,  // displayHeader
                false,  // displayAddButton
                false   // displayRemoveButton
            );

            // Draw each step element
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= taskGroup.steps.Count) return;
                var step = taskGroup.steps[index];

                string statusIcon = step.IsValid() ? "✅" : "⚠️";
                string typeIcon = GetStepTypeIcon(step.type);

                // Selection highlighting
                bool isSelected = (_selectedItemType == "step" && _selectedItem == step);
                if (isSelected)
                {
                    EditorGUI.DrawRect(rect, Color.blue * 0.3f);
                }

                // Leave space for drag handle (built-in) and delete button
                Rect labelRect = new Rect(rect.x, rect.y, rect.width - 30, rect.height);
                Rect deleteRect = new Rect(rect.x + rect.width - 25, rect.y, 25, rect.height);

                // Draw step info
                EditorGUI.LabelField(labelRect, $"{statusIcon} {typeIcon} {step.stepName}");

                // Delete button
                if (GUI.Button(deleteRect, "❌"))
                {
                    if (EditorUtility.DisplayDialog("Delete Step",
                        $"Delete step '{step.stepName}'?", "Delete", "Cancel"))
                    {
                        taskGroup.steps.RemoveAt(index);
                        _callbacks?.OnAutoSave();
                        // Clear the cache to force recreation
                        _stepReorderableLists.Remove(taskGroup);
                    }
                }
            };

            // Handle selection
            list.onSelectCallback = (ReorderableList l) =>
            {
                if (l.index >= 0 && l.index < taskGroup.steps.Count)
                {
                    SelectItem(taskGroup.steps[l.index], "step");
                }
            };

            // Handle reorder - auto-save
            list.onReorderCallback = (ReorderableList l) =>
            {
                _callbacks?.OnAutoSave();
            };

            list.elementHeight = EditorGUIUtility.singleLineHeight + 4;

            _stepReorderableLists[taskGroup] = list;
        }

        return list;
    }

    /// <summary>
    /// Get or create reorderable list for a module's task groups
    /// </summary>
    private ReorderableList GetOrCreateTaskGroupReorderableList(TrainingModule module)
    {
        if (module?.taskGroups == null) return null;

        if (!_taskGroupReorderableLists.TryGetValue(module, out var list) || list == null)
        {
            list = new ReorderableList(
                module.taskGroups,
                typeof(TaskGroup),
                true,   // draggable - this gives smooth drag feedback!
                false,  // displayHeader
                false,  // displayAddButton
                false   // displayRemoveButton
            );

            // Draw TaskGroup header in ReorderableList for smooth dragging
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= module.taskGroups.Count) return;
                var taskGroup = module.taskGroups[index];

                // Selection highlighting
                bool isSelected = (_selectedItemType == "taskgroup" && _selectedItem == taskGroup);
                if (isSelected)
                {
                    EditorGUI.DrawRect(rect, Color.blue * 0.3f);
                }

                // Layout rects (leave space for drag handle, foldout, and buttons)
                Rect foldoutRect = new Rect(rect.x, rect.y, rect.width - 55, rect.height);
                Rect addBtnRect = new Rect(rect.x + rect.width - 52, rect.y, 25, rect.height);
                Rect delBtnRect = new Rect(rect.x + rect.width - 25, rect.y, 25, rect.height);

                // Foldout (toggles isExpanded)
                taskGroup.isExpanded = EditorGUI.Foldout(
                    foldoutRect,
                    taskGroup.isExpanded,
                    $"📁 {taskGroup.groupName}",
                    true
                );

                // Add step button
                if (GUI.Button(addBtnRect, "➕"))
                {
                    ShowAddStepMenu(taskGroup);
                }

                // Delete button
                if (GUI.Button(delBtnRect, "❌"))
                {
                    if (EditorUtility.DisplayDialog("Delete Task Group",
                        $"Delete '{taskGroup.groupName}'?", "Delete", "Cancel"))
                    {
                        module.taskGroups.RemoveAt(index);
                        _callbacks?.OnAutoSave();
                        // Clear caches
                        _taskGroupReorderableLists.Remove(module);
                        if (_stepReorderableLists.ContainsKey(taskGroup))
                            _stepReorderableLists.Remove(taskGroup);
                    }
                }
            };

            // Handle selection
            list.onSelectCallback = (ReorderableList l) =>
            {
                if (l.index >= 0 && l.index < module.taskGroups.Count)
                {
                    SelectItem(module.taskGroups[l.index], "taskgroup");
                }
            };

            // Handle reorder - auto-save
            list.onReorderCallback = (ReorderableList l) =>
            {
                _callbacks?.OnAutoSave();
            };

            list.elementHeight = EditorGUIUtility.singleLineHeight + 4;

            _taskGroupReorderableLists[module] = list;
        }

        return list;
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

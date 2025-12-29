// SequencePropertiesPanel.cs
// Extracted properties panel for Sequence tab
// Part of Phase 5: Sequence Properties Panel extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Draws the properties panel (right side) for the Sequence tab.
/// Handles editing of TrainingProgram, TrainingModule, TaskGroup, and InteractionStep properties.
/// </summary>
public class SequencePropertiesPanel
{
    // Callback for auto-save
    private readonly System.Action _onAutoSave;

    // Scroll position
    private Vector2 _scrollPosition;

    // Cached styles
    private GUIStyle _successStyle;
    private GUIStyle _errorStyle;

    public SequencePropertiesPanel(System.Action onAutoSave)
    {
        _onAutoSave = onAutoSave;
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
    /// Draw the properties panel
    /// </summary>
    public void Draw(object selectedItem, string selectedItemType)
    {
        // Ensure styles are initialized
        EnsureStylesInitialized();

        EditorGUILayout.BeginVertical("box");

        try
        {
            // Header
            EditorGUILayout.LabelField("Properties", VRTrainingEditorStyles.SubHeaderStyle);
            EditorGUILayout.Space(5);

            // Content based on selection
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            try
            {
                if (selectedItem == null)
                {
                    EditorGUILayout.HelpBox("Select an item from the hierarchy to edit its properties.", MessageType.Info);
                }
                else
                {
                    DrawSelectedItemProperties(selectedItem, selectedItemType);
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing properties: {e.Message}", MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
    }

    private void EnsureStylesInitialized()
    {
        if (_successStyle == null)
        {
            _successStyle = VRTrainingEditorStyles.SuccessStyle;
            _errorStyle = VRTrainingEditorStyles.ErrorStyle;
        }
    }

    /// <summary>
    /// Draw properties for the currently selected item
    /// </summary>
    private void DrawSelectedItemProperties(object selectedItem, string selectedItemType)
    {
        EditorGUI.BeginChangeCheck();

        switch (selectedItemType)
        {
            case "program":
                DrawProgramProperties((TrainingProgram)selectedItem);
                break;
            case "module":
                DrawModuleProperties((TrainingModule)selectedItem);
                break;
            case "taskgroup":
                DrawTaskGroupProperties((TaskGroup)selectedItem);
                break;
            case "step":
                DrawStepProperties((InteractionStep)selectedItem);
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            // Auto-save when changes are made
            _onAutoSave?.Invoke();
        }
    }

    /// <summary>
    /// Draw program properties
    /// </summary>
    private void DrawProgramProperties(TrainingProgram program)
    {
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
    private void DrawModuleProperties(TrainingModule module)
    {
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
    private void DrawTaskGroupProperties(TaskGroup taskGroup)
    {
        EditorGUILayout.LabelField("Task Group Settings", EditorStyles.boldLabel);

        taskGroup.groupName = EditorGUILayout.TextField("Group Name", taskGroup.groupName);

        EditorGUILayout.LabelField("Description");
        taskGroup.description = EditorGUILayout.TextArea(taskGroup.description, GUILayout.Height(60));

        EditorGUILayout.Space(10);

        // Sequential Flow Control
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
    private void DrawStepProperties(InteractionStep step)
    {
        EditorGUILayout.LabelField("Step Settings", EditorStyles.boldLabel);

        // Basic properties
        step.stepName = EditorGUILayout.TextField("Step Name", step.stepName);
        step.type = (InteractionStep.StepType)EditorGUILayout.EnumPopup("Type", step.type);

        EditorGUILayout.Space(10);

        // Target objects based on type
        DrawTargetObjectFields(step);

        // Type-specific settings
        DrawTypeSpecificSettings(step);

        // Execution settings
        DrawExecutionSettings(step);

        // Guidance Arrows
        DrawGuidanceArrows(step);

        // Validation
        DrawValidationStatus(step);
    }

    private void DrawTargetObjectFields(InteractionStep step)
    {
        if (step.type == InteractionStep.StepType.Grab ||
            step.type == InteractionStep.StepType.GrabAndSnap ||
            step.type == InteractionStep.StepType.TurnKnob ||
            step.type == InteractionStep.StepType.WaitForScriptCondition)
        {
            EditorGUILayout.LabelField("Target Objects", EditorStyles.boldLabel);

            // Target Object field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Object", GUILayout.Width(100));

            GameObject currentTarget = step.targetObject.GameObject;
            GameObject newTarget = (GameObject)EditorGUILayout.ObjectField(currentTarget, typeof(GameObject), true);

            if (newTarget != currentTarget)
            {
                step.targetObject.GameObject = newTarget;
            }

            EditorGUILayout.EndHorizontal();

            // Component validation for target object based on step type
            if (step.targetObject.GameObject != null)
            {
                if (step.type == InteractionStep.StepType.Grab)
                {
                    DrawGrabTargetValidation(step.targetObject.GameObject);
                }
                else if (step.type == InteractionStep.StepType.GrabAndSnap)
                {
                    DrawGrabTargetValidation(step.targetObject.GameObject);
                }
                else if (step.type == InteractionStep.StepType.TurnKnob)
                {
                    DrawKnobTargetValidation(step.targetObject.GameObject);
                }
            }

            if (step.type == InteractionStep.StepType.GrabAndSnap)
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Destination", GUILayout.Width(100));

                GameObject currentDest = step.destination.GameObject;
                GameObject newDest = (GameObject)EditorGUILayout.ObjectField(currentDest, typeof(GameObject), true);

                if (newDest != currentDest)
                {
                    step.destination.GameObject = newDest;
                }

                EditorGUILayout.EndHorizontal();

                // Component validation for destination (snap socket)
                if (step.destination.GameObject != null)
                {
                    DrawSnapDestinationValidation(step.destination.GameObject);
                }
            }
        }
    }

    /// <summary>
    /// Draw validation for Grab target objects (XRGrabInteractable/Grabbable, Rigidbody, Collider)
    /// </summary>
    private void DrawGrabTargetValidation(GameObject target)
    {
        var framework = VRFrameworkDetector.DetectCurrentFramework();
        var validationMessages = new System.Collections.Generic.List<string>();
        bool hasErrors = false;

        // Check for interactable component based on framework
        if (framework == VRFramework.AutoHands)
        {
            var grabbable = target.GetComponent<Autohand.Grabbable>();
            if (grabbable != null)
            {
                validationMessages.Add("\u2705 Grabbable component found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing Grabbable component!");
                hasErrors = true;
            }
        }
        else // XRI or None (default to XRI)
        {
            var grabInteractable = target.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                validationMessages.Add("\u2705 XRGrabInteractable found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing XRGrabInteractable component!");
                hasErrors = true;
            }
        }

        // Check for Rigidbody
        var rigidbody = target.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            validationMessages.Add("\u2705 Rigidbody found");
        }
        else
        {
            validationMessages.Add("\u26A0\uFE0F Missing Rigidbody component!");
            hasErrors = true;
        }

        // Check for Collider (on object or children)
        var collider = target.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            validationMessages.Add("\u2705 Collider found");
        }
        else
        {
            validationMessages.Add("\u26A0\uFE0F Missing Collider component!");
            hasErrors = true;
        }

        // Check for any valid interaction tag
        string[] validTags = { "grab", "knob", "screw", "valve" };
        bool hasValidTag = System.Array.Exists(validTags, tag => target.CompareTag(tag));

        if (hasValidTag)
        {
            validationMessages.Add($"\u2705 Tagged as '{target.tag}'");
        }
        else
        {
            validationMessages.Add("\u26A0\uFE0F Object should be tagged as 'grab', 'knob', 'screw', or 'valve'");
        }

        // Display validation result
        string message = string.Join("\n", validationMessages);
        EditorGUILayout.HelpBox(message, hasErrors ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// Draw validation for TurnKnob target objects (Grabbable + KnobController + HingeJoint)
    /// Simplified validation - AutoHands focuses on controller script presence
    /// </summary>
    private void DrawKnobTargetValidation(GameObject target)
    {
        var framework = VRFrameworkDetector.DetectCurrentFramework();
        var validationMessages = new System.Collections.Generic.List<string>();
        bool hasErrors = false;

        // Check for interactable component based on framework
        if (framework == VRFramework.AutoHands)
        {
            var grabbable = target.GetComponent<Autohand.Grabbable>();
            if (grabbable != null)
            {
                validationMessages.Add("\u2705 Grabbable component found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing Grabbable component!");
                hasErrors = true;
            }

            // Check for AutoHandsKnobController (CRITICAL)
            var autoKnobController = target.GetComponent<AutoHandsKnobController>();
            if (autoKnobController != null)
            {
                validationMessages.Add($"\u2705 AutoHandsKnobController found (Angle: {autoKnobController.CurrentAngle:F1}\u00B0)");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing AutoHandsKnobController component! (CRITICAL)");
                hasErrors = true;
            }

            // Check for knob tag
            if (!target.CompareTag("knob"))
            {
                validationMessages.Add("\u26A0\uFE0F Object should be tagged as 'knob' for consistency");
            }
            else
            {
                validationMessages.Add("\u2705 Tagged as 'knob'");
            }
        }
        else // XRI
        {
            var grabInteractable = target.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                validationMessages.Add("\u2705 XRGrabInteractable found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing XRGrabInteractable component!");
                hasErrors = true;
            }

            // Check for KnobController (CRITICAL)
            var knobController = target.GetComponent<KnobController>();
            if (knobController != null)
            {
                validationMessages.Add($"\u2705 KnobController found (Angle: {knobController.CurrentAngle:F1}\u00B0)");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing KnobController component! (CRITICAL)");
                hasErrors = true;
            }

            // Check for HingeJoint
            var hingeJoint = target.GetComponent<HingeJoint>();
            if (hingeJoint != null)
            {
                string limitInfo = hingeJoint.useLimits
                    ? $"Limits: {hingeJoint.limits.min:F0}\u00B0 to {hingeJoint.limits.max:F0}\u00B0"
                    : "No limits set";
                validationMessages.Add($"\u2705 HingeJoint found ({limitInfo})");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing HingeJoint component!");
                hasErrors = true;
            }

            // Check for knob tag
            if (!target.CompareTag("knob"))
            {
                validationMessages.Add("\u26A0\uFE0F Object should be tagged as 'knob' for consistency");
            }
        }

        // Display validation result
        string message = string.Join("\n", validationMessages);
        EditorGUILayout.HelpBox(message, hasErrors ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// Draw validation for Snap destination objects (XRSocketInteractor/PlacePoint)
    /// Simplified validation - AutoHands only checks PlacePoint + tag
    /// </summary>
    private void DrawSnapDestinationValidation(GameObject destination)
    {
        var framework = VRFrameworkDetector.DetectCurrentFramework();
        var validationMessages = new System.Collections.Generic.List<string>();
        bool hasErrors = false;

        // Check for socket component based on framework
        if (framework == VRFramework.AutoHands)
        {
            // AutoHands validation: Only check PlacePoint component
            var placePoint = GetAutoHandsPlacePoint(destination);
            if (placePoint != null)
            {
                validationMessages.Add("\u2705 PlacePoint component found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing PlacePoint component!");
                hasErrors = true;
            }

            // Check for snap tag
            if (!destination.CompareTag("snap"))
            {
                validationMessages.Add("\u26A0\uFE0F Destination should be tagged as 'snap' for consistency");
            }
            else
            {
                validationMessages.Add("\u2705 Tagged as 'snap'");
            }
        }
        else // XRI
        {
            var socketInteractor = destination.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null)
            {
                validationMessages.Add("\u2705 XRSocketInteractor found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing XRSocketInteractor component!");
                hasErrors = true;
            }

            // Check for SnapValidator (XRI-specific)
            var snapValidator = destination.GetComponent<SnapValidator>();
            if (snapValidator != null)
            {
                validationMessages.Add("\u2705 SnapValidator found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing SnapValidator component (recommended for sequence validation)");
            }

            // Check for Collider with isTrigger
            var collider = destination.GetComponent<Collider>();
            if (collider != null)
            {
                if (collider.isTrigger)
                {
                    validationMessages.Add("\u2705 Trigger Collider found");
                }
                else
                {
                    validationMessages.Add("\u26A0\uFE0F Collider should be set as Trigger for socket detection");
                }
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing Collider component!");
                hasErrors = true;
            }

            // Check for snap tag
            if (!destination.CompareTag("snap"))
            {
                validationMessages.Add("\u26A0\uFE0F Destination should be tagged as 'snap' for consistency");
            }
        }

        // Display validation result
        string message = string.Join("\n", validationMessages);
        EditorGUILayout.HelpBox(message, hasErrors ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// Helper to get AutoHands PlacePoint component using reflection
    /// </summary>
    private MonoBehaviour GetAutoHandsPlacePoint(GameObject obj)
    {
        var components = obj.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && component.GetType().Name == "PlacePoint")
            {
                return component;
            }
        }
        return null;
    }

    /// <summary>
    /// Draw validation for Valve target objects (ScrewController/AutoHandsScrewController, Grabbable)
    /// Simplified validation - AutoHands focuses on controller script presence
    /// </summary>
    private void DrawValveTargetValidation(GameObject target)
    {
        var framework = VRFrameworkDetector.DetectCurrentFramework();
        var validationMessages = new System.Collections.Generic.List<string>();
        bool hasErrors = false;

        // Check for interactable component based on framework
        if (framework == VRFramework.AutoHands)
        {
            var grabbable = target.GetComponent<Autohand.Grabbable>();
            if (grabbable != null)
            {
                validationMessages.Add("\u2705 Grabbable component found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing Grabbable component!");
                hasErrors = true;
            }

            // Check for AutoHandsScrewController (V1 or V2) - CRITICAL
            var screwController = target.GetComponent<AutoHandsScrewController>();
            var screwControllerV2 = target.GetComponent<AutoHandsScrewControllerV2>();
            if (screwController != null)
            {
                validationMessages.Add($"\u2705 AutoHandsScrewController found (Current Rotation: {screwController.CurrentRotation:F1}\u00B0)");
            }
            else if (screwControllerV2 != null)
            {
                validationMessages.Add($"\u2705 AutoHandsScrewControllerV2 found (Current Rotation: {screwControllerV2.CurrentRotation:F1}\u00B0)");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing AutoHandsScrewController or V2 component! (CRITICAL)");
                hasErrors = true;
            }

            // Check for valve/screw tag
            if (target.CompareTag("valve") || target.CompareTag("screw"))
            {
                validationMessages.Add($"\u2705 Tagged as '{target.tag}'");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Object should be tagged as 'valve' or 'screw' for consistency");
            }
        }
        else // XRI
        {
            var grabInteractable = target.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                validationMessages.Add("\u2705 XRGrabInteractable found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing XRGrabInteractable component!");
                hasErrors = true;
            }

            // Check for ScrewController - CRITICAL
            var screwController = target.GetComponent<ScrewController>();
            if (screwController != null)
            {
                validationMessages.Add($"\u2705 ScrewController found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing ScrewController component! (CRITICAL)");
                hasErrors = true;
            }

            // Check for Rigidbody
            var rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                validationMessages.Add("\u2705 Rigidbody found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing Rigidbody component!");
                hasErrors = true;
            }

            // Check for valve tag
            if (!target.CompareTag("valve"))
            {
                validationMessages.Add("\u26A0\uFE0F Object should be tagged as 'valve' for consistency");
            }
        }

        // Display validation result
        string message = string.Join("\n", validationMessages);
        EditorGUILayout.HelpBox(message, hasErrors ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// Draw validation for Valve socket objects (XRSocketInteractor/PlacePoint)
    /// Simplified validation - AutoHands only checks PlacePoint + tag
    /// </summary>
    private void DrawValveSocketValidation(GameObject socket)
    {
        var framework = VRFrameworkDetector.DetectCurrentFramework();
        var validationMessages = new System.Collections.Generic.List<string>();
        bool hasErrors = false;

        // Check for socket component based on framework
        if (framework == VRFramework.AutoHands)
        {
            // AutoHands validation: Only check PlacePoint component
            var placePoint = GetAutoHandsPlacePoint(socket);
            if (placePoint != null)
            {
                validationMessages.Add("\u2705 PlacePoint component found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing PlacePoint component!");
                hasErrors = true;
            }

            // Check for snap tag
            if (!socket.CompareTag("snap"))
            {
                validationMessages.Add("\u26A0\uFE0F Socket should be tagged as 'snap' for consistency");
            }
            else
            {
                validationMessages.Add("\u2705 Tagged as 'snap'");
            }
        }
        else // XRI
        {
            var socketInteractor = socket.GetComponent<XRSocketInteractor>();
            if (socketInteractor != null)
            {
                validationMessages.Add("\u2705 XRSocketInteractor found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing XRSocketInteractor component!");
                hasErrors = true;
            }

            // Check for SnapValidator (XRI-specific)
            var snapValidator = socket.GetComponent<SnapValidator>();
            if (snapValidator != null)
            {
                validationMessages.Add("\u2705 SnapValidator found");
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing SnapValidator component (recommended for sequence validation)");
            }

            // Check for Collider with isTrigger
            var collider = socket.GetComponent<Collider>();
            if (collider != null)
            {
                if (collider.isTrigger)
                {
                    validationMessages.Add("\u2705 Trigger Collider found");
                }
                else
                {
                    validationMessages.Add("\u26A0\uFE0F Collider should be set as Trigger for socket detection");
                }
            }
            else
            {
                validationMessages.Add("\u26A0\uFE0F Missing Collider component!");
                hasErrors = true;
            }

            // Check for snap tag
            if (!socket.CompareTag("snap"))
            {
                validationMessages.Add("\u26A0\uFE0F Socket should be tagged as 'snap' for consistency");
            }
        }

        // Display validation result
        string message = string.Join("\n", validationMessages);
        EditorGUILayout.HelpBox(message, hasErrors ? MessageType.Warning : MessageType.Info);
    }

    private void DrawTypeSpecificSettings(InteractionStep step)
    {
        // WaitForScriptCondition info
        if (step.type == InteractionStep.StepType.WaitForScriptCondition)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Target Object must have a component that implements ISequenceCondition interface.\n\n" +
                "Available condition types:\n" +
                "• DummyCondition (for testing)\n" +
                "• ButtonPressCondition\n" +
                "• Custom conditions (inherit from BaseSequenceCondition)",
                MessageType.Info);
        }

        // Knob settings
        if (step.type == InteractionStep.StepType.TurnKnob)
        {
            DrawKnobSettings(step);
        }

        // Screw settings
        if (step.type == InteractionStep.StepType.TightenScrew ||
            step.type == InteractionStep.StepType.LoosenScrew ||
            step.type == InteractionStep.StepType.InstallScrew ||
            step.type == InteractionStep.StepType.RemoveScrew)
        {
            DrawScrewSettings(step);
        }

        // Teleport settings
        if (step.type == InteractionStep.StepType.Teleport)
        {
            DrawTeleportSettings(step);
        }
    }

    private void DrawKnobSettings(InteractionStep step)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Knob Settings", EditorStyles.boldLabel);
        step.targetAngle = EditorGUILayout.FloatField("Target Angle", step.targetAngle);
        step.angleTolerance = EditorGUILayout.FloatField("Angle Tolerance", step.angleTolerance);

        // Rotation direction dropdown
        EditorGUILayout.Space(3);
        step.knobRotationType = (InteractionStep.KnobRotationType)EditorGUILayout.EnumPopup(
            new GUIContent("Rotation Direction", "Required rotation direction based on HingeJoint limits"),
            step.knobRotationType
        );

        // Help text for rotation direction
        string directionHelp = step.knobRotationType switch
        {
            InteractionStep.KnobRotationType.OpenToMax => "Opening: Rotate toward max limit (increasing angle)",
            InteractionStep.KnobRotationType.CloseToMin => "Closing: Rotate toward min limit (decreasing angle)",
            InteractionStep.KnobRotationType.Any => "Any direction is acceptable",
            _ => ""
        };
        if (!string.IsNullOrEmpty(directionHelp))
        {
            EditorGUILayout.HelpBox(directionHelp, MessageType.Info);
        }
    }

    private void DrawScrewSettings(InteractionStep step)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("\uD83D\uDD27 Valve Settings", EditorStyles.boldLabel);

        // Target Object field (valve)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Object", GUILayout.Width(100));
        step.targetObject.GameObject = (GameObject)EditorGUILayout.ObjectField(
            step.targetObject.GameObject, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();

        // Validation for target object (valve)
        if (step.targetObject.GameObject != null)
        {
            DrawValveTargetValidation(step.targetObject.GameObject);
        }

        EditorGUILayout.Space(5);

        // Target Socket field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Socket", GUILayout.Width(100));
        step.targetSocket.GameObject = (GameObject)EditorGUILayout.ObjectField(
            step.targetSocket.GameObject, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();

        // Validation for target socket
        if (step.targetSocket.GameObject != null)
        {
            DrawValveSocketValidation(step.targetSocket.GameObject);
        }

        EditorGUILayout.Space(5);

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
        if (step.type == InteractionStep.StepType.TightenScrew ||
            step.type == InteractionStep.StepType.InstallScrew)
        {
            EditorGUILayout.Space(3);
            step.tightenThreshold = EditorGUILayout.Slider("Tighten Degrees", step.tightenThreshold, 10f, 360f);
        }

        if (step.type == InteractionStep.StepType.LoosenScrew ||
            step.type == InteractionStep.StepType.RemoveScrew)
        {
            EditorGUILayout.Space(3);
            step.loosenThreshold = EditorGUILayout.Slider("Loosen Degrees", step.loosenThreshold, 10f, 360f);
        }

        if (step.type == InteractionStep.StepType.InstallScrew ||
            step.type == InteractionStep.StepType.RemoveScrew)
        {
            // Complete operations show both thresholds
            EditorGUILayout.Space(3);
            step.tightenThreshold = EditorGUILayout.Slider("Tighten Degrees", step.tightenThreshold, 10f, 360f);
            step.loosenThreshold = EditorGUILayout.Slider("Loosen Degrees", step.loosenThreshold, 10f, 360f);
        }

        // Common settings
        EditorGUILayout.Space(3);
        step.screwAngleTolerance = EditorGUILayout.Slider("Angle Tolerance", step.screwAngleTolerance, 1f, 15f);

        // Advanced settings
        EditorGUILayout.Space(3);
        step.rotationDampening = EditorGUILayout.Slider("Rotation Dampening", step.rotationDampening, 0f, 10f);
        if (step.rotationDampening == 0f)
        {
            EditorGUILayout.HelpBox("Set to 0 to use profile default", MessageType.Info);
        }
    }

    private void DrawTeleportSettings(InteractionStep step)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🚀 Teleport Settings", EditorStyles.boldLabel);

        // Wrist Button field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wrist Button", GUILayout.Width(100));
        GameObject currentButton = step.wristButton.GameObject;
        GameObject newButton = (GameObject)EditorGUILayout.ObjectField(currentButton, typeof(GameObject), true);
        if (newButton != currentButton)
        {
            step.wristButton.GameObject = newButton;
        }
        EditorGUILayout.EndHorizontal();

        // Validate WristUIButton component
        if (step.wristButton.GameObject != null)
        {
            var wristUIButton = step.wristButton.GameObject.GetComponent<WristUIButton>();
            if (wristUIButton == null)
            {
                EditorGUILayout.HelpBox("⚠️ Selected GameObject does not have WristUIButton component!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("✅ Valid WristUIButton component found", MessageType.Info);
            }
        }

        EditorGUILayout.Space(3);

        // Teleport Destination field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Destination", GUILayout.Width(100));
        GameObject currentDest = step.teleportDestination.GameObject;
        GameObject newDest = (GameObject)EditorGUILayout.ObjectField(currentDest, typeof(GameObject), true);
        if (newDest != currentDest)
        {
            step.teleportDestination.GameObject = newDest;
        }
        EditorGUILayout.EndHorizontal();

        // Validate TeleportController component
        if (step.teleportDestination.GameObject != null)
        {
            var teleportController = step.teleportDestination.GameObject.GetComponent<TeleportController>();
            if (teleportController == null)
            {
                EditorGUILayout.HelpBox("⚠️ Selected GameObject does not have TeleportController component!", MessageType.Warning);
            }
            else
            {
                // Show TeleportController info
                string info = $"✅ TeleportController found\n" +
                             $"Recentering: {(teleportController.enableRecentering ? "Enabled" : "Disabled")}\n" +
                             $"Preview: {(teleportController.showDestinationPreview ? "Visible" : "Hidden")}";
                if (teleportController.autoHandPlayerReference == null)
                {
                    info += "\n⚠️ AutoHandPlayer reference not set on controller!";
                }
                EditorGUILayout.HelpBox(info, teleportController.autoHandPlayerReference == null ? MessageType.Warning : MessageType.Info);
            }

            // Check for teleportPoint tag
            if (!step.teleportDestination.GameObject.CompareTag("teleportPoint"))
            {
                EditorGUILayout.HelpBox("⚠️ Destination should be tagged as 'teleportPoint' for consistency", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(3);

        // Recentering settings
        EditorGUILayout.LabelField("Recentering Settings", EditorStyles.boldLabel);
        step.enableRecentering = EditorGUILayout.Toggle("Enable Recentering", step.enableRecentering);

        if (step.enableRecentering)
        {
            step.recenteringDelay = EditorGUILayout.Slider("Recentering Delay", step.recenteringDelay, 0f, 2f);
            EditorGUILayout.HelpBox("XR tracking origin will recenter after teleport", MessageType.Info);
        }
    }

    private void DrawExecutionSettings(InteractionStep step)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Execution Settings", EditorStyles.boldLabel);
        step.allowParallel = EditorGUILayout.Toggle("Allow Parallel", step.allowParallel);
        step.isOptional = EditorGUILayout.Toggle("Is Optional", step.isOptional);

        // Hint
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Instruction");
        step.hint = EditorGUILayout.TextArea(step.hint, GUILayout.Height(40));
    }

    private void DrawGuidanceArrows(InteractionStep step)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Guidance Arrows", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Place arrow GameObjects independently in scene, then reference them here. Arrows will show/hide automatically based on step progress.", MessageType.Info);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Target Arrow", EditorStyles.miniBoldLabel);
        step.targetArrow.GameObject = (GameObject)EditorGUILayout.ObjectField(
            "Arrow GameObject",
            step.targetArrow.GameObject,
            typeof(GameObject),
            true
        );

        if (step.targetArrow.GameObject != null)
        {
            EditorGUI.indentLevel++;
            step.hideTargetArrowAfterGrab = EditorGUILayout.Toggle("Hide After Grab", step.hideTargetArrowAfterGrab);
            EditorGUI.indentLevel--;

            // Validate arrow has GuidanceArrow component
            if (step.targetArrow.GameObject.GetComponent<GuidanceArrow>() == null)
            {
                EditorGUILayout.HelpBox("Warning: Arrow GameObject needs GuidanceArrow component!", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Destination Arrow (Optional)", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("For GrabAndSnap/Valve steps - shows after object is grabbed", EditorStyles.miniLabel);
        step.destinationArrow.GameObject = (GameObject)EditorGUILayout.ObjectField(
            "Arrow GameObject",
            step.destinationArrow.GameObject,
            typeof(GameObject),
            true
        );

        if (step.destinationArrow.GameObject != null)
        {
            EditorGUI.indentLevel++;
            step.showDestinationAfterGrab = EditorGUILayout.Toggle("Show After Grab", step.showDestinationAfterGrab);
            EditorGUI.indentLevel--;

            // Validate arrow has GuidanceArrow component
            if (step.destinationArrow.GameObject.GetComponent<GuidanceArrow>() == null)
            {
                EditorGUILayout.HelpBox("Warning: Arrow GameObject needs GuidanceArrow component!", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawValidationStatus(InteractionStep step)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        bool isValid = step.IsValid();
        string validationMessage = step.GetValidationMessage();

        GUIStyle validationStyle = isValid ? _successStyle : _errorStyle;
        EditorGUILayout.LabelField($"Status: {validationMessage}", validationStyle);

        if (!isValid)
        {
            EditorGUILayout.HelpBox("This step has validation errors. Check the target objects and settings above.", MessageType.Warning);
        }
    }
}
#endif

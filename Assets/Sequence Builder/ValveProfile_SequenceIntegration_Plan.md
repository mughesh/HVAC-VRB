# Valve Profile Sequence Integration - Implementation Plan

## Overview
This document outlines the phase-by-phase implementation plan for integrating the sophisticated ValveProfile interaction system with the existing TrainingSequence controller. The valve system supports complex forward/reverse flow operations (grab → snap → tighten → loosen → remove) and needs to be integrated following established patterns while exposing essential controls for sequence design.

## Current State Analysis

### Existing Valve System ✅ COMPLETED
- **ValveProfile.cs**: Comprehensive ScriptableObject with rotation mechanics, socket compatibility, physics settings
- **ValveController.cs**: Complex state machine with forward/reverse flow, socket interaction, rotation tracking
- **Working Features**: Full interaction lifecycle, proper state transitions, socket management, rotation dampening

### Existing Sequence Integration Pattern (Knob Reference)
- **Data Model**: `InteractionStep.StepType.TurnKnob` with `targetAngle`, `angleTolerance` parameters
- **Controller Integration**: `TrainingSequenceController` subscribes to `KnobController.OnAngleChanged` events
- **UI Controls**: Simple float fields in `VRInteractionSetupWindow.cs` sequence editor
- **Validation**: Angle range checking, component validation, error reporting

## Integration Architecture

### Core Design Principles
1. **Follow Existing Patterns**: Mirror knob integration approach for consistency
2. **Essential Controls Only**: Expose necessary parameters without overwhelming UI
3. **Profile-First Design**: Use profile as defaults, allow sequence-level overrides
4. **Iterative Testing**: Validate each phase before proceeding
5. **Backward Compatibility**: Don't break existing sequence functionality

### Key Integration Points
- Extend `InteractionStep` with valve-specific step types and parameters
- Integrate `ValveController` events with `TrainingSequenceController`
- Add valve UI controls to sequence editor following knob pattern
- Implement parameter override system for per-object customization

---

## Phase 1: Extend Sequence Data Model
**Duration**: 1-2 days  
**Objective**: Add valve step types and essential parameters to the sequence system data structures

### Implementation Tasks

#### 1.1 Add Valve Step Types to Enum
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequence.cs`

**Changes**:
```csharp
public enum StepType 
{
    // Existing
    Grab, GrabAndSnap, TurnKnob, WaitForCondition, ShowInstruction,
    
    // New valve step types
    TightenValve,    // Forward flow: grab → snap → tighten
    LoosenValve,     // Reverse flow: loosen → remove
    InstallValve,    // Complete forward flow (grab → snap → tighten)
    RemoveValve      // Complete reverse flow (loosen → remove)
}
```

#### 1.2 Add Valve Parameters to InteractionStep Class
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequence.cs`

**Add after existing knob settings**:
```csharp
[Header("Valve Settings")]
[Tooltip("For valve operations: Rotation axis (X=1,0,0 Y=0,1,0 Z=0,0,1)")]
public Vector3 rotationAxis = Vector3.up;

[Tooltip("For TightenValve/InstallValve: Degrees of rotation required to tighten")]
[Range(10f, 360f)]
public float tightenThreshold = 90f;

[Tooltip("For LoosenValve/RemoveValve: Degrees of reverse rotation required to loosen")]  
[Range(10f, 360f)]
public float loosenThreshold = 90f;

[Tooltip("For valve operations: Angle completion tolerance")]
[Range(1f, 15f)]
public float valveAngleTolerance = 5f;

[Tooltip("Socket for valve operations (TightenValve, LoosenValve)")]
public GameObjectReference targetSocket = new GameObjectReference();

[Header("Valve Advanced Settings")]
[Tooltip("Rotation dampening/friction override (0 = use profile default)")]
[Range(0f, 10f)]
public float rotationDampening = 0f; // 0 means use profile default
```

#### 1.3 Update Validation Logic
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequence.cs`

**Update `IsValid()` method**:
```csharp
case StepType.TightenValve:
case StepType.LoosenValve:
    return targetObject != null && targetObject.IsValid && 
           targetSocket != null && targetSocket.IsValid;
           
case StepType.InstallValve:
case StepType.RemoveValve:
    return targetObject != null && targetObject.IsValid && 
           targetSocket != null && targetSocket.IsValid;
```

**Update `GetValidationMessage()` method**:
```csharp
case StepType.TightenValve:
    return "Requires target valve object and socket";
case StepType.LoosenValve:
    return "Requires target valve object and socket";  
case StepType.InstallValve:
    return "Requires target valve object and socket for complete installation";
case StepType.RemoveValve:
    return "Requires target valve object and socket for complete removal";
```

### Testing Phase 1

#### Test Case 1.1: Data Model Validation
**Objective**: Verify new step types and parameters are properly integrated

**Test Steps**:
1. Create new `TrainingSequenceAsset` in editor
2. Add a TaskGroup with valve steps
3. Verify new step types appear in enum dropdown
4. Set valve parameters (rotation axis, thresholds)
5. Save and reload asset

**Success Criteria**:
- All 4 new valve step types appear in editor
- Valve parameters are visible and editable
- Parameters save/load correctly with asset
- Validation messages show for incomplete valve steps

#### Test Case 1.2: Parameter Serialization
**Objective**: Ensure valve parameters persist correctly

**Test Steps**:
1. Create valve step with custom parameters:
   - Rotation Axis: (1,0,0) 
   - Tighten Threshold: 180°
   - Loosen Threshold: 45°
   - Valve Angle Tolerance: 10°
2. Save asset and restart Unity
3. Reload asset and verify parameters

**Success Criteria**:
- All custom parameters preserved after Unity restart
- Parameters display correctly in inspector
- Default values applied to new steps

#### Test Case 1.3: Validation Logic
**Objective**: Confirm validation works for valve steps

**Test Steps**:
1. Create `TightenValve` step with missing target object
2. Create `LoosenValve` step with missing socket
3. Create complete valve steps
4. Check validation results

**Success Criteria**:
- Missing references show validation errors
- Error messages are clear and specific
- Complete steps pass validation
- `IsValid()` returns correct boolean values

---

## Phase 2: TrainingSequenceController Integration
**Duration**: 2-3 days  
**Objective**: Integrate ValveController events with sequence controller following knob pattern

### Implementation Tasks

#### 2.1 Add ValveController Detection and Storage
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequenceController.cs`

**Add to class fields**:
```csharp
private Dictionary<GameObject, ValveController> valveControllers = new Dictionary<GameObject, ValveController>();
private Dictionary<ValveController, System.Action<ValveState>> valveStateEventDelegates = new Dictionary<ValveController, System.Action<ValveState>>();
private Dictionary<ValveController, System.Action> valveTightenedEventDelegates = new Dictionary<ValveController, System.Action>();
private Dictionary<ValveController, System.Action> valveLoosened EventDelegates = new Dictionary<ValveController, System.Action>();
```

#### 2.2 Add ValveController Discovery in Initialize()
**Add after knob controller discovery**:
```csharp
// Find all valve controllers
var valveControllers = FindObjectsOfType<ValveController>();
foreach (var valveController in valveControllers)
{
    this.valveControllers[valveController.gameObject] = valveController;
}
LogDebug($"Found {valveControllers.Length} valve controllers in scene");
```

#### 2.3 Implement Valve Step Execution
**Add new method**:
```csharp
private void ExecuteValveStep(InteractionStep step)
{
    GameObject targetObject = step.targetObject.GameObject;
    
    if (targetObject == null)
    {
        LogError($"Valve step '{step.stepName}': target object not found");
        return;
    }
    
    if (valveControllers.TryGetValue(targetObject, out ValveController valveController))
    {
        // Apply step-specific parameters if overrides are set
        ApplyValveStepParameters(valveController, step);
        
        // Subscribe to appropriate events based on step type
        switch (step.type)
        {
            case InteractionStep.StepType.TightenValve:
            case InteractionStep.StepType.InstallValve:
                SubscribeToValveTightenEvents(step, valveController);
                break;
                
            case InteractionStep.StepType.LoosenValve:
            case InteractionStep.StepType.RemoveValve:
                SubscribeToValveLoosenEvents(step, valveController);
                break;
        }
        
        LogDebug($"Subscribed to valve events for: {targetObject.name} (Step: {step.type})");
    }
    else
    {
        LogError($"ValveController not found on {targetObject.name}");
    }
}
```

#### 2.4 Implement Event Subscription Methods
```csharp
private void SubscribeToValveTightenEvents(InteractionStep step, ValveController valveController)
{
    System.Action tightenDelegate = () => OnValveTightened(step);
    valveTightenedEventDelegates[valveController] = tightenDelegate;
    valveController.OnValveTightened += tightenDelegate;
}

private void SubscribeToValveLoosenEvents(InteractionStep step, ValveController valveController)
{
    System.Action loosenDelegate = () => OnValveLoosened(step);
    valveLoosened EventDelegates[valveController] = loosenDelegate;
    valveController.OnValveLoosened += loosenDelegate;
}

private void OnValveTightened(InteractionStep step)
{
    LogDebug($"Valve tightened for step: {step.stepName}");
    MarkStepCompleted(step);
}

private void OnValveLoosened(InteractionStep step)
{
    LogDebug($"Valve loosened for step: {step.stepName}");
    
    if (step.type == InteractionStep.StepType.LoosenValve || 
        step.type == InteractionStep.StepType.RemoveValve)
    {
        MarkStepCompleted(step);
    }
}
```

#### 2.5 Implement Parameter Override System
```csharp
private void ApplyValveStepParameters(ValveController valveController, InteractionStep step)
{
    // Get current valve profile
    var profile = GetValveProfile(valveController);
    if (profile == null) return;
    
    // Apply sequence-level parameter overrides
    bool profileChanged = false;
    
    if (step.rotationAxis != Vector3.up) // Check if axis was customized
    {
        profile.rotationAxis = step.rotationAxis;
        profileChanged = true;
    }
    
    if (step.tightenThreshold != 90f) // Check if threshold was customized  
    {
        profile.tightenThreshold = step.tightenThreshold;
        profileChanged = true;
    }
    
    if (step.loosenThreshold != 90f)
    {
        profile.loosenThreshold = step.loosenThreshold;
        profileChanged = true;
    }
    
    if (step.valveAngleTolerance != 5f)
    {
        profile.angleTolerance = step.valveAngleTolerance;
        profileChanged = true;
    }
    
    if (step.rotationDampening > 0f)
    {
        profile.rotationDampening = step.rotationDampening;
        profileChanged = true;
    }
    
    if (profileChanged)
    {
        valveController.Configure(profile);
        LogDebug($"Applied sequence parameter overrides to {valveController.name}");
    }
}
```

### Testing Phase 2

#### Test Case 2.1: ValveController Detection
**Objective**: Verify valve controllers are found and stored correctly

**Test Setup**:
1. Create test scene with 3 valve objects (different profiles)
2. Add ValveProfile to each object
3. Add TrainingSequenceController to scene

**Test Steps**:
1. Start scene
2. Check console for "Found X valve controllers" message
3. Verify all valve objects are in controller dictionary

**Success Criteria**:
- All valve controllers detected at scene start
- Console shows correct count
- No null reference errors

#### Test Case 2.2: Valve Step Execution - Tighten Flow
**Objective**: Test forward flow (tighten) step completion

**Test Setup**:
1. Sequence with `TightenValve` step
2. Target object with valve profile (90° threshold)
3. Socket object configured

**Test Steps**:
1. Start sequence
2. Grab valve → snap to socket → rotate 90°
3. Verify step completion

**Success Criteria**:
- Step activates when sequence starts
- Events subscribed without errors
- Step completes when valve tightened
- Console shows "Valve tightened for step" message

#### Test Case 2.3: Valve Step Execution - Loosen Flow  
**Objective**: Test reverse flow (loosen) step completion

**Test Setup**:
1. Sequence with `LoosenValve` step
2. Valve already in TIGHT state in socket
3. 90° loosen threshold

**Test Steps**:
1. Start sequence with valve pre-positioned
2. Grab valve → rotate backwards 90°
3. Verify step completion

**Success Criteria**:
- Step activates correctly
- Loosen events trigger step completion
- Valve transitions through expected states
- Step marked complete after loosening

#### Test Case 2.4: Parameter Override System
**Objective**: Verify sequence parameters override profile defaults

**Test Setup**:
1. ValveProfile with default 90° thresholds
2. Sequence step with custom 180° tighten threshold
3. Custom rotation axis (X instead of Y)

**Test Steps**:
1. Start sequence
2. Check valve controller parameters after step activation
3. Perform valve interaction with new parameters

**Success Criteria**:
- Profile updated with sequence parameters
- Console shows "Applied sequence parameter overrides" 
- Valve requires 180° rotation (not 90°) for completion
- Rotation occurs on correct axis

---

## Phase 3: Sequence Editor UI Integration
**Duration**: 2-3 days  
**Objective**: Add valve-specific UI controls to sequence editor following knob pattern

### Implementation Tasks

#### 3.1 Add Valve Step Types to Add Menu
**File**: `Assets/VRTrainingKit/Scripts/VRInteractionSetupWindow.cs`

**Update `ShowAddStepMenu()` method**:
```csharp
private void ShowAddStepMenu(TaskGroup taskGroup)
{
    GenericMenu menu = new GenericMenu();
    
    // Existing items
    menu.AddItem(new GUIContent("Grab Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.Grab));
    menu.AddItem(new GUIContent("Grab and Snap Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.GrabAndSnap));
    menu.AddItem(new GUIContent("Turn Knob Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.TurnKnob));
    
    // New valve items  
    menu.AddSeparator("");
    menu.AddItem(new GUIContent("Valve Operations/Tighten Valve"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.TightenValve));
    menu.AddItem(new GUIContent("Valve Operations/Loosen Valve"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.LoosenValve));
    menu.AddItem(new GUIContent("Valve Operations/Install Valve (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.InstallValve));
    menu.AddItem(new GUIContent("Valve Operations/Remove Valve (Complete)"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.RemoveValve));
    
    // Existing items
    menu.AddSeparator("");
    menu.AddItem(new GUIContent("Wait Condition Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.WaitForCondition));
    menu.AddItem(new GUIContent("Show Instruction Step"), false, () => AddNewStep(taskGroup, InteractionStep.StepType.ShowInstruction));
    
    menu.ShowAsContext();
}
```

#### 3.2 Add Valve Icons to Step Display
**Update `GetStepTypeIcon()` method**:
```csharp
private string GetStepTypeIcon(InteractionStep.StepType stepType)
{
    switch (stepType)
    {
        // Existing
        case InteractionStep.StepType.Grab: return "✋";
        case InteractionStep.StepType.GrabAndSnap: return "🔗";  
        case InteractionStep.StepType.TurnKnob: return "🔄";
        case InteractionStep.StepType.WaitForCondition: return "⏳";
        case InteractionStep.StepType.ShowInstruction: return "💬";
        
        // New valve icons
        case InteractionStep.StepType.TightenValve: return "🔧"; 
        case InteractionStep.StepType.LoosenValve: return "🔓";
        case InteractionStep.StepType.InstallValve: return "⚙️";
        case InteractionStep.StepType.RemoveValve: return "🔨";
        
        default: return "❓";
    }
}
```

#### 3.3 Add Valve Settings UI Panel
**Update `DrawStepDetails()` method after knob settings section**:
```csharp
// Valve-specific settings
if (step.type == InteractionStep.StepType.TightenValve ||
    step.type == InteractionStep.StepType.LoosenValve ||
    step.type == InteractionStep.StepType.InstallValve ||
    step.type == InteractionStep.StepType.RemoveValve)
{
    EditorGUILayout.Space(5);
    EditorGUILayout.LabelField("Valve Settings", EditorStyles.boldLabel);
    
    // Target Socket field
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("Target Socket", GUILayout.Width(150));
    
    // GameObject field for socket
    step.targetSocket.GameObject = (GameObject)EditorGUILayout.ObjectField(
        step.targetSocket.GameObject, typeof(GameObject), true);
        
    // Quick-select button for sockets in scene
    if (GUILayout.Button("🎯", GUILayout.Width(30)))
    {
        ShowSocketSelectionMenu(step);
    }
    EditorGUILayout.EndHorizontal();
    
    // Rotation axis selection
    EditorGUILayout.Space(3);
    EditorGUILayout.LabelField("Rotation Axis");
    EditorGUILayout.BeginHorizontal();
    
    bool isXAxis = GUILayout.Toggle(step.rotationAxis == Vector3.right, "X-Axis");
    bool isYAxis = GUILayout.Toggle(step.rotationAxis == Vector3.up, "Y-Axis"); 
    bool isZAxis = GUILayout.Toggle(step.rotationAxis == Vector3.forward, "Z-Axis");
    
    if (isXAxis && step.rotationAxis != Vector3.right) step.rotationAxis = Vector3.right;
    if (isYAxis && step.rotationAxis != Vector3.up) step.rotationAxis = Vector3.up;
    if (isZAxis && step.rotationAxis != Vector3.forward) step.rotationAxis = Vector3.forward;
    
    EditorGUILayout.EndHorizontal();
    
    // Threshold settings based on step type
    if (step.type == InteractionStep.StepType.TightenValve || 
        step.type == InteractionStep.StepType.InstallValve)
    {
        EditorGUILayout.Space(3);
        step.tightenThreshold = EditorGUILayout.Slider("Tighten Degrees", step.tightenThreshold, 10f, 360f);
    }
    
    if (step.type == InteractionStep.StepType.LoosenValve ||
        step.type == InteractionStep.StepType.RemoveValve)
    {
        EditorGUILayout.Space(3);  
        step.loosenThreshold = EditorGUILayout.Slider("Loosen Degrees", step.loosenThreshold, 10f, 360f);
    }
    
    if (step.type == InteractionStep.StepType.InstallValve ||
        step.type == InteractionStep.StepType.RemoveValve)
    {
        // Complete operations show both thresholds
        EditorGUILayout.Space(3);
        step.tightenThreshold = EditorGUILayout.Slider("Tighten Degrees", step.tightenThreshold, 10f, 360f);
        step.loosenThreshold = EditorGUILayout.Slider("Loosen Degrees", step.loosenThreshold, 10f, 360f);
    }
    
    // Common settings
    EditorGUILayout.Space(3);
    step.valveAngleTolerance = EditorGUILayout.Slider("Angle Tolerance", step.valveAngleTolerance, 1f, 15f);
    
    // Advanced settings (collapsible)
    EditorGUILayout.Space(5);
    showValveAdvanced = EditorGUILayout.Foldout(showValveAdvanced, "Advanced Settings");
    if (showValveAdvanced)
    {
        EditorGUILayout.BeginVertical("box");
        step.rotationDampening = EditorGUILayout.Slider("Rotation Dampening", step.rotationDampening, 0f, 10f);
        EditorGUILayout.HelpBox("Set to 0 to use profile default", MessageType.Info);
        EditorGUILayout.EndVertical();
    }
}
```

#### 3.4 Add Socket Selection Helper
**Add new method**:
```csharp
private void ShowSocketSelectionMenu(InteractionStep step)
{
    GenericMenu menu = new GenericMenu();
    
    // Find all socket objects in scene
    var socketObjects = FindObjectsOfType<GameObject>()
        .Where(go => go.GetComponent<XRSocketInteractor>() != null)
        .ToArray();
    
    if (socketObjects.Length == 0)
    {
        menu.AddDisabledItem(new GUIContent("No sockets found in scene"));
    }
    else
    {
        foreach (var socket in socketObjects)
        {
            menu.AddItem(new GUIContent(socket.name), false, () => {
                step.targetSocket.GameObject = socket;
            });
        }
    }
    
    menu.ShowAsContext();
}

private bool showValveAdvanced = false; // Add to class fields
```

### Testing Phase 3

#### Test Case 3.1: UI Menu Integration
**Objective**: Verify valve steps appear in add menu with correct organization

**Test Steps**:
1. Open sequence editor 
2. Right-click on task group to add step
3. Check menu structure and valve options

**Success Criteria**:
- "Valve Operations" submenu appears
- All 4 valve step types listed
- Clear naming (Tighten/Loosen/Install/Remove)
- Proper menu separation from other step types

#### Test Case 3.2: Valve Settings Panel Display
**Objective**: Confirm valve UI controls appear and function correctly

**Test Setup**:
1. Create `TightenValve` step in sequence editor
2. Select the step to show details panel

**Test Steps**:
1. Verify "Valve Settings" section appears
2. Test socket selection field and quick-select button
3. Test rotation axis toggle buttons (X/Y/Z)  
4. Test threshold sliders (range validation)
5. Test angle tolerance slider
6. Test advanced settings foldout

**Success Criteria**:
- All valve controls visible and functional
- Axis toggles work exclusively (only one active)
- Sliders constrained to valid ranges
- Socket quick-select shows available sockets
- Advanced settings collapsible

#### Test Case 3.3: Step Icon Display
**Objective**: Verify valve steps show correct icons in tree view

**Test Setup**:
1. Create sequence with all 4 valve step types
2. View in tree view panel

**Test Steps**:
1. Check each step type displays unique icon
2. Verify icons are distinct from existing step types

**Success Criteria**:
- TightenValve: 🔧 wrench icon
- LoosenValve: 🔓 unlock icon  
- InstallValve: ⚙️ gear icon
- RemoveValve: 🔨 hammer icon
- Icons display consistently in tree view

#### Test Case 3.4: Parameter Persistence
**Objective**: Ensure UI changes save correctly to sequence asset

**Test Steps**:
1. Create valve step with custom settings:
   - X-axis rotation
   - 180° tighten threshold
   - 10° tolerance
   - 2.0 dampening
2. Save sequence asset
3. Restart Unity and reload asset
4. Check settings preserved

**Success Criteria**:
- All parameter changes persist through save/reload
- Custom values display correctly in UI
- No values reset to defaults unexpectedly

---

## Phase 4: Parameter Override System & Runtime Application
**Duration**: 2-3 days  
**Objective**: Implement robust parameter override system and runtime application of sequence-level settings

### Implementation Tasks

#### 4.1 Create Valve Parameter Override System
**Add new class**: `Assets/VRTrainingKit/Scripts/ValveParameterOverride.cs`

```csharp
/// <summary>
/// Manages sequence-level parameter overrides for valve profiles
/// </summary>
public class ValveParameterOverride
{
    public static void ApplySequenceParameters(ValveController valveController, InteractionStep step)
    {
        if (valveController == null || step == null) return;
        
        // Create a runtime copy of the profile to avoid modifying the asset
        var originalProfile = GetValveProfile(valveController);
        if (originalProfile == null) return;
        
        var runtimeProfile = CreateRuntimeProfileCopy(originalProfile);
        
        // Apply sequence overrides to runtime copy
        ApplyOverrides(runtimeProfile, step);
        
        // Configure valve with modified profile
        valveController.Configure(runtimeProfile);
        
        LogParameterOverrides(valveController.gameObject.name, step);
    }
    
    private static ValveProfile CreateRuntimeProfileCopy(ValveProfile original)
    {
        var copy = ScriptableObject.CreateInstance<ValveProfile>();
        
        // Copy all fields from original profile
        copy.profileName = $"{original.profileName}_Runtime";
        copy.rotationAxis = original.rotationAxis;
        copy.tightenThreshold = original.tightenThreshold;
        copy.loosenThreshold = original.loosenThreshold;
        copy.angleTolerance = original.angleTolerance;
        copy.compatibleSocketTags = original.compatibleSocketTags;
        copy.rotationDampening = original.rotationDampening;
        // ... copy other necessary fields
        
        return copy;
    }
    
    private static void ApplyOverrides(ValveProfile profile, InteractionStep step)
    {
        // Apply non-default values as overrides
        if (step.rotationAxis != Vector3.up)
            profile.rotationAxis = step.rotationAxis;
            
        if (step.tightenThreshold != 90f)
            profile.tightenThreshold = step.tightenThreshold;
            
        if (step.loosenThreshold != 90f)
            profile.loosenThreshold = step.loosenThreshold;
            
        if (step.valveAngleTolerance != 5f)
            profile.angleTolerance = step.valveAngleTolerance;
            
        if (step.rotationDampening > 0f)
            profile.rotationDampening = step.rotationDampening;
    }
    
    private static void LogParameterOverrides(string objectName, InteractionStep step)
    {
        var overrides = new List<string>();
        
        if (step.rotationAxis != Vector3.up) 
            overrides.Add($"Axis: {step.rotationAxis}");
        if (step.tightenThreshold != 90f) 
            overrides.Add($"Tighten: {step.tightenThreshold}°");
        if (step.loosenThreshold != 90f) 
            overrides.Add($"Loosen: {step.loosenThreshold}°");
        if (step.valveAngleTolerance != 5f) 
            overrides.Add($"Tolerance: {step.valveAngleTolerance}°");
        if (step.rotationDampening > 0f) 
            overrides.Add($"Dampening: {step.rotationDampening}");
            
        if (overrides.Count > 0)
        {
            Debug.Log($"[ValveParameterOverride] Applied to {objectName}: {string.Join(", ", overrides)}");
        }
    }
}
```

#### 4.2 Update TrainingSequenceController Integration
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequenceController.cs`

**Replace existing `ApplyValveStepParameters()` with**:
```csharp
private void ApplyValveStepParameters(ValveController valveController, InteractionStep step)
{
    ValveParameterOverride.ApplySequenceParameters(valveController, step);
}
```

#### 4.3 Add Parameter Restoration System
**Add to TrainingSequenceController**:
```csharp
private Dictionary<ValveController, ValveProfile> originalValveProfiles = new Dictionary<ValveController, ValveProfile>();

private void StoreOriginalValveProfile(ValveController valveController)
{
    var originalProfile = GetValveProfile(valveController);
    if (originalProfile != null && !originalValveProfiles.ContainsKey(valveController))
    {
        originalValveProfiles[valveController] = originalProfile;
    }
}

private void RestoreOriginalValveProfile(ValveController valveController)
{
    if (originalValveProfiles.TryGetValue(valveController, out ValveProfile originalProfile))
    {
        valveController.Configure(originalProfile);
        LogDebug($"Restored original profile for {valveController.gameObject.name}");
    }
}

// Call in cleanup methods
protected override void OnSequenceCompleted()
{
    base.OnSequenceCompleted();
    
    // Restore all valve profiles
    foreach (var kvp in originalValveProfiles)
    {
        RestoreOriginalValveProfile(kvp.Key);
    }
    originalValveProfiles.Clear();
}
```

#### 4.4 Add Valve Step Validation
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequenceController.cs`

**Add after knob validation**:
```csharp
private void ValidateValveSteps()
{
    LogDebug("Validating valve steps...");
    
    foreach (var module in currentSequenceAsset.trainingProgram.modules)
    {
        foreach (var taskGroup in module.taskGroups)
        {
            foreach (var step in taskGroup.steps)
            {
                if (IsValveStep(step.type))
                {
                    ValidateValveStep(step);
                }
            }
        }
    }
}

private bool IsValveStep(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.TightenValve ||
           stepType == InteractionStep.StepType.LoosenValve ||
           stepType == InteractionStep.StepType.InstallValve ||
           stepType == InteractionStep.StepType.RemoveValve;
}

private void ValidateValveStep(InteractionStep step)
{
    GameObject valveObject = step.targetObject.GameObject;
    GameObject socketObject = step.targetSocket.GameObject;
    
    if (valveObject == null)
    {
        LogError($"Valve step '{step.stepName}': valve object reference is null");
        return;
    }
    
    if (socketObject == null)
    {
        LogError($"Valve step '{step.stepName}': socket object reference is null");
        return;
    }
    
    // Check valve has ValveController
    ValveController valveController = valveObject.GetComponent<ValveController>();
    if (valveController == null)
    {
        LogError($"Valve step '{step.stepName}': {valveObject.name} missing ValveController component");
        return;
    }
    
    // Check socket has XRSocketInteractor
    var socketInteractor = socketObject.GetComponent<XRSocketInteractor>();
    if (socketInteractor == null)
    {
        LogError($"Valve step '{step.stepName}': {socketObject.name} missing XRSocketInteractor component");
        return;
    }
    
    // Check valve-socket compatibility
    if (!valveController.IsSocketCompatible(socketObject))
    {
        LogWarning($"Valve step '{step.stepName}': {valveObject.name} may not be compatible with socket {socketObject.name}");
    }
    
    // Validate parameter ranges
    if (step.tightenThreshold < 10f || step.tightenThreshold > 360f)
        LogWarning($"Valve step '{step.stepName}': tightenThreshold {step.tightenThreshold}° outside recommended range 10-360°");
        
    if (step.loosenThreshold < 10f || step.loosenThreshold > 360f)
        LogWarning($"Valve step '{step.stepName}': loosenThreshold {step.loosenThreshold}° outside recommended range 10-360°");
        
    if (step.valveAngleTolerance < 1f || step.valveAngleTolerance > 45f)
        LogWarning($"Valve step '{step.stepName}': valveAngleTolerance {step.valveAngleTolerance}° outside recommended range 1-45°");
    
    LogDebug($"Validated valve step '{step.stepName}': target={step.tightenThreshold}°, tolerance=±{step.valveAngleTolerance}°");
}
```

### Testing Phase 4

#### Test Case 4.1: Parameter Override Application
**Objective**: Verify sequence parameters correctly override profile defaults without modifying assets

**Test Setup**:
1. ValveProfile asset with default values (90° thresholds, Y-axis rotation)
2. Sequence step with overrides (180° tighten, X-axis rotation)
3. Multiple valve objects using same profile

**Test Steps**:
1. Start sequence with valve step
2. Check valve controller receives custom parameters
3. Verify original profile asset unchanged
4. Test with second valve (should use profile defaults)

**Success Criteria**:
- Target valve gets sequence parameters (180°, X-axis)
- Original profile asset unchanged on disk
- Other valves with same profile unaffected
- Console shows override log messages

#### Test Case 4.2: Profile Restoration After Sequence
**Objective**: Ensure original profiles restored when sequence completes

**Test Setup**:
1. Valve with known profile configuration
2. Sequence that applies overrides
3. Complete sequence or cancel mid-execution

**Test Steps**:
1. Note original profile settings
2. Start sequence (applies overrides)
3. Complete or cancel sequence
4. Check valve reverted to original settings

**Success Criteria**:
- Original profile settings restored after completion
- Restoration works on sequence cancel/failure
- Multiple valves all restored correctly
- Console shows "Restored original profile" messages

#### Test Case 4.3: Valve Step Validation
**Objective**: Confirm validation catches common configuration errors

**Test Scenarios**:
1. **Missing Components**: Valve without ValveController, socket without XRSocketInteractor
2. **Invalid Parameters**: Extreme threshold values (5°, 400°), invalid tolerance
3. **Incompatible Sockets**: Valve with restricted socket compatibility
4. **Null References**: Missing valve or socket GameObject references

**Test Steps**:
1. Create sequence with each error scenario
2. Run validation
3. Check console for appropriate error/warning messages

**Success Criteria**:
- Missing components detected with clear error messages
- Parameter range violations flagged as warnings
- Socket compatibility issues noted
- Null references reported as errors
- Validation prevents sequence start with critical errors

#### Test Case 4.4: Runtime Parameter Handling
**Objective**: Test parameter changes during sequence execution

**Test Setup**:
1. Sequence with multiple valve steps using different parameters
2. Steps have varying thresholds and axes

**Test Steps**:
1. Execute sequence step-by-step
2. Monitor parameter application for each step
3. Verify correct behavior with different settings

**Success Criteria**:
- Each step applies its specific parameters
- Parameters change correctly between steps
- No parameter bleed-through between objects
- Valve behavior matches configured parameters

---

## Phase 5: Validation, Testing, and Polish
**Duration**: 2-3 days  
**Objective**: Comprehensive testing, error handling, and user experience polish

### Implementation Tasks

#### 5.1 Enhanced Validation System
**File**: `Assets/VRTrainingKit/Scripts/TrainingSequenceAssetValidation.cs` (new file)

```csharp
/// <summary>
/// Comprehensive validation for valve integration
/// </summary>
public static class ValveSequenceValidation
{
    public static ValidationResult ValidateValveIntegration(TrainingSequenceAsset asset)
    {
        var result = new ValidationResult();
        
        ValidateValveStepConfiguration(asset, result);
        ValidateValveSocketCompatibility(asset, result);
        ValidateSequenceFlow(asset, result);
        ValidatePerformanceImplications(asset, result);
        
        return result;
    }
    
    private static void ValidateValveStepConfiguration(TrainingSequenceAsset asset, ValidationResult result)
    {
        foreach (var step in asset.GetAllSteps().Where(s => IsValveStep(s.type)))
        {
            // Validate required references
            if (step.targetObject?.GameObject == null)
                result.errors.Add($"Valve step '{step.stepName}': Missing valve object reference");
                
            if (step.targetSocket?.GameObject == null)
                result.errors.Add($"Valve step '{step.stepName}': Missing socket object reference");
            
            // Validate parameter consistency
            if (step.type == InteractionStep.StepType.InstallValve || 
                step.type == InteractionStep.StepType.RemoveValve)
            {
                if (step.tightenThreshold <= 0 || step.loosenThreshold <= 0)
                    result.errors.Add($"Complete valve step '{step.stepName}': Both tighten and loosen thresholds required");
            }
            
            // Validate logical parameter values
            if (step.valveAngleTolerance >= step.tightenThreshold * 0.5f)
                result.warnings.Add($"Valve step '{step.stepName}': Angle tolerance too large relative to threshold");
            
            // Check rotation axis validity
            if (step.rotationAxis.magnitude < 0.9f || step.rotationAxis.magnitude > 1.1f)
                result.warnings.Add($"Valve step '{step.stepName}': Rotation axis should be normalized unit vector");
        }
    }
    
    private static void ValidateSequenceFlow(TrainingSequenceAsset asset, ValidationResult result)
    {
        // Check for logical step ordering
        foreach (var taskGroup in asset.GetAllTaskGroups())
        {
            var valveSteps = taskGroup.steps.Where(s => IsValveStep(s.type)).ToList();
            
            for (int i = 0; i < valveSteps.Count - 1; i++)
            {
                var currentStep = valveSteps[i];
                var nextStep = valveSteps[i + 1];
                
                // Check for conflicting operations on same valve
                if (currentStep.targetObject?.GameObject == nextStep.targetObject?.GameObject)
                {
                    if (IsConflictingSequence(currentStep.type, nextStep.type))
                    {
                        result.warnings.Add($"Potentially conflicting valve operations: '{currentStep.stepName}' followed by '{nextStep.stepName}' on same valve");
                    }
                }
            }
        }
    }
    
    private static bool IsConflictingSequence(InteractionStep.StepType first, InteractionStep.StepType second)
    {
        // Define conflicting step patterns
        var conflicts = new Dictionary<InteractionStep.StepType, InteractionStep.StepType[]>
        {
            { InteractionStep.StepType.TightenValve, new[] { InteractionStep.StepType.TightenValve } },
            { InteractionStep.StepType.LoosenValve, new[] { InteractionStep.StepType.LoosenValve } },
            { InteractionStep.StepType.InstallValve, new[] { InteractionStep.StepType.InstallValve, InteractionStep.StepType.TightenValve } },
            { InteractionStep.StepType.RemoveValve, new[] { InteractionStep.StepType.RemoveValve, InteractionStep.StepType.LoosenValve } }
        };
        
        return conflicts.ContainsKey(first) && conflicts[first].Contains(second);
    }
}
```

#### 5.2 Error Recovery and User Guidance
**File**: `Assets/VRTrainingKit/Scripts/ValveSequenceErrorHandler.cs` (new file)

```csharp
/// <summary>
/// Handles errors and provides user guidance for valve operations
/// </summary>
public class ValveSequenceErrorHandler : MonoBehaviour
{
    [Header("Error Display")]
    public Canvas errorCanvas;
    public Text errorMessageText;
    public Button retryButton;
    public Button skipButton;
    
    private TrainingSequenceController sequenceController;
    
    private void Start()
    {
        sequenceController = FindObjectOfType<TrainingSequenceController>();
        
        retryButton.onClick.AddListener(RetryCurrentStep);
        skipButton.onClick.AddListener(SkipCurrentStep);
        
        HideErrorDisplay();
    }
    
    public void HandleValveOperationError(InteractionStep step, string errorMessage)
    {
        ShowErrorDisplay($"Valve Operation Error in step '{step.stepName}':\n{errorMessage}");
        
        // Log detailed error information
        Debug.LogError($"[ValveSequenceErrorHandler] Step: {step.stepName}, Error: {errorMessage}");
        
        // Provide contextual guidance
        string guidance = GetUserGuidance(step, errorMessage);
        if (!string.IsNullOrEmpty(guidance))
        {
            ShowErrorDisplay($"{errorMessage}\n\nGuidance: {guidance}");
        }
    }
    
    private string GetUserGuidance(InteractionStep step, string error)
    {
        // Provide specific guidance based on common error patterns
        if (error.Contains("not found") || error.Contains("null"))
        {
            return "Check that the valve and socket objects are properly configured in the scene.";
        }
        
        if (error.Contains("incompatible"))
        {
            return "Verify the valve profile allows connection to the specified socket.";
        }
        
        if (error.Contains("angle") || error.Contains("rotation"))
        {
            return $"Try rotating the valve slowly. Required: {step.tightenThreshold}° ± {step.valveAngleTolerance}°";
        }
        
        return "Review the step configuration and try again.";
    }
    
    private void ShowErrorDisplay(string message)
    {
        errorMessageText.text = message;
        errorCanvas.gameObject.SetActive(true);
    }
    
    private void HideErrorDisplay()
    {
        errorCanvas.gameObject.SetActive(false);
    }
    
    private void RetryCurrentStep()
    {
        HideErrorDisplay();
        sequenceController?.RetryCurrentStep();
    }
    
    private void SkipCurrentStep()
    {
        HideErrorDisplay();
        sequenceController?.SkipCurrentStep();
    }
}
```

#### 5.3 Performance Optimization
**Add to TrainingSequenceController**:

```csharp
// Optimize valve event subscriptions
private readonly Dictionary<ValveController, HashSet<System.Action>> valveEventSubscriptions = new Dictionary<ValveController, HashSet<System.Action>>();

private void OptimizeValveEventSubscription(ValveController valveController, InteractionStep step)
{
    // Prevent duplicate event subscriptions
    if (!valveEventSubscriptions.ContainsKey(valveController))
    {
        valveEventSubscriptions[valveController] = new HashSet<System.Action>();
    }
    
    // Only subscribe to events we actually need for this step type
    switch (step.type)
    {
        case InteractionStep.StepType.TightenValve:
        case InteractionStep.StepType.InstallValve:
            SubscribeToTightenEvent(valveController, step);
            break;
            
        case InteractionStep.StepType.LoosenValve:
        case InteractionStep.StepType.RemoveValve:
            SubscribeToLoosenEvent(valveController, step);
            break;
    }
}

private void CleanupValveEventSubscriptions()
{
    foreach (var kvp in valveEventSubscriptions)
    {
        var valveController = kvp.Key;
        if (valveController != null)
        {
            // Unsubscribe from all events
            foreach (var eventDelegate in kvp.Value)
            {
                // Clean up specific event subscriptions
            }
        }
    }
    
    valveEventSubscriptions.Clear();
}
```

#### 5.4 Documentation and Examples
**File**: `Assets/VRTrainingKit/ValveIntegration_Examples.md` (new file)

```markdown
# Valve Integration Examples

## Common Valve Sequence Patterns

### Pattern 1: Basic Valve Installation
```
TaskGroup: "Install Main Valve"
├── TightenValve
    ├── Target: MainValve
    ├── Socket: PrimarySocket
    ├── Tighten Threshold: 90°
    └── Tolerance: 5°
```

### Pattern 2: Complete Valve Service
```
TaskGroup: "Valve Service Procedure"
├── LoosenValve (remove old valve)
├── RemoveValve (complete removal)
├── InstallValve (install new valve)
└── TightenValve (final tightening)
```

### Pattern 3: Multi-Valve System
```
TaskGroup: "Primary Circuit Setup"
├── TightenValve (Inlet Valve - 180°)
├── TightenValve (Outlet Valve - 90°) [parallel]
└── TightenValve (Pressure Valve - 45°)
```

## Best Practices

1. **Use appropriate step types**: TightenValve for single operations, InstallValve for complete workflows
2. **Set realistic thresholds**: Match physical valve requirements
3. **Allow sufficient tolerance**: 5-10° for most applications
4. **Consider parallel operations**: Independent valves can be operated simultaneously
5. **Validate socket compatibility**: Ensure valve profiles match socket tags
```

### Testing Phase 5

#### Test Case 5.1: Comprehensive Validation
**Objective**: Verify complete validation system catches all error categories

**Test Scenarios**:
1. **Configuration Errors**: Missing references, invalid parameters
2. **Logic Errors**: Conflicting step sequences, impossible operations
3. **Performance Issues**: Too many valve controllers, excessive event subscriptions
4. **Edge Cases**: Extreme parameter values, unusual configurations

**Test Steps**:
1. Create sequence with each error type
2. Run validation system
3. Verify appropriate error/warning categories
4. Check error messages are actionable

**Success Criteria**:
- All error categories detected reliably
- Clear, actionable error messages
- Appropriate severity levels (error vs warning)
- Validation completes without false positives

#### Test Case 5.2: Error Recovery System
**Objective**: Test error handling and user guidance system

**Test Setup**:
1. Sequence with valve step that will fail
2. Error handler UI configured
3. Multiple error scenarios

**Test Steps**:
1. Trigger various valve operation errors
2. Verify error display and guidance
3. Test retry and skip functionality
4. Check error logging and reporting

**Success Criteria**:
- Errors display user-friendly messages
- Contextual guidance helps resolve issues
- Retry/skip buttons function correctly
- Error recovery doesn't break sequence state

#### Test Case 5.3: Performance Under Load
**Objective**: Verify system handles multiple valves efficiently

**Test Setup**:
1. Scene with 10+ valve objects
2. Complex sequence with many valve steps
3. Multiple parallel valve operations

**Test Steps**:
1. Monitor frame rate during sequence execution
2. Check memory allocation patterns
3. Measure event subscription overhead
4. Test sequence completion time

**Success Criteria**:
- Maintains target frame rate (60fps+)
- No significant memory leaks
- Event subscription cleanup works
- Sequence execution time reasonable

#### Test Case 5.4: End-to-End User Workflow
**Objective**: Complete user experience validation

**Test Workflow**:
1. Create new training sequence from scratch
2. Add valve steps with custom parameters
3. Configure valve objects in scene
4. Test sequence execution completely
5. Modify and re-test sequence

**Success Criteria**:
- Complete workflow is intuitive and clear
- No blocking UI issues or confusing states
- Valve operations feel natural and responsive
- Documentation and examples are helpful
- System integrates seamlessly with existing patterns

---

## Success Criteria Summary

### Phase 1: Data Model Extension ✅
- New valve step types available in sequence system
- Valve parameters integrated with proper validation
- Serialization and persistence working correctly

### Phase 2: Controller Integration ✅
- ValveController events integrated with sequence controller
- Parameter override system functional
- Step completion detection reliable

### Phase 3: UI Integration ✅
- Valve controls accessible in sequence editor
- UI follows established patterns and conventions
- Parameter input validation and user guidance

### Phase 4: Runtime Systems ✅
- Profile override system preserves asset integrity
- Runtime parameter application works correctly
- Comprehensive validation prevents configuration errors

### Phase 5: Production Ready ✅
- Error handling and user guidance systems
- Performance optimization for multiple valves
- Complete documentation and examples
- End-to-end user workflow validation

## Post-Implementation Maintenance

### Version Compatibility
- Maintain backward compatibility with existing sequences
- Provide migration tools if data format changes
- Document version-specific features and limitations

### Future Enhancements
- Additional valve step types as needed
- Enhanced visual feedback systems
- Integration with other complex interaction profiles
- Advanced sequence flow control features

This implementation plan provides a structured approach to integrating the sophisticated valve interaction profile with the training sequence system while maintaining the quality and usability standards of the existing framework.
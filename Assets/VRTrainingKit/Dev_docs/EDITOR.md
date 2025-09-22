# VR Training Kit - Unity Editor Integration

## Overview

The Unity Editor Integration provides comprehensive tools for creating, configuring, and managing VR training scenarios within the Unity Editor. It includes custom windows, property drawers, asset creation workflows, and visual editing interfaces.

## Editor Window System

### VRInteractionSetupWindow

The central hub for VR Training Kit configuration, accessible via `Window > VR Training > Setup Assistant`.

```csharp
[MenuItem("VR Training/Setup Assistant")]
public static void ShowWindow()
{
    var window = GetWindow<VRInteractionSetupWindow>("VR Training Setup");
    window.minSize = new Vector2(400, 500);
}

public class VRInteractionSetupWindow : EditorWindow
{
    private enum Tab
    {
        Setup,      // Scene object configuration
        Configure,  // Profile management
        Sequence,   // Training sequence creation
        Validate    // Validation and troubleshooting
    }
}
```

### Multi-Tab Interface Architecture

#### Tab 1: Setup - Scene Configuration

**Purpose**: Configure VR interactions for scene objects
**Features**:
- Automatic scene scanning for tagged objects
- Real-time object categorization
- Bulk component application
- Interactive layer management

```csharp
private void DrawSetupTab()
{
    EditorGUILayout.LabelField("Scene Setup", headerStyle);

    // Scene scanning
    if (GUILayout.Button("Scan Scene for Objects"))
    {
        sceneAnalysis = InteractionSetupService.ScanScene();
    }

    if (sceneAnalysis != null)
    {
        // Object categories with counts
        DrawObjectCategory("Grab Objects", sceneAnalysis.grabObjects, selectedGrabProfile);
        DrawObjectCategory("Knob Objects", sceneAnalysis.knobObjects, selectedKnobProfile);
        DrawObjectCategory("Snap Points", sceneAnalysis.snapObjects, selectedSnapProfile);
        DrawObjectCategory("Tool Objects", sceneAnalysis.toolObjects, selectedToolProfile);
        DrawObjectCategory("Valve Objects", sceneAnalysis.valveObjects, selectedValveProfile);

        // Bulk operations
        if (GUILayout.Button("Apply All Components"))
        {
            ApplyComponentsToAllObjects();
        }
    }
}
```

**Object Category Display**:
```csharp
private void DrawObjectCategory(string categoryName, List<GameObject> objects, InteractionProfile profile)
{
    EditorGUILayout.BeginVertical("box");
    EditorGUILayout.LabelField($"{categoryName} ({objects.Count})", subHeaderStyle);

    // Status indicators
    foreach (var obj in objects.Take(10)) // Limit display for performance
    {
        EditorGUILayout.BeginHorizontal();

        // Object name with clickable selection
        if (GUILayout.Button(obj.name, EditorStyles.linkLabel))
        {
            Selection.activeGameObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        // Configuration status
        bool isConfigured = IsObjectConfigured(obj, profile?.GetType());
        EditorGUILayout.LabelField(isConfigured ? "✓" : "○", GUILayout.Width(20));

        // Interaction layer dropdown
        if (isConfigured)
        {
            LayerMask currentLayer = InteractionLayerManager.GetInteractionLayer(obj);
            LayerMask newLayer = InteractionLayerManager.DrawLayerMaskDropdown(currentLayer, GUILayout.Width(120));

            if (newLayer != currentLayer)
            {
                InteractionLayerManager.SetInteractionLayer(obj, newLayer);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    // Bulk actions
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button($"Select All {categoryName}"))
    {
        Selection.objects = objects.ToArray();
    }

    if (profile != null && GUILayout.Button($"Apply {profile.name}"))
    {
        InteractionSetupService.ApplyComponentsToObjects(objects, profile);
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.EndVertical();
}
```

#### Tab 2: Configure - Profile Management

**Purpose**: Manage ScriptableObject profiles for different interaction types
**Features**:
- Profile asset selection and creation
- Real-time profile discovery
- Profile editing shortcuts

```csharp
private void DrawConfigureTab()
{
    EditorGUILayout.LabelField("Profile Configuration", headerStyle);

    configScrollPos = EditorGUILayout.BeginScrollView(configScrollPos);

    // Profile sections for each type
    DrawProfileSection<GrabProfile>("Grab Profile", ref selectedGrabProfile);
    DrawProfileSection<KnobProfile>("Knob Profile", ref selectedKnobProfile);
    DrawProfileSection<SnapProfile>("Snap Profile", ref selectedSnapProfile);
    DrawProfileSection<ToolProfile>("Tool Profile", ref selectedToolProfile);
    DrawProfileSection<ValveProfile>("Valve Profile", ref selectedValveProfile);

    EditorGUILayout.Space();

    // Bulk operations
    if (GUILayout.Button("Create All Default Profiles"))
    {
        CreateAllDefaultProfiles();
    }

    EditorGUILayout.EndScrollView();
}

private void DrawProfileSection<T>(string sectionName, ref T selectedProfile) where T : InteractionProfile
{
    EditorGUILayout.BeginVertical("box");
    EditorGUILayout.LabelField(sectionName, subHeaderStyle);

    // Profile selection
    selectedProfile = (T)EditorGUILayout.ObjectField("Profile Asset", selectedProfile, typeof(T), false);

    if (selectedProfile == null)
    {
        // Auto-discovery of available profiles
        string[] profileGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (profileGuids.Length > 0)
        {
            EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
            foreach (string guid in profileGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T profile = AssetDatabase.LoadAssetAtPath<T>(path);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  • {profile.name}", EditorStyles.miniLabel);
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedProfile = profile;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // Profile creation
        if (GUILayout.Button($"Create New {typeof(T).Name}"))
        {
            CreateNewProfile<T>($"{typeof(T).Name}");
        }
    }
    else
    {
        // Profile editing
        if (GUILayout.Button("Edit Profile"))
        {
            Selection.activeObject = selectedProfile;
        }
    }

    EditorGUILayout.EndVertical();
}
```

#### Tab 3: Sequence - Training Sequence Editor

**Purpose**: Create and edit hierarchical training sequences
**Features**:
- Two-panel interface (40% tree view, 60% properties)
- Hierarchical data visualization
- Real-time validation feedback
- Asset management integration

```csharp
private void DrawSequenceTab()
{
    EditorGUILayout.LabelField("Training Sequence Editor", headerStyle);

    // Asset selection
    DrawTrainingAssetSelection();

    if (currentTrainingAsset != null && currentProgram != null)
    {
        EditorGUILayout.Space();
        DrawTwoPanelEditor();
    }
}

private void DrawTwoPanelEditor()
{
    EditorGUILayout.BeginHorizontal();

    // LEFT PANEL: Tree View (40%)
    float treeViewWidth = position.width * splitterPosition;
    EditorGUILayout.BeginVertical(GUILayout.Width(treeViewWidth));
    DrawTreeView();
    EditorGUILayout.EndVertical();

    // SPLITTER
    DrawSplitter();

    // RIGHT PANEL: Properties (60%)
    EditorGUILayout.BeginVertical();
    DrawPropertiesPanel();
    EditorGUILayout.EndVertical();

    EditorGUILayout.EndHorizontal();
}

private void DrawTreeView()
{
    EditorGUILayout.LabelField("Training Structure", EditorStyles.boldLabel);

    treeViewScrollPos = EditorGUILayout.BeginScrollView(treeViewScrollPos);

    // Program level
    DrawProgramNode(currentProgram);

    // Module level
    for (int moduleIndex = 0; moduleIndex < currentProgram.modules.Count; moduleIndex++)
    {
        var module = currentProgram.modules[moduleIndex];
        DrawModuleNode(module, moduleIndex);
    }

    EditorGUILayout.EndScrollView();
}

private void DrawModuleNode(TrainingModule module, int moduleIndex)
{
    EditorGUILayout.BeginHorizontal();

    // Expansion toggle
    module.isExpanded = EditorGUILayout.Foldout(module.isExpanded,
        $"📚 {module.moduleName} ({module.taskGroups.Count} groups)");

    // Module actions
    if (GUILayout.Button("➕", GUILayout.Width(25)))
    {
        ShowModuleAddMenu(moduleIndex);
    }
    if (GUILayout.Button("❌", GUILayout.Width(25)))
    {
        if (EditorUtility.DisplayDialog("Delete Module",
            $"Are you sure you want to delete module '{module.moduleName}'?", "Yes", "No"))
        {
            currentProgram.modules.RemoveAt(moduleIndex);
            MarkAssetDirty();
        }
    }

    EditorGUILayout.EndHorizontal();

    // Selection highlighting
    if (selectedHierarchyItem == module)
    {
        EditorGUILayout.BeginVertical("selectionRect");
        EditorGUILayout.LabelField($"Selected: {module.moduleName}");
        EditorGUILayout.EndVertical();
    }

    // Handle selection
    if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
    {
        SelectHierarchyItem(module, "module");
    }

    // Task groups (if expanded)
    if (module.isExpanded)
    {
        EditorGUI.indentLevel++;
        for (int groupIndex = 0; groupIndex < module.taskGroups.Count; groupIndex++)
        {
            DrawTaskGroupNode(module.taskGroups[groupIndex], moduleIndex, groupIndex);
        }
        EditorGUI.indentLevel--;
    }
}
```

#### Tab 4: Validate - Validation and Troubleshooting

**Purpose**: Comprehensive validation and issue detection
**Features**:
- Scene validation
- Asset validation
- Missing component detection
- Error categorization (errors vs warnings)

```csharp
private void DrawValidateTab()
{
    EditorGUILayout.LabelField("Validation & Troubleshooting", headerStyle);

    // Validation buttons
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Validate Scene Setup"))
    {
        ValidateSceneSetup();
    }
    if (GUILayout.Button("Validate Training Assets"))
    {
        ValidateTrainingAssets();
    }
    EditorGUILayout.EndHorizontal();

    // Validation results
    if (validationIssues.Count > 0)
    {
        validateScrollPos = EditorGUILayout.BeginScrollView(validateScrollPos);

        foreach (var issue in validationIssues)
        {
            GUIStyle style = issue.StartsWith("ERROR") ? errorStyle : warningStyle;
            EditorGUILayout.LabelField(issue, style);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Clear Results"))
        {
            validationIssues.Clear();
        }
    }
}

private void ValidateSceneSetup()
{
    validationIssues.Clear();

    // Check for required components
    var grabObjects = GameObject.FindGameObjectsWithTag("grab");
    foreach (var obj in grabObjects)
    {
        if (obj.GetComponent<XRGrabInteractable>() == null)
        {
            validationIssues.Add($"ERROR: {obj.name} tagged 'grab' but missing XRGrabInteractable");
        }
        if (obj.GetComponent<Rigidbody>() == null)
        {
            validationIssues.Add($"WARNING: {obj.name} missing Rigidbody (recommended for grab objects)");
        }
    }

    // Check knob objects
    var knobObjects = GameObject.FindGameObjectsWithTag("knob");
    foreach (var obj in knobObjects)
    {
        if (obj.GetComponent<KnobController>() == null)
        {
            validationIssues.Add($"ERROR: {obj.name} tagged 'knob' but missing KnobController");
        }
        if (obj.GetComponent<HingeJoint>() == null)
        {
            validationIssues.Add($"ERROR: {obj.name} missing HingeJoint (required for knobs)");
        }
    }

    // ... additional validation logic
}
```

## Custom Property Drawers

### GameObjectReferenceDrawer

Provides seamless Unity Inspector integration for `GameObjectReference`:

```csharp
[CustomPropertyDrawer(typeof(GameObjectReference))]
public class GameObjectReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get serialized fields
        var gameObjectProp = property.FindPropertyRelative("_gameObject");
        var gameObjectNameProp = property.FindPropertyRelative("_gameObjectName");
        var scenePathProp = property.FindPropertyRelative("_scenePath");
        var isValidProp = property.FindPropertyRelative("_isValid");

        // Current reference resolution
        GameObject currentGameObject = gameObjectProp.objectReferenceValue as GameObject;

        // Fallback name resolution for editor display
        if (currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue))
        {
            currentGameObject = GameObject.Find(gameObjectNameProp.stringValue);
        }

        // Visual feedback calculation
        bool showWarning = currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue);
        float fieldWidth = showWarning ? position.width - 25 : position.width;
        Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);

        // Standard GameObject field with type filtering
        EditorGUI.BeginChangeCheck();
        GameObject newGameObject = (GameObject)EditorGUI.ObjectField(
            fieldRect, label, currentGameObject, typeof(GameObject), true);

        // Update all internal fields on change
        if (EditorGUI.EndChangeCheck())
        {
            gameObjectProp.objectReferenceValue = newGameObject;
            gameObjectNameProp.stringValue = newGameObject?.name ?? "";
            scenePathProp.stringValue = newGameObject?.scene.path ?? "";
            isValidProp.boolValue = newGameObject != null;

            // Force serialization update
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        // Warning icon for broken references
        if (showWarning)
        {
            Rect warningRect = new Rect(position.x + position.width - 20, position.y, 20, position.height);
            string tooltipText = currentGameObject != null ?
                $"Reference found by name: {gameObjectNameProp.stringValue}" :
                $"Missing GameObject: {gameObjectNameProp.stringValue}";
            EditorGUI.LabelField(warningRect, new GUIContent("⚠", tooltipText));
        }

        EditorGUI.EndProperty();
    }
}
```

### Key Features of Property Drawer

1. **Seamless Integration**: Appears as standard GameObject field in Inspector
2. **Visual Validation**: Warning icons for broken references
3. **Automatic Resolution**: Attempts to find objects by name when direct reference fails
4. **Tooltip Feedback**: Detailed information about reference status
5. **Real-time Updates**: Immediate serialization and dirty marking

## Asset Creation Workflows

### ScriptableObject Creation

All profiles use Unity's `[CreateAssetMenu]` attribute for easy asset creation:

```csharp
// Profile creation menu items
[CreateAssetMenu(fileName = "GrabProfile", menuName = "VR Training/Grab Profile")]
public class GrabProfile : InteractionProfile { /* ... */ }

[CreateAssetMenu(fileName = "KnobProfile", menuName = "VR Training/Knob Profile")]
public class KnobProfile : InteractionProfile { /* ... */ }

[CreateAssetMenu(fileName = "ValveProfile", menuName = "VR Training/Valve Profile")]
public class ValveProfile : InteractionProfile { /* ... */ }

[CreateAssetMenu(fileName = "TrainingSequence", menuName = "VR Training/Training Sequence Asset")]
public class TrainingSequenceAsset : ScriptableObject { /* ... */ }
```

### Programmatic Asset Creation

```csharp
private void CreateNewProfile<T>(string fileName) where T : ScriptableObject
{
    T newProfile = ScriptableObject.CreateInstance<T>();

    string path = EditorUtility.SaveFilePanelInProject(
        $"Save {typeof(T).Name}",
        fileName,
        "asset",
        $"Create a new {typeof(T).Name}");

    if (!string.IsNullOrEmpty(path))
    {
        AssetDatabase.CreateAsset(newProfile, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = newProfile;
        EditorUtility.FocusProjectWindow();
    }
}

private void CreateAllDefaultProfiles()
{
    string resourcesPath = "Assets/VRTrainingKit/Resources";

    // Ensure Resources folder exists
    if (!AssetDatabase.IsValidFolder(resourcesPath))
    {
        AssetDatabase.CreateFolder("Assets/VRTrainingKit", "Resources");
    }

    // Create default profiles
    CreateDefaultProfile<GrabProfile>(resourcesPath + "/GrabProfile.asset");
    CreateDefaultProfile<KnobProfile>(resourcesPath + "/KnobProfile.asset");
    CreateDefaultProfile<SnapProfile>(resourcesPath + "/SnapProfile.asset");
    CreateDefaultProfile<ToolProfile>(resourcesPath + "/ToolProfile.asset");
    CreateDefaultProfile<ValveProfile>(resourcesPath + "/ValveProfile.asset");

    AssetDatabase.Refresh();
}
```

## Visual Styling System

### GUIStyle Initialization

```csharp
private void InitializeStyles()
{
    headerStyle = new GUIStyle(EditorStyles.boldLabel)
    {
        fontSize = 16,
        normal = { textColor = Color.white }
    };

    subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
    {
        fontSize = 12,
        normal = { textColor = Color.gray }
    };

    successStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = Color.green }
    };

    warningStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = Color.yellow }
    };

    errorStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = Color.red }
    };
}
```

### Icon System

The editor uses emoji icons for visual identification:

```csharp
// Hierarchy icons
"📋" // Program
"📚" // Module
"📁" // Task Group
"✋" // Grab Step
"🔗" // GrabAndSnap Step
"🔄" // TurnKnob Step
"🔧" // Valve Steps
"⏳" // WaitForCondition Step
"💬" // ShowInstruction Step

// Status icons
"✓" // Configured/Valid
"○" // Pending configuration
"⚠" // Warning/Issues
"❌" // Error/Delete action
"➕" // Add action
```

## Play Mode Integration

### State Persistence

The editor window handles play mode transitions gracefully:

```csharp
private void OnEnable()
{
    // Subscribe to play mode state changes
    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    // Restore previous state
    if (EditorPrefs.HasKey("VRTrainingKit_LastSceneAnalysisValid"))
    {
        bool wasValid = EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid");
        if (wasValid)
        {
            sceneAnalysis = InteractionSetupService.ScanScene();
        }
    }
}

private void OnPlayModeStateChanged(PlayModeStateChange state)
{
    switch (state)
    {
        case PlayModeStateChange.EnteredPlayMode:
            // Refresh analysis for runtime state
            if (sceneAnalysis != null)
            {
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
            break;

        case PlayModeStateChange.EnteredEditMode:
            // Restore editor state
            RefreshTrainingAssetReferences();
            if (EditorPrefs.GetBool("VRTrainingKit_LastSceneAnalysisValid"))
            {
                sceneAnalysis = InteractionSetupService.ScanScene();
            }
            break;
    }
}
```

### Asset Reference Refreshing

Critical for maintaining data integrity across play mode transitions:

```csharp
private void RefreshTrainingAssetReferences()
{
    if (currentTrainingAsset != null)
    {
        string currentAssetName = currentTrainingAsset.name;
        LoadAvailableTrainingAssets();

        // Restore previously selected asset
        for (int i = 0; i < availableAssets.Length; i++)
        {
            if (availableAssets[i].name == currentAssetName)
            {
                selectedAssetIndex = i;
                currentTrainingAsset = availableAssets[i];
                currentProgram = currentTrainingAsset.Program;
                break;
            }
        }
    }
}
```

## Editor Performance Optimization

### Lazy Loading

```csharp
// Load assets only when needed
private void LoadAvailableTrainingAssets()
{
    if (!assetsLoaded)
    {
        string[] assetGuids = AssetDatabase.FindAssets("t:TrainingSequenceAsset");
        availableAssets = new TrainingSequenceAsset[assetGuids.Length];

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            availableAssets[i] = AssetDatabase.LoadAssetAtPath<TrainingSequenceAsset>(path);
        }

        assetsLoaded = true;
    }
}
```

### Efficient Rendering

```csharp
// Limit object display for performance
private void DrawObjectCategory(string categoryName, List<GameObject> objects, InteractionProfile profile)
{
    // Show first 10 objects directly
    foreach (var obj in objects.Take(10))
    {
        DrawObjectLine(obj, profile);
    }

    // Show count for remaining objects
    if (objects.Count > 10)
    {
        EditorGUILayout.LabelField($"... and {objects.Count - 10} more objects", EditorStyles.miniLabel);
    }
}
```

### Smart Dirty Marking

```csharp
private void MarkAssetDirty()
{
    if (currentTrainingAsset != null)
    {
        EditorUtility.SetDirty(currentTrainingAsset);
        AssetDatabase.SaveAssets();
    }
}

// Only mark dirty when actually changed
if (EditorGUI.EndChangeCheck())
{
    MarkAssetDirty();
}
```

## Integration with Unity Systems

### Asset Database Integration

```csharp
// Asset discovery
string[] profileGuids = AssetDatabase.FindAssets("t:GrabProfile");
foreach (string guid in profileGuids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    GrabProfile profile = AssetDatabase.LoadAssetAtPath<GrabProfile>(path);
}

// Asset creation
AssetDatabase.CreateAsset(newProfile, savePath);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

### Selection System Integration

```csharp
// Object selection in scene
if (GUILayout.Button(obj.name, EditorStyles.linkLabel))
{
    Selection.activeGameObject = obj;
    EditorGUIUtility.PingObject(obj);
}

// Asset selection in project
if (GUILayout.Button("Edit Profile"))
{
    Selection.activeObject = selectedProfile;
    EditorUtility.FocusProjectWindow();
}
```

### Undo System Integration

```csharp
// Register undo operations
Undo.RecordObject(currentTrainingAsset, "Modify Training Sequence");

// Apply changes
ModifyTrainingSequence();

// Mark as dirty for save
EditorUtility.SetDirty(currentTrainingAsset);
```

## Development Workflow Integration

### Quick Setup Workflow

1. **Window Access**: `Window > VR Training > Setup Assistant`
2. **Scene Scan**: Automatic detection of tagged objects
3. **Profile Selection**: Choose or create appropriate profiles
4. **Bulk Application**: Apply configurations to multiple objects
5. **Validation**: Check for issues and missing components

### Sequence Creation Workflow

1. **Asset Creation**: Right-click → Create → VR Training → Training Sequence Asset
2. **Template Selection**: Use factory methods for common patterns
3. **Visual Editing**: Two-panel interface for structure and properties
4. **Real-time Validation**: Immediate feedback on configuration issues
5. **Testing**: Play mode integration for runtime testing

### Debugging Integration

```csharp
// Console integration
Debug.Log($"[VRTrainingKit] {message}");
Debug.LogWarning($"[VRTrainingKit] {warning}");
Debug.LogError($"[VRTrainingKit] {error}");

// Inspector integration
[Header("Debug Info")]
[SerializeField] private int debugCurrentModule;
[SerializeField] private int debugActiveSteps;

// Gizmo integration
private void OnDrawGizmosSelected()
{
    if (profile != null)
    {
        Gizmos.color = profile.gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
```

The Unity Editor Integration provides a comprehensive development environment for VR training scenarios, combining automated workflows with detailed manual control for maximum flexibility and ease of use.
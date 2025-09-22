# VR Training Kit - GameObject Reference System

## Overview

The GameObject Reference System provides a robust solution for serializing GameObject references in Unity ScriptableObjects and asset files. It solves the common problem of GameObject references breaking when saved to assets by implementing a fallback resolution system.

## Core Problem Solved

### Unity Serialization Limitations

Unity's built-in GameObject references work well for MonoBehaviours attached to GameObjects in scenes, but have limitations with ScriptableObject assets:

1. **Scene References in Assets**: Direct GameObject references in ScriptableObject assets often break
2. **Cross-Scene References**: References don't persist across scene loads
3. **Asset Portability**: Assets with GameObject references are not portable between projects
4. **Runtime Resolution**: References need to be resolved dynamically at runtime

### GameObjectReference Solution

The `GameObjectReference` class provides a safe, serializable alternative that:
- Stores both direct references and name-based fallbacks
- Automatically resolves broken references at runtime
- Integrates seamlessly with Unity's Inspector
- Provides validation and error feedback

## GameObjectReference Implementation

### Core Class Structure

```csharp
[System.Serializable]
public class GameObjectReference
{
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private string _gameObjectName = "";
    [SerializeField] private string _scenePath = "";
    [SerializeField] private bool _isValid = false;

    public GameObject GameObject { get; set; }
    public bool IsValid { get; }
    public string GameObjectName { get; }
    public string ScenePath { get; }
}
```

### Smart Resolution Property

The key feature is the intelligent GameObject property getter:

```csharp
public GameObject GameObject
{
    get
    {
        // PHASE 1: Try direct reference first (fastest)
        if (_gameObject != null)
            return _gameObject;

        // PHASE 2: Fallback to name-based search
        if (!string.IsNullOrEmpty(_gameObjectName))
        {
            var found = GameObject.Find(_gameObjectName);
            if (found != null)
            {
                // PHASE 3: Cache the found reference at runtime only
                if (Application.isPlaying)
                {
                    _gameObject = found;
                    _isValid = true;
                }
                return found;
            }
        }

        // PHASE 4: Reference not found
        return null;
    }
    set
    {
        _gameObject = value;
        _gameObjectName = value != null ? value.name : "";
        _scenePath = value != null && value.scene.IsValid() ? value.scene.path : "";
        _isValid = value != null;
    }
}
```

### Resolution Strategy

1. **Direct Reference**: Fast O(1) lookup for valid references
2. **Name-Based Fallback**: `GameObject.Find()` for broken direct references
3. **Runtime Caching**: Update direct reference at runtime (not in editor)
4. **Validation**: Track validity state for debugging

## Implicit Conversion Operators

### Seamless Integration

GameObjectReference provides implicit conversions for transparent usage:

```csharp
// Implicit conversion FROM GameObject
public static implicit operator GameObjectReference(GameObject gameObject)
{
    return new GameObjectReference(gameObject);
}

// Implicit conversion TO GameObject
public static implicit operator GameObject(GameObjectReference reference)
{
    return reference?.GameObject;
}
```

### Usage Examples

```csharp
// Direct assignment (uses implicit conversion)
GameObjectReference targetRef = someGameObject;

// Direct usage (uses implicit conversion)
if (targetRef != null)
{
    targetRef.SetActive(true);
}

// Method parameters work transparently
void ProcessObject(GameObject obj) { /* ... */ }
ProcessObject(targetRef); // Automatic conversion
```

## Unity Inspector Integration

### Custom Property Drawer

The `GameObjectReferenceDrawer` provides seamless Unity Inspector integration:

```csharp
[CustomPropertyDrawer(typeof(GameObjectReference))]
public class GameObjectReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get serialized fields
        var gameObjectProp = property.FindPropertyRelative("_gameObject");
        var gameObjectNameProp = property.FindPropertyRelative("_gameObjectName");

        // Current reference resolution
        GameObject currentGameObject = gameObjectProp.objectReferenceValue as GameObject;

        // Fallback name resolution for display
        if (currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue))
        {
            currentGameObject = GameObject.Find(gameObjectNameProp.stringValue);
        }

        // Standard GameObject field
        GameObject newGameObject = (GameObject)EditorGUI.ObjectField(
            position, label, currentGameObject, typeof(GameObject), true);

        // Update all internal fields on change
        if (EditorGUI.EndChangeCheck())
        {
            gameObjectProp.objectReferenceValue = newGameObject;
            gameObjectNameProp.stringValue = newGameObject?.name ?? "";
            // ... update other fields
        }
    }
}
```

### Visual Feedback System

The property drawer provides visual validation feedback:

**Valid Reference**: Normal GameObject field appearance
**Missing Reference with Name**: Warning icon (⚠) with tooltip
**Null Reference**: Empty field

```csharp
// Warning icon display logic
bool showWarning = currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue);
if (showWarning)
{
    string tooltipText = currentGameObject != null ?
        $"Reference found by name: {gameObjectNameProp.stringValue}" :
        $"Missing GameObject: {gameObjectNameProp.stringValue}";
    EditorGUI.LabelField(warningRect, new GUIContent("⚠", tooltipText));
}
```

## Serialization Behavior

### Editor vs Runtime Behavior

The system behaves differently in editor vs runtime for optimization:

**Editor Mode (Application.isPlaying = false)**:
- Does not cache found references to avoid serialization issues
- Searches by name each time for validation
- Updates only when explicitly assigned

**Runtime Mode (Application.isPlaying = true)**:
- Caches found references for performance
- Updates direct reference when found by name
- Optimizes subsequent lookups

### Asset Serialization

GameObjectReference stores multiple pieces of information for robust serialization:

```csharp
[SerializeField] private GameObject _gameObject;      // Direct reference (may break)
[SerializeField] private string _gameObjectName;     // Name fallback (reliable)
[SerializeField] private string _scenePath;          // Scene context (debugging)
[SerializeField] private bool _isValid;              // Validation state
```

## Validation System

### Real-Time Validation

```csharp
public bool IsValid => _gameObject != null ||
    (!string.IsNullOrEmpty(_gameObjectName) && GameObject.Find(_gameObjectName) != null);
```

### Display Name Resolution

```csharp
public override string ToString()
{
    if (_gameObject != null)
        return _gameObject.name;

    if (!string.IsNullOrEmpty(_gameObjectName))
    {
        var found = GameObject.Find(_gameObjectName);
        if (found != null)
            return _gameObjectName; // Object exists
        else
            return $"{_gameObjectName} (Missing)"; // Object missing
    }

    return "None";
}
```

## Usage Patterns

### 1. **Basic Assignment Pattern**

```csharp
public class TrainingStep
{
    [Header("Target Objects")]
    public GameObjectReference targetObject = new GameObjectReference();
    public GameObjectReference destination = new GameObjectReference();
}

// Usage
var step = new TrainingStep();
step.targetObject = wrenchGameObject;     // Implicit conversion
step.destination = toolboxGameObject;     // Implicit conversion
```

### 2. **Null Checking Pattern**

```csharp
// Check if reference is valid
if (targetRef.IsValid)
{
    var obj = targetRef.GameObject;
    if (obj != null)
    {
        obj.SetActive(true);
    }
}

// Or use direct conversion with null check
GameObject obj = targetRef;
if (obj != null)
{
    obj.SetActive(true);
}
```

### 3. **Collection Pattern**

```csharp
[Header("Socket Compatibility")]
public GameObjectReference[] specificCompatibleSockets;

// Usage
foreach (var socketRef in specificCompatibleSockets)
{
    GameObject socket = socketRef; // Implicit conversion
    if (socket != null)
    {
        ConfigureSocket(socket);
    }
}
```

### 4. **Validation Pattern**

```csharp
public bool ValidateReferences()
{
    var invalidRefs = new List<string>();

    if (!targetObject.IsValid)
        invalidRefs.Add($"Target Object: {targetObject.GameObjectName}");

    if (!destination.IsValid)
        invalidRefs.Add($"Destination: {destination.GameObjectName}");

    if (invalidRefs.Count > 0)
    {
        Debug.LogWarning($"Invalid references found: {string.Join(", ", invalidRefs)}");
        return false;
    }

    return true;
}
```

## Performance Considerations

### Lookup Optimization

The system optimizes lookups through multiple strategies:

1. **Direct Reference Priority**: O(1) lookup when direct reference is valid
2. **Runtime Caching**: Cache successful name-based lookups
3. **Lazy Resolution**: Only search when GameObject property is accessed
4. **Editor-Only Search**: Avoid expensive searches in builds when possible

### GameObject.Find() Usage

`GameObject.Find()` is used as a fallback but optimized:

```csharp
// Cached lookup pattern
private static Dictionary<string, GameObject> nameCache = new Dictionary<string, GameObject>();

private GameObject FindGameObjectCached(string name)
{
    if (nameCache.ContainsKey(name))
    {
        var cached = nameCache[name];
        if (cached != null) return cached;
        nameCache.Remove(name); // Clean up destroyed objects
    }

    var found = GameObject.Find(name);
    if (found != null)
    {
        nameCache[name] = found;
    }

    return found;
}
```

## Best Practices

### 1. **Assignment Best Practices**

```csharp
// Good: Direct assignment
targetRef = myGameObject;

// Good: Constructor assignment
var newRef = new GameObjectReference(myGameObject);

// Avoid: Manual field setting
// targetRef._gameObjectName = "SomeName"; // Don't do this
```

### 2. **Validation Best Practices**

```csharp
// Always validate before use
public void ProcessStep(InteractionStep step)
{
    if (!step.targetObject.IsValid)
    {
        Debug.LogError($"Invalid target object in step: {step.stepName}");
        return;
    }

    GameObject target = step.targetObject;
    // Safe to use target here
}
```

### 3. **Error Handling Best Practices**

```csharp
// Graceful degradation
GameObject target = targetRef;
if (target == null)
{
    Debug.LogWarning($"Could not resolve reference: {targetRef.GameObjectName}");

    // Attempt alternative resolution
    target = GameObject.FindWithTag("DefaultTarget");

    if (target == null)
    {
        Debug.LogError("No fallback target available");
        return;
    }
}
```

### 4. **Debugging Best Practices**

```csharp
// Comprehensive logging
public void LogReferenceStatus(GameObjectReference reference, string context)
{
    Debug.Log($"[{context}] Reference Status:");
    Debug.Log($"  Name: {reference.GameObjectName}");
    Debug.Log($"  Valid: {reference.IsValid}");
    Debug.Log($"  Scene: {reference.ScenePath}");
    Debug.Log($"  Resolved: {(reference.GameObject != null ? "Yes" : "No")}");
}
```

## Troubleshooting Common Issues

### 1. **Reference Not Found at Runtime**

**Problem**: GameObject.Find() returns null
**Causes**:
- Object was destroyed
- Object is inactive (GameObject.Find() only finds active objects)
- Object name changed
- Object is in different scene

**Solutions**:
```csharp
// Find inactive objects
var found = Resources.FindObjectsOfTypeAll<GameObject>()
    .FirstOrDefault(obj => obj.name == targetName);

// Find by tag instead of name
var found = GameObject.FindWithTag(targetTag);

// Store additional search criteria
[SerializeField] private string objectTag;
[SerializeField] private int instanceId;
```

### 2. **Inspector Shows Warning Icon**

**Problem**: ⚠ icon appears next to reference field
**Meaning**: Direct reference is broken but name is stored
**Resolution**:
- Object may exist but isn't directly referenced
- Try reassigning the object in Inspector
- Check if object was renamed or moved

### 3. **References Break on Scene Load**

**Problem**: References become null after scene changes
**Solution**:
- GameObjectReference automatically handles this
- Objects will be found by name when accessed
- Ensure object names remain consistent

### 4. **Performance Issues with Many References**

**Problem**: Too many GameObject.Find() calls
**Solutions**:
```csharp
// Cache lookups
private static Dictionary<string, GameObject> globalCache =
    new Dictionary<string, GameObject>();

// Batch resolution
public static void ResolveAllReferences(IEnumerable<GameObjectReference> references)
{
    var allObjects = FindObjectsOfType<GameObject>();
    var nameMap = allObjects.ToDictionary(obj => obj.name, obj => obj);

    foreach (var reference in references)
    {
        if (reference._gameObject == null && !string.IsNullOrEmpty(reference._gameObjectName))
        {
            if (nameMap.TryGetValue(reference._gameObjectName, out GameObject found))
            {
                reference._gameObject = found;
            }
        }
    }
}
```

## Integration with Other Systems

### Step Handler Integration

```csharp
public class GrabStepHandler : BaseStepHandler
{
    public override void StartStep(InteractionStep step)
    {
        // Direct usage with automatic resolution
        GameObject target = step.targetObject;
        if (target == null)
        {
            LogError($"Target object not found: {step.targetObject.GameObjectName}");
            return;
        }

        // Use target safely...
    }
}
```

### Profile System Integration

```csharp
public class ValveProfile : InteractionProfile
{
    public GameObjectReference[] specificCompatibleSockets;

    public bool IsSocketCompatible(GameObject socket)
    {
        foreach (var socketRef in specificCompatibleSockets)
        {
            if (socketRef.GameObject == socket)
                return true;
        }
        return false;
    }
}
```

### Editor Window Integration

```csharp
public class VRInteractionSetupWindow : EditorWindow
{
    private void DrawObjectReference(GameObjectReference reference, string label)
    {
        EditorGUILayout.BeginHorizontal();

        // The property drawer handles the GameObject field automatically
        EditorGUILayout.PropertyField(serializedProperty);

        // Additional validation UI
        if (!reference.IsValid)
        {
            EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
        }

        EditorGUILayout.EndHorizontal();
    }
}
```

The GameObject Reference System provides a robust foundation for handling GameObject references in VR training sequences, ensuring that complex training scenarios remain functional across scene loads, asset serialization, and runtime execution.
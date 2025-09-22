# VR Training Kit - Profile Configuration System

## Overview

The Profile System is a ScriptableObject-based configuration architecture that defines interaction behaviors for VR objects. It provides a data-driven approach to setting up XR Interaction Toolkit components, physics, and custom controllers.

## Core Architecture

### Base Profile Class

```csharp
public abstract class InteractionProfile : ScriptableObject
{
    [Header("Base Settings")]
    public string profileName = "New Profile";
    public Color gizmoColor = Color.cyan;

    public abstract void ApplyToGameObject(GameObject target);
    public abstract bool ValidateGameObject(GameObject target);
}
```

### Profile Lifecycle

1. **Creation**: ScriptableObject assets created via `[CreateAssetMenu]`
2. **Discovery**: Loaded from Resources folder or explicitly assigned
3. **Validation**: `ValidateGameObject()` ensures compatibility
4. **Application**: `ApplyToGameObject()` configures target object
5. **Runtime Override**: Dynamic profile modification for sequence-specific needs

## Profile Implementations

### 1. **GrabProfile** (Simple Interactions)

**Purpose**: Configures objects for basic grabbing interactions
**XRI Components**: `XRGrabInteractable`, `Rigidbody`, `Collider`

```csharp
[CreateAssetMenu(fileName = "GrabProfile", menuName = "VR Training/Grab Profile")]
public class GrabProfile : InteractionProfile
{
    [Header("Grab Settings")]
    public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.VelocityTracking;
    public bool trackPosition = true;
    public bool trackRotation = true;
    public bool throwOnDetach = true;

    [Header("Physics Settings")]
    public float throwVelocityScale = 1.5f;
    public float throwAngularVelocityScale = 1.0f;

    [Header("Collider Settings")]
    public ColliderType colliderType = ColliderType.Box;
    public bool addColliderToMeshChild = true;
}
```

**Key Features**:
- Intelligent collider placement (parent vs. mesh child)
- Automatic Rigidbody configuration based on movement type
- Physics throw parameters for realistic detachment

**Application Pattern**:
```csharp
public override void ApplyToGameObject(GameObject target)
{
    // Add XRGrabInteractable
    XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
    if (grabInteractable == null)
        grabInteractable = target.AddComponent<XRGrabInteractable>();

    // Configure settings
    grabInteractable.movementType = movementType;
    grabInteractable.trackPosition = trackPosition;

    // Ensure Rigidbody exists
    Rigidbody rb = target.GetComponent<Rigidbody>();
    if (rb == null)
    {
        rb = target.AddComponent<Rigidbody>();
        rb.isKinematic = (movementType == XRBaseInteractable.MovementType.Kinematic);
    }

    // Smart collider placement
    GameObject colliderTarget = addColliderToMeshChild ? FindMeshChild(target) ?? target : target;
    if (colliderTarget.GetComponent<Collider>() == null)
        AddCollider(colliderTarget, colliderType);
}
```

### 2. **SnapProfile** (Socket Interactions)

**Purpose**: Configures socket points for object snapping
**XRI Components**: `XRSocketInteractor`, `SphereCollider`, `SnapValidator`

```csharp
[CreateAssetMenu(fileName = "SnapProfile", menuName = "VR Training/Snap Profile")]
public class SnapProfile : InteractionProfile
{
    [Header("Socket Settings")]
    public float socketRadius = 0.1f;
    public bool showInteractableHoverMeshes = true;
    public Material hoverMaterial;

    [Header("Validation")]
    public string[] acceptedTags = new string[] { "grab" };
    public bool requireSpecificObjects = false;
    public GameObject[] specificAcceptedObjects;
}
```

**Key Features**:
- Configurable acceptance criteria (tags or specific objects)
- Visual feedback with hover materials
- Custom validation through `SnapValidator` component

### 3. **KnobProfile** (Rotational Interactions)

**Purpose**: Configures rotatable objects with physics joints
**XRI Components**: `XRGrabInteractable`, `HingeJoint`, `KnobController`

```csharp
[CreateAssetMenu(fileName = "KnobProfile", menuName = "VR Training/Knob Profile")]
public class KnobProfile : InteractionProfile
{
    [Header("Knob Settings")]
    public RotationAxis rotationAxis = RotationAxis.Y;
    public float minAngle = -90f;
    public float maxAngle = 180f;
    public bool useLimits = true;

    [Header("Hinge Joint Settings")]
    public bool autoConfigureConnectedAnchor = true;
    public bool useSpring = true;
    public float springValue = 0f;
    public float damper = 0.1f;
}
```

**Critical Configuration**:
```csharp
// MUST use these settings for joint to work properly
grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
grabInteractable.trackPosition = false;  // Rotation only
grabInteractable.trackRotation = true;
rb.isKinematic = false;  // Required for HingeJoint
```

**Key Features**:
- Physics-based rotation with `HingeJoint`
- Angle limits and spring damping
- Snap-to-angle functionality
- Haptic feedback integration

### 4. **ValveProfile** (Complex State Machines)

**Purpose**: Configures complex valve interactions with multi-state behavior
**Components**: `XRGrabInteractable`, `ValveController`, state management

```csharp
[CreateAssetMenu(fileName = "ValveProfile", menuName = "VR Training/Valve Profile")]
public class ValveProfile : InteractionProfile
{
    [Header("Valve Mechanics")]
    public Vector3 rotationAxis = Vector3.up;
    public float tightenThreshold = 50f;
    public float loosenThreshold = 90f;
    public float angleTolerance = 5f;

    [Header("Socket Compatibility")]
    public string[] compatibleSocketTags = {"valve_socket"};
    public GameObjectReference[] specificCompatibleSockets;
    public bool requireSpecificSockets = false;

    [Header("Physics Settings")]
    public float rotationDampening = 5f;
    public float dampeningSpeed = 10f;
    public float positionTolerance = 0.01f;
    public float velocityThreshold = 0.05f;
}
```

**Key Features**:
- Multi-state valve behavior (Free → Locked → Loose/Tight)
- Socket compatibility system
- Rotation dampening for realistic feel
- Visual and haptic feedback
- Progress indicators

### 5. **ToolProfile** (Specialized Tools)

**Purpose**: Configures tool-specific interactions
**Features**: Custom tool behaviors, specialized validation

## Profile Application System

### 1. **Automated Scene Setup**

```csharp
// InteractionSetupService pattern
public static void ApplyComponentsToObjects(List<GameObject> objects, InteractionProfile profile)
{
    foreach (var obj in objects)
    {
        if (profile.ValidateGameObject(obj))
        {
            profile.ApplyToGameObject(obj);
        }
    }
}
```

### 2. **Tag-Based Discovery**

```csharp
public static SceneAnalysis ScanScene()
{
    SceneAnalysis analysis = new SceneAnalysis();
    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

    foreach (var obj in allObjects)
    {
        if (obj.CompareTag("grab"))
            analysis.grabObjects.Add(obj);
        else if (obj.CompareTag("knob"))
            analysis.knobObjects.Add(obj);
        // ... other tags
    }

    return analysis;
}
```

### 3. **Default Profile Loading**

```csharp
// Load from Resources folder
public static T LoadDefaultProfile<T>() where T : InteractionProfile
{
    return Resources.Load<T>(typeof(T).Name);
}
```

## Runtime Profile Override System

### 1. **Dynamic Profile Creation**

Used by `ValveStepHandler` for sequence-specific parameter overrides:

```csharp
void ApplyValveStepParameters(InteractionStep step, ValveController valveController)
{
    var currentProfile = GetValveProfile(valveController);

    // Create runtime profile with overrides
    var runtimeProfile = ScriptableObject.CreateInstance<ValveProfile>();

    // Copy base settings
    runtimeProfile.profileName = $"{currentProfile.profileName}_Runtime_{step.type}";
    runtimeProfile.rotationAxis = currentProfile.rotationAxis;

    // Apply selective overrides based on step type
    if (IsTightenStep(step.type))
    {
        if (step.tightenThreshold != currentProfile.tightenThreshold)
        {
            runtimeProfile.tightenThreshold = step.tightenThreshold;
        }
    }

    // Apply the modified profile
    valveController.Configure(runtimeProfile);
}
```

### 2. **Override Patterns**

**Step-Specific Overrides**:
- Tighten steps only override tighten threshold
- Loosen steps only override loosen threshold
- Prevents cross-contamination of parameters

**Timing Considerations**:
- Apply overrides EARLY in step lifecycle
- Before state transitions that might reset parameters
- Use reflection when needed to access private profile fields

## Validation System

### 1. **Basic Validation**

```csharp
public override bool ValidateGameObject(GameObject target)
{
    return target != null && target.CompareTag("expectedTag");
}
```

### 2. **Component Validation**

```csharp
public override bool ValidateGameObject(GameObject target)
{
    // Check for required components
    if (target.GetComponent<MeshRenderer>() == null)
        return false;

    // Check for conflicting components
    if (target.GetComponent<CharacterController>() != null)
        return false;

    return true;
}
```

### 3. **Complex Validation**

```csharp
// SnapValidator example
public bool IsValidForSocket(GameObject obj)
{
    // Tag-based validation
    if (acceptedTags.Contains(obj.tag))
        return true;

    // Specific object validation
    if (requireSpecificObjects && specificAcceptedObjects.Contains(obj))
        return true;

    return false;
}
```

## Common Profile Patterns

### 1. **Component Addition Pattern**

```csharp
// Safe component addition
T component = target.GetComponent<T>();
if (component == null)
{
    component = target.AddComponent<T>();
    Debug.Log($"Added {typeof(T).Name} to {target.name}");
}
```

### 2. **Mesh Child Discovery Pattern**

```csharp
private GameObject FindMeshChild(GameObject parent)
{
    MeshRenderer meshRenderer = parent.GetComponentInChildren<MeshRenderer>();
    if (meshRenderer != null && meshRenderer.gameObject != parent)
    {
        return meshRenderer.gameObject;
    }
    return null;
}
```

### 3. **Smart Collider Addition Pattern**

```csharp
private void AddCollider(GameObject target, ColliderType type)
{
    MeshRenderer renderer = target.GetComponent<MeshRenderer>();
    Bounds bounds = renderer?.bounds ?? new Bounds(Vector3.zero, Vector3.one);

    switch (type)
    {
        case ColliderType.Box:
            BoxCollider boxCol = target.AddComponent<BoxCollider>();
            if (renderer != null)
            {
                boxCol.center = target.transform.InverseTransformPoint(bounds.center);
                boxCol.size = bounds.size;
            }
            break;
        // ... other collider types
    }
}
```

### 4. **Configuration Copy Pattern**

```csharp
public void CopySettingsFrom(InteractionProfile source)
{
    if (source is ValveProfile sourceValve && this is ValveProfile targetValve)
    {
        targetValve.rotationAxis = sourceValve.rotationAxis;
        targetValve.tightenThreshold = sourceValve.tightenThreshold;
        // ... copy other relevant settings
    }
}
```

## Asset Management

### 1. **Resource Organization**

```
Assets/VRTrainingKit/Resources/
├── GrabProfile.asset      # Default grab profile
├── KnobProfile.asset      # Default knob profile
├── SnapProfile.asset      # Default snap profile
├── ToolProfile.asset      # Default tool profile
└── ValveProfile.asset     # Default valve profile
```

### 2. **Profile Creation Workflow**

1. **Right-click in Project** → Create → VR Training → [Profile Type]
2. **Configure settings** in Inspector
3. **Save to Resources** for auto-loading or assign explicitly
4. **Test validation** with target objects

### 3. **Profile Variants**

Create specialized profiles for different scenarios:
- `HeavyGrabProfile` - High mass objects
- `PrecisionKnobProfile` - Fine rotation control
- `IndustrialValveProfile` - High threshold values

## Performance Considerations

### 1. **Component Caching**

Profiles should not perform expensive operations during `ApplyToGameObject()`:

```csharp
// Good: Direct component access
var grab = target.GetComponent<XRGrabInteractable>();

// Bad: Expensive searches
var grab = FindObjectsOfType<XRGrabInteractable>().FirstOrDefault(x => x.gameObject == target);
```

### 2. **Runtime Profile Lifecycle**

```csharp
// Create runtime profile
var runtimeProfile = ScriptableObject.CreateInstance<ValveProfile>();

// Use it
valveController.Configure(runtimeProfile);

// Clean up when done (important!)
if (runtimeProfile != null)
{
    ScriptableObject.DestroyImmediate(runtimeProfile);
}
```

### 3. **Validation Optimization**

Cache validation results when possible:

```csharp
private Dictionary<GameObject, bool> validationCache = new Dictionary<GameObject, bool>();

public bool ValidateGameObjectCached(GameObject target)
{
    if (!validationCache.ContainsKey(target))
    {
        validationCache[target] = ValidateGameObject(target);
    }
    return validationCache[target];
}
```

## Debugging and Troubleshooting

### 1. **Profile Application Logging**

```csharp
public override void ApplyToGameObject(GameObject target)
{
    Debug.Log($"[{GetType().Name}] Applying profile '{profileName}' to {target.name}");

    // ... apply settings

    Debug.Log($"[{GetType().Name}] Successfully configured {target.name}");
}
```

### 2. **Validation Debugging**

```csharp
public override bool ValidateGameObject(GameObject target)
{
    if (target == null)
    {
        Debug.LogWarning($"[{GetType().Name}] Validation failed: target is null");
        return false;
    }

    if (!target.CompareTag("expectedTag"))
    {
        Debug.LogWarning($"[{GetType().Name}] Validation failed: {target.name} has tag '{target.tag}', expected 'expectedTag'");
        return false;
    }

    return true;
}
```

### 3. **Component Verification**

```csharp
public void VerifyConfiguration(GameObject target)
{
    var requiredComponents = new System.Type[] { typeof(XRGrabInteractable), typeof(Rigidbody) };

    foreach (var componentType in requiredComponents)
    {
        if (target.GetComponent(componentType) == null)
        {
            Debug.LogError($"Missing required component {componentType.Name} on {target.name}");
        }
    }
}
```

The profile system provides a flexible, data-driven approach to configuring VR interactions while maintaining clean separation between configuration and runtime behavior.
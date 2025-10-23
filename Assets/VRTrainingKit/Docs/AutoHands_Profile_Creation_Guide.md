# AutoHands Profile Creation Guide

**Complete step-by-step guide for creating AutoHands interaction profiles**

This document provides comprehensive instructions for creating AutoHands interaction profiles that integrate with the VR Training Kit framework. Use this as a reference when building new profiles (Snap, Knob, Valve, etc.).

---

## Table of Contents

1. [Understanding the Architecture](#understanding-the-architecture)
2. [Profile Creation Process](#profile-creation-process)
3. [Step Handler Creation Process](#step-handler-creation-process)
4. [UI Integration Process](#ui-integration-process)
5. [Testing and Validation](#testing-and-validation)
6. [Reference Examples](#reference-examples)
7. [Troubleshooting Guide](#troubleshooting-guide)

---

## Understanding the Architecture

### Framework Overview

The VR Training Kit uses a **modular, framework-agnostic architecture** with three core components:

```
┌─────────────────────┐
│ Interaction Profile │ ──► Configures GameObject components
└─────────────────────┘
          │
          ▼
┌─────────────────────┐
│   Step Handler      │ ──► Handles runtime events & sequence completion
└─────────────────────┘
          │
          ▼
┌─────────────────────┐
│  UI Integration     │ ──► Setup Assistant validation & display
└─────────────────────┘
```

### Key Design Principles

1. **Framework Isolation**: Complete separation between XRI and AutoHands
2. **Direct Type References**: Use `typeof()` instead of reflection when possible
3. **Modular Architecture**: Each profile/handler is independent
4. **Event-Based Completion**: Handlers subscribe to framework events
5. **Reuse Existing Patterns**: Mirror XRI implementations for consistency

---

## Profile Creation Process

### Step 1: Understand the AutoHands Component

**Before creating a profile, thoroughly research the target AutoHands component.**

#### Research Checklist:

1. **Locate the Component Script**
   - Path: `Assets/AutoHand/Scripts/[ComponentType]/[ComponentName].cs`
   - Example: `Assets/AutoHand/Scripts/Grabbable/Grabbable.cs`

2. **Identify the Namespace**
   ```csharp
   namespace Autohand {
       public class Grabbable : GrabbableBase, IGrabbableEvents {
   ```
   ✅ Namespace is `Autohand`

3. **Find All Public Properties**
   - Look for `public` fields and properties
   - Note their types (enums, bools, floats, Vector3, etc.)
   - Understand what each property controls

4. **Identify Enum Types**
   ```csharp
   public enum HandGrabType {
       Default,
       HandToGrabbable,
       GrabbableToHand
   }
   ```
   ✅ Note: Enum is `HandGrabType` (not `GrabType`)

5. **Find Event Signatures**
   ```csharp
   public HandGrabEvent OnGrabEvent;
   public HandGrabEvent OnReleaseEvent;
   ```
   Event signature: `(Hand hand, Grabbable grabbable)`

6. **Check Documentation**
   - AutoHands docs: https://earnest-robot.gitbook.io/auto-hand-docs
   - Verify property names match your findings

---

### Step 2: Create the Profile Script

**Location**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/`

**Template Structure**:

```csharp
// [ComponentName]Profile.cs
// AutoHands implementation of [interaction type] using [ComponentName] component
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for [interaction type] using [ComponentName] component
/// </summary>
[CreateAssetMenu(fileName = "AutoHands[Type]Profile", menuName = "VR Training/AutoHands/[Type] Profile", order = X)]
public class AutoHands[Type]Profile : AutoHandsInteractionProfile
{
    // STEP 1: Declare properties matching AutoHands component
    // STEP 2: Implement ApplyToGameObject()
    // STEP 3: Implement ConfigureComponent()
    // STEP 4: Implement ValidateAutoHandsGameObject()
}
```

---

### Step 3: Declare Profile Properties

**Map AutoHands component properties to profile properties.**

#### Property Declaration Rules:

1. **Use Direct Enum Types** (not strings)
   ```csharp
   // ❌ WRONG - String-based (causes type errors)
   public string grabType = "Default";

   // ✅ CORRECT - Direct enum type
   public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;
   ```

2. **Match Exact Property Names**
   ```csharp
   // AutoHands component has:
   public bool singleHandOnly = false;

   // Profile should have EXACT same name:
   public bool singleHandOnly = false;
   ```

3. **Use Unity Tooltips**
   ```csharp
   [Header("Grab Settings")]
   [Tooltip("Grab behavior type - Default, HandToGrabbable, GrabbableToHand")]
   public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;
   ```

4. **Organize Properties by Category**
   ```csharp
   [Header("Core Settings")]
   // Core properties here

   [Header("Grab Behavior")]
   // Behavior properties here

   [Header("Physics Settings")]
   // Physics properties here

   [Header("Advanced Settings")]
   // Advanced properties here
   ```

#### Example from AutoHandsGrabProfile.cs:

```csharp
[Header("AutoHands Grab Settings")]
[Tooltip("Grab behavior type - Default, HandToGrabbable, GrabbableToHand")]
public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;

[Tooltip("Grab pose type - Grab, Pinch")]
public Autohand.HandGrabPoseType grabPoseType = Autohand.HandGrabPoseType.Grab;

[Tooltip("Which hands can grab this object - both, left, right")]
public Autohand.HandType handType = Autohand.HandType.both;

[Tooltip("Whether only one hand can grab at a time")]
public bool singleHandOnly = false;

[Header("Physics Settings")]
[Tooltip("Throw power multiplier when releasing")]
public float throwPower = 1f;

[Tooltip("Force required to break the grab joint")]
public float jointBreakForce = 3500f;
```

**Reference**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/AutoHandsGrabProfile.cs` (Lines 14-84)

---

### Step 4: Implement ApplyToGameObject()

**This method adds the AutoHands component and configures it.**

#### Template:

```csharp
public override void ApplyToGameObject(GameObject target)
{
    LogDebug($"Applying AutoHands [ComponentName] to: {target.name}");

    // STEP 1: Ensure required Unity components exist
    Rigidbody rb = EnsureRigidbody(target, isKinematic: false);
    EnsureCollider(target, colliderType);

    // STEP 2: Add or get AutoHands component using DIRECT TYPE REFERENCE
    var component = target.GetComponent<Autohand.[ComponentName]>();
    if (component == null)
    {
        component = target.AddComponent<Autohand.[ComponentName]>();
        LogDebug($"✅ Added [ComponentName] component to {target.name}");
    }

    // STEP 3: Configure the component
    if (component != null)
    {
        Configure[ComponentName]Component(component);
        LogDebug($"✅ Successfully configured [ComponentName] on {target.name}");
    }
    else
    {
        LogError($"❌ Failed to add [ComponentName] component to {target.name}");
    }
}
```

#### Key Points:

1. **Use Direct Type References** - `GetComponent<Autohand.Grabbable>()` NOT `GetComponent(System.Type.GetType("..."))`
2. **Log Actions** - Use `LogDebug()` for visibility
3. **Handle Component Addition** - Use `GetComponent` then `AddComponent` if null
4. **Call Configuration Method** - Separate configuration logic

**Reference**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/AutoHandsGrabProfile.cs` (Lines 90-119)

---

### Step 5: Implement Component Configuration

**This method sets all component properties using direct assignment.**

#### Template:

```csharp
private void Configure[ComponentName]Component(Autohand.[ComponentName] component)
{
    LogDebug($"Configuring [ComponentName] component: {component.GetType().Name}");

    // STEP 1: Direct property assignment for CORE properties (always exist)
    component.propertyName1 = propertyName1;
    component.propertyName2 = propertyName2;
    component.propertyName3 = propertyName3;

    // STEP 2: Safe property assignment for OPTIONAL properties (may not exist in all versions)
    SetPropertySafely(component, "optionalProperty1", optionalProperty1);
    SetPropertySafely(component, "optionalProperty2", optionalProperty2);

    LogDebug($"✅ Configured [ComponentName]: key properties logged here");
}
```

#### Property Assignment Strategies:

**Strategy 1: Direct Assignment** (for guaranteed properties)
```csharp
// Core properties that ALWAYS exist in the component
grabbable.grabType = grabType;
grabbable.handType = handType;
grabbable.singleHandOnly = singleHandOnly;
```

**Strategy 2: Safe Assignment** (for optional/version-specific properties)
```csharp
// Properties that might not exist in all AutoHands versions
SetPropertySafely(grabbable, "makeChildrenGrabbable", makeChildrenGrabbable);
SetPropertySafely(grabbable, "holdPositionOffset", holdPositionOffset);
```

#### Helper Methods:

```csharp
private void SetPropertySafely<T>(Autohand.[ComponentName] component, string propertyName, T value)
{
    try
    {
        var property = component.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(component, value);
            LogDebug($"✅ Set {propertyName} = {value}");
            return;
        }

        var field = component.GetType().GetField(propertyName);
        if (field != null)
        {
            field.SetValue(component, value);
            LogDebug($"✅ Set {propertyName} = {value}");
            return;
        }

        LogDebug($"⚠️ Property/field '{propertyName}' not found (might not exist in this AutoHands version)");
    }
    catch (System.Exception ex)
    {
        LogWarning($"⚠️ Could not set {propertyName}: {ex.Message}");
    }
}
```

**Reference**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/AutoHandsGrabProfile.cs` (Lines 122-207)

---

### Step 6: Implement Validation

**Validate that target object is suitable for the interaction.**

#### Template:

```csharp
protected override bool ValidateAutoHandsGameObject(GameObject target)
{
    // STEP 1: Check tag
    if (!target.CompareTag("[expected_tag]"))
    {
        LogError($"GameObject {target.name} must have '[expected_tag]' tag for AutoHands[Type]Profile");
        return false;
    }

    // STEP 2: Additional validation (optional)
    // Check for specific requirements, parent/child relationships, etc.

    LogDebug($"✅ {target.name} is valid for AutoHands [type] interaction");
    return true;
}
```

#### Validation Examples:

```csharp
// Grab profile - simple tag check
protected override bool ValidateAutoHandsGameObject(GameObject target)
{
    if (!target.CompareTag("grab"))
    {
        LogError($"GameObject {target.name} must have 'grab' tag");
        return false;
    }
    return true;
}

// Snap profile - tag + additional checks
protected override bool ValidateAutoHandsGameObject(GameObject target)
{
    if (!target.CompareTag("snap"))
    {
        LogError($"GameObject {target.name} must have 'snap' tag");
        return false;
    }

    // Additional validation for snap-specific requirements
    if (requireTriggerCollider)
    {
        var collider = target.GetComponent<Collider>();
        if (collider != null && !collider.isTrigger)
        {
            LogWarning($"Snap point should have a trigger collider");
        }
    }

    return true;
}
```

**Reference**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/AutoHandsGrabProfile.cs` (Lines 210-217)

---

### Step 7: Create Default Profile Asset

**Create a default profile asset for auto-loading by the system.**

#### Steps:

1. **Open Unity Editor**
2. **Navigate to Resources folder**: `Assets/VRTrainingKit/Resources/Auto hands - Profiles/`
3. **Right-click** → Create → VR Training → AutoHands → [Type] Profile
4. **Name the asset**: `AutoHands[Type]Profile` (exact match)
5. **Configure default values** in the Inspector
6. **Save the asset**

#### Default Value Guidelines:

- Use safe, general-purpose defaults
- Match common use cases (e.g., `grabType = Default`)
- Consider physics realism (e.g., `jointBreakForce = 3500`)
- Document defaults in tooltips

**Note**: The system auto-loads profiles from `Resources/Auto hands - Profiles/` folder.

---

## Step Handler Creation Process

### Step 1: Understand Event Signatures

**Research the AutoHands component's events.**

#### Event Research Steps:

1. **Locate Events in Component Script**
   ```csharp
   // In Grabbable.cs
   public HandGrabEvent OnGrabEvent;      // Line 164
   public HandGrabEvent OnReleaseEvent;   // Line 167
   ```

2. **Find Event Type Definition**
   ```csharp
   [System.Serializable]
   public class HandGrabEvent : UnityEvent<Hand, Grabbable> { }
   ```
   ✅ Event signature: `(Hand hand, Grabbable grabbable)`

3. **Identify When Events Fire**
   ```csharp
   // Search for "OnGrabEvent?.Invoke"
   OnGrabEvent?.Invoke(hand, this);  // Line 537 - fires when grabbed
   OnReleaseEvent?.Invoke(hand, this); // Line 560 - fires when released
   ```

4. **Compare with XRI Event Signatures**
   ```csharp
   // XRI: selectEntered.AddListener((args) => OnGrabbed(step, args));
   // AutoHands: grabbable.OnGrabEvent += (hand, grab) => OnGrabbed(step, hand, grab);
   ```

---

### Step 2: Create Step Handler Script

**Location**: `Assets/VRTrainingKit/Scripts/StepHandlers/`

**Naming Convention**: `AutoHands[Type]StepHandler.cs`

#### Template:

```csharp
// AutoHands[Type]StepHandler.cs
// Handles [type] interaction steps in training sequences using AutoHands framework
using UnityEngine;
using Autohand;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for [type]-based interaction steps using AutoHands framework
/// Manages [ComponentName] event subscriptions and step completion
/// Mirrors the structure of XRI [Type]StepHandler but uses AutoHands components
/// </summary>
public class AutoHands[Type]StepHandler : BaseAutoHandsStepHandler
{
    // STEP 1: Component cache
    // STEP 2: Implement CanHandle()
    // STEP 3: Implement Initialize()
    // STEP 4: Implement StartStep()
    // STEP 5: Implement StopStep()
    // STEP 6: Implement Cleanup()
    // STEP 7: Implement event handlers
}
```

---

### Step 3: Implement Component Caching

**Cache AutoHands components at initialization for performance.**

#### Template:

```csharp
// Component cache for [component] components
private Dictionary<GameObject, [ComponentType]> cachedComponents = new Dictionary<GameObject, [ComponentType]>();

// Active step tracking
private Dictionary<InteractionStep, [ComponentType]> activeSteps = new Dictionary<InteractionStep, [ComponentType]>();

void Awake()
{
    CacheComponents();
}

/// <summary>
/// Cache all AutoHands [ComponentName] components in the scene
/// </summary>
void CacheComponents()
{
    LogDebug("🔧 Caching AutoHands [ComponentName] components...");

    cachedComponents.Clear();

    var components = FindObjectsOfType<Autohand.[ComponentName]>();
    foreach (var component in components)
    {
        cachedComponents[component.gameObject] = component;
        LogDebug($"🔧 Cached AutoHands component: {component.name}");
    }

    LogInfo($"🔧 Cached {cachedComponents.Count} AutoHands [ComponentName] components");
}
```

**Reference**: `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsGrabStepHandler.cs` (Lines 16-20, 117-133)

---

### Step 4: Implement Core Handler Methods

#### CanHandle() Method:

```csharp
public override bool CanHandle(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.[YourStepType];
}
```

#### Initialize() Method:

```csharp
public override void Initialize(ModularTrainingSequenceController controller)
{
    base.Initialize(controller);
    LogInfo("🔧 AutoHands[Type]StepHandler initialized");

    // Refresh cache in case scene changed
    CacheComponents();
}
```

---

### Step 5: Implement Event Subscription

**Subscribe to AutoHands events when a step starts.**

#### StartStep() Template:

```csharp
public override void StartStep(InteractionStep step)
{
    LogDebug($"🔧 Starting AutoHands [type] step: {step.stepName}");

    // STEP 1: Get target object
    var targetObject = step.targetObject.GameObject;
    if (targetObject == null)
    {
        LogError($"Target object is null for step: {step.stepName}");
        return;
    }

    // STEP 2: Get cached component
    if (!cachedComponents.ContainsKey(targetObject))
    {
        LogError($"No AutoHands [ComponentName] found for object: {targetObject.name}");
        return;
    }

    var component = cachedComponents[targetObject];

    // STEP 3: Subscribe to events
    component.OnEventName += (param1, param2) => OnEventHandler(step, param1, param2);

    // STEP 4: Track active step
    activeSteps[step] = component;

    LogDebug($"🔧 Subscribed to AutoHands events for: {targetObject.name}");
}
```

**Key Points:**

1. **Use Lambda Expressions** for event subscription with step context
   ```csharp
   // ✅ CORRECT - Captures step in closure
   component.OnGrabEvent += (hand, grab) => OnObjectGrabbed(step, hand, grab);

   // ❌ WRONG - Can't pass step parameter
   component.OnGrabEvent += OnObjectGrabbed;
   ```

2. **Validate Component Exists** before subscribing
3. **Track Active Steps** for cleanup

**Reference**: `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsGrabStepHandler.cs` (Lines 41-68)

---

### Step 6: Implement Event Unsubscription

**Unsubscribe from events when a step stops.**

#### StopStep() Template:

```csharp
public override void StopStep(InteractionStep step)
{
    LogDebug($"🔧 Stopping AutoHands [type] step: {step.stepName}");

    if (activeSteps.ContainsKey(step))
    {
        var component = activeSteps[step];

        // Unsubscribe from events
        // NOTE: Must match EXACT subscription signature
        component.OnEventName -= (param1, param2) => OnEventHandler(step, param1, param2);

        // Remove from tracking
        activeSteps.Remove(step);

        LogDebug($"🔧 Unsubscribed from AutoHands events for step: {step.stepName}");
    }
}
```

**Important**: The unsubscription signature must EXACTLY match the subscription signature, including the lambda expression.

**Reference**: `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsGrabStepHandler.cs` (Lines 70-86)

---

### Step 7: Implement Event Handlers

**Handle AutoHands events and complete steps when conditions are met.**

#### Event Handler Template:

```csharp
/// <summary>
/// Handle [event] from AutoHands [ComponentName] component
/// Event signature: ([parameters])
/// </summary>
void OnEventHandler(InteractionStep step, [parameters])
{
    // STEP 1: Check if step already completed
    if (step.isCompleted) return;

    // STEP 2: Extract relevant objects/data from event parameters
    var actualObject = [extract from parameters];
    var expectedObject = step.targetObject.GameObject;

    // STEP 3: Log for debugging
    LogDebug($"🔧 Event occurred: actual={actualObject.name}, expected={expectedObject?.name}");

    // STEP 4: Validate conditions
    if (actualObject == expectedObject)
    {
        // STEP 5: Complete the step
        LogDebug($"🔧 Conditions met! Completing step: {step.stepName}");
        CompleteStep(step, $"[Descriptive reason]: {actualObject.name}");
    }
    else
    {
        LogDebug($"🔧 Conditions not met - [explanation]");
    }
}
```

#### Example from AutoHandsGrabStepHandler:

```csharp
void OnObjectGrabbed(InteractionStep step, Hand hand, Grabbable grabbable)
{
    if (step.isCompleted) return;

    var grabbedObject = grabbable.gameObject;
    var expectedObject = step.targetObject.GameObject;

    LogDebug($"🤏 AutoHands object grabbed: {grabbedObject.name}, expected: {expectedObject?.name}");
    LogDebug($"🤏 Grabbed by hand: {hand.name}");

    if (grabbedObject == expectedObject)
    {
        LogDebug($"🤏 AutoHands grab match! Completing step: {step.stepName}");
        CompleteStep(step, $"Grabbed {grabbedObject.name} with AutoHands");
    }
    else
    {
        LogDebug($"🤏 AutoHands grab mismatch - grabbed {grabbedObject.name} but expected {expectedObject?.name}");
    }
}
```

**Reference**: `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsGrabStepHandler.cs` (Lines 135-152)

---

### Step 8: Implement Cleanup

**Clean up all resources when the handler is destroyed.**

#### Template:

```csharp
public override void Cleanup()
{
    LogDebug("🔧 Cleaning up AutoHands [type] step handler...");

    // Stop all active steps (creates copy to avoid modification during iteration)
    var activeStepsList = new List<InteractionStep>(activeSteps.Keys);
    foreach (var step in activeStepsList)
    {
        StopStep(step);
    }

    // Clear cache
    cachedComponents.Clear();

    base.Cleanup();
}
```

**Important**: Create a copy of activeSteps.Keys before iterating, since StopStep() modifies the dictionary.

**Reference**: `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsGrabStepHandler.cs` (Lines 88-103)

---

## UI Integration Process

### Step 1: Update Setup Tab Validation

**Make the Setup tab recognize configured AutoHands components.**

**Location**: `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**What to Update**: The `DrawObjectList()` method's configuration detection logic

#### Find the Validation Code:

Search for: `"Configure first"` (around line 813)

#### Update Pattern:

```csharp
// Check if configured (framework-aware)
bool isConfigured = false;
XRBaseInteractable interactable = null;
XRSocketInteractor socketInteractor = null;

// Detect current framework
var currentFramework = VRFrameworkDetector.DetectCurrentFramework();

if (currentFramework == VRFramework.XRI)
{
    // XRI validation
    if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve")
    {
        interactable = obj.GetComponent<XRGrabInteractable>();
        isConfigured = interactable != null;
    }
    else if (tag == "snap")
    {
        socketInteractor = obj.GetComponent<XRSocketInteractor>();
        isConfigured = socketInteractor != null;
    }
}
else if (currentFramework == VRFramework.AutoHands)
{
    // AutoHands validation - ADD YOUR COMPONENT CHECK HERE
    if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve")
    {
        var grabbable = obj.GetComponent<Autohand.Grabbable>();
        isConfigured = grabbable != null;
    }
    else if (tag == "snap")
    {
        // For PlacePoint or other snap components
        var components = obj.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && component.GetType().Name == "PlacePoint")
            {
                isConfigured = true;
                break;
            }
        }
    }
}
```

**Key Pattern for Different Component Types:**

1. **For Grabbable-type Components** (grab, knob, tool, valve):
   ```csharp
   var component = obj.GetComponent<Autohand.[ComponentName]>();
   isConfigured = component != null;
   ```

2. **For Snap/PlacePoint-type Components** (if using reflection):
   ```csharp
   var components = obj.GetComponents<MonoBehaviour>();
   foreach (var component in components)
   {
       if (component != null && component.GetType().Name == "[ComponentName]")
       {
           isConfigured = true;
           break;
       }
   }
   ```

**Reference**: `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs` (Lines 757-801)

---

### Step 2: Update Layer Display Logic

**Handle AutoHands objects in the layer display area.**

#### Update Pattern:

```csharp
// Layer mask dropdown (only if configured and XRI framework)
if (isConfigured)
{
    if (currentFramework == VRFramework.XRI && (interactable != null || socketInteractor != null))
    {
        // XRI layer mask dropdown code...
    }
    else if (currentFramework == VRFramework.AutoHands)
    {
        // AutoHands doesn't use XRI interaction layers
        EditorGUILayout.LabelField("✓ Configured (AutoHands)", EditorStyles.miniLabel, GUILayout.Width(150));
    }
    else
    {
        EditorGUILayout.LabelField("Default", EditorStyles.miniLabel, GUILayout.Width(150));
    }
}
else
{
    EditorGUILayout.LabelField("Configure first", EditorStyles.miniLabel, GUILayout.Width(150));
}
```

**Note**: AutoHands components don't use XRI's interaction layer system, so we display a simple "✓ Configured (AutoHands)" label instead of a dropdown.

**Reference**: `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs` (Lines 809-856)

---

## Testing and Validation

### Test 1: Profile Application Test

**Verify the profile correctly configures objects.**

#### Test Steps:

1. **Create Test Script** (optional, for manual testing):
   ```csharp
   public class Test[Type]Profile : MonoBehaviour
   {
       public AutoHands[Type]Profile testProfile;
       public GameObject testTarget;

       [ContextMenu("Apply Profile")]
       void ApplyProfile()
       {
           testProfile.ApplyToGameObject(testTarget);
       }
   }
   ```

2. **Create Test Object**:
   - GameObject with appropriate tag (e.g., `grab`, `snap`)
   - No components initially

3. **Apply Profile**:
   - Via Setup Assistant Configure button
   - Or via test script context menu

4. **Verify Components Added**:
   - ✅ AutoHands component present
   - ✅ Rigidbody present
   - ✅ Collider present
   - ✅ Component properties match profile values

5. **Check Console Logs**:
   - Enable `enableAutoHandsDebugLogging = true` in profile
   - Look for `✅ Successfully configured...` messages
   - No `❌ Failed to...` errors

---

### Test 2: Setup Assistant Integration Test

**Verify UI correctly displays configured status.**

#### Test Steps:

1. **Open Setup Assistant**: Window → VR Training → Setup Assistant
2. **Go to Setup Tab**
3. **Click "Scan Scene"**
4. **Check Object List**:
   - ✅ Configured objects show green checkmark `✓`
   - ✅ Status shows `"✓ Configured (AutoHands)"`
   - ✅ Unconfigured objects show `○` and `"Configure first"`

5. **Test Configure Button**:
   - Select unconfigured object
   - Click "Configure"
   - Verify checkmark appears after configuration

---

### Test 3: Sequence Integration Test

**Verify step handler correctly handles events.**

#### Test Steps:

1. **Add Handler to Scene**:
   - Create empty GameObject (e.g., "AutoHands Step Handlers")
   - Add `AutoHands[Type]StepHandler` component

2. **Create Test Sequence**:
   - Open Sequence Builder tab
   - Create simple sequence with one [type] step
   - Point step to configured test object

3. **Test Runtime Behavior**:
   - Enter Play mode
   - Start the sequence
   - Perform the interaction (grab, snap, turn, etc.)
   - **Expected**: Step completes with console log

4. **Check Console Logs**:
   ```
   [AutoHands[Type]StepHandler] 🔧 Cached X AutoHands components
   [AutoHands[Type]StepHandler] 🔧 Starting AutoHands [type] step: [name]
   [AutoHands[Type]StepHandler] 🔧 Subscribed to AutoHands events for: [object]
   [AutoHands[Type]StepHandler] 🔧 Event occurred: [details]
   [AutoHands[Type]StepHandler] 🔧 Conditions met! Completing step: [name]
   ```

---

### Test 4: Framework Detection Test

**Verify correct framework is detected.**

#### Test Steps:

1. **Check Framework Status**:
   - Open Setup Assistant → Setup Tab
   - Look at "VR Framework Status" section
   - **Expected**: `Detected Framework: Auto Hand`

2. **Verify Profile Loading**:
   - Go to Configure tab
   - **Expected**: AutoHands profiles loaded
   - **Expected**: No XRI profile warnings

---

## Reference Examples

### Complete AutoHands Grab Profile

**Full working example with all features:**

**Script**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/AutoHandsGrabProfile.cs`

**Key Features Demonstrated**:
- ✅ Direct enum type usage
- ✅ Comprehensive property mapping
- ✅ Direct component type references
- ✅ Safe property assignment fallback
- ✅ Proper validation
- ✅ Clear logging

**Lines to Study**:
- Lines 14-84: Property declarations
- Lines 90-119: ApplyToGameObject implementation
- Lines 122-164: Component configuration
- Lines 181-207: Safe property assignment helpers
- Lines 210-217: Validation

---

### Complete AutoHands Grab Step Handler

**Full working example with event handling:**

**Script**: `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsGrabStepHandler.cs`

**Key Features Demonstrated**:
- ✅ Component caching pattern
- ✅ Event subscription with lambda closures
- ✅ Event handler implementation
- ✅ Step completion logic
- ✅ Proper cleanup

**Lines to Study**:
- Lines 16-20: Cache dictionaries
- Lines 41-68: StartStep with event subscription
- Lines 70-86: StopStep with event unsubscription
- Lines 117-133: Component caching
- Lines 135-152: Event handler implementation

---

### XRI Grab Profile (for comparison)

**Compare AutoHands approach with XRI approach:**

**Script**: `Assets/VRTrainingKit/Scripts/Profiles/Implementations/GrabProfile.cs`

**Key Differences**:
- XRI uses `XRGrabInteractable` vs AutoHands uses `Grabbable`
- XRI has `movementType`, `trackPosition` vs AutoHands has `grabType`, `grabPoseType`
- Both follow same overall structure

---

### XRI Grab Step Handler (for comparison)

**Compare event handling approaches:**

**Script**: `Assets/VRTrainingKit/Scripts/StepHandlers/GrabStepHandler.cs`

**Key Differences**:
- XRI: `selectEntered` event vs AutoHands: `OnGrabEvent`
- XRI: Event args parameter vs AutoHands: Hand and Grabbable parameters
- Same caching and cleanup patterns

---

## Troubleshooting Guide

### Problem: "Type cannot be null" Error

**Cause**: Using reflection-based type lookup instead of direct type references

**Solution**:
```csharp
// ❌ WRONG
Component component = target.GetComponent(System.Type.GetType("Autohand.ComponentName"));

// ✅ CORRECT
var component = target.GetComponent<Autohand.ComponentName>();
```

---

### Problem: Profile Properties Don't Show in Inspector

**Cause**: Using wrong field types or visibility modifiers

**Solution**:
```csharp
// ❌ WRONG - private field
private Autohand.HandGrabType grabType;

// ✅ CORRECT - public field
public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;
```

---

### Problem: Component Properties Not Getting Set

**Cause 1**: Wrong property names (case-sensitive)
```csharp
// Check actual property name in AutoHands component
// Must match EXACTLY including case
```

**Cause 2**: Using enum strings instead of enum values
```csharp
// ❌ WRONG
SetComponentProperty(component, "grabType", "Default");

// ✅ CORRECT
component.grabType = Autohand.HandGrabType.Default;
```

---

### Problem: Events Not Firing

**Cause 1**: Wrong event name
```csharp
// Check AutoHands component for exact event names
// Example: OnGrabEvent (not OnGrab or OnGrabbed)
```

**Cause 2**: Event signature mismatch
```csharp
// ❌ WRONG - method signature doesn't match event
void OnGrabbed(Grabbable grab) { }

// ✅ CORRECT - matches HandGrabEvent signature
void OnGrabbed(Hand hand, Grabbable grab) { }
```

---

### Problem: Objects Show "Configure First" After Configuration

**Cause**: UI validation not updated for AutoHands components

**Solution**: Update `VRInteractionSetupWindow.cs` validation logic (see [UI Integration Process](#ui-integration-process))

---

### Problem: Step Doesn't Complete When Event Fires

**Debugging Steps**:

1. **Check Event Subscription**:
   ```csharp
   // Add debug log in StartStep
   LogDebug($"Subscribing to events for: {targetObject.name}");
   ```

2. **Check Event Handler Called**:
   ```csharp
   // Add debug log at start of event handler
   LogDebug($"Event handler called! Step: {step.stepName}");
   ```

3. **Check Condition Matching**:
   ```csharp
   // Log both actual and expected values
   LogDebug($"Actual: {actualObject.name}, Expected: {expectedObject?.name}");
   LogDebug($"Match: {actualObject == expectedObject}");
   ```

4. **Check Step Not Already Completed**:
   ```csharp
   // Check at start of event handler
   if (step.isCompleted)
   {
       LogDebug($"Step already completed, ignoring event");
       return;
   }
   ```

---

## Quick Reference Checklist

### Creating a New AutoHands Profile

- [ ] Research AutoHands component thoroughly
- [ ] Document component namespace, properties, and enums
- [ ] Create profile script in `Profiles/Implementations/AutoHands/`
- [ ] Declare properties using direct enum types
- [ ] Implement `ApplyToGameObject()` with direct type references
- [ ] Implement component configuration method
- [ ] Implement validation method
- [ ] Create default profile asset in Resources folder
- [ ] Test profile application on test object
- [ ] Verify properties set correctly in Inspector

### Creating a New AutoHands Step Handler

- [ ] Research AutoHands component events
- [ ] Document event names and signatures
- [ ] Create step handler script in `StepHandlers/`
- [ ] Extend `BaseAutoHandsStepHandler`
- [ ] Implement component caching
- [ ] Implement `CanHandle()` method
- [ ] Implement `Initialize()` method
- [ ] Implement `StartStep()` with event subscription
- [ ] Implement `StopStep()` with event unsubscription
- [ ] Implement event handler methods
- [ ] Implement `Cleanup()` method
- [ ] Test event handling in Play mode

### Integrating with Setup Assistant UI

- [ ] Update validation logic in `VRInteractionSetupWindow.cs`
- [ ] Add framework detection check
- [ ] Add AutoHands component detection
- [ ] Update layer display logic for AutoHands
- [ ] Test Setup tab shows correct status
- [ ] Test Configure button works
- [ ] Verify checkmarks appear after configuration

---

## Summary

This guide covers the complete process of creating AutoHands interaction profiles:

1. **Profile Creation**: Research component → Declare properties → Implement configuration → Validate
2. **Step Handler Creation**: Research events → Implement caching → Handle events → Complete steps
3. **UI Integration**: Update validation → Update display logic → Test UI
4. **Testing**: Verify all integration points work correctly

**Key Principles**:
- ✅ Use direct type references (avoid reflection when possible)
- ✅ Mirror existing XRI patterns for consistency
- ✅ Use framework-aware validation
- ✅ Log extensively for debugging
- ✅ Test each component independently

**Next Steps**:
- Use this guide to create **AutoHandsSnapProfile** + **AutoHandsSnapStepHandler**
- Then create **AutoHandsKnobProfile** + **AutoHandsKnobStepHandler**
- Finally create **AutoHandsValveProfile** + **AutoHandsValveStepHandler**

---

**Document Version**: 1.0
**Last Updated**: 2025-09-30
**Created By**: Claude Code (Anthropic)
**For**: HVAC VR Training Kit - AutoHands Integration

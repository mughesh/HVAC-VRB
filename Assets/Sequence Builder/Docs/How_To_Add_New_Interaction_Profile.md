# How to Add a New Interaction Profile to VRTrainingKit

**Version:** 1.0
**Date:** December 2024
**Example Profile:** Teleport Interaction (AutoHands)

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Understanding](#architecture-understanding)
3. [Part A: Laying the Foundation](#part-a-laying-the-foundation)
4. [Part B: Connecting with UI](#part-b-connecting-with-ui)
5. [Testing Checklist](#testing-checklist)
6. [Troubleshooting](#troubleshooting)

---

## Overview

Adding a new interaction profile to VRTrainingKit involves two major parts:
- **Part A: Laying the Foundation** - Core data structures, runtime components, and step handlers
- **Part B: Connecting with UI** - Editor window integration across multiple tabs


**Files You'll Modify:** ~7 files
**Files You'll Create:** ~3 files

---

## Architecture Understanding

### The Three-Layer System

```
┌─────────────────────────────────────────────────────────────┐
│  LAYER 1: DATA STRUCTURES (TrainingSequence.cs)            │
│  - Step type enum                                            │
│  - Step fields (GameObject references, settings)            │
│  - Validation logic                                          │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  LAYER 2: RUNTIME COMPONENTS                                 │
│  - InteractionProfile (ScriptableObject for configuration)  │
│  - Controller (MonoBehaviour for runtime execution)         │
│  - StepHandler (Connects events to completion)              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  LAYER 3: EDITOR UI (VRInteractionSetupWindow.cs)          │
│  - Setup Tab (scan & apply profiles)                        │
│  - Configure Tab (profile selection)                        │
│  - Sequence Tab (step property editing)                     │
└─────────────────────────────────────────────────────────────┘
```

### Key Concepts

- **InteractionProfile**: ScriptableObject that stores configuration settings
- **Controller Component**: MonoBehaviour added to scene objects for runtime behavior
- **StepHandler**: Manages step lifecycle (start, monitor, complete)
- **GameObjectReference**: Safe reference system that survives scene reloads

---

## Part A: Laying the Foundation

### Step 1: Add Step Type to Enum

**File:** `Assets/VRTrainingKit/Scripts/SequenceSystem/Data/TrainingSequence.cs`

**Location:** Line ~344 - `InteractionStep.StepType` enum

**What to add:**
```csharp
public enum StepType
{
    Grab,
    GrabAndSnap,
    TurnKnob,
    WaitForCondition,
    ShowInstruction,
    TightenValve,
    LoosenValve,
    InstallValve,
    RemoveValve,
    WaitForScriptCondition,
    Teleport              // ← ADD YOUR NEW TYPE HERE
}
```

**Naming Convention:** Use descriptive verb (Grab, Teleport, Press, Toggle, etc.)

---

### Step 2: Add Step-Specific Fields

**File:** `Assets/VRTrainingKit/Scripts/SequenceSystem/Data/TrainingSequence.cs`

**Location:** After line ~447 (after "Guidance Arrows" section)

**What to add:**
```csharp
[Header("Teleport Settings")]
[Tooltip("For Teleport: The wrist button that triggers teleport")]
public GameObjectReference wristButton = new GameObjectReference();

[Tooltip("For Teleport: Destination teleport point GameObject (tagged 'teleportPoint')")]
public GameObjectReference teleportDestination = new GameObjectReference();

[Tooltip("For Teleport: Enable XR recentering after teleport")]
public bool enableRecentering = true;

[Tooltip("For Teleport: Delay before recentering (seconds)")]
[Range(0f, 2f)]
public float recenteringDelay = 0.5f;
```

**Important Notes:**
- Use `GameObjectReference` for scene object references (NOT `GameObject` directly)
- Use `[Header]` to group related fields
- Add clear tooltips for designer guidance
- Use `[Range]` for numerical constraints

---

### Step 3: Add Validation Logic

**File:** `Assets/VRTrainingKit/Scripts/SequenceSystem/Data/TrainingSequence.cs`

**Location A:** In `IsValid()` method (line ~468)

```csharp
case StepType.Teleport:
    return wristButton != null && wristButton.IsValid &&
           teleportDestination != null && teleportDestination.IsValid;
```

**Location B:** In `GetValidationMessage()` method (line ~525)

```csharp
case StepType.Teleport:
    if ((wristButton == null || !wristButton.IsValid) &&
        (teleportDestination == null || !teleportDestination.IsValid))
        return "Missing wrist button and teleport destination";
    if (wristButton == null || !wristButton.IsValid)
        return "Missing or invalid wrist button reference";
    if (teleportDestination == null || !teleportDestination.IsValid)
        return "Missing or invalid teleport destination";
    break;
```

**Pattern:** Always check ALL required GameObjectReferences for validity

---

### Step 4: Create the Interaction Profile

**File:** `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/AutoHandsTeleportProfile.cs` (CREATE NEW)

**Template:**
```csharp
// AutoHandsTeleportProfile.cs
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for configuring teleport destination points
/// Applies TeleportController component with position/rotation metadata
/// </summary>
[CreateAssetMenu(
    fileName = "AutoHandsTeleportProfile",
    menuName = "VR Training/AutoHands/Teleport Profile",
    order = 5)]
public class AutoHandsTeleportProfile : AutoHandsInteractionProfile
{
    [Header("Recentering Settings")]
    [Tooltip("Enable XRInput subsystem recentering after teleport")]
    public bool enableRecentering = true;

    [Tooltip("Delay before triggering recentering (seconds)")]
    [Range(0f, 2f)]
    public float recenteringDelay = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Show destination preview indicator in scene")]
    public bool showDestinationPreview = true;

    [Tooltip("Preview indicator color")]
    public Color previewColor = new Color(0, 1, 0, 0.5f);

    /// <summary>
    /// Apply TeleportController component to destination point
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying TeleportController to: {target.name}");

        // Add controller component
        var controller = target.GetComponent<TeleportController>();
        if (controller == null)
        {
            controller = target.AddComponent<TeleportController>();
            LogDebug($"✅ Added TeleportController to {target.name}");
        }

        // Configure controller with profile settings
        controller.enableRecentering = enableRecentering;
        controller.recenteringDelay = recenteringDelay;
        controller.showDestinationPreview = showDestinationPreview;
        controller.previewColor = previewColor;

        LogDebug($"✅ Configured TeleportController on {target.name}");
    }

    /// <summary>
    /// Validate teleport destination point
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        if (!target.CompareTag("teleportPoint"))
        {
            LogError($"GameObject {target.name} must have 'teleportPoint' tag");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands teleport interaction");
        return true;
    }
}
```

**Key Points:**
- Inherit from `AutoHandsInteractionProfile` (or `InteractionProfile` for XRI)
- Add `[CreateAssetMenu]` for asset creation
- Override `ApplyToGameObject()` - adds components and configures them
- Override `ValidateAutoHandsGameObject()` - checks tag and requirements
- Use `LogDebug()`, `LogInfo()`, `LogError()` for consistent logging

---

### Step 5: Create the Runtime Controller

**File:** `Assets/VRTrainingKit/Scripts/Core/Controllers/TeleportController.cs` (CREATE NEW)

**Template:**
```csharp
// TeleportController.cs
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Runtime component for teleport destination points
/// Executes teleport logic and handles XR recentering
/// </summary>
public class TeleportController : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Reference to AutoHandPlayer in scene")]
    public GameObject autoHandPlayerReference;

    [Header("Recentering Settings")]
    public bool enableRecentering = true;
    [Range(0f, 2f)]
    public float recenteringDelay = 0.5f;

    [Header("Visual Feedback")]
    public bool showDestinationPreview = true;
    public Color previewColor = new Color(0, 1, 0, 0.5f);

    private GameObject previewIndicator;
    private Coroutine recenteringCoroutine;

    void Start()
    {
        if (showDestinationPreview)
        {
            CreatePreviewIndicator();
        }

        if (autoHandPlayerReference == null)
        {
            Debug.LogError($"[TeleportController] {name}: AutoHandPlayer reference is null!");
        }
    }

    void OnDestroy()
    {
        if (previewIndicator != null)
        {
            Destroy(previewIndicator);
        }
    }

    /// <summary>
    /// Execute teleport to this destination point
    /// PUBLIC METHOD - Called by step handler
    /// </summary>
    public void ExecuteTeleport()
    {
        if (autoHandPlayerReference == null)
        {
            Debug.LogError($"[TeleportController] Cannot teleport - player reference is null");
            return;
        }

        Vector3 finalPosition = transform.position;
        Quaternion finalRotation = transform.rotation;

        Debug.Log($"[TeleportController] Teleporting to {name} at {finalPosition}");

        // Execute teleport
        autoHandPlayerReference.transform.SetPositionAndRotation(finalPosition, finalRotation);

        // Trigger recentering if enabled
        if (enableRecentering)
        {
            if (recenteringCoroutine != null)
            {
                StopCoroutine(recenteringCoroutine);
            }
            recenteringCoroutine = StartCoroutine(HandleRecentering());
        }
    }

    private IEnumerator HandleRecentering()
    {
        Debug.Log($"[TeleportController] Starting recentering sequence");
        yield return new WaitForSeconds(recenteringDelay);

        // XR recentering logic here
        // (See full implementation in actual TeleportController.cs)
    }

    private void CreatePreviewIndicator()
    {
        // Create visual indicator (cylinder disc)
        previewIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        previewIndicator.name = $"{name}_Preview";
        previewIndicator.transform.SetParent(transform);
        previewIndicator.transform.localPosition = Vector3.zero;
        previewIndicator.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);

        // Configure transparent material
        var renderer = previewIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = previewColor;
            renderer.material = material;
        }

        // Remove collider (preview only)
        Destroy(previewIndicator.GetComponent<Collider>());
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw position indicator
        Gizmos.color = previewColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);

        // Draw label
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        Handles.Label(transform.position + Vector3.up * 0.5f, $"Teleport: {name}", style);
    }
#endif
}
```

**Key Points:**
- Public method(s) that step handler will call (e.g., `ExecuteTeleport()`)
- Store configuration fields (set by profile)
- Optional: Visual feedback (gizmos, preview indicators)
- Optional: Editor-only debug visualization with `OnDrawGizmos()`

---

### Step 6: Update InteractionSetupService for Scanning

**File:** `Assets/VRTrainingKit/Scripts/Core/Services/InteractionSetupService.cs`

**Location A:** Add list to `SceneAnalysis` class (line ~12)

```csharp
public class SceneAnalysis
{
    public List<GameObject> grabObjects = new List<GameObject>();
    public List<GameObject> knobObjects = new List<GameObject>();
    public List<GameObject> snapObjects = new List<GameObject>();
    // ... existing lists ...
    public List<GameObject> teleportObjects = new List<GameObject>();  // ← ADD THIS

    public int TotalInteractables => grabObjects.Count + knobObjects.Count +
        snapObjects.Count + /* ... */ + teleportObjects.Count;  // ← UPDATE THIS
}
```

**Location B:** Add scanning logic in `ScanScene()` method (line ~28)

```csharp
else if (obj.CompareTag("teleportPoint"))
{
    analysis.teleportObjects.Add(obj);
    Debug.Log($"[InteractionSetupService] Found teleport point: {obj.name}");
}
```

**Location C:** Add to debug output (line ~70)

```csharp
Debug.Log($"  - Teleport Points: {analysis.teleportObjects.Count}");
```

**Pattern:** Always update all three locations (list, scanning, debug output)

---

### Step 7: Create the Step Handler

**File:** `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsTeleportStepHandler.cs` (CREATE NEW)

**Template:**
```csharp
// AutoHandsTeleportStepHandler.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Handler for teleport interaction steps using AutoHands framework
/// Manages event subscriptions and step completion
/// </summary>
public class AutoHandsTeleportStepHandler : BaseAutoHandsStepHandler
{
    // Component caches
    private Dictionary<GameObject, TeleportController> teleportControllers =
        new Dictionary<GameObject, TeleportController>();
    private Dictionary<GameObject, WristUIButton> wristButtons =
        new Dictionary<GameObject, WristUIButton>();

    // Active step tracking
    private Dictionary<InteractionStep, TeleportController> activeStepTeleports =
        new Dictionary<InteractionStep, TeleportController>();
    private Dictionary<InteractionStep, WristUIButton> activeStepButtons =
        new Dictionary<InteractionStep, WristUIButton>();
    private Dictionary<InteractionStep, UnityAction> buttonEventDelegates =
        new Dictionary<InteractionStep, UnityAction>();

    void Awake()
    {
        CacheTeleportControllers();
        CacheWristButtons();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.Teleport;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("🚀 AutoHandsTeleportStepHandler initialized");

        CacheTeleportControllers();
        CacheWristButtons();
    }

    public override void StartStep(InteractionStep step)
    {
        LogDebug($"🚀 Starting teleport step: {step.stepName}");

        // Get destination and validate
        var destinationObject = step.teleportDestination?.GameObject;
        if (destinationObject == null || !teleportControllers.ContainsKey(destinationObject))
        {
            LogError($"No TeleportController found for step: {step.stepName}");
            return;
        }

        var teleportController = teleportControllers[destinationObject];

        // Get button and validate
        var buttonObject = step.wristButton?.GameObject;
        if (buttonObject == null || !wristButtons.ContainsKey(buttonObject))
        {
            LogError($"No WristUIButton found for step: {step.stepName}");
            return;
        }

        var wristButton = wristButtons[buttonObject];

        // Create delegate that captures step context
        UnityAction buttonDelegate = () => OnTeleportButtonPressed(step, teleportController);

        // Subscribe to button press event
        wristButton.OnButtonPressed.AddListener(buttonDelegate);

        // Track active step
        activeStepTeleports[step] = teleportController;
        activeStepButtons[step] = wristButton;
        buttonEventDelegates[step] = buttonDelegate;

        LogDebug($"🚀 Subscribed to button: {buttonObject.name} → {destinationObject.name}");
    }

    public override void StopStep(InteractionStep step)
    {
        if (activeStepButtons.ContainsKey(step) && buttonEventDelegates.ContainsKey(step))
        {
            var wristButton = activeStepButtons[step];
            var buttonDelegate = buttonEventDelegates[step];

            // Unsubscribe from button event
            wristButton.OnButtonPressed.RemoveListener(buttonDelegate);

            // Remove from tracking
            activeStepTeleports.Remove(step);
            activeStepButtons.Remove(step);
            buttonEventDelegates.Remove(step);

            LogDebug($"🚀 Unsubscribed from button for step: {step.stepName}");
        }
    }

    public override void Cleanup()
    {
        LogDebug("🚀 Cleaning up teleport step handler");

        // Stop all active steps
        var activeSteps = new List<InteractionStep>(activeStepTeleports.Keys);
        foreach (var step in activeSteps)
        {
            StopStep(step);
        }

        teleportControllers.Clear();
        wristButtons.Clear();
        buttonEventDelegates.Clear();

        base.Cleanup();
    }

    void CacheTeleportControllers()
    {
        teleportControllers.Clear();
        var controllers = FindObjectsOfType<TeleportController>();
        foreach (var controller in controllers)
        {
            teleportControllers[controller.gameObject] = controller;
        }
        LogInfo($"🚀 Cached {teleportControllers.Count} TeleportController components");
    }

    void CacheWristButtons()
    {
        wristButtons.Clear();
        var buttons = FindObjectsOfType<WristUIButton>();
        foreach (var button in buttons)
        {
            wristButtons[button.gameObject] = button;
        }
        LogInfo($"🚀 Cached {wristButtons.Count} WristUIButton components");
    }

    void OnTeleportButtonPressed(InteractionStep step, TeleportController teleportController)
    {
        if (step.isCompleted) return;

        LogDebug($"🚀 Button pressed! Executing teleport: {step.stepName}");

        // Execute teleport
        teleportController.ExecuteTeleport();

        // Complete step
        CompleteStep(step, $"Teleported to {teleportController.name}");
    }
}
```

**Key Points:**
- Inherit from `BaseAutoHandsStepHandler` (or `BaseStepHandler` for XRI)
- Override `CanHandle()` to return true for your step type
- Cache components in `Awake()` or `Initialize()`
- Subscribe to events in `StartStep()`
- Unsubscribe in `StopStep()` - **CRITICAL** to prevent memory leaks
- Call `CompleteStep(step, reason)` when done
- Use dictionaries to track active steps

---

### Step 8: Register Handler in ModularTrainingSequenceController

**File:** `Assets/VRTrainingKit/Scripts/Core/Controllers/ModularTrainingSequenceController.cs`

**Location:** In `CreateDefaultAutoHandsHandlers()` method (line ~241)

**What to add:**
```csharp
void CreateDefaultAutoHandsHandlers()
{
    LogInfo("🏗️ Creating default AutoHands handlers...");

    var grabHandler = new GameObject("AutoHandsGrabStepHandler").AddComponent<AutoHandsGrabStepHandler>();
    var snapHandler = new GameObject("AutoHandsSnapStepHandler").AddComponent<AutoHandsSnapStepHandler>();
    // ... existing handlers ...
    var teleportHandler = new GameObject("AutoHandsTeleportStepHandler")
        .AddComponent<AutoHandsTeleportStepHandler>();  // ← ADD THIS

    // Set as children
    grabHandler.transform.SetParent(transform);
    // ... existing SetParent calls ...
    teleportHandler.transform.SetParent(transform);  // ← ADD THIS

    // Register handlers
    RegisterHandler(grabHandler);
    // ... existing RegisterHandler calls ...
    RegisterHandler(teleportHandler);  // ← ADD THIS
}
```

**Pattern:** Always add in all three places (create, SetParent, RegisterHandler)

---

### ✅ Part A Complete!

At this point, you should be able to:
- ✅ Compile without errors
- ✅ See Teleport in step type dropdown
- ✅ Create profile assets
- ✅ Apply profiles to tagged objects

---

## Part B: Connecting with UI

This is the **trickiest part** with multiple integration points across the editor window.

---

### Step 9: Add Profile Field Variables

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**Location:** Line ~30-50 (private field declarations)

**What to add:**
```csharp
// Around line 37
private InteractionProfile selectedTurnProfile;
private InteractionProfile selectedTeleportProfile;  // ← ADD THIS
private Vector2 configScrollPos;

// Around line 47
private List<InteractionProfile> cachedTurnProfiles;
private List<InteractionProfile> cachedTeleportProfiles;  // ← ADD THIS
```

**Pattern:** Add both `selectedXProfile` and `cachedXProfiles` fields

---

### Step 10: Add Profile Cache Refresh

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**Location A:** In `RefreshProfileCaches()` method (line ~391)

```csharp
private void RefreshProfileCaches()
{
    RefreshGrabProfileCache();
    RefreshKnobProfileCache();
    // ... existing calls ...
    RefreshTeleportProfileCache();  // ← ADD THIS
}
```

**Location B:** Create new cache method after existing cache methods (line ~554)

```csharp
private void RefreshTeleportProfileCache()
{
    cachedTeleportProfiles = new List<InteractionProfile>();

    string[] autoHandsTeleportGuids = AssetDatabase.FindAssets("t:AutoHandsTeleportProfile");
    foreach (string guid in autoHandsTeleportGuids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
        if (profile != null && IsTeleportProfile(profile))
        {
            cachedTeleportProfiles.Add(profile);
        }
    }
}
```

**Location C:** Create helper method at end of file (line ~3140)

```csharp
private bool IsTeleportProfile(InteractionProfile profile)
{
    return profile != null && profile.GetType().Name.Contains("Teleport");
}
```

**Pattern:** Three locations - call in RefreshProfileCaches, create Refresh method, create Is*Profile helper

---

### Step 11: Add Setup Tab UI

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**Location:** In `DrawSetupTab()` method, after existing DrawObjectGroup calls (line ~724)

```csharp
// Turn objects
DrawObjectGroup("Turn Objects", sceneAnalysis.turnObjects, "turn", selectedTurnProfile);
EditorGUILayout.Space(10);

// Teleport points - ADD THIS ENTIRE SECTION
DrawObjectGroup("🚀 Teleport Points", sceneAnalysis.teleportObjects, "teleportPoint", selectedTeleportProfile);

EditorGUILayout.EndScrollView();
```

**Visual Output:** This adds a collapsible section showing all tagged teleport points

---

### Step 12: Add Configure Tab UI

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**Location:** In `DrawConfigureTab()` method, before `EditorGUILayout.EndScrollView()` (line ~1378)

```csharp
EditorGUILayout.EndVertical();  // End of Turn Profile section

EditorGUILayout.Space(10);

// Teleport Profile - ADD THIS ENTIRE SECTION
EditorGUILayout.BeginVertical("box");
EditorGUILayout.LabelField("🚀 Teleport Profile", subHeaderStyle);
var teleportProfileTemp = EditorGUILayout.ObjectField(
    "Profile Asset", selectedTeleportProfile, typeof(InteractionProfile), false) as InteractionProfile;

if (teleportProfileTemp != null && IsTeleportProfile(teleportProfileTemp))
{
    selectedTeleportProfile = teleportProfileTemp;
}
else if (teleportProfileTemp != null)
{
    EditorUtility.DisplayDialog("Invalid Profile Type",
        $"The selected profile '{teleportProfileTemp.name}' is not a teleport-type profile.", "OK");
}

if (selectedTeleportProfile == null)
{
    if (cachedTeleportProfiles != null && cachedTeleportProfiles.Count > 0)
    {
        EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
        foreach (var profile in cachedTeleportProfiles)
        {
            if (profile != null)
            {
                EditorGUILayout.BeginHorizontal();
                string frameworkType = GetProfileFrameworkType(profile);
                EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedTeleportProfile = profile;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
    else
    {
        EditorGUILayout.HelpBox("No teleport profiles found in project.", MessageType.Info);
    }

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Create New Teleport Profile"))
    {
        CreateNewProfile<AutoHandsTeleportProfile>("TeleportProfile");
        RefreshTeleportProfileCache();
    }
    if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
    {
        RefreshTeleportProfileCache();
    }
    EditorGUILayout.EndHorizontal();
}
else
{
    if (GUILayout.Button("Edit Profile"))
    {
        Selection.activeObject = selectedTeleportProfile;
    }
}
EditorGUILayout.EndVertical();

EditorGUILayout.EndScrollView();  // ← This should be right after
```

**Visual Output:** Adds profile selection dropdown, create button, and available profiles list

---

### Step 13: Add Sequence Tab Icon

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**Location:** In `GetStepTypeIcon()` method (line ~1803)

```csharp
private string GetStepTypeIcon(InteractionStep.StepType stepType)
{
    switch (stepType)
    {
        case InteractionStep.StepType.Grab: return "✋";
        case InteractionStep.StepType.GrabAndSnap: return "🔗";
        // ... existing cases ...
        case InteractionStep.StepType.WaitForScriptCondition: return "⚙️";
        case InteractionStep.StepType.Teleport: return "🚀";  // ← ADD THIS
        default: return "❓";
    }
}
```

**Visual Output:** Shows 🚀 icon in tree view for teleport steps

---

### Step 14: Add Sequence Tab Properties Panel

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**Location:** In `DrawStepProperties()` method, after valve section and before "Execution Settings" (line ~2177)

```csharp
        }  // End of valve section
    }

    // Teleport-specific settings - ADD THIS ENTIRE SECTION
    if (step.type == InteractionStep.StepType.Teleport)
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
                string info = $"✅ TeleportController found\n" +
                             $"Recentering: {(teleportController.enableRecentering ? "Enabled" : "Disabled")}\n" +
                             $"Preview: {(teleportController.showDestinationPreview ? "Visible" : "Hidden")}";
                if (teleportController.autoHandPlayerReference == null)
                {
                    info += "\n⚠️ AutoHandPlayer reference not set on controller!";
                }
                EditorGUILayout.HelpBox(info, teleportController.autoHandPlayerReference == null ? MessageType.Warning : MessageType.Info);
            }

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

    // Execution settings
    EditorGUILayout.Space(10);
    EditorGUILayout.LabelField("Execution Settings", EditorStyles.boldLabel);
```

**Visual Output:**
- GameObject pickers for button and destination
- Real-time component validation
- Controller status display
- Recentering toggle and slider

---

### ✅ Part B Complete!

At this point, the UI integration is complete across all tabs!

---

## Testing Checklist

### Phase 1: Compilation & Asset Creation
- [ ] Project compiles without errors
- [ ] Can create profile via Create menu (right-click → Create → VR Training → AutoHands → Teleport Profile)
- [ ] Profile shows all fields in Inspector
- [ ] Can create Unity tag "teleportPoint"

### Phase 2: Setup Tab
- [ ] Tag GameObject as "teleportPoint"
- [ ] Open Setup Assistant → Setup tab
- [ ] Click "Scan Scene"
- [ ] See "🚀 Teleport Points" section with object listed
- [ ] Select profile from dropdown
- [ ] Click Apply button
- [ ] TeleportController component added to GameObject

### Phase 3: Configure Tab
- [ ] Open Configure tab
- [ ] See "🚀 Teleport Profile" section at bottom
- [ ] Click "Create New Teleport Profile" works
- [ ] Profile appears in available profiles list
- [ ] Can select profile from list
- [ ] "Edit Profile" button selects asset in Project window

### Phase 4: Sequence Tab - Tree View
- [ ] Open Sequence tab
- [ ] Create or load TrainingSequenceAsset
- [ ] Add step, set type to Teleport
- [ ] 🚀 icon appears in tree view
- [ ] Step shows in hierarchy

### Phase 5: Sequence Tab - Properties Panel
- [ ] Select teleport step in tree view
- [ ] Properties panel shows "🚀 Teleport Settings" header
- [ ] Can drag GameObject to "Wrist Button" field
- [ ] Validation shows ✅ or ⚠️ for WristUIButton component
- [ ] Can drag GameObject to "Destination" field
- [ ] Validation shows ✅ or ⚠️ for TeleportController
- [ ] Recentering toggle works
- [ ] Delay slider appears when recentering enabled

### Phase 6: Runtime Execution
- [ ] Add ModularTrainingSequenceController to scene
- [ ] Assign TrainingSequenceAsset
- [ ] Enter Play mode
- [ ] Console shows "🚀 AutoHandsTeleportStepHandler initialized"
- [ ] Console shows cached component counts
- [ ] Trigger interaction (e.g., press button)
- [ ] Controller.Execute*() method is called
- [ ] Step completes and advances to next step

---

## Troubleshooting

### Common Issues

**Issue:** "Type or namespace not found" errors
- **Solution:** Ensure NO NAMESPACE declaration in new scripts (follow project pattern)

**Issue:** Profile doesn't appear in Configure tab
- **Solution:** Check `RefreshTeleportProfileCache()` is called in `RefreshProfileCaches()`
- **Solution:** Check `IsTeleportProfile()` helper method exists

**Issue:** Step properties don't show in Sequence tab
- **Solution:** Verify `if (step.type == InteractionStep.StepType.Teleport)` condition in `DrawStepProperties()`
- **Solution:** Check step type icon added to `GetStepTypeIcon()`

**Issue:** Handler doesn't execute at runtime
- **Solution:** Verify handler registered in `CreateDefaultAutoHandsHandlers()`
- **Solution:** Check `CanHandle()` returns true for your step type
- **Solution:** Verify components are cached in `Awake()`

**Issue:** GameObjectReference shows as "Missing"
- **Solution:** Use `GameObjectReference` type, not `GameObject` directly
- **Solution:** Check object is in active scene (not prefab)
- **Solution:** Verify object hasn't been renamed/deleted

---

## File Summary

### Files Created (3)
1. `AutoHandsTeleportProfile.cs` - Profile ScriptableObject
2. `TeleportController.cs` - Runtime controller component
3. `AutoHandsTeleportStepHandler.cs` - Step handler

### Files Modified (4)
1. `TrainingSequence.cs` - Enum, fields, validation (3 locations)
2. `InteractionSetupService.cs` - Scene scanning (3 locations)
3. `ModularTrainingSequenceController.cs` - Handler registration (3 locations)
4. `VRInteractionSetupWindow.cs` - UI integration (7 locations)

---

## Quick Reference: UI Integration Locations

**VRInteractionSetupWindow.cs** - All locations to modify:

| Location | Line # | What to Add |
|----------|--------|-------------|
| Field declarations | ~38 | `selectedTeleportProfile` |
| Field declarations | ~48 | `cachedTeleportProfiles` |
| RefreshProfileCaches() | ~399 | Call to `RefreshTeleportProfileCache()` |
| New method | ~556 | `RefreshTeleportProfileCache()` method |
| Helper method | ~3146 | `IsTeleportProfile()` method |
| DrawSetupTab() | ~728 | DrawObjectGroup call |
| DrawConfigureTab() | ~1382 | Teleport profile section (60 lines) |
| GetStepTypeIcon() | ~1817 | Teleport icon case |
| DrawStepProperties() | ~2178 | Teleport properties section (80 lines) |

---

## Tips for AI Code Generation

When asking AI to create a new profile, provide:
1. **This document** as context
2. **Step type name** (e.g., "ButtonPress", "SliderAdjust")
3. **Required components** (e.g., "needs Button component")
4. **Interaction trigger** (e.g., "completes when button clicked")
5. **Framework** (AutoHands or XRI)
6. **Tag name** (e.g., "pressButton", "snapPoint")

**Example Prompt:**
```
Using the VRTrainingKit guide, create a "ButtonPress" interaction profile for AutoHands:
- Step type: ButtonPress
- Tag: pressButton
- Required component: UnityEngine.UI.Button
- Completion trigger: Button.onClick event
- Controller method: ActivateButton()
```

---

## Changelog

**Version 1.0** (December 2024)
- Initial document creation
- Based on Teleport interaction implementation
- Covers AutoHands framework patterns

---

**End of Document**

For questions or issues, refer to existing implementations in the VRTrainingKit codebase or consult the project's CLAUDE.md file.

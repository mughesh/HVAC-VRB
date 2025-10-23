# TurnByCount Implementation Plan - FINAL

## Architecture Decision

**Approach:** Create standalone TurnByCountProfile following AutoHandsValveControllerV2 pattern

### Clear Object Categorization

| Object Type | Example | Interaction Profile | Steps Required |
|-------------|---------|-------------------|----------------|
| **Stationary rotatable** | Cylinder valve, LP knob, GSV/LSV (without tool) | `KnobProfile` (existing) | 1 step: TurnKnob |
| **Moveable turnable** | Allen key, tools | `TurnByCountProfile` (NEW) | 2 steps: GrabAndSnap → TurnByCount |

### Two-Step Workflow Example

**Opening LSV with Allen Key (0.5 turns clockwise):**

```
Step 1 (existing): GrabAndSnap
  ├── Object: Allen key
  ├── Destination: LSV socket (created by SnapProfile)
  └── Result: Allen key snaps to LSV, attached via PlacePoint

Step 2 (NEW): TurnByCount
  ├── Object: Allen key (already in socket)
  ├── Turn count: 0.5 turns
  ├── Direction: Clockwise
  ├── Result: HingeJoint created, user rotates, step completes at 180°
  └── Cleanup: HingeJoint destroyed on completion
```

---

## Components to Create

### 1. **TurnByCountProfile.cs** (ScriptableObject)
Pattern: Simplified ValveProfile

```csharp
[CreateAssetMenu(fileName = "New Turn By Count Profile",
                 menuName = "VR Training/Turn By Count Profile")]
public class TurnByCountProfile : InteractionProfile
{
    [Header("Rotation Settings")]
    public float turnCount = 1.0f;
    public RotationDirection direction = RotationDirection.Clockwise;
    public Vector3 rotationAxis = Vector3.up;
    public float angleTolerance = 15f;

    [Header("HingeJoint Physics")]
    public bool useSpring = true;
    public float springValue = 50f;
    public float springDamper = 10f;
    public float springTargetPosition = 0f;
    public bool autoConfigureConnectedAnchor = true;

    [Header("Socket Compatibility (from SnapProfile)")]
    public string[] compatibleSocketTags = { "TurnSocket" };

    public enum RotationDirection { Clockwise, AntiClockwise }

    public override void ApplyToGameObject(GameObject target) {
        // Add AutoHandsTurnByCountController
        // Configure with this profile
    }
}
```

**Key Differences from ValveProfile:**
- ✅ Simpler: Only `turnCount` + `direction` (no tighten/loosen thresholds)
- ✅ No state materials (no looseMaterial, tightMaterial)
- ✅ No positioning validation (PlacePoint handles that)
- ✅ Cleaner intent (turn X times, not tighten/loosen workflow)

---

### 2. **AutoHandsTurnByCountController.cs** (Runtime Component)
Pattern: Simplified AutoHandsValveControllerV2

```csharp
public class AutoHandsTurnByCountController : MonoBehaviour
{
    [SerializeField] private TurnByCountProfile profile;

    // States: Unlocked → Locked → Unlocked (simpler than Valve)
    private enum State { Unlocked, Locked }
    private State currentState = State.Unlocked;

    // Components
    private Autohand.Grabbable grabbable;
    private HingeJoint hingeJoint;
    private Component currentPlacePoint;

    // Rotation tracking
    private float currentRotation = 0f;

    // Events
    public event Action OnTurnComplete;

    // Lifecycle:
    // 1. OnSocketSnapped → WaitAndAddHingeJoint()
    // 2. AddHingeJoint() → Track rotation
    // 3. CheckCompletion() → Fire OnTurnComplete
    // 4. OnTurnComplete (handler) → RemoveHingeJoint() → State.Unlocked
}
```

**Key Simplifications from ValveControllerV2:**
- ✅ No Tight/Loose substates (just Unlocked/Locked)
- ✅ One event: OnTurnComplete (not Tightened/Loosened)
- ✅ One threshold: `turnCount * 360°` (not two thresholds)
- ✅ No removal logic (handler controls when HingeJoint is destroyed)

---

### 3. **AutoHandsTurnByCountStepHandler.cs** (Step Handler)

```csharp
public class AutoHandsTurnByCountStepHandler : BaseAutoHandsStepHandler
{
    private Dictionary<InteractionStep, AutoHandsTurnByCountController> activeSteps;

    public override bool CanHandle(InteractionStep.StepType stepType) {
        return stepType == InteractionStep.StepType.TurnByCount;
    }

    public override void StartStep(InteractionStep step) {
        // 1. Get target object (should already be in socket from previous GrabAndSnap step)
        // 2. Find/add AutoHandsTurnByCountController
        // 3. Configure controller with step parameters (override profile if needed)
        // 4. Subscribe to OnTurnComplete
    }

    void OnTurnComplete(InteractionStep step, AutoHandsTurnByCountController controller) {
        CompleteStep(step, $"Turned {step.turnCount} turns {step.direction}");
        // Optional: Remove HingeJoint here if needed
    }
}
```

---

## Phased Implementation (4-5 hours)

### **PHASE 1: TurnByCountProfile ScriptableObject** (45 min)

**Files to Create:**
- `Assets/VRTrainingKit/Scripts/Profiles/Implementations/AutoHands/TurnByCountProfile.cs`

**Tasks:**
1. Create TurnByCountProfile class extending InteractionProfile
2. Add fields (turnCount, direction, rotationAxis, angleTolerance)
3. Add HingeJoint physics settings (copy from ValveProfile)
4. Add socket compatibility settings
5. Implement ApplyToGameObject() (adds AutoHandsTurnByCountController)
6. Implement ValidateGameObject()
7. Add [CreateAssetMenu] attribute

**Test Checkpoint:**
- [ ] Create TurnByCount profile asset in Unity
- [ ] Set turnCount = 0.5, direction = Clockwise
- [ ] Verify inspector shows all fields
- [ ] Save asset

**Code Structure:**
```csharp
// TurnByCountProfile.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Turn By Count Profile",
                 menuName = "VR Training/AutoHands/Turn By Count Profile")]
public class TurnByCountProfile : InteractionProfile
{
    public enum RotationDirection { Clockwise, AntiClockwise }

    [Header("Turn Settings")]
    [Tooltip("Number of complete rotations required (0.5 = 180°, 1.0 = 360°)")]
    [Range(0.1f, 10f)]
    public float turnCount = 1.0f;

    [Tooltip("Direction of rotation required")]
    public RotationDirection direction = RotationDirection.Clockwise;

    [Tooltip("Axis around which object rotates (usually Y-axis for vertical rotation)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Angle tolerance in degrees (allows some wiggle room)")]
    [Range(1f, 45f)]
    public float angleTolerance = 15f;

    [Header("HingeJoint Physics Settings")]
    // ... copy from ValveProfile

    [Header("Socket Compatibility")]
    public string[] compatibleSocketTags = { "TurnSocket" };

    public override void ApplyToGameObject(GameObject target)
    {
        // 1. Add Grabbable (if not exists)
        // 2. Add Rigidbody (if not exists)
        // 3. Add AutoHandsTurnByCountController
        // 4. Call controller.Configure(this)
    }

    public override bool ValidateGameObject(GameObject target)
    {
        // Check for required components
        return target.GetComponent<Autohand.Grabbable>() != null;
    }
}
```

---

### **PHASE 2: AutoHandsTurnByCountController** (2 hours)

**Files to Create:**
- `Assets/VRTrainingKit/Scripts/Core/Controllers/AutoHandsTurnByCountController.cs`

**Tasks:**
1. Copy AutoHandsValveControllerV2.cs as starting template
2. Simplify state machine (remove Tight/Loose substates)
3. Remove tighten/loosen logic, keep only turn completion logic
4. Remove looseMaterial, tightMaterial references
5. Update CheckRotationThreshold() to single threshold
6. Rename events to OnTurnComplete
7. Test with simple scene

**Key Methods:**
```csharp
// Core lifecycle methods
private void OnSocketSnapped(Component placePoint, Component snappedGrabbable)
private IEnumerator WaitAndAddHingeJoint()
private void AddHingeJoint()
private void RemoveHingeJoint()
private void OnSocketUnsnapped(Component placePoint, Component removedGrabbable)

// Rotation tracking
private void Update()  // Tracks rotation while grabbed
private void TrackRotation()
private void CheckRotationThreshold()  // Single threshold check

// Event
public event Action OnTurnComplete;
```

**Rotation Logic:**
```csharp
void CheckRotationThreshold()
{
    if (profile == null || hingeJoint == null) return;

    float currentAngle = hingeJoint.angle;
    float targetAngle = profile.turnCount * 360f;

    // Check direction
    bool reachedTarget = false;

    if (profile.direction == TurnByCountProfile.RotationDirection.Clockwise) {
        // Clockwise = negative angle in Unity
        reachedTarget = currentAngle <= -(targetAngle - profile.angleTolerance);
    } else {
        // AntiClockwise = positive angle
        reachedTarget = currentAngle >= (targetAngle - profile.angleTolerance);
    }

    if (reachedTarget) {
        Debug.Log($"✅ Turn complete! Angle: {currentAngle:F1}°");
        OnTurnComplete?.Invoke();
    }
}
```

**Test Checkpoint:**
- [ ] Create test scene with allen key + LSV socket
- [ ] Manually snap allen key to socket
- [ ] Verify HingeJoint is created
- [ ] Grab and rotate 180° clockwise
- [ ] Verify OnTurnComplete fires in console
- [ ] Test anti-clockwise direction
- [ ] Test 1 full turn
- [ ] Test tolerance (rotate 345°, should complete with 15° tolerance)

---

### **PHASE 3: TurnByCountStepHandler** (1 hour)

**Files to Create:**
- `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsTurnByCountStepHandler.cs`

**Tasks:**
1. Copy AutoHandsValveStepHandler.cs as template
2. Simplify (remove tighten/loosen event subscriptions)
3. Subscribe only to OnTurnComplete
4. Handle controller initialization if object doesn't have controller yet
5. Apply step parameter overrides if needed
6. Register in ModularTrainingSequenceController

**Code Structure:**
```csharp
public class AutoHandsTurnByCountStepHandler : BaseAutoHandsStepHandler
{
    private Dictionary<GameObject, AutoHandsTurnByCountController> controllerCache;
    private Dictionary<InteractionStep, AutoHandsTurnByCountController> activeSteps;
    private Dictionary<AutoHandsTurnByCountController, System.Action> eventDelegates;

    public override bool CanHandle(InteractionStep.StepType stepType) {
        return stepType == InteractionStep.StepType.TurnByCount;
    }

    public override void StartStep(InteractionStep step)
    {
        var targetObject = step.targetObject.GameObject;

        // Get or add controller
        var controller = GetOrAddController(targetObject);

        // Apply step parameter overrides (if step has custom turnCount, etc.)
        ApplyStepOverrides(controller, step);

        // Subscribe to OnTurnComplete
        System.Action completeDelegate = () => OnTurnComplete(step, controller);
        eventDelegates[controller] = completeDelegate;
        controller.OnTurnComplete += completeDelegate;

        activeSteps[step] = controller;
    }

    void OnTurnComplete(InteractionStep step, AutoHandsTurnByCountController controller)
    {
        if (step.isCompleted) return;

        LogInfo($"✅ Turn complete: {step.stepName}");
        CompleteStep(step, $"Turned {step.turnCount} turns {step.rotationDirection}");
    }
}
```

**Test Checkpoint:**
- [ ] Create TrainingSequenceAsset with 2 steps:
  - Step 1: GrabAndSnap (allen key → LSV socket)
  - Step 2: TurnByCount (0.5 turns clockwise)
- [ ] Add ModularTrainingSequenceController to scene
- [ ] Assign sequence asset
- [ ] Play scene
- [ ] Complete step 1 (snap allen key)
- [ ] Verify HingeJoint appears
- [ ] Rotate 180° clockwise
- [ ] Verify step 2 completes
- [ ] Check console logs for confirmation

---

### **PHASE 4: Integration & GUI Support** (1 hour)

**Tasks:**
1. Add TurnByCount to InteractionStep.StepType enum
2. Add rotationDirection, turnCount, rotationTolerance fields to InteractionStep
3. Add GUI fields in VRInteractionSetupWindow
4. Register handler in ModularTrainingSequenceController.InitializeHandlers()
5. Create default TurnByCount profile in Resources folder

**Files to Modify:**
- `TrainingSequence.cs` - Add enum + fields
- `VRInteractionSetupWindow.cs` - Add GUI controls
- `ModularTrainingSequenceController.cs` - Register handler

**GUI Code:**
```csharp
// In VRInteractionSetupWindow.cs - Properties Panel
if (selectedStep.stepType == InteractionStep.StepType.TurnByCount) {
    EditorGUILayout.LabelField("Turn Settings", EditorStyles.boldLabel);

    selectedStep.rotationDirection = (InteractionStep.RotationDirection)
        EditorGUILayout.EnumPopup("Direction", selectedStep.rotationDirection);

    selectedStep.turnCount = EditorGUILayout.Slider("Turn Count",
        selectedStep.turnCount, 0.1f, 10f);

    selectedStep.rotationTolerance = EditorGUILayout.FloatField(
        "Tolerance (degrees)", selectedStep.rotationTolerance);

    EditorGUILayout.HelpBox(
        $"Rotate {selectedStep.turnCount} turn(s) {selectedStep.rotationDirection}\n" +
        $"({selectedStep.turnCount * 360f}° ± {selectedStep.rotationTolerance}°)",
        MessageType.Info);
}
```

**Test Checkpoint:**
- [ ] Open Sequence tab in Setup Assistant
- [ ] Create new TurnByCount step in GUI
- [ ] Verify direction dropdown works
- [ ] Verify turnCount slider works
- [ ] Set turnCount = 0.5, save asset
- [ ] Reload asset, verify values persist
- [ ] Create full allen key workflow sequence (GrabAndSnap + TurnByCount)

---

### **PHASE 5: Module Integration** (1 hour)

**Tasks:**
1. Update Blended Refrigerant Charging sequence with TurnByCount steps
2. Test all valve operations (LSV, GSV)
3. Test cylinder valve with KnobProfile (verify it still works)
4. Full integration test of Hose Connection phase

**Updated Step Breakdown:**

| Step | Description | Interaction Type | Notes |
|------|-------------|------------------|-------|
| 1 | Remove LSV cap | GrabAndSnap | Cap → table |
| 2a | Grab allen key | Grab | Pick up tool |
| 2b | Snap allen key to LSV | GrabAndSnap | Allen key → LSV socket |
| 2c | Turn allen key clockwise | **TurnByCount** | **0.5 turns, NEW** |
| 2d | Remove allen key | Grab | Take off LSV |
| 3 | Remove GSV cap | GrabAndSnap | Cap → table |
| 4a-d | Same pattern for GSV | Grab + GrabAndSnap + **TurnByCount** + Grab | |
| 11 | Open cylinder valve | **TurnKnob** | Use existing KnobProfile |
| 18 | Open LP knob | **TurnKnob** | Use existing KnobProfile |

**Test Checkpoint:**
- [ ] Full playthrough of Hose Connection phase (steps 1-10)
- [ ] Verify allen key snap-turn-remove workflow
- [ ] Test cylinder valve turn (existing KnobProfile)
- [ ] Verify no conflicts between TurnByCount and TurnKnob

---

## File Structure

```
Assets/VRTrainingKit/
├── Scripts/
│   ├── Profiles/
│   │   ├── Base/
│   │   │   └── InteractionProfile.cs (existing)
│   │   └── Implementations/
│   │       └── AutoHands/
│   │           ├── TurnByCountProfile.cs (NEW)
│   │           ├── AutoHandsValveProfile.cs (existing)
│   │           └── AutoHandsKnobProfile.cs (existing)
│   ├── Core/
│   │   └── Controllers/
│   │       ├── AutoHandsTurnByCountController.cs (NEW)
│   │       ├── AutoHandsValveControllerV2.cs (existing - reference)
│   │       └── AutoHandsKnobController.cs (existing - for stationary objects)
│   ├── StepHandlers/
│   │   ├── AutoHandsTurnByCountStepHandler.cs (NEW)
│   │   ├── AutoHandsValveStepHandler.cs (existing - reference)
│   │   └── AutoHandsKnobStepHandler.cs (existing)
│   └── SequenceSystem/
│       └── Data/
│           └── TrainingSequence.cs (modify - add enum + fields)
└── Resources/
    └── AutoHands - Profiles/
        └── DefaultTurnByCountProfile.asset (NEW)
```

---

## Decision Summary

| Aspect | Decision |
|--------|----------|
| **Architecture** | Option 1: Standalone TurnByCountProfile + Controller |
| **Stationary objects** | Use existing KnobProfile (cylinder valve, LP knob) |
| **Moveable tools** | Use new TurnByCount (allen keys, tools with snap-first) |
| **Socket handling** | Use existing SnapProfile + PlacePoint |
| **Grab animations** | Default AutoHands behavior (don't over-engineer) |
| **State machine** | Simplified: Unlocked → Locked → Unlocked |
| **Events** | Single event: OnTurnComplete |
| **Pattern source** | Based on AutoHandsValveControllerV2 (proven, tested) |

---

## Estimated Time

| Phase | Time | Cumulative |
|-------|------|------------|
| Phase 1: Profile | 45 min | 45 min |
| Phase 2: Controller | 2 hours | 2h 45min |
| Phase 3: Handler | 1 hour | 3h 45min |
| Phase 4: Integration | 1 hour | 4h 45min |
| Phase 5: Module Testing | 1 hour | 5h 45min |
| **Total** | **~6 hours** | |

---

## Next Step

Ready to start **Phase 1: TurnByCountProfile**?

I can create the complete TurnByCountProfile.cs file following the exact pattern from ValveProfile with the simplifications we discussed.

Would you like me to:
1. **Start Phase 1 now** - Create TurnByCountProfile.cs
2. **Review this plan first** - Ask questions, clarify anything
3. **See code samples** - Show more detailed code before starting

Let me know and we'll proceed phase by phase with testing checkpoints! 🚀

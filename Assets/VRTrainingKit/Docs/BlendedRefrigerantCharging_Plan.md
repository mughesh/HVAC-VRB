# Blended Refrigerant Charging by Weight - Implementation Plan

## Module Overview

**Training Module:** Blended Refrigerant Charging by Weight (R-410A)
**Category:** Residential AC Service > Refrigerant Charging & Recovery
**Total Steps:** 33
**Sequential Flow:** Yes (no phase skipping allowed)
**Estimated Implementation Time:** 5 days

### Learning Objectives
- Train users on proper R-410A blended refrigerant charging procedure
- Emphasize liquid-phase charging (inverted cylinder) to maintain blend ratio
- Monitor charging by weight using digital scale (target: 0.95kg)
- Monitor pressure (target: 130 psi) and current (target: >7.6A)

---

## Equipment List

| Equipment | Asset Location | Notes |
|-----------|---------------|-------|
| R-410A Cylinder | `Assets/R410A Cylinder/` | Must be inverted on scale |
| Weighing Scale | TBD | Battery powered, displays weight in grams |
| Gauge Manifold | `Assets/gauge metere/` | R-410A readings, LP/HP knobs |
| Clamp Meter | TBD | Attaches to brown wire, shows current |
| Red Hose | TBD | LSV connection |
| Blue Hose | TBD | GSV connection |
| Yellow Hose | TBD | Cylinder to manifold |
| Allen Keys (×2) | TBD | For LSV & GSV operations |
| Indoor Unit | TBD | Wall-mounted |
| Outdoor Unit | `Assets/Outdoor Unit/` | De-cased, on table |
| Specification Sticker | TBD | On outdoor unit |

---

## Phase Breakdown & Step Mapping

### **Phase 1: Hose Connection** (Steps 1-10)
Setup hoses and prepare valves for refrigerant charging.

| # | Step Description | Interaction Type | Object(s) | Parameters | Notes |
|---|------------------|------------------|-----------|------------|-------|
| 1 | Remove LSV cap | `GrabAndSnap` | LSV Cap → Table/Aside | destination: table snap point | Simple cap removal |
| 2 | Open LSV with allen key | `TurnByCount` | LSV Valve | direction: Clockwise, count: 0.5 | **NEW TYPE** - Half turn to open |
| 3 | Remove GSV cap | `GrabAndSnap` | GSV Cap → Table/Aside | destination: table snap point | Simple cap removal |
| 4 | Open GSV with allen key | `TurnByCount` | GSV Valve | direction: Clockwise, count: 0.5 | **NEW TYPE** - Half turn to open |
| 5 | Connect male yellow hose to cylinder | `GrabAndSnap` | Yellow Hose Male → Cylinder Port | Auto-tighten on snap | Includes tightening hint |
| 6 | Connect female yellow hose to manifold | `GrabAndSnap` | Yellow Hose Female → Manifold Intermediate | Auto-tighten on snap | Includes tightening hint |
| 7 | Connect male blue hose to GSV | `GrabAndSnap` | Blue Hose Male → GSV Port | Auto-tighten on snap | Includes tightening hint |
| 8 | Connect female blue hose to manifold | `GrabAndSnap` | Blue Hose Female → Manifold Low Side | Auto-tighten on snap | Includes tightening hint |
| 9 | Connect male red hose to LSV | `GrabAndSnap` | Red Hose Male → LSV Port | Auto-tighten on snap | Includes tightening hint |
| 10 | Connect female red hose to manifold | `GrabAndSnap` | Red Hose Female → Manifold High Side | Auto-tighten on snap | Includes tightening hint |

**Phase Summary:**
- **Interaction Types Used:** GrabAndSnap (8), TurnByCount (2)
- **New Features Required:** TurnByCount step type
- **Visual Hints:** Display "Hint: To tighten, grab the end of the hose and rotate your hand clockwise" for steps 5-10

---

### **Phase 2: Purging** (Steps 11-17)
Remove air and contaminants from hoses and prepare for charging.

| # | Step Description | Interaction Type | Object(s) | Parameters | Notes |
|---|------------------|------------------|-----------|------------|-------|
| 11 | Open R-410A cylinder valve | `TurnByCount` | Cylinder Valve | direction: AntiClockwise, count: 1.0 | **NEW TYPE** - Full turn to open |
| 12 | Loosen yellow hose at manifold | `TurnByCount` | Yellow Hose Female | direction: AntiClockwise, count: 0.5 | **NEW TYPE** - Triggers purge VFX/SFX |
| 13 | Tighten yellow hose after 2 seconds | `TurnByCount` OR Auto | Yellow Hose Female | direction: Clockwise, count: 0.5 | Could auto-complete after timer |
| 14 | Turn on AC power switch | `WaitForScriptCondition` | AC Power Switch | condition: ButtonPressCondition | **NEW TYPE** - Button interaction |
| 15 | Press ON button on weighing scale | `WaitForScriptCondition` | Scale ON Button | condition: ButtonPressCondition | **NEW TYPE** - Scale powers on, shows 5000g |
| 16 | Press ZERO button on scale | `WaitForScriptCondition` | Scale ZERO Button | condition: ButtonPressCondition | **NEW TYPE** - Scale resets to 0g |
| 17 | Attach clamp meter to brown wire | `GrabAndSnap` | Clamp Meter → AC Wire | destination: brown wire snap point | Clamp snaps to wire |

**Phase Summary:**
- **Interaction Types Used:** TurnByCount (3), WaitForScriptCondition (3), GrabAndSnap (1)
- **New Features Required:** TurnByCount, WaitForScriptCondition, ButtonPressCondition script
- **VFX/SFX:** Purging mist effect on step 12, compressor sound on step 14, AC louver opens on step 14

---

### **Phase 3: Refrigerant Charging** (Steps 18-23)
Charge R-410A into system while monitoring weight, pressure, and current.

| # | Step Description | Interaction Type | Object(s) | Parameters | Notes |
|---|------------------|------------------|-----------|------------|-------|
| 18 | Open LP knob on gauge manifold | `TurnByCount` | LP Knob | direction: AntiClockwise, count: 1.0 | **NEW TYPE** - Opens flow to AC |
| 19 | Observe weight drop on scale | `ShowInstruction` OR `WaitForScriptCondition` | Scale Display | condition: TimedAutoCondition (2s) | Optional - could be just instruction + timer |
| 20 | Touch tube near AC valves | `WaitForScriptCondition` | Tube near LSV/GSV | condition: TouchInteractionCondition | **NEW TYPE** - Hand shows blue tinge on touch |
| 21 | Wait for pressure to reach 130 psi | `WaitForScriptCondition` | Gauge Manifold LP Gauge | condition: PressureThresholdCondition(130) | **NEW TYPE** - Monitors LP gauge reading |
| 22 | Wait for 0.95kg refrigerant charged | `WaitForScriptCondition` | Weighing Scale | condition: WeightThresholdCondition(0.95kg) | **NEW TYPE** - Scale shows -950g (0.95kg charged) |
| 23 | Check clamp meter reading | `ShowInstruction` OR `WaitForScriptCondition` | Clamp Meter | condition: CurrentThresholdCondition(7.6A) | Could be instruction + auto after 21 & 22 |

**Phase Summary:**
- **Interaction Types Used:** TurnByCount (1), WaitForScriptCondition (4-5), ShowInstruction (1-2)
- **New Features Required:** TurnByCount, WaitForScriptCondition, Multiple condition scripts
- **Condition Scripts Needed:** PressureThresholdCondition, WeightThresholdCondition, TouchInteractionCondition, (optional) CurrentThresholdCondition
- **Visual Feedback:** Hand blue tinge on step 20

---

### **Phase 4: Hose Disconnection** (Steps 24-33)
Safely close valves and disconnect all hoses.

| # | Step Description | Interaction Type | Object(s) | Parameters | Notes |
|---|------------------|------------------|-----------|------------|-------|
| 24 | Close R-410A cylinder valve | `TurnByCount` | Cylinder Valve | direction: Clockwise, count: 1.0 | **NEW TYPE** - Opposite of step 11 |
| 25 | Close LP knob on manifold | `TurnByCount` | LP Knob | direction: Clockwise, count: 1.0 | **NEW TYPE** - Opposite of step 18 |
| 26 | Close GSV with allen key | `TurnByCount` | GSV Valve | direction: AntiClockwise, count: 0.5 | **NEW TYPE** - Half turn to close |
| 27 | Close LSV with allen key | `TurnByCount` | LSV Valve | direction: AntiClockwise, count: 0.5 | **NEW TYPE** - Half turn to close |
| 28 | Remove male yellow hose | `Grab` | Yellow Hose Male | - | Just grab to remove (no snap needed) |
| 29 | Remove female yellow hose | `Grab` | Yellow Hose Female | - | Just grab to remove |
| 30 | Remove male red hose | `Grab` | Red Hose Male | - | Just grab to remove |
| 31 | Remove female red hose | `Grab` | Red Hose Female | - | Just grab to remove |
| 32 | Remove male blue hose | `Grab` | Blue Hose Male | - | Just grab to remove |
| 33 | Remove female blue hose | `Grab` | Blue Hose Female | - | Just grab to remove |

**Phase Summary:**
- **Interaction Types Used:** TurnByCount (4), Grab (6)
- **New Features Required:** TurnByCount
- **Visual Hints:** Display "Hint: To loosen, grab the end of the hose and rotate your hand anti-clockwise" for steps 28-33

---

## Implementation Strategy

### New Features Required

#### 1. **TurnByCount Step Type** (Priority: HIGH)

**Purpose:** Complete step based on rotation count in a specific direction, not angle thresholds.

**Use Cases:**
- Valve operations (0.5 turn, 1 turn)
- Knob rotations
- Hose loosening/tightening (optional - could be auto on snap/grab)

**Data Structure:**
```csharp
// Add to InteractionStep.StepType enum
public enum StepType {
    // ... existing types
    TurnByCount,
}

// Add to InteractionStep class
public enum RotationDirection {
    Clockwise,
    AntiClockwise
}

[Header("Turn By Count Settings")]
public RotationDirection rotationDirection = RotationDirection.Clockwise;
[Range(0.1f, 10f)]
public float turnCount = 1.0f;
[Tooltip("Angle tolerance for completion (degrees)")]
public float rotationTolerance = 15f;  // Allows some wiggle room
```

**Implementation Approach:**
- **Option A:** Track cumulative rotation in one direction (ignore reverse rotation)
- **Option B:** Track net rotation (reverse rotation subtracts from progress)
- **Recommendation:** Option A for simpler UX (prevents accidental reversal penalties)

**Technical Details:**
- Based on AutoHandsValveController pattern
- May create HingeJoint at runtime (like valve controller)
- Or directly track hand rotation without joint (like knob controller)
- Handler monitors rotation and completes when `totalRotation >= (turnCount * 360f - tolerance)`

**Steps Using This Type:** 2, 4, 11, 12, 13, 18, 24, 25, 26, 27 (10 steps, 30% of module)

---

#### 2. **WaitForScriptCondition Step Type** (Priority: HIGH)

**Purpose:** Allow custom scripts to define step completion without creating new step types.

**Use Cases:**
- Button presses (AC switch, scale buttons)
- Sensor thresholds (weight, pressure, current)
- Custom interactions (touch detection, proximity)
- Timed auto-completion

**Data Structure:**
```csharp
// Add to InteractionStep.StepType enum
WaitForScriptCondition

// Create interface
public interface ISequenceCondition {
    bool IsConditionMet { get; }
    void ResetCondition();
    string GetStatusMessage();  // For debugging/UI
}

// InteractionStep uses existing targetObject field
// Handler finds ISequenceCondition component on targetObject
```

**Condition Scripts to Create:**
1. **ButtonPressCondition** - Generic button/switch interaction
2. **WeightThresholdCondition** - Monitor scale, complete when weight change reaches threshold
3. **PressureThresholdCondition** - Monitor gauge, complete when pressure reaches target
4. **CurrentThresholdCondition** - Monitor clamp meter, complete when current reaches target
5. **TouchInteractionCondition** - Complete when object is grabbed/touched
6. **TimedAutoCondition** - Auto-complete after X seconds (for observation steps)

**Steps Using This Type:** 14, 15, 16, 19, 20, 21, 22, 23 (8 steps, 24% of module)

---

### Interaction Type Usage Summary

| Interaction Type | Step Count | Percentage | Notes |
|------------------|-----------|------------|-------|
| **TurnByCount** (NEW) | 10 | 30% | Core new feature for valve/knob operations |
| **WaitForScriptCondition** (NEW) | 8 | 24% | Flexible system for custom interactions |
| **GrabAndSnap** | 9 | 27% | Existing - hose connections, cap removal, clamp attachment |
| **Grab** | 6 | 18% | Existing - hose removal |
| **ShowInstruction** | 0-2 | 0-6% | Existing - optional for observation steps |
| **Total** | 33 | 100% | |

**Key Insight:** 54% of steps require new features, 46% use existing framework.

---

## Simplification Decisions

### 1. Allen Key Interactions
**Decision:** Skip explicit allen key grab steps for speed.

**Implementation:**
- Make LSV/GSV directly rotatable without needing to grab allen key first
- More practical for VR training (focus on valve operation, not tool handling)
- Saves ~4 interaction steps

**Alternative (if time permits):**
- Add explicit "Grab allen key" steps before valve operations
- Adds realism but increases complexity

---

### 2. Hose Tightening/Loosening
**Decision:** Auto-tighten on snap, auto-loosen on grab.

**Implementation:**
- GrabAndSnap automatically includes tightening animation
- Grab automatically includes loosening animation
- Display hints but don't require explicit rotation interaction
- Saves ~12 TurnByCount steps

**Alternative (more realistic):**
- Separate snap and tighten steps
- Requires manual TurnByCount for each hose end
- Total would be 45+ steps instead of 33

---

### 3. Observation Steps (19, 23)
**Decision:** Use ShowInstruction + TimedAutoCondition for simple observations.

**Implementation:**
- Step 19 (Observe weight drop): ShowInstruction + 2s auto-complete
- Step 23 (Check clamp meter): ShowInstruction + auto-complete after steps 21-22 done
- Simpler than full sensor condition scripts

**Alternative:**
- Full WaitForScriptCondition with sensors
- More complex, not much added value for observation-only steps

---

## Phase-Based Implementation Plan

### **Phase 1: TurnByCount Data Structure** (Day 1, Morning)
- [ ] Add `TurnByCount` to `InteractionStep.StepType` enum
- [ ] Add `RotationDirection` enum to `TrainingSequence.cs`
- [ ] Add `rotationDirection`, `turnCount`, `rotationTolerance` fields to `InteractionStep`
- [ ] Update `InteractionStep.IsValid()` for TurnByCount validation
- [ ] Update `InteractionStep.GetValidationMessage()` for TurnByCount

**Testing:** GUI editor shows new fields, validation works.

---

### **Phase 2: AutoHandsTurnByCountHandler** (Day 1, Afternoon)
- [ ] Analyze `AutoHandsValveController` and `AutoHandsKnobController` for rotation tracking
- [ ] Create `AutoHandsTurnByCountHandler.cs` extending `BaseAutoHandsStepHandler`
- [ ] Implement rotation tracking (cumulative, direction-aware)
- [ ] Implement completion logic (when rotation >= target)
- [ ] Add haptic feedback on completion
- [ ] Register handler in `ModularTrainingSequenceController.InitializeHandlers()`

**Testing:** Create simple test scene with one turnable object, test 0.5, 1.0, 2.0 turn counts.

---

### **Phase 3: TurnByCount GUI Support** (Day 1, Evening)
- [ ] Update `VRInteractionSetupWindow` Sequence tab to show TurnByCount properties
- [ ] Add direction dropdown (Clockwise/AntiClockwise)
- [ ] Add turnCount slider
- [ ] Add rotationTolerance field

**Testing:** Create steps in GUI, save asset, validate data.

---

### **Phase 4: WaitForScriptCondition Structure** (Day 2, Morning)
- [ ] Create `ISequenceCondition.cs` interface
- [ ] Add `WaitForScriptCondition` to `InteractionStep.StepType` enum
- [ ] Create `WaitForScriptConditionHandler.cs` extending `BaseAutoHandsStepHandler`
- [ ] Implement condition polling and completion
- [ ] Register handler in `ModularTrainingSequenceController`

**Testing:** Create dummy condition script, test step completion.

---

### **Phase 5: Condition Scripts** (Day 2, Afternoon-Evening)
- [ ] Create `ButtonPressCondition.cs` - For AC switch, scale buttons
- [ ] Create `WeightThresholdCondition.cs` - For scale monitoring
- [ ] Create `PressureThresholdCondition.cs` - For gauge monitoring
- [ ] Create `TouchInteractionCondition.cs` - For tube touch step
- [ ] Create `TimedAutoCondition.cs` - For observation steps
- [ ] (Optional) Create `CurrentThresholdCondition.cs` - For clamp meter

**Testing:** Test each script individually with WaitForScriptCondition handler.

---

### **Phase 6: Build Sequence Asset** (Day 3)
- [ ] Use GUI Sequence tab to create "Blended Refrigerant Charging" program
- [ ] Create 4 modules (Hose Connection, Purging, Refrigerant Charging, Disconnection)
- [ ] Add all 33 steps with proper parameters
- [ ] Validate asset (check for errors/warnings)
- [ ] Save as `BlendedRefrigerantCharging.asset`

**Testing:** Load asset in ModularTrainingSequenceController, verify steps load correctly.

---

### **Phase 7: Scene Setup & Integration** (Day 4)
- [ ] Set up equipment in scene (cylinder, scale, manifold, AC units, hoses)
- [ ] Tag objects appropriately
- [ ] Attach condition scripts to relevant objects
- [ ] Configure snap points for hose connections
- [ ] Set up instruction board and menu board
- [ ] Assign sequence asset to controller

**Testing:** Full walkthrough, fix object placement and snapping issues.

---

### **Phase 8: Polish & VFX** (Day 5)
- [ ] Add purging mist effect (step 12)
- [ ] Add compressor sound (step 14)
- [ ] Add AC louver animation (step 14)
- [ ] Add hand blue tinge effect (step 20)
- [ ] Add scale display animations (steps 15-16, 19, 22)
- [ ] Add gauge pressure animations (step 21)
- [ ] Add clamp meter current display (step 21-23)
- [ ] Add success message UI
- [ ] Add tightening/loosening hints

**Testing:** Full playthrough with visual/audio feedback.

---

## Technical Notes & Considerations

### AutoHands Rotation Tracking Pattern
Based on existing AutoHands controllers:
- `AutoHandsValveController` creates HingeJoint at runtime
- Tracks angle via `HingeJoint.angle` or direct rotation comparison
- May use `Valve.rotationOffset` to track cumulative rotation
- Completion fires when angle threshold reached

**For TurnByCount:**
- Could create temporary HingeJoint with unlimited angle limits
- Track cumulative rotation in correct direction
- Destroy joint on step completion

### Rotation Direction Validation
```csharp
// In handler Update()
float currentAngle = GetCurrentAngle();
float deltaAngle = Mathf.DeltaAngle(previousAngle, currentAngle);

if (rotationDirection == Clockwise && deltaAngle < 0) {
    cumulativeRotation += Mathf.Abs(deltaAngle);
} else if (rotationDirection == AntiClockwise && deltaAngle > 0) {
    cumulativeRotation += deltaAngle;
}

if (cumulativeRotation >= targetRotation - tolerance) {
    CompleteStep();
}
```

### Hose Connection Auto-Tightening
- Could use SnapValidator's `OnObjectSnapped` event
- Play tightening animation on snap
- No separate TurnByCount needed
- Cleaner UX

### Scale & Gauge Display System
- May need dynamic UI canvas for scale display
- Gauge readings could use existing gauge manifold UI
- Consider TextMeshPro for numeric displays
- Update displays based on game state (weight decreases as refrigerant flows)

---

## Risk Mitigation

### Risk 1: Rotation Tracking Complexity
**Risk:** AutoHands rotation tracking may be complex or buggy.
**Mitigation:** Start with simple angle tracking, iterate based on testing.

### Risk 2: Condition Script Coupling
**Risk:** Condition scripts may need deep integration with game systems.
**Mitigation:** Keep scripts simple, use public properties for thresholds, mock data if needed.

### Risk 3: Timeline Pressure
**Risk:** 5 days is tight for full module with new features.
**Mitigation:** Prioritize core functionality, cut VFX/SFX if needed, use placeholder UI.

---

## Success Criteria

### Minimum Viable Product (MVP)
- [ ] All 33 steps functional
- [ ] TurnByCount working for valves and knobs
- [ ] WaitForScriptCondition working for button presses
- [ ] Weight and pressure monitoring functional
- [ ] Sequential flow enforced
- [ ] Success message displays

### Nice-to-Have
- [ ] Purging mist VFX
- [ ] Hand temperature visual feedback
- [ ] Smooth gauge/scale animations
- [ ] Detailed hints and guidance cues
- [ ] Haptic feedback for all interactions

---

## Next Steps

1. **Immediate:** Analyze AutoHandsValveController and AutoHandsKnobController
2. **Phase 1:** Implement TurnByCount data structure
3. **Phase 2:** Implement AutoHandsTurnByCountHandler
4. **Phase 3:** Test with simple scene
5. **Iterate:** Fix issues before moving to WaitForScriptCondition

---

## TurnByCount Implementation - Technical Deep Dive

### AutoHands Controller Pattern Analysis

After analyzing `AutoHandsValveControllerV2.cs` and `AutoHandsKnobController.cs`, here are the key patterns:

#### **AutoHandsValveControllerV2 Pattern** (HingeJoint-based)
- **Runtime HingeJoint Creation:** Creates HingeJoint when object snaps to socket
- **Angle Tracking:** Uses `hingeJoint.angle` property for precise rotation measurement
- **Limits:** Sets `JointLimits.min` and `JointLimits.max` from profile thresholds
- **State Machine:** Unlocked → Locked(Loose) → Locked(Tight) → Unlocked
- **Event System:** Fires OnValveTightened, OnValveLoosened based on angle thresholds
- **Cleanup:** Destroys HingeJoint when loosened to allow removal

**Pros:**
- Physics-based, smooth rotation
- Reliable angle tracking via Unity's HingeJoint
- Can set hard limits to prevent over-rotation

**Cons:**
- Requires socket snapping
- More complex setup
- HingeJoint overhead

---

#### **AutoHandsKnobController Pattern** (Transform-based)
- **Dual Tracking:** Can use HingeJoint OR transform rotation
- **Angle Reading:** `GetHingeAngle()` if joint exists, else `GetTransformAngle()`
- **Continuous Events:** Fires `OnAngleChanged` every Update while grabbed
- **No Socket Required:** Works as standalone grabbable object
- **Simpler:** No runtime joint creation, just reads rotation

**Pros:**
- Simpler implementation
- No socket dependency
- Works with any grabbable object
- Lower overhead

**Cons:**
- Transform angles can wrap (360° → 0°)
- May need extra logic to handle cumulative rotation

---

### Recommended Approach for TurnByCount

**Use Transform-Based Tracking (KnobController Pattern)**

**Rationale:**
1. TurnByCount doesn't need socket snapping
2. Valves/knobs in our module are already placed (LSV, GSV, cylinder valve, etc.)
3. Simpler implementation = faster development
4. Can work with ANY grabbable object (valves, knobs, hose ends)

**Implementation Strategy:**
```csharp
// Track cumulative rotation in ONE direction only
private float cumulativeRotation = 0f;
private Quaternion lastRotation;

void TrackRotation() {
    Quaternion currentRot = transform.rotation;
    Quaternion deltaRot = currentRot * Quaternion.Inverse(lastRotation);

    float deltaAngle;
    Vector3 axis;
    deltaRot.ToAngleAxis(out deltaAngle, out axis);

    // Check rotation direction
    bool isCorrectDirection = CheckRotationDirection(axis, deltaAngle);

    if (isCorrectDirection && Mathf.Abs(deltaAngle) < 180f) {
        cumulativeRotation += Mathf.Abs(deltaAngle);
    }

    lastRotation = currentRot;

    // Check completion
    float targetRotation = turnCount * 360f;
    if (cumulativeRotation >= targetRotation - rotationTolerance) {
        CompleteStep();
    }
}
```

---

### Phased Implementation Plan

#### **PHASE 1: Data Structure (30 min - ITERATIVE CHECKPOINT)**

**Goal:** Add TurnByCount to core data model, validate in GUI

**Tasks:**
1. Add `TurnByCount` to `InteractionStep.StepType` enum (TrainingSequence.cs:~line 50)
2. Add `RotationDirection` enum to `TrainingSequence.cs`:
   ```csharp
   public enum RotationDirection {
       Clockwise,
       AntiClockwise
   }
   ```
3. Add fields to `InteractionStep` class (TrainingSequence.cs:~line 80):
   ```csharp
   [Header("Turn By Count Settings")]
   public RotationDirection rotationDirection = RotationDirection.Clockwise;
   [Range(0.1f, 10f)]
   public float turnCount = 1.0f;
   [Tooltip("Angle tolerance in degrees")]
   public float rotationTolerance = 15f;
   ```
4. Update `InteractionStep.IsValid()` method:
   ```csharp
   if (stepType == StepType.TurnByCount) {
       if (targetObject == null) return false;
       if (turnCount <= 0) return false;
       return true;
   }
   ```
5. Update `InteractionStep.GetValidationMessage()` for TurnByCount

**Test Checkpoint:**
- [ ] Open VRInteractionSetupWindow Sequence tab
- [ ] Create new program with one TurnByCount step
- [ ] Verify new fields appear in properties panel
- [ ] Set turnCount = 0.5, direction = Clockwise
- [ ] Save asset
- [ ] Reload asset - verify data persists
- [ ] Check validation (set turnCount = 0, should show error)

**Files Modified:**
- `Assets/VRTrainingKit/Scripts/SequenceSystem/Data/TrainingSequence.cs`

---

#### **PHASE 2: GUI Support (30 min - ITERATIVE CHECKPOINT)**

**Goal:** Show TurnByCount properties in Sequence Editor

**Tasks:**
1. Open `VRInteractionSetupWindow.cs` (find Sequence tab section)
2. Locate properties panel rendering (search for "ShowInstruction" or "TurnKnob" to find property UI code)
3. Add TurnByCount property UI:
   ```csharp
   if (selectedStep.stepType == InteractionStep.StepType.TurnByCount) {
       // Rotation Direction dropdown
       EditorGUILayout.LabelField("Rotation Direction", EditorStyles.boldLabel);
       selectedStep.rotationDirection = (TrainingSequence.RotationDirection)
           EditorGUILayout.EnumPopup("Direction", selectedStep.rotationDirection);

       // Turn Count slider
       EditorGUILayout.Space();
       EditorGUILayout.LabelField("Turn Count", EditorStyles.boldLabel);
       selectedStep.turnCount = EditorGUILayout.Slider(
           "Turns", selectedStep.turnCount, 0.1f, 10f);

       // Tolerance field
       EditorGUILayout.Space();
       selectedStep.rotationTolerance = EditorGUILayout.FloatField(
           "Tolerance (degrees)", selectedStep.rotationTolerance);

       // Helper text
       EditorGUILayout.HelpBox(
           $"Object must rotate {selectedStep.turnCount} turn(s) {selectedStep.rotationDirection}",
           MessageType.Info);
   }
   ```

**Test Checkpoint:**
- [ ] Open Sequence tab
- [ ] Create TurnByCount step
- [ ] Verify direction dropdown shows Clockwise/AntiClockwise
- [ ] Verify turnCount slider works (0.1 to 10)
- [ ] Verify tolerance field editable
- [ ] Change values, save asset, reload - verify persistence
- [ ] Check helper text displays correctly

**Files Modified:**
- `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

---

#### **PHASE 3: Create TurnByCount Handler (2 hours - ITERATIVE CHECKPOINT)**

**Goal:** Implement handler that tracks rotation and completes step

**Tasks:**
1. Create `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsTurnByCountHandler.cs`
2. Extend `BaseAutoHandsStepHandler`
3. Implement core logic (see detailed code below)
4. Register handler in `ModularTrainingSequenceController.InitializeHandlers()`

**Detailed Implementation:**

```csharp
// AutoHandsTurnByCountHandler.cs
using UnityEngine;
using System.Collections.Generic;

public class AutoHandsTurnByCountHandler : BaseAutoHandsStepHandler
{
    // Active step tracking
    private class RotationTracker {
        public InteractionStep step;
        public GameObject targetObject;
        public Autohand.Grabbable grabbable;
        public Quaternion lastRotation;
        public float cumulativeRotation = 0f;
        public bool isGrabbed = false;
    }

    private Dictionary<InteractionStep, RotationTracker> activeTrackers =
        new Dictionary<InteractionStep, RotationTracker>();

    public override bool CanHandle(InteractionStep.StepType stepType) {
        return stepType == InteractionStep.StepType.TurnByCount;
    }

    public override void Initialize(ModularTrainingSequenceController controller) {
        base.Initialize(controller);
        LogInfo("🔄 AutoHandsTurnByCountHandler initialized");
    }

    public override void StartStep(InteractionStep step) {
        LogDebug($"🔄 Starting TurnByCount step: {step.stepName}");

        var targetObject = step.targetObject.GameObject;
        if (targetObject == null) {
            LogError($"Target object is null for step: {step.stepName}");
            return;
        }

        // Get Grabbable component
        var grabbable = targetObject.GetComponent<Autohand.Grabbable>();
        if (grabbable == null) {
            LogError($"No Grabbable component on {targetObject.name}");
            return;
        }

        // Create rotation tracker
        var tracker = new RotationTracker {
            step = step,
            targetObject = targetObject,
            grabbable = grabbable,
            lastRotation = targetObject.transform.rotation,
            cumulativeRotation = 0f,
            isGrabbed = false
        };

        // Subscribe to grab events
        grabbable.OnGrabEvent += (hand, grb) => OnGrab(tracker, hand, grb);
        grabbable.OnReleaseEvent += (hand, grb) => OnRelease(tracker, hand, grb);

        activeTrackers[step] = tracker;

        LogInfo($"🔄 Tracking rotation for {targetObject.name}: " +
                $"{step.turnCount} turn(s) {step.rotationDirection}, " +
                $"tolerance: {step.rotationTolerance}°");
    }

    public override void StopStep(InteractionStep step) {
        if (activeTrackers.ContainsKey(step)) {
            var tracker = activeTrackers[step];

            // Unsubscribe from events
            tracker.grabbable.OnGrabEvent -= (hand, grb) => OnGrab(tracker, hand, grb);
            tracker.grabbable.OnReleaseEvent -= (hand, grb) => OnRelease(tracker, hand, grb);

            activeTrackers.Remove(step);
            LogDebug($"🔄 Stopped tracking: {step.stepName}");
        }
    }

    public override void Cleanup() {
        var steps = new List<InteractionStep>(activeTrackers.Keys);
        foreach (var step in steps) {
            StopStep(step);
        }
        base.Cleanup();
    }

    void OnGrab(RotationTracker tracker, Autohand.Hand hand, Autohand.Grabbable grabbable) {
        tracker.isGrabbed = true;
        tracker.lastRotation = tracker.targetObject.transform.rotation;
        LogDebug($"🔄 Grabbed {tracker.targetObject.name}");
    }

    void OnRelease(RotationTracker tracker, Autohand.Hand hand, Autohand.Grabbable grabbable) {
        tracker.isGrabbed = false;
        LogDebug($"🔄 Released {tracker.targetObject.name} - " +
                 $"Cumulative rotation: {tracker.cumulativeRotation:F1}° " +
                 $"({tracker.cumulativeRotation / 360f:F2} turns)");
    }

    void Update() {
        foreach (var kvp in activeTrackers) {
            var step = kvp.Key;
            var tracker = kvp.Value;

            if (tracker.isGrabbed && !step.isCompleted) {
                TrackRotation(step, tracker);
            }
        }
    }

    void TrackRotation(InteractionStep step, RotationTracker tracker) {
        Quaternion currentRotation = tracker.targetObject.transform.rotation;
        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(tracker.lastRotation);

        // Extract angle
        float deltaAngle;
        Vector3 axis;
        deltaRotation.ToAngleAxis(out deltaAngle, out axis);

        // Normalize angle to -180 to 180
        if (deltaAngle > 180f) deltaAngle -= 360f;

        // Check if rotation is in correct direction
        bool isCorrectDirection = false;

        if (step.rotationDirection == TrainingSequence.RotationDirection.Clockwise) {
            // Clockwise = negative angle (Unity convention)
            isCorrectDirection = deltaAngle < 0;
        } else {
            // AntiClockwise = positive angle
            isCorrectDirection = deltaAngle > 0;
        }

        // Accumulate rotation (ignore wrong direction)
        if (isCorrectDirection && Mathf.Abs(deltaAngle) < 180f) {
            tracker.cumulativeRotation += Mathf.Abs(deltaAngle);

            // Log progress every 45 degrees
            if (Mathf.FloorToInt(tracker.cumulativeRotation / 45f) >
                Mathf.FloorToInt((tracker.cumulativeRotation - Mathf.Abs(deltaAngle)) / 45f)) {
                float turns = tracker.cumulativeRotation / 360f;
                LogDebug($"🔄 {tracker.targetObject.name}: {turns:F2} / {step.turnCount} turns");
            }
        }

        tracker.lastRotation = currentRotation;

        // Check completion
        float targetRotation = step.turnCount * 360f;
        if (tracker.cumulativeRotation >= targetRotation - step.rotationTolerance) {
            LogInfo($"🔄 ✅ Turn complete! {tracker.cumulativeRotation:F1}° " +
                    $"({tracker.cumulativeRotation / 360f:F2} turns)");
            CompleteStep(step, $"Rotated {step.turnCount} turn(s) {step.rotationDirection}");
        }
    }
}
```

**Test Checkpoint:**
- [ ] Create test scene with simple grabbable cube
- [ ] Add Autohand.Grabbable component to cube
- [ ] Create TrainingSequenceAsset with one TurnByCount step (0.5 turns clockwise)
- [ ] Assign cube as target object
- [ ] Attach ModularTrainingSequenceController to scene
- [ ] Assign sequence asset
- [ ] Play scene
- [ ] Grab cube with hand, rotate 180° clockwise
- [ ] Verify step completes (check console logs)
- [ ] Test 1 full turn clockwise
- [ ] Test 0.5 turns anti-clockwise
- [ ] Verify wrong direction doesn't count (rotate opposite direction, step shouldn't complete)

**Files Modified:**
- `Assets/VRTrainingKit/Scripts/StepHandlers/AutoHandsTurnByCountHandler.cs` (NEW)
- `Assets/VRTrainingKit/Scripts/Core/Controllers/ModularTrainingSequenceController.cs` (add to InitializeHandlers)

---

#### **PHASE 4: Integration & Testing (1 hour - ITERATIVE CHECKPOINT)**

**Goal:** Register handler and test with real valve objects

**Tasks:**
1. Open `ModularTrainingSequenceController.cs`
2. Find `InitializeHandlers()` method
3. Add TurnByCount handler initialization:
   ```csharp
   private void InitializeHandlers() {
       // ... existing handlers

       // TurnByCount handler
       var turnByCountHandler = gameObject.AddComponent<AutoHandsTurnByCountHandler>();
       RegisterHandler(turnByCountHandler);
   }
   ```
4. Test with actual module objects (LSV, GSV, cylinder valve)

**Test Checkpoint:**
- [ ] Find LSV valve in existing scene
- [ ] Verify it has Grabbable component
- [ ] Create sequence with step: "Open LSV - 0.5 turns clockwise"
- [ ] Assign LSV as target
- [ ] Play scene, grab LSV, rotate 180° clockwise
- [ ] Verify step completes
- [ ] Test cylinder valve - 1 full turn anti-clockwise
- [ ] Test LP knob on gauge manifold - 1 turn anti-clockwise
- [ ] Test that wrong direction doesn't increment progress
- [ ] Test tolerance (e.g., turnCount = 1.0, tolerance = 15°, verify 345° completes)

**Files Modified:**
- `Assets/VRTrainingKit/Scripts/Core/Controllers/ModularTrainingSequenceController.cs`

---

#### **PHASE 5: Refinement & Edge Cases (1 hour - OPTIONAL)**

**Goal:** Handle edge cases and add polish

**Edge Cases to Handle:**
1. **Multiple grab-release cycles:** Cumulative rotation should persist across grabs
2. **Object with HingeJoint:** Prefer hingeJoint.angle over transform rotation
3. **Visual feedback:** Optional glow/outline when grabbed
4. **Audio feedback:** Optional click sound every 90° or on completion
5. **Progress indicator:** Optional UI showing turn progress

**Optional Enhancements:**
```csharp
// In AutoHandsTurnByCountHandler

// 1. Handle existing HingeJoint
float GetCurrentAngle(GameObject obj) {
    var hinge = obj.GetComponent<HingeJoint>();
    if (hinge != null) {
        return hinge.angle;  // More accurate
    }
    return GetTransformAngle(obj);
}

// 2. Visual feedback
void OnGrab(RotationTracker tracker, ...) {
    // Add outline to object
    AddOutline(tracker.targetObject);
}

void OnRelease(RotationTracker tracker, ...) {
    RemoveOutline(tracker.targetObject);
}

// 3. Audio feedback
void TrackRotation(...) {
    // ... rotation tracking

    // Play click every 90°
    int quarterTurns = Mathf.FloorToInt(tracker.cumulativeRotation / 90f);
    if (quarterTurns > tracker.lastQuarterTurns) {
        PlayClickSound();
        tracker.lastQuarterTurns = quarterTurns;
    }
}
```

**Test Checkpoint:**
- [ ] Test with object that already has HingeJoint (like existing valve)
- [ ] Verify angle tracking still works
- [ ] Test grab → rotate 90° → release → grab → rotate 90° → should complete at 180°
- [ ] Verify no double-counting when rotating back and forth

---

### Implementation Risks & Solutions

#### **Risk 1: Rotation Direction Ambiguity**
**Problem:** Unity rotation axes can be confusing (local vs world, clockwise definition)

**Solution:**
- Test with visual indicators (debug arrows)
- Document expected behavior for each axis
- Allow profile to specify rotation axis override if needed

#### **Risk 2: 360° Wrap-Around**
**Problem:** Transform angles wrap at 360° (359° → 0°)

**Solution:**
- Use `Quaternion` math instead of euler angles
- Track delta rotations, not absolute angles
- Implemented in Phase 3 code with `Quaternion.ToAngleAxis`

#### **Risk 3: Object Already Has HingeJoint**
**Problem:** Existing valves may have HingeJoint with limits

**Solution:**
- Check for HingeJoint first
- Use `hingeJoint.angle` if available (more reliable)
- Falls back to transform rotation

#### **Risk 4: Handler Not Registered**
**Problem:** Easy to forget registering new handler

**Solution:**
- Clear error message if handler missing
- Validation in sequence controller
- Test early in Phase 4

---

### Code Integration Points

| File | Section | Change Type | Lines |
|------|---------|-------------|-------|
| `TrainingSequence.cs` | StepType enum | Add TurnByCount | ~50 |
| `TrainingSequence.cs` | RotationDirection enum | Add new enum | ~55 |
| `TrainingSequence.cs` | InteractionStep class | Add 3 fields | ~120 |
| `TrainingSequence.cs` | IsValid() method | Add validation | ~180 |
| `VRInteractionSetupWindow.cs` | Properties panel | Add UI fields | ~450 |
| `AutoHandsTurnByCountHandler.cs` | NEW FILE | Full implementation | 200 lines |
| `ModularTrainingSequenceController.cs` | InitializeHandlers() | Register handler | ~85 |

**Total Code Changes:** ~250 new lines, ~20 lines modified

**Estimated Time:** 4-5 hours (with testing)

---

### Testing Strategy

#### **Unit Tests (Per Phase)**
- Phase 1: Data validation
- Phase 2: GUI rendering
- Phase 3: Rotation tracking logic
- Phase 4: Integration with controller

#### **Integration Test Scenarios**
1. **Simple rotation:** 1 turn clockwise on unlocked object
2. **Half turn:** 0.5 turns (180°) for valve caps
3. **Multi-turn:** 3 turns for threaded connections
4. **Direction validation:** Wrong direction doesn't count
5. **Tolerance:** 345° should complete 1.0 turn (15° tolerance)
6. **Multiple grabs:** Rotate 90°, release, grab, rotate 90°, should complete
7. **Existing HingeJoint:** Works with valve objects
8. **Edge case:** Very fast rotation (ensure no missed frames)

---

## Summary: Why This Approach Works

1. **Minimal Complexity:** No runtime joint creation, just rotation tracking
2. **Reusable:** Works with ANY grabbable object (valves, knobs, hose ends, caps)
3. **Proven Pattern:** Based on working AutoHandsKnobController
4. **Fast Development:** 4-5 hours total vs 2-3 days for joint-based approach
5. **Testable:** Clear checkpoints after each phase
6. **Maintainable:** Simple code, easy to debug
7. **Flexible:** Can add HingeJoint support later if needed

---

**Document Version:** 2.0
**Last Updated:** 2025-10-22
**Status:** Technical Analysis Complete - Ready for Phased Implementation

# ToolProfile Implementation Plan

## Overview
Implementation plan for creating a new `ToolProfile` interaction system for grab+snap+rotate tools (like allen keys) using a state machine approach with RigidBody constraints and kinematic transitions.

## Completed Phases

### ✅ Phase 1: Core ToolProfile Setup
**Status: COMPLETED**
- ✅ Created `ToolProfile.cs` extending InteractionProfile
- ✅ Added configuration options (rotation axis, friction, snap settings)
- ✅ Implemented tag validation for "tool" tagged objects
- ✅ Added CreateAssetMenu integration
- ✅ Added dynamic attach support (enabled by default)

### ✅ Phase 2: Socket Integration 
**Status: COMPLETED**
- ✅ Confirmed tool objects work with existing XRSocketInteractor system
- ✅ Tools configured as XRGrabInteractables are automatically compatible with sockets
- ✅ Integration with existing SnapProfile and SnapValidator system

### ✅ Phase 2.1: System Integration Fixes
**Status: COMPLETED**
- ✅ Added interaction layer support to ToolProfile objects
- ✅ Added validation indicators (green checkmarks) for tool objects
- ✅ Fixed ToolProfile persistence in play mode
- ✅ Updated InteractionSetupService for complete tool support
- ✅ UI improvements: Individual Configure buttons, single Edit Layers button

## Upcoming Phases

### 🔄 Phase 3: Constraint Application on Snap
**Goal**: Lock position, allow only rotation when snapped

**Implementation**:
- Create ToolController component for state management
- On snap: enable isKinematic + RigidBody position constraints
- Allow only specified axis rotation (typically Y-axis)
- On unsnap: remove constraints, restore normal grabbability

**Test Cases**:
- Snap allen key to socket
- Verify position is locked (can't move)
- Verify rotation is allowed on specified axis only
- Verify other axes are constrained

### 🔄 Phase 4: Rotation Tracking System
**Goal**: Track cumulative rotation from snap point

**Implementation**:
- Add rotation tracking in ToolController
- Implement angle accumulation system
- Handle rotation wrapping (0-360° vs -180° to +180°)
- Debug logging of rotation values
- Event system for rotation changes

**Test Cases**:
- Snap allen key, rotate it in both directions
- Check console for accurate rotation tracking
- Verify no rotation drift over time
- Test rotation wrapping edge cases

### 🔄 Phase 5: Lock State Implementation
**Goal**: Lock object after sufficient rotation (tightening)

**Implementation**:
- Add lock state when rotation exceeds tighten threshold
- Disable socket interactor when locked (prevents accidental unsnapping)
- Visual feedback (material change, outline, etc.)
- Update sequence controller integration
- Audio feedback for lock/unlock events

**Test Cases**:
- Rotate allen key past tighten threshold (e.g., 180°)
- Object should "lock" and change appearance
- Try to unsnap (should be prevented)
- Verify sequence controller receives lock events

### 🔄 Phase 6: Reverse Flow (Unlocking)
**Goal**: Allow loosening by reverse rotation

**Implementation**:
- Track reverse rotation from locked state
- Unlock when sufficient reverse rotation achieved
- Re-enable socket interactor for normal removal
- Handle partial loosening vs full unlock
- Sequence validation for unlock requirements

**Test Cases**:
- Lock an allen key (from Phase 5)
- Grab it (shouldn't move from position)
- Rotate backward past loosen threshold
- Should unlock and become normally grabbable
- Test partial vs full loosening behavior

## Technical Architecture

### State Machine Design
```
FREE_GRAB → SOCKETED → LOCKED ⟷ UNLOCKED → FREE_GRAB
    ↑         ↓         ↑         ↓         ↑
    └─────────┴─────────┴─────────┴─────────┘
```

    State Machine:

    Forward:  Idle → Grabbed → Snapped → [Kinematic+Constraints] → Rotating → Locked
    Reverse:  Locked → [Grab-No-Move] → Rotating → [Constraints Off] → Unlocked → Grabbed

### Key Components
- **ToolProfile**: Configuration asset (ScriptableObject)
- **ToolController**: Runtime state management and physics control
- **Existing XRI Components**: XRGrabInteractable, XRSocketInteractor
- **Physics System**: RigidBody constraints, kinematic transitions

### Configuration Parameters
```csharp
[Header("Tool Settings")]
public Vector3 rotationAxis = Vector3.up;
public float tightenAngle = 180f;       // Degrees to lock
public float loosenAngle = 90f;         // Reverse degrees to unlock

[Header("Physics Settings")]
public float rotationFriction = 2f;     // Drag during rotation
public bool snapToAngles = true;
public float snapIncrement = 15f;

[Header("Feedback")]
public bool useHapticFeedback = true;
public AudioClip lockSound;
public AudioClip unlockSound;
```

### Integration Points
- **Sequence Controller**: OnToolLocked, OnToolUnlocked events
- **Validation System**: Check tool states in sequence conditions
- **Visual Feedback**: Material swapping, outline effects
- **Layer System**: Full compatibility with interaction layers

## Implementation Philosophy
- **Leverage Existing Systems**: Use standard XRI components where possible
- **Iterative Development**: Test each phase before proceeding
- **Robust State Management**: Clear state transitions with validation
- **User Experience Focus**: Smooth interactions, clear feedback
- **Compatibility First**: Works with Physics Hands and other interaction systems

## Testing Strategy
- **Unit Testing**: Each phase tested individually
- **Integration Testing**: Verify compatibility with existing systems
- **User Experience Testing**: Allen key workflow feels natural
- **Edge Case Testing**: Handle rotation wrapping, state conflicts, etc.

## Success Criteria
By completion, the ToolProfile system should provide:
1. Smooth grab → snap → rotate → lock workflow
2. Intuitive reverse loosening mechanics
3. Full integration with sequence management
4. Visual and haptic feedback
5. Compatibility with existing VR Training Kit architecture

---

*Generated for VR Training Kit - Tool Interaction System*
*Last Updated: Current Phase 3 Implementation*
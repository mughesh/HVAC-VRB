  ⎿ VR Training Kit: Complex Tool Interaction Profile Implementation Plan

    Executive Summary

    Implementing a sophisticated tool interaction system with forward flow (grab → snap → rotate to tighten → lock)
     and reverse flow (grab locked tool → rotate to loosen → unlock → move freely). Using kinematic + rigidbody
    constraints approach for clean state management.

    Current Architecture Analysis

    ✅ Strengths

    - Solid profile-based architecture with ScriptableObject system
    - Comprehensive sequence system with hierarchical training programs
    - Strong editor integration via VRInteractionSetupWindow
    - Basic ToolProfile infrastructure already exists

    ⚠️ Architectural Issues to Address

    1. Misplaced classes: SnapValidator and SequenceValidator incorrectly in KnobController.cs
    2. Incomplete ToolProfile: Current implementation only handles grab behavior
    3. Missing complex interaction support in sequence system

    Implementation Plan (5 Phases)

    Phase 1: Architectural Cleanup + Basic Tool-Socket Pairing

    Tasks:

    - Extract misplaced classes to separate files:
      - SnapValidator.cs (from KnobController.cs:307-423)
      - SequenceValidator.cs (from KnobController.cs:428-527)
    - Update dependencies and fix any namespace issues
    - Enhance ToolProfile with socket compatibility:
      - Target socket reference/tag system
      - Rotation axis and angle parameters
      - Lock/unlock thresholds

    Testing & Validation:

    - Test Scene: Create simple scene with:
      - One tool object (tagged "tool")
      - One socket object (tagged "snap")
      - Basic pairing validation working
    - Validation Checklist:
      - SnapValidator works as standalone component
      - SequenceValidator works as standalone component
      - Tool can identify its target socket
      - No compilation errors after refactor

    ---
    Phase 2: Forward Flow Implementation (Tighten)

    Tasks:

    - Create ToolController component:
      - State machine: Idle → Grabbed → Snapped → Rotating → Locked
      - Rigidbody constraint management
      - Rotation angle tracking on specified axis
    - Implement state transitions:
      - Grab → Snap detection and validation
      - Snap → Kinematic mode + position/rotation constraints
      - Rotation threshold → Locked state
      - Disable socket interactor when locked

    Testing & Validation:

    - Test Scenario: "Bolt Tightening"
      - User grabs wrench → snaps to bolt → rotates 90° → wrench locks in place
    - Validation Checklist:
      - Tool becomes grabbable initially
      - Tool snaps to designated socket correctly
      - Tool becomes kinematic when snapped (no movement)
      - Only specified rotation axis remains free
      - Rotation angle properly tracked
      - Tool locks after rotation threshold met
      - Socket interactor disabled when locked

    ---
    Phase 3: Reverse Flow Implementation (Loosen)

    Tasks:

    - Extend ToolController for reverse operations:
      - Handle grab-but-no-move for locked tools
      - Reverse rotation detection and validation
      - Unlock conditions and state management
    - Implement unlock sequence:
      - Detect reverse rotation beyond threshold
      - Remove rigidbody constraints
      - Disable kinematic mode
      - Re-enable normal grab behavior

    Testing & Validation:

    - Test Scenario: "Bolt Removal"
      - Locked wrench → grab (no movement) → rotate -90° → wrench unlocks → freely movable
    - Validation Checklist:
      - Locked tool can be selected but doesn't move
      - Reverse rotation properly detected
      - Tool unlocks after reverse rotation threshold
      - All constraints removed when unlocked
      - Tool becomes freely grabbable again
      - Visual/haptic feedback for state changes

    ---
    Phase 4: Sequence System Integration

    Tasks:

    - Add new step types to InteractionStep.StepType:
      - ToolTighten - Forward flow operation
      - ToolLoosen - Reverse flow operation
    - Update TrainingSequence system:
      - Tool operation parameters (target tool, socket, angles)
      - Validation logic for tool steps
      - Completion tracking integration
    - Extend sequence editor:
      - Tool step configuration UI
      - Tool-socket pairing interface
      - Forward/reverse flow parameter settings

    Testing & Validation:

    - Test Scenario: "Complete HVAC Maintenance Sequence"
      - Step 1: ToolTighten (wrench → bolt socket, 90°)
      - Step 2: ToolLoosen (wrench → bolt socket, -90°)
      - Step 3: GrabAndSnap (wrench → tool storage)
    - Validation Checklist:
      - Tool steps appear in sequence editor
      - Tool-socket pairing configurable in editor
      - Forward/reverse parameters settable
      - Sequence validation works for tool steps
      - Step completion properly tracked
      - Training program flows correctly through tool operations

    ---
    Phase 5: Polish & Advanced Features

    Tasks:

    - Enhanced editor integration:
      - Tool-socket pairing configuration in sequence editor
      - Visual feedback for locked/unlocked states
      - Runtime debugging and validation tools
    - Advanced features:
      - Multiple rotation thresholds (partial → full tightening)
      - Tool compatibility validation (wrench types, socket sizes)
      - Error handling and user feedback
      - Performance optimizations

    Testing & Validation:

    - Comprehensive Test Suite:
      - Multiple tool types (wrenches, screwdrivers, etc.)
      - Various socket configurations
      - Complex multi-step sequences
      - Error condition handling
    - User Acceptance Testing:
      - Intuitive tool behavior
      - Clear visual feedback
      - Smooth state transitions
      - Reliable sequence completion

    Technical Architecture

    Component Interaction Flow:

    ToolProfile → applies to GameObject → adds:
    ├── XRGrabInteractable (grab behavior)
    ├── ToolController (state machine & constraints)
    ├── Collider + Rigidbody (physics)
    └── Socket compatibility validation

    Socket Objects → SnapProfile → adds:
    ├── XRSocketInteractor (snap detection)
    ├── SnapValidator (tool compatibility)
    └── Tool state integration

    State Machine:

    Forward:  Idle → Grabbed → Snapped → [Kinematic+Constraints] → Rotating → Locked
    Reverse:  Locked → [Grab-No-Move] → Rotating → [Constraints Off] → Unlocked → Grabbed

    ### State Machine Design
    ```
    FREE_GRAB → SOCKETED → LOCKED ⟷ UNLOCKED → FREE_GRAB
        ↑         ↓         ↑         ↓         ↑
        └─────────┴─────────┴─────────┴─────────┘

    Physics Implementation:

    // Forward flow - lock position, allow rotation
    rigidbody.isKinematic = true;
    rigidbody.constraints = RigidbodyConstraints.FreezePosition |
                           RigidbodyConstraints.FreezeRotationX |
                           RigidbodyConstraints.FreezeRotationZ; // Allow Y-axis rotation only

    // Reverse flow - unlock everything
    rigidbody.isKinematic = false;
    rigidbody.constraints = RigidbodyConstraints.None;

    Key Benefits

    🎯 Addresses All Requirements

    - Complex tool interactions: Full grab → snap → rotate → lock workflow
    - Bidirectional flow: Both tightening and loosening operations
    - Sequence integration: Configurable in training programs
    - Architecture cleanup: Moves misplaced classes to proper files

    🔧 Leverages Existing Systems

    - Builds on proven profile architecture
    - Reuses existing XRI components
    - Maintains consistency with current patterns
    - No over-engineering - uses simple kinematic + constraints approach

    📈 Scalable & Future-Proof

    - Iterative implementation with validation at each phase
    - Extensible for other complex tools (screwdrivers, allen keys, etc.)
    - Clear separation of concerns
    - Comprehensive testing strategy

    Testing Strategy Summary

    Each phase includes specific test scenarios and validation checklists to ensure:
    - Functionality works as expected
    - No regressions in existing systems
    - Smooth integration between components
    - User experience remains intuitive
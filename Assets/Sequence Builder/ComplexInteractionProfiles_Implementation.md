# Complex Interaction Profiles - Implementation Plan

## Overview
This document outlines the phase-by-phase implementation plan for both Valve Style and Tool Style interaction profiles. The plan is designed to build incrementally, with testing at each phase to ensure stability and functionality.

## Recommendation: Start with Valve Style Profile

### Why Valve Style First?
1. **More Complex State Machine**: The valve's substates (LOOSE/TIGHT) within the LOCKED state will drive better architectural decisions
2. **Current Code Foundation**: Existing ToolProfile/ToolController is closer to valve behavior
3. **Architectural Benefits**: Valve style implementation will create reusable components for Tool style
4. **Risk Management**: Tackling the complex case first reduces integration risks

## Phase-by-Phase Implementation Plan

---

## Phase 1: Architecture Foundation & Valve Profile Setup
**Duration**: 2-3 days  
**Priority**: Critical foundation work

### Tasks

#### 1.1 Create ValveProfile ScriptableObject
- **File**: `Assets/VRTrainingKit/Scripts/ValveProfile.cs`
- **Extends**: `InteractionProfile`
- **Key Components**:
  ```csharp
  public class ValveProfile : InteractionProfile
  {
      [Header("Valve Mechanics")]
      public Vector3 rotationAxis = Vector3.up;
      public float tightenThreshold = 90f;
      public float loosenThreshold = 90f;
      public float angleTolerance = 5f;
      
      [Header("Socket Compatibility")]
      public string[] compatibleSocketTags = {"valve_socket"};
      public GameObjectReference[] specificCompatibleSockets;
      public bool requireSpecificSockets = false;
  }
  ```

#### 1.2 Refactor Current ToolController to ValveController
- **File**: Rename `ToolController.cs` → `ValveController.cs`
- **Key Changes**:
  - Add substate enum: `ValveSubstate { None, Loose, Tight }`
  - Modify state machine to support LOCKED substates
  - Update event system with valve-specific events
  - Implement grab-release detection for unlock transition

#### 1.3 Create Base State Machine Infrastructure
```csharp
public enum ValveState { Unlocked, Locked }
public enum ValveSubstate { None, Loose, Tight }

public class ValveController : MonoBehaviour
{
    // State management
    private ValveState currentState = ValveState.Unlocked;
    private ValveSubstate currentSubstate = ValveSubstate.None;
    
    // Core events
    public event Action<ValveState> OnStateChanged;
    public event Action<ValveSubstate> OnSubstateChanged;
    public event Action OnValveTightened;
    public event Action OnValveLoosened;
}
```

### Testing Criteria for Phase 1

#### Test Scene Setup
- **Scene**: Create `ValveTestScene.unity`
- **Objects**:
  - Simple valve object tagged "valve"
  - Socket object with XRSocketInteractor tagged "valve_socket"
  - VR rig for testing

#### Validation Checklist
- [ ] ValveProfile asset can be created via Create menu
- [ ] ValveProfile applies to valve objects without errors
- [ ] ValveController state machine initializes correctly
- [ ] Basic grab/release events fire properly
- [ ] Socket snapping transitions to LOCKED state
- [ ] Console shows clear debug messages for state changes
- [ ] No compilation errors or warnings

#### Success Criteria
- **Grab**: Valve can be grabbed and moved when UNLOCKED
- **Snap**: Valve snaps to socket and transitions to LOCKED-LOOSE
- **Position Lock**: Valve cannot be moved when LOCKED, only rotated
- **Debug Output**: Clear state transition logging

---

## Phase 2: Forward Flow Implementation (Tightening)
**Duration**: 2-3 days  
**Priority**: Core valve functionality

### Tasks

#### 2.1 Implement LOCKED-LOOSE Substate Logic
```csharp
private void TransitionToLockedLoose()
{
    currentState = ValveState.Locked;
    currentSubstate = ValveSubstate.Loose;
    
    // Configure for rotation-only interaction
    ApplyLockedConstraints();
    DisableSocketInteractor();
    ResetRotationTracking();
    
    OnStateChanged?.Invoke(currentState);
    OnSubstateChanged?.Invoke(currentSubstate);
}

private void ApplyLockedConstraints()
{
    // Position frozen, selective rotation allowed
    grabInteractable.trackPosition = false;
    grabInteractable.trackRotation = true;
    
    rigidbody.isKinematic = true;
    rigidbody.constraints = RigidbodyConstraints.FreezePosition;
    
    // Allow rotation only on specified axis
    if (profile.rotationAxis.x == 0) 
        rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX;
    // ... similar for Y and Z axes
}
```

#### 2.2 Implement Rotation Tracking System
```csharp
private void TrackRotation()
{
    if (!grabInteractable.isSelected || currentState != ValveState.Locked) return;
    
    Vector3 currentRotation = transform.eulerAngles;
    Vector3 deltaRotation = currentRotation - lastRotation;
    
    // Handle 360° wrapping
    // ... angle wrapping logic
    
    // Calculate rotation on specified axis
    float axisRotation = Vector3.Dot(deltaRotation, profile.rotationAxis);
    totalRotation += axisRotation;
    
    lastRotation = currentRotation;
    OnRotationChanged?.Invoke(totalRotation);
    
    CheckTighteningThreshold();
}
```

#### 2.3 Implement Tightening Completion
```csharp
private void CheckTighteningThreshold()
{
    if (currentSubstate == ValveSubstate.Loose)
    {
        if (totalRotation >= profile.tightenThreshold - profile.angleTolerance)
        {
            TransitionToLockedTight();
        }
    }
}

private void TransitionToLockedTight()
{
    currentSubstate = ValveSubstate.Tight;
    ResetRotationTracking(); // Reset for loosening phase
    
    OnTighteningComplete?.Invoke();
    OnSubstateChanged?.Invoke(currentSubstate);
}
```

### Testing Criteria for Phase 2

#### Test Scenarios
1. **Basic Tightening**:
   - Grab valve → snap to socket → rotate 90° clockwise → should transition to TIGHT
   
2. **Partial Tightening**:
   - Rotate 45° → release grab → re-grab → rotate another 45° → should complete
   
3. **Over-Rotation**:
   - Rotate 120° when only 90° required → should still transition to TIGHT

#### Validation Checklist
- [ ] Valve transitions to LOCKED-LOOSE when snapped
- [ ] Rotation tracking works smoothly without jumps
- [ ] Tightening threshold detection works correctly
- [ ] Progress events fire with accurate values (0-1)
- [ ] Visual feedback shows valve state clearly
- [ ] Haptic feedback occurs at threshold completion
- [ ] Over-rotation doesn't break the system

#### Success Criteria
- **Smooth Rotation**: No physics glitches during rotation
- **Accurate Tracking**: Rotation angles match visual rotation
- **Clear Feedback**: User knows when tightening is complete
- **State Consistency**: Valve stays in TIGHT state after completion

---

## Phase 3: Reverse Flow Implementation (Loosening)
**Duration**: 2-3 days  
**Priority**: Complete valve lifecycle

### Tasks

#### 3.1 Implement LOCKED-TIGHT State Behavior
```csharp
private void CheckLooseningThreshold()
{
    if (currentSubstate == ValveSubstate.Tight)
    {
        // Check for reverse rotation (negative values)
        if (totalRotation <= -profile.loosenThreshold + profile.angleTolerance)
        {
            // CRITICAL: Re-enable socket while user still holds grab
            EnableSocketInteractor();
            TransitionToLockedLoose();
        }
    }
}
```

#### 3.2 Implement Critical Socket Re-enable Logic
```csharp
private void EnableSocketInteractor()
{
    if (currentSocket != null)
    {
        var socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
        {
            socketInteractor.socketActive = true;
            Debug.Log("Socket re-enabled - valve will snap when released");
        }
    }
}
```

#### 3.3 Implement Grab Release Detection for Unlock
```csharp
private void OnGrabReleased(SelectExitEventArgs args)
{
    if (currentState == ValveState.Locked && currentSubstate == ValveSubstate.Loose)
    {
        // Check if loosening was completed
        if (socketInteractor.socketActive)
        {
            StartCoroutine(WaitForSnapThenUnlock());
        }
    }
}

private IEnumerator WaitForSnapThenUnlock()
{
    yield return new WaitForEndOfFrame(); // Allow snap to occur
    
    // Transition to unlocked
    TransitionToUnlocked();
}

private void TransitionToUnlocked()
{
    currentState = ValveState.Unlocked;
    currentSubstate = ValveSubstate.None;
    
    RemoveAllConstraints();
    currentSocket = null;
    
    OnStateChanged?.Invoke(currentState);
    OnValveLoosened?.Invoke();
}
```

### Testing Criteria for Phase 3

#### Test Scenarios
1. **Complete Cycle**:
   - UNLOCKED → snap → LOCKED-LOOSE → tighten → LOCKED-TIGHT → loosen → UNLOCKED
   
2. **Partial Loosening**:
   - From TIGHT state, rotate -45° → release → should stay TIGHT
   - Re-grab → rotate -45° more → should complete loosening
   
3. **Socket Re-enable Timing**:
   - Verify socket becomes active during loosening, not after release

#### Validation Checklist
- [ ] Loosening rotation tracking works (negative values)
- [ ] Socket interactor re-enables at correct time
- [ ] Valve auto-snaps to socket after release
- [ ] Transition to UNLOCKED occurs after snap
- [ ] Valve becomes grabbable for removal
- [ ] Full cycle can be repeated multiple times
- [ ] No physics artifacts during state transitions

#### Success Criteria
- **Smooth Reverse Flow**: Loosening feels natural and responsive
- **Correct Timing**: Socket re-enable happens while user holds grab
- **Clean Unlock**: Valve cleanly transitions to removable state
- **Repeatable**: Multiple install/remove cycles work flawlessly

---

## Phase 4: Tool Style Profile Implementation
**Duration**: 3-4 days  
**Priority**: Second interaction pattern

### Tasks

#### 4.1 Create ToolTaskProvider Component
```csharp
public enum ToolTask { None, Tighten, Loosen }

public class ToolTaskProvider : MonoBehaviour
{
    [Header("Task Configuration")]
    public ToolTask requiredTask = ToolTask.Tighten;
    public float taskAngle = 90f;
    public Vector3 rotationAxis = Vector3.up;
    public float angleTolerance = 5f;
    
    public ToolTask GetTaskForTool(GameObject tool)
    {
        return requiredTask;
    }
}
```

#### 4.2 Create ToolStyleProfile & ToolStyleController
- **Base on**: Valve architecture but with task-based logic
- **Key Differences**:
  - Task assignment from socket
  - Immediate unlock after task completion
  - Auto-transition to UNLOCKED (no manual release required)

#### 4.3 Implement Task-Based State Machine
```csharp
public class ToolStyleController : MonoBehaviour
{
    private ToolTask assignedTask = ToolTask.None;
    private ToolTaskProvider currentTaskProvider;
    
    public void OnSocketSnapped(GameObject socket)
    {
        currentTaskProvider = socket.GetComponent<ToolTaskProvider>();
        assignedTask = currentTaskProvider.GetTaskForTool(gameObject);
        
        TransitionToLockedWorking();
    }
    
    private void CheckTaskCompletion()
    {
        bool completed = false;
        
        switch (assignedTask)
        {
            case ToolTask.Tighten:
                completed = totalRotation >= currentTaskProvider.taskAngle;
                break;
            case ToolTask.Loosen:
                completed = totalRotation <= -currentTaskProvider.taskAngle;
                break;
        }
        
        if (completed)
        {
            CompleteTask();
        }
    }
    
    private void CompleteTask()
    {
        EnableSocketInteractor();
        StartCoroutine(AutoUnlockAfterDelay());
    }
}
```

### Testing Criteria for Phase 4

#### Test Scenarios
1. **Tightening Task**:
   - Tool snaps to socket with TIGHTEN task → rotates 90° → auto-unlocks
   
2. **Loosening Task**:
   - Tool snaps to socket with LOOSEN task → rotates -90° → auto-unlocks
   
3. **Multiple Tasks**:
   - Same tool performs TIGHTEN on bolt A, then LOOSEN on bolt B

#### Validation Checklist
- [ ] ToolTaskProvider correctly assigns tasks to tools
- [ ] Tool receives correct task when snapped
- [ ] Task completion detection works for both tighten/loosen
- [ ] Auto-unlock occurs after brief delay
- [ ] Tool becomes immediately removable after task
- [ ] Multiple tools can work simultaneously
- [ ] Task progress indicators work correctly

#### Success Criteria
- **Clear Task Assignment**: Tool knows what to do when snapped
- **Immediate Availability**: Tool ready for removal as soon as task is done
- **Multi-Task Capability**: Same tool can do different tasks in sequence
- **Performance**: Multiple tools work without conflicts

---

## Phase 5: Sequence System Integration
**Duration**: 3-4 days  
**Priority**: Training system integration

### Tasks

#### 5.1 Add New Step Types to InteractionStep
```csharp
public enum InteractionStepType 
{
    // Existing
    Grab, GrabAndSnap, TurnKnob, WaitForCondition, ShowInstruction,
    
    // New valve types
    ValveInstall,    // Full forward flow
    ValveRemove,     // Full reverse flow
    ValveTighten,    // Partial: tighten only
    ValveLoosen,     // Partial: loosen only
    
    // New tool types
    ToolTighten,     // Use tool to tighten
    ToolLoosen,      // Use tool to loosen
    ToolOperation    // Generic tool task
}
```

#### 5.2 Create Step Configuration Classes
```csharp
[System.Serializable]
public class ValveInteractionStep : InteractionStep
{
    public GameObjectReference valveObject;
    public GameObjectReference targetSocket;
    public float tightenAngle = 90f;
    public float loosenAngle = 90f;
    public Vector3 rotationAxis = Vector3.up;
    
    public override bool ValidateStep()
    {
        // Validation logic for valve steps
    }
}

[System.Serializable]
public class ToolOperationStep : InteractionStep
{
    public GameObjectReference toolObject;
    public GameObjectReference targetSocket;
    public ToolTask specificTask = ToolTask.None; // None = determined by socket
    public float taskAngle = 90f;
    
    public override bool ValidateStep()
    {
        // Validation logic for tool steps
    }
}
```

#### 5.3 Update Sequence Editor GUI
- Add valve/tool step configuration panels
- Tool-socket pairing interface
- Angle/threshold parameter controls
- Visual validation feedback

#### 5.4 Implement Sequence Step Execution
```csharp
public class ValveStepExecutor : StepExecutor
{
    public override void ExecuteStep(InteractionStep step)
    {
        var valveStep = step as ValveInteractionStep;
        var valveController = valveStep.valveObject.GameObject.GetComponent<ValveController>();
        
        // Subscribe to completion events
        valveController.OnValveTightened += () => CompleteStep();
        valveController.OnValveLoosened += () => CompleteStep();
    }
}
```

### Testing Criteria for Phase 5

#### Test Scenarios
1. **Complete Training Sequence**:
   ```
   Step 1: ValveInstall (main_valve → gauge_socket)
   Step 2: ToolTighten (wrench → bolt_A)
   Step 3: ToolTighten (wrench → bolt_B)  
   Step 4: ToolLoosen (wrench → bolt_A)
   Step 5: ValveRemove (main_valve)
   ```

2. **Mixed Profile Sequence**:
   - Combine valve steps, tool steps, and existing grab/snap steps

3. **Validation Testing**:
   - Invalid object references should show clear errors
   - Missing components should be detected
   - Socket compatibility should be validated

#### Validation Checklist
- [ ] All new step types appear in sequence editor
- [ ] Step configuration UI works for valve/tool parameters
- [ ] Sequence validation catches configuration errors
- [ ] Step execution triggers correctly from controller events
- [ ] Progress tracking works throughout sequence
- [ ] Multiple step types can be mixed in one sequence
- [ ] Sequence editor performance remains smooth

#### Success Criteria
- **Complete Integration**: New steps work seamlessly with existing system
- **User-Friendly Editor**: Clear, intuitive configuration interface
- **Robust Validation**: Catches errors before runtime
- **Reliable Execution**: Steps complete consistently

---

## Phase 6: Polish & Advanced Features
**Duration**: 2-3 days  
**Priority**: User experience optimization

### Tasks

#### 6.1 Enhanced Visual Feedback
- State-based material changes
- Progress indicators (rotation arcs)
- Socket compatibility highlighting
- Error state visual feedback

#### 6.2 Audio & Haptic Enhancement  
- State transition sound effects
- Rotation progress haptic pulses
- Completion confirmation feedback
- Error/warning audio cues

#### 6.3 Advanced Editor Features
- Profile preview in scene view
- Runtime debugging tools
- Performance optimization
- Comprehensive error handling

#### 6.4 Documentation & Examples
- Update CLAUDE.md with new profile information
- Create example scenes for both profile types
- Add inline code documentation
- Performance guidelines

### Testing Criteria for Phase 6

#### Test Scenarios
1. **Polish Validation**:
   - Visual feedback is clear and immediate
   - Audio cues enhance understanding
   - Haptic feedback feels natural

2. **Performance Testing**:
   - Multiple complex interactions run smoothly
   - No frame drops during state transitions
   - Memory usage remains stable

3. **User Experience Testing**:
   - Intuitive interaction flow
   - Clear error messages
   - Responsive feedback

#### Validation Checklist
- [ ] All visual feedback states implemented
- [ ] Audio feedback enhances experience
- [ ] Haptic feedback feels appropriate
- [ ] Performance meets target framerate
- [ ] Error handling is comprehensive
- [ ] Documentation is complete

#### Success Criteria
- **Professional Polish**: System feels production-ready
- **Excellent Performance**: Smooth operation under load
- **Clear Documentation**: Easy for others to understand and extend

---

## Testing Strategy Overview

### Test Scene Architecture
```
VRTrainingKit/TestScenes/
├── ValveTest_Basic.unity          # Phase 1-3 valve testing
├── ToolTest_Basic.unity           # Phase 4 tool testing
├── Integration_Test.unity         # Phase 5 sequence testing
├── Performance_Test.unity         # Multiple interactions
└── UserExperience_Test.unity      # Polish validation
```

### Automated Testing Considerations
- Unit tests for state machine logic
- Integration tests for XRI component interaction
- Performance benchmarks for complex scenes
- Validation tests for sequence configuration

### Manual Testing Protocol
1. **Daily Smoke Tests**: Basic functionality after each day's work
2. **Phase Completion Tests**: Full validation before moving to next phase
3. **Integration Tests**: Cross-phase compatibility validation
4. **User Acceptance Tests**: Real-world usage scenarios

## Risk Mitigation

### Technical Risks
- **Physics Conflicts**: Careful constraint management and testing
- **State Machine Complexity**: Clear state documentation and debugging tools
- **Performance**: Regular profiling and optimization

### Integration Risks  
- **Existing System Compatibility**: Gradual integration with extensive testing
- **Editor Integration**: Incremental UI development with validation
- **Sequence System Changes**: Backward compatibility maintenance

### Schedule Risks
- **Scope Creep**: Clear phase boundaries and success criteria
- **Technical Blockers**: Alternative approaches documented
- **Testing Time**: Adequate testing time built into each phase

## Success Metrics

### Functionality Metrics
- **State Transitions**: 100% reliable state machine operation
- **Physics Integration**: No constraint conflicts or glitches
- **Sequence Integration**: All new step types work correctly

### Performance Metrics
- **Frame Rate**: Maintain 90fps with multiple active interactions
- **Memory Usage**: No memory leaks during extended testing
- **Load Time**: Profile application under 100ms

### User Experience Metrics
- **Learning Time**: New users understand interaction within 30 seconds
- **Error Recovery**: Clear feedback for all error conditions
- **Workflow Efficiency**: Complex sequences execute smoothly

This phased approach ensures steady progress while maintaining system stability and user experience quality throughout the development process.
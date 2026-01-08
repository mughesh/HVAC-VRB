# Tool Style Profile - Complete Design Specification

## Overview
The Tool Style Profile handles tools that perform specific tasks (tighten or loosen) on objects and can be removed immediately after task completion. This pattern is ideal for allen keys, wrenches, screwdrivers, and similar tools that work on nuts, bolts, screws, and other fasteners.

## Core Concept
**Tool Behavior**: Tool is used to perform a specific task (either tighten or loosen something), and once that task is completed, the tool can be immediately removed from the socket. The tool doesn't need to be "loosened" like a valve - it completes its job and becomes removable.

## Key Differences from Valve Style
- **Task-Based Logic**: Tool has a predetermined task (tighten OR loosen) when approaching a socket
- **Immediate Unlock**: Once task is completed, tool becomes removable immediately
- **No Reverse Flow Required**: Tool doesn't need to be "undone" to remove it
- **Dynamic Task Assignment**: Same tool can tighten one bolt and loosen another in sequence

## Critical Grabbing Rules
- **UNLOCKED State**: Full grab capability (move + rotate freely)
- **LOCKED-WORKING State**: Rotation-only grab (no movement, position frozen)
- **LOCKED-COMPLETE State**: Brief state before auto-unlock, no interaction needed

## State Machine Architecture

### Primary States
```
UNLOCKED ←→ LOCKED
           ↗    ↘
    (snapped)  (substates)
                ├── WORKING (performing task)
                └── COMPLETE (task done, auto-unlocking)
```

### Detailed State Definitions

#### 1. UNLOCKED State
**Description**: Tool can be freely grabbed and moved around

**Properties**:
- **XRGrabInteractable**: Fully enabled (trackPosition = true, trackRotation = true)
- **Rigidbody**: isKinematic = false, constraints = None
- **Socket Interactor**: N/A (tool not in socket)
- **Physics**: Full physics simulation active
- **Task Status**: No assigned task

**User Capabilities**:
- ✅ Grab and move tool anywhere
- ✅ Rotate tool freely  
- ✅ Snap to compatible sockets
- ✅ Full spatial control

**Valid Transitions**:
- → LOCKED-WORKING (when snapped to compatible socket with assigned task)

---

#### 2. LOCKED-WORKING State
**Description**: Tool snapped to socket, position frozen, performing assigned task

**Properties**:
- **XRGrabInteractable**: Enabled but modified (trackPosition = false, trackRotation = true)
- **Rigidbody**: isKinematic = true, constraints = FreezePosition + selective rotation freeze
- **Socket Interactor**: Disabled (prevents removal)
- **Rotation**: Only allowed on specified axis
- **Task Status**: Active task (TIGHTEN or LOOSEN)

**User Capabilities**:
- ❌ Cannot move tool from socket position
- ✅ Can grab tool for rotation only
- ✅ Must rotate in task-specified direction
- ❌ Cannot remove from socket

**Task Logic**:
- **TIGHTEN Task**: Requires forward/clockwise rotation
- **LOOSEN Task**: Requires reverse/counterclockwise rotation
- **Progress Tracking**: Monitor rotation toward task completion
- **Completion Detection**: When rotation threshold reached for assigned task

**Valid Transitions**:
- → LOCKED-COMPLETE (when task rotation threshold met)

---

#### 3. LOCKED-COMPLETE State
**Description**: Task completed, tool ready for immediate removal

**Properties**:
- **XRGrabInteractable**: Same as WORKING state temporarily  
- **Socket Interactor**: Re-enabled immediately
- **Duration**: Brief transitional state (auto-advances)
- **Task Status**: COMPLETED

**Behavior**:
- **Immediate Socket Re-enable**: Socket interactor activated as soon as task completes
- **Auto-Unlock**: Automatically transitions to UNLOCKED after brief delay
- **Feedback**: Task completion visual/haptic feedback

**Valid Transitions**:
- → UNLOCKED (automatic after short delay)

## Task Assignment Logic

### Socket-Based Task Assignment
Each socket that works with tools must have a **ToolTaskProvider** component that specifies what task the tool should perform:

```csharp
public enum ToolTask
{
    None,
    Tighten,
    Loosen
}

public class ToolTaskProvider : MonoBehaviour
{
    [Header("Task Configuration")]
    public ToolTask requiredTask = ToolTask.Tighten;
    
    [Header("Task Parameters")]
    public float taskAngle = 90f;           // Degrees needed for task completion
    public Vector3 rotationAxis = Vector3.up; // Axis of rotation
    public float angleTolerance = 5f;       // Completion tolerance
    
    [Header("Visual Feedback")]
    public Material workingMaterial;        // Material while task in progress
    public Material completeMaterial;       // Material when task completed
    
    public ToolTask GetTaskForTool(GameObject tool)
    {
        // Could implement logic to determine task based on tool type,
        // current state of the target object, or other factors
        return requiredTask;
    }
}
```

### Dynamic Task Determination
```csharp
// Example: Bolt that needs tightening vs one that needs loosening
public class SmartToolTaskProvider : ToolTaskProvider
{
    [Header("Dynamic Task Logic")]
    public bool isCurrentlyTightened = false;
    
    public override ToolTask GetTaskForTool(GameObject tool)
    {
        // If bolt is loose, tool should tighten it
        // If bolt is tight, tool should loosen it
        return isCurrentlyTightened ? ToolTask.Loosen : ToolTask.Tighten;
    }
}
```

## Detailed Event Flow Diagrams

### Forward Flow (Tightening Task)
```
User grabs tool (UNLOCKED state)
- trackPosition = true, trackRotation = true
- rigidbody.isKinematic = false
     ↓
User brings tool to socket with TIGHTEN task
     ↓
SnapValidator.OnObjectSnapped() triggered
     ↓
ToolTaskProvider determines task = TIGHTEN
     ↓
ToolController.OnSocketSnapped() → LOCKED-WORKING
- trackPosition = false, trackRotation = true
- rigidbody.isKinematic = true  
- Apply position freeze + selective rotation constraints
- Disable socket interactor
- Set assignedTask = TIGHTEN
- Reset rotation tracking to 0°
     ↓
User grabs tool (rotation only, no movement)
     ↓
User rotates tool in tightening direction (clockwise)
- Track rotation angle accumulation
- Fire OnRotationChanged events
- Update task progress (0-1)
     ↓
Rotation >= taskAngle (e.g., 90°)
     ↓
ToolController transitions to LOCKED-COMPLETE
- Fire OnTaskCompleted event
- Re-enable socket interactor immediately
- Schedule auto-unlock after brief delay
     ↓
Brief delay for feedback (0.5 seconds)
     ↓
Auto-transition to UNLOCKED
- trackPosition = true, trackRotation = true
- rigidbody.isKinematic = false
- Remove all constraints
- assignedTask = None
     ↓
User can immediately grab tool to remove it
```

### Reverse Flow (Loosening Task)
```
User grabs tool (UNLOCKED state)
     ↓
User brings tool to socket with LOOSEN task
     ↓
ToolTaskProvider determines task = LOOSEN
     ↓
ToolController.OnSocketSnapped() → LOCKED-WORKING
- Same constraint setup as tightening
- Set assignedTask = LOOSEN
     ↓
User rotates tool in loosening direction (counterclockwise)
- Track negative rotation angle accumulation
- Fire OnRotationChanged events (negative values)
- Update task progress (0-1)
     ↓
Rotation <= -taskAngle (e.g., -90°)
     ↓
ToolController transitions to LOCKED-COMPLETE
- Fire OnTaskCompleted event  
- Re-enable socket interactor immediately
- Schedule auto-unlock after brief delay
     ↓
Auto-transition to UNLOCKED
     ↓
User can immediately grab tool to remove it
```

## Technical Implementation Details

### Task-Aware Rotation Tracking
```csharp
private void TrackRotation()
{
    Vector3 currentRotation = transform.eulerAngles;
    Vector3 deltaRotation = currentRotation - lastRotation;

    // Handle 360° wrapping
    if (deltaRotation.x > 180) deltaRotation.x -= 360;
    if (deltaRotation.y > 180) deltaRotation.y -= 360;
    if (deltaRotation.z > 180) deltaRotation.z -= 360;
    if (deltaRotation.x < -180) deltaRotation.x += 360;
    if (deltaRotation.y < -180) deltaRotation.y += 360;
    if (deltaRotation.z < -180) deltaRotation.z += 360;

    // Get rotation on specified axis
    float axisRotation = Vector3.Dot(deltaRotation, currentTaskProvider.rotationAxis);
    totalRotation += axisRotation;
    
    lastRotation = currentRotation;
    
    // Update progress based on assigned task
    UpdateTaskProgress();
    
    // Fire events
    OnRotationChanged?.Invoke(totalRotation);
    
    // Check for task completion
    CheckTaskCompletion();
}

private void UpdateTaskProgress()
{
    float progress = 0f;
    
    switch (assignedTask)
    {
        case ToolTask.Tighten:
            progress = Mathf.Clamp01(totalRotation / currentTaskProvider.taskAngle);
            OnTighteningProgress?.Invoke(progress);
            break;
            
        case ToolTask.Loosen:  
            progress = Mathf.Clamp01(-totalRotation / currentTaskProvider.taskAngle);
            OnLooseningProgress?.Invoke(progress);
            break;
    }
}
```

### Task Completion Detection
```csharp
private void CheckTaskCompletion()
{
    if (currentState != ToolState.LockedWorking) return;
    
    bool taskCompleted = false;
    
    switch (assignedTask)
    {
        case ToolTask.Tighten:
            taskCompleted = totalRotation >= (currentTaskProvider.taskAngle - currentTaskProvider.angleTolerance);
            break;
            
        case ToolTask.Loosen:
            taskCompleted = totalRotation <= -(currentTaskProvider.taskAngle - currentTaskProvider.angleTolerance);
            break;
    }
    
    if (taskCompleted)
    {
        CompleteTask();
    }
}

private void CompleteTask()
{
    // Transition to complete state
    currentState = ToolState.LockedComplete;
    currentSubstate = ToolSubstate.Complete;
    
    // Immediately re-enable socket interactor
    EnableSocketInteractor();
    
    // Fire completion events
    OnTaskCompleted?.Invoke(assignedTask);
    OnStateChanged?.Invoke(currentState);
    
    // Schedule auto-unlock
    StartCoroutine(AutoUnlockAfterDelay());
}

private IEnumerator AutoUnlockAfterDelay()
{
    yield return new WaitForSeconds(profile.autoUnlockDelay);
    TransitionToUnlocked();
}
```

### Socket Integration with Task Assignment
```csharp
public void OnSocketSnapped(GameObject socket)
{
    currentSocket = socket;
    
    // Get task provider from socket
    currentTaskProvider = socket.GetComponent<ToolTaskProvider>();
    if (currentTaskProvider == null)
    {
        Debug.LogError($"Socket {socket.name} missing ToolTaskProvider component!");
        return;
    }
    
    // Determine task for this tool
    assignedTask = currentTaskProvider.GetTaskForTool(gameObject);
    
    // Transition to working state
    currentState = ToolState.LockedWorking;  
    currentSubstate = ToolSubstate.Working;
    
    // Apply constraints and setup
    ApplyLockedConstraints();
    DisableSocketInteractor();
    ResetRotationTracking();
    
    // Fire events
    OnStateChanged?.Invoke(currentState);
    OnTaskAssigned?.Invoke(assignedTask);
    
    Debug.Log($"Tool {gameObject.name} assigned task: {assignedTask} on socket {socket.name}");
}
```

## Profile Configuration Parameters

### ToolStyleProfile ScriptableObject
```csharp
[CreateAssetMenu(fileName = "ToolStyleProfile", menuName = "VR Training/Tool Style Profile")]
public class ToolStyleProfile : InteractionProfile
{
    [Header("Tool Mechanics")]
    [Tooltip("Default rotation axis for tool operation")]
    public Vector3 defaultRotationAxis = Vector3.up;
    
    [Tooltip("Default angle required for task completion")]
    public float defaultTaskAngle = 90f;
    
    [Tooltip("Angle tolerance for task completion")]
    public float angleTolerance = 5f;

    [Header("Task Completion")]
    [Tooltip("Delay before auto-unlock after task completion")]
    public float autoUnlockDelay = 0.5f;
    
    [Tooltip("Haptic feedback intensity during task")]
    public float hapticIntensity = 0.3f;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this tool can work with")]
    public string[] compatibleSocketTags = {"tool_socket", "bolt", "screw"};
    
    [Tooltip("Specific socket objects this tool works with")]
    public GameObjectReference[] specificCompatibleSockets;
    
    [Tooltip("Use specific socket objects instead of tag-based matching")]
    public bool requireSpecificSockets = false;

    [Header("Visual Feedback")]
    [Tooltip("Material when tool is working on task")]
    public Material workingMaterial;
    
    [Tooltip("Material when task is completed")]
    public Material completeMaterial;
    
    [Tooltip("Show progress indicator during task")]
    public bool showProgressIndicator = true;

    public override void ApplyToGameObject(GameObject target)
    {
        // Add XRGrabInteractable, Rigidbody, Collider
        // Add ToolStyleController component  
        // Configure with this profile
    }
    
    public override bool ValidateGameObject(GameObject target)
    {
        return target != null && target.CompareTag("tool");
    }
}
```

## Event System Architecture

### ToolStyleController Events
```csharp
public class ToolStyleController : MonoBehaviour
{
    // State management events
    public event Action<ToolState> OnStateChanged;
    public event Action<ToolSubstate> OnSubstateChanged;

    // Task lifecycle events
    public event Action<ToolTask> OnTaskAssigned;    // Task determined for socket
    public event Action<ToolTask> OnTaskCompleted;   // Task successfully completed
    public event Action OnToolSnapped;              // Snapped to socket
    public event Action OnToolRemoved;              // Removed from socket

    // Progress tracking events
    public event Action<float> OnRotationChanged;      // Real-time angle
    public event Action<float> OnTighteningProgress;   // 0-1 tightening progress
    public event Action<float> OnLooseningProgress;    // 0-1 loosening progress
    public event Action<float> OnTaskProgress;         // 0-1 current task progress
    
    // Error events
    public event Action OnWrongDirection;      // Rotation in wrong direction for task
    public event Action OnNoTaskAssigned;     // Socket missing task provider
    public event Action OnIncompatibleSocket; // Tool not compatible with socket
}
```

### Integration with Sequence System
```csharp
// Subscribe to tool events for sequence step completion
toolController.OnTaskCompleted += (task) => {
    if (task == ToolTask.Tighten)
    {
        sequenceController.CompleteStep("tighten_bolt_A");
    }
    else if (task == ToolTask.Loosen)
    {
        sequenceController.CompleteStep("loosen_bolt_B");
    }
};
```

## Sequence Builder Integration

### New Step Types for Tool Operations
```csharp
public enum InteractionStepType 
{
    // Existing types
    Grab, GrabAndSnap, TurnKnob, WaitForCondition, ShowInstruction,
    ValveInstall, ValveRemove, ValveTighten, ValveLoosen,
    
    // New tool-specific types
    ToolTighten,     // Use tool to tighten something
    ToolLoosen,      // Use tool to loosen something
    ToolOperation,   // Generic tool operation (task determined by socket)
    MultiTool        // Tool performs multiple tasks in sequence
}
```

### Step Configuration Properties
```csharp
[System.Serializable]
public class ToolOperationStep : InteractionStep
{
    [Header("Tool Configuration")]
    public GameObjectReference toolObject;
    public GameObjectReference targetSocket;
    
    [Header("Task Configuration")]
    public ToolTask specificTask = ToolTask.None; // None = determined by socket
    public float taskAngle = 90f;
    public Vector3 rotationAxis = Vector3.up;
    public float angleTolerance = 5f;
    
    [Header("Sequence Integration")]
    public bool waitForCompletion = true;
    public bool removeToolAfterTask = false;  // Auto-remove or leave for user
    public string completionMessage;
    public bool showProgressIndicator = true;
    
    public override bool ValidateStep()
    {
        // Validate tool object has ToolStyleController
        if (!toolObject.GameObject?.GetComponent<ToolStyleController>()) return false;
        
        // Validate socket has ToolTaskProvider (if specific task not set)
        if (specificTask == ToolTask.None)
        {
            if (!targetSocket.GameObject?.GetComponent<ToolTaskProvider>()) return false;
        }
        
        // Validate socket has XRSocketInteractor
        if (!targetSocket.GameObject?.GetComponent<XRSocketInteractor>()) return false;
        
        // Validate tool-socket compatibility
        var controller = toolObject.GameObject.GetComponent<ToolStyleController>();
        return controller.IsSocketCompatible(targetSocket.GameObject);
    }
}
```

### Multi-Step Tool Operations
```csharp
[System.Serializable]
public class MultiToolStep : InteractionStep
{
    [Header("Multi-Task Configuration")]
    public GameObjectReference toolObject;
    public ToolTaskOperation[] operations;
    
    [System.Serializable]
    public class ToolTaskOperation
    {
        public GameObjectReference socket;
        public ToolTask task;
        public float angle = 90f;
        public string description;
    }
    
    // Example: Use allen key to loosen 3 bolts, then tighten 2 different bolts
}
```

## Advanced Task Logic

### Conditional Task Assignment
```csharp
public class ConditionalTaskProvider : ToolTaskProvider
{
    [Header("Conditional Logic")]
    public string conditionVariable = "main_valve_status";
    public string tightenConditionValue = "installed";
    public string loosenConditionValue = "needs_removal";
    
    public override ToolTask GetTaskForTool(GameObject tool)
    {
        // Check sequence state or game variables
        var sequenceController = FindObjectOfType<SequenceController>();
        string currentValue = sequenceController?.GetVariable(conditionVariable);
        
        if (currentValue == tightenConditionValue)
            return ToolTask.Tighten;
        else if (currentValue == loosenConditionValue)  
            return ToolTask.Loosen;
        else
            return ToolTask.None; // Invalid state
    }
}
```

### Progressive Tightening/Loosening
```csharp
public class ProgressiveTaskProvider : ToolTaskProvider
{
    [Header("Progressive Task")]
    public float[] tighteningStages = {30f, 60f, 90f}; // Multiple completion points
    public int currentStage = 0;
    
    public override float GetRequiredAngle()
    {
        return currentStage < tighteningStages.Length ? 
               tighteningStages[currentStage] : 
               tighteningStages[tighteningStages.Length - 1];
    }
    
    public void AdvanceStage()
    {
        if (currentStage < tighteningStages.Length - 1)
        {
            currentStage++;
            OnStageAdvanced?.Invoke(currentStage);
        }
    }
}
```

## Visual Feedback System

### Task-Based Material Changes
```csharp
private void UpdateVisualFeedback()
{
    var renderer = GetComponent<Renderer>();
    if (renderer == null) return;
    
    switch (currentState)
    {
        case ToolState.LockedWorking:
            renderer.material = profile.workingMaterial;
            break;
            
        case ToolState.LockedComplete:
            renderer.material = profile.completeMaterial;
            break;
            
        default:
            renderer.material = originalMaterial;
            break;
    }
    
    // Update socket visual feedback too
    UpdateSocketFeedback();
}

private void UpdateSocketFeedback()
{
    if (currentSocket && currentTaskProvider)
    {
        var socketRenderer = currentSocket.GetComponent<Renderer>();
        if (socketRenderer != null)
        {
            switch (assignedTask)
            {
                case ToolTask.Tighten:
                    socketRenderer.material = currentTaskProvider.tightenMaterial;
                    break;
                case ToolTask.Loosen:
                    socketRenderer.material = currentTaskProvider.loosenMaterial;
                    break;
            }
        }
    }
}
```

### Progress Indicators with Task Awareness
```csharp
private void UpdateProgressIndicator()
{
    if (!profile.showProgressIndicator) return;
    
    float progress = 0f;
    string taskDescription = "";
    
    switch (assignedTask)
    {
        case ToolTask.Tighten:
            progress = Mathf.Clamp01(totalRotation / currentTaskProvider.taskAngle);
            taskDescription = $"Tightening: {progress:P0}";
            OnTighteningProgress?.Invoke(progress);
            break;
            
        case ToolTask.Loosen:
            progress = Mathf.Clamp01(-totalRotation / currentTaskProvider.taskAngle);  
            taskDescription = $"Loosening: {progress:P0}";
            OnLooseningProgress?.Invoke(progress);
            break;
    }
    
    OnTaskProgress?.Invoke(progress);
    
    // Update UI elements
    if (progressUI != null)
    {
        progressUI.SetProgress(progress, taskDescription);
    }
}
```

## Error Handling & Edge Cases

### Missing Task Provider
```csharp
public void OnSocketSnapped(GameObject socket)
{
    currentSocket = socket;
    currentTaskProvider = socket.GetComponent<ToolTaskProvider>();
    
    if (currentTaskProvider == null)
    {
        Debug.LogError($"Socket {socket.name} missing ToolTaskProvider - cannot assign task!");
        OnNoTaskAssigned?.Invoke();
        
        // Auto-eject from socket
        StartCoroutine(EjectFromInvalidSocket());
        return;
    }
    
    // Continue with normal snapping logic...
}

private IEnumerator EjectFromInvalidSocket()
{
    yield return new WaitForSeconds(0.5f);
    
    // Force release from socket
    if (currentSocket?.GetComponent<XRSocketInteractor>() is var socketInteractor && socketInteractor)
    {
        var interactable = socketInteractor.GetOldestInteractableSelected();
        socketInteractor.interactionManager.SelectExit(socketInteractor, interactable);
    }
}
```

### Wrong Direction Rotation Detection
```csharp
private void DetectWrongDirection()
{
    float expectedDirection = assignedTask == ToolTask.Tighten ? 1f : -1f;
    float actualDirection = Mathf.Sign(totalRotation);
    
    if (Mathf.Abs(totalRotation) > 10f && actualDirection != expectedDirection)
    {
        OnWrongDirection?.Invoke();
        
        // Haptic feedback for wrong direction
        if (grabInteractable.isSelected)
        {
            var interactor = grabInteractable.GetOldestInteractorSelecting();
            if (interactor is XRBaseControllerInteractor controller)
            {
                controller.SendHapticImpulse(0.5f, 0.1f);
            }
        }
        
        // Show visual feedback
        StartCoroutine(ShowWrongDirectionFeedback());
    }
}
```

### Task Interruption Handling
```csharp
public void OnSocketReleased(GameObject socket)
{
    if (currentState == ToolState.LockedWorking)
    {
        // Task was interrupted before completion
        OnTaskInterrupted?.Invoke(assignedTask);
        
        // Save progress for potential resume
        savedProgress = totalRotation;
        savedTask = assignedTask;
    }
    
    // Clean up state
    TransitionToUnlocked();
    currentSocket = null;
    currentTaskProvider = null;
    assignedTask = ToolTask.None;
}

public void ResumeInterruptedTask()
{
    if (savedTask != ToolTask.None)
    {
        totalRotation = savedProgress;
        assignedTask = savedTask;
        // Continue from where left off
    }
}
```

## Testing Scenarios & Validation

### Basic Functionality Tests
1. **Task Assignment**: Tool receives correct task when snapped to socket
2. **Tightening Task**: Forward rotation completes tightening task correctly
3. **Loosening Task**: Reverse rotation completes loosening task correctly  
4. **Immediate Unlock**: Tool becomes removable immediately after task completion
5. **Auto-Unlock**: Tool automatically transitions to unlocked after brief delay
6. **Multiple Tasks**: Same tool can perform different tasks on different sockets

### Task-Specific Tests
1. **Wrong Direction**: Rotation in wrong direction doesn't advance task progress
2. **Partial Completion**: Releasing tool before task completion preserves progress
3. **Task Resume**: Re-snapping to same socket can continue interrupted task
4. **No Task Provider**: Tool handles sockets without task providers gracefully
5. **Incompatible Socket**: Tool rejects sockets it's not compatible with

### Integration Tests
1. **Sequence Steps**: ToolTighten and ToolLoosen steps complete correctly
2. **Multiple Tools**: Several tools work on different sockets simultaneously  
3. **Mixed Profiles**: Tool Style and Valve Style profiles work together
4. **Performance**: Smooth operation with complex multi-tool sequences

### User Experience Tests
1. **Clear Task Indication**: User understands what task to perform
2. **Progress Feedback**: Clear indication of task completion progress
3. **Immediate Response**: Tool becomes removable as soon as task is done
4. **Intuitive Workflow**: Tool behavior matches real-world tool usage expectations
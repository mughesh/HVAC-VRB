# Valve Style Profile - Complete Design Specification

## Overview
The Valve Style Profile handles objects that must be grabbed, placed in a socket, tightened, and can only be removed when properly loosened. This pattern is ideal for valves, fittings, bolts, and similar components that require a complete installation/removal cycle.

## Core Concept
**Valve Behavior**: Object can only be grabbed for movement when in UNLOCKED state. In all LOCKED states, the object can only be grabbed for rotation in place, not for movement.

## Critical Grabbing Rules
- **UNLOCKED State**: Full grab capability (move + rotate freely)
- **LOCKED-LOOSE State**: Rotation-only grab (no movement, position frozen)
- **LOCKED-TIGHT State**: Rotation-only grab (no movement, position frozen)

## State Machine Architecture

### Primary States
```
UNLOCKED ←→ LOCKED
           ↗    ↘
    (snapped)  (substates)
                ├── LOOSE (needs tightening)
                └── TIGHT (needs loosening)
```

### Detailed State Definitions

#### 1. UNLOCKED State
**Description**: Object can be freely grabbed and moved around

**Properties**:
- **XRGrabInteractable**: Fully enabled (trackPosition = true, trackRotation = true)
- **Rigidbody**: isKinematic = false, constraints = None
- **Socket Interactor**: N/A (object not in socket)
- **Physics**: Full physics simulation active

**User Capabilities**:
- ✅ Grab and move object anywhere
- ✅ Rotate object freely
- ✅ Snap to compatible sockets
- ✅ Full spatial control

**Valid Transitions**:
- → LOCKED-LOOSE (when snapped to compatible socket)

---

#### 2. LOCKED-LOOSE State
**Description**: Object just snapped to socket, position frozen, needs tightening

**Properties**:
- **XRGrabInteractable**: Enabled but modified (trackPosition = false, trackRotation = true)
- **Rigidbody**: isKinematic = true, constraints = FreezePosition + selective rotation freeze
- **Socket Interactor**: Disabled (prevents removal)
- **Rotation**: Only allowed on specified axis (e.g., Y-axis)

**User Capabilities**:
- ❌ Cannot move object from socket position
- ✅ Can grab object for rotation only
- ✅ Must rotate in tightening direction
- ❌ Cannot remove from socket

**Rotation Requirements**:
- **Direction**: Forward/clockwise rotation (tightening)
- **Threshold**: Must reach specified angle (e.g., 90°)
- **Feedback**: Progress indication during rotation

**Valid Transitions**:
- → LOCKED-TIGHT (after tightening rotation threshold met)

---

#### 3. LOCKED-TIGHT State  
**Description**: Object is properly tightened and secured, needs loosening to remove

**Properties**:
- **XRGrabInteractable**: Enabled but modified (trackPosition = false, trackRotation = true)
- **Rigidbody**: Same constraints as LOOSE state
- **Socket Interactor**: Disabled (prevents removal)
- **Rotation**: Only allowed on specified axis

**User Capabilities**:
- ❌ Cannot move object from socket position
- ✅ Can grab object for rotation only
- ✅ Must rotate in loosening direction
- ❌ Cannot remove from socket

**Rotation Requirements**:
- **Direction**: Reverse/counterclockwise rotation (loosening)
- **Threshold**: Must reach specified angle (e.g., -90°)
- **Critical Behavior**: Socket interactor re-enables DURING rotation when threshold met

**Valid Transitions**:
- → UNLOCKED (after loosening rotation threshold met AND user releases grab)

## Detailed Event Flow Diagrams

### Forward Flow (Installation)
```
User grabs valve (UNLOCKED state)
- trackPosition = true, trackRotation = true
- rigidbody.isKinematic = false
     ↓
User brings valve to compatible socket
     ↓
SnapValidator.OnObjectSnapped() triggered
     ↓
ValveController.OnSocketSnapped() → LOCKED-LOOSE
- trackPosition = false, trackRotation = true  
- rigidbody.isKinematic = true
- Apply position freeze + selective rotation constraints
- Disable socket interactor
- Reset rotation tracking to 0°
     ↓
User grabs valve (rotation only, no movement)
     ↓
User rotates valve in tightening direction
- Track rotation angle accumulation
- Fire OnRotationChanged events
     ↓
Rotation >= tightenThreshold (e.g., 90°)
     ↓
ValveController transitions to LOCKED-TIGHT
- Fire OnTighteningComplete event
- Visual/haptic feedback: "Valve secured"
- Reset rotation tracking for loosening phase
```

### Reverse Flow (Removal)
```
Valve in LOCKED-TIGHT state
     ↓
User grabs valve (rotation only, no movement)
     ↓
User rotates valve in loosening direction
- Track reverse rotation angle accumulation  
- Fire OnRotationChanged events (negative values)
     ↓
Rotation <= -loosenThreshold (e.g., -90°)
     ↓
ValveController transitions to LOCKED-LOOSE
- Re-enable socket interactor (CRITICAL: while user still holds grab)
- Fire OnLooseningComplete event
- Object now "snapped" to socket via interactor
     ↓
User releases grab
     ↓
ValveController transitions to UNLOCKED
- trackPosition = true, trackRotation = true
- rigidbody.isKinematic = false
- Remove all constraints
- Object automatically stays in socket due to proximity/snap
     ↓
User can now grab valve to remove it from socket
```

## Technical Implementation Details

### XRGrabInteractable Configuration

#### UNLOCKED State
```csharp
grabInteractable.trackPosition = true;
grabInteractable.trackRotation = true;
grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
```

#### LOCKED States (LOOSE and TIGHT)
```csharp
grabInteractable.trackPosition = false;  // Prevent movement
grabInteractable.trackRotation = true;   // Allow rotation only
grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
```

### Physics Constraints Management

#### UNLOCKED State Physics
```csharp
rigidbody.isKinematic = false;
rigidbody.constraints = RigidbodyConstraints.None;
rigidbody.useGravity = true;
```

#### LOCKED States Physics (both LOOSE and TIGHT)
```csharp
rigidbody.isKinematic = true;
rigidbody.constraints = RigidbodyConstraints.FreezePosition;

// Allow rotation only on specified axis (e.g., Y-axis only)
if (profile.rotationAxis.x == 0) 
    rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX;
if (profile.rotationAxis.y == 0) 
    rigidbody.constraints |= RigidbodyConstraints.FreezeRotationY;
if (profile.rotationAxis.z == 0) 
    rigidbody.constraints |= RigidbodyConstraints.FreezeRotationZ;
```

### Rotation Tracking System

#### Angle Accumulation with Direction Awareness
```csharp
private void TrackRotation()
{
    Vector3 currentRotation = transform.eulerAngles;
    Vector3 deltaRotation = currentRotation - lastRotation;

    // Handle 360° wrapping for smooth tracking
    if (deltaRotation.x > 180) deltaRotation.x -= 360;
    if (deltaRotation.y > 180) deltaRotation.y -= 360; 
    if (deltaRotation.z > 180) deltaRotation.z -= 360;
    if (deltaRotation.x < -180) deltaRotation.x += 360;
    if (deltaRotation.y < -180) deltaRotation.y += 360;
    if (deltaRotation.z < -180) deltaRotation.z += 360;

    // Calculate rotation on the specified axis
    float axisRotation = Vector3.Dot(deltaRotation, profile.rotationAxis);
    totalRotation += axisRotation;
    
    lastRotation = currentRotation;
    
    // Fire rotation events
    OnRotationChanged?.Invoke(totalRotation);
    
    // Check for state transitions
    CheckRotationThresholds();
}
```

#### State Transition Logic
```csharp
private void CheckRotationThresholds()
{
    switch (currentSubstate)
    {
        case ValveSubstate.Loose:
            // Forward rotation: tightening
            if (totalRotation >= profile.tightenThreshold - profile.angleTolerance)
            {
                TransitionToTight();
            }
            break;
            
        case ValveSubstate.Tight:
            // Reverse rotation: loosening  
            if (totalRotation <= -profile.loosenThreshold + profile.angleTolerance)
            {
                // CRITICAL: Re-enable socket while user still holds grab
                EnableSocketInteractor();
                TransitionToLoose();
            }
            break;
    }
}
```

### Socket Interactor Management

#### Critical Timing for Reverse Flow
```csharp
private void EnableSocketInteractor()
{
    if (currentSocket != null)
    {
        var socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
        {
            socketInteractor.socketActive = true;
            Debug.Log("Socket re-enabled during loosening - valve will snap when released");
        }
    }
}

private void DisableSocketInteractor()
{
    if (currentSocket != null)
    {
        var socketInteractor = currentSocket.GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
        {
            socketInteractor.socketActive = false;
            Debug.Log("Socket disabled - valve cannot be removed");
        }
    }
}
```

### State Transition Methods

#### Forward Transitions
```csharp
public void OnSocketSnapped(GameObject socket)
{
    currentSocket = socket;
    currentState = ValveState.Locked;
    currentSubstate = ValveSubstate.Loose;
    
    // Configure for rotation-only interaction
    ApplyLockedConstraints();
    DisableSocketInteractor();
    ResetRotationTracking();
    
    OnStateChanged?.Invoke(currentState);
    OnSubstateChanged?.Invoke(currentSubstate);
}

private void TransitionToTight()
{
    currentSubstate = ValveSubstate.Tight;
    ResetRotationTracking(); // Reset for loosening phase
    
    OnTighteningComplete?.Invoke();
    OnSubstateChanged?.Invoke(currentSubstate);
}
```

#### Reverse Transition with Release Detection
```csharp
private void TransitionToLoose()
{
    currentSubstate = ValveSubstate.Loose;
    // Socket already re-enabled by this point
    
    OnLooseningComplete?.Invoke();
    OnSubstateChanged?.Invoke(currentSubstate);
    
    // Start monitoring for grab release
    StartCoroutine(WaitForGrabRelease());
}

private IEnumerator WaitForGrabRelease()
{
    // Wait until user releases grab
    while (grabInteractable.isSelected)
    {
        yield return null;
    }
    
    // Now transition to unlocked
    TransitionToUnlocked();
}

private void TransitionToUnlocked()
{
    currentState = ValveState.Unlocked;
    currentSubstate = ValveSubstate.None;
    
    // Restore full grab capabilities
    RemoveAllConstraints();
    currentSocket = null;
    
    OnStateChanged?.Invoke(currentState);
}
```

## Profile Configuration Parameters

### ValveProfile ScriptableObject
```csharp
[CreateAssetMenu(fileName = "ValveProfile", menuName = "VR Training/Valve Profile")]
public class ValveProfile : InteractionProfile
{
    [Header("Valve Mechanics")]
    [Tooltip("Axis around which valve rotates (e.g., Vector3.up for Y-axis)")]
    public Vector3 rotationAxis = Vector3.up;
    
    [Tooltip("Degrees of rotation required to tighten valve")]
    public float tightenThreshold = 90f;
    
    [Tooltip("Degrees of reverse rotation required to loosen valve")]  
    public float loosenThreshold = 90f;
    
    [Tooltip("Angle tolerance for threshold completion")]
    public float angleTolerance = 5f;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this valve can work with")]
    public string[] compatibleSocketTags = {"valve_socket"};
    
    [Tooltip("Specific socket objects this valve works with")]
    public GameObjectReference[] specificCompatibleSockets;
    
    [Tooltip("Use specific socket objects instead of tag-based matching")]
    public bool requireSpecificSockets = false;

    [Header("Interaction Feel")]
    [Tooltip("Rotation speed multiplier when valve is locked")]
    public float lockedRotationSpeed = 1.0f;
    
    [Tooltip("Haptic feedback intensity during rotation")]
    public float hapticIntensity = 0.3f;
    
    [Tooltip("Visual feedback materials for different states")]
    public Material looseMaterial;
    public Material tightMaterial;

    // Inherited from InteractionProfile
    public override void ApplyToGameObject(GameObject target)
    {
        // Add XRGrabInteractable, Rigidbody, Collider
        // Add ValveController component
        // Configure with this profile
    }
    
    public override bool ValidateGameObject(GameObject target)
    {
        return target != null && target.CompareTag("valve");
    }
}
```

## Event System Architecture

### ValveController Events
```csharp
public class ValveController : MonoBehaviour
{
    // State management events
    public event Action<ValveState> OnStateChanged;
    public event Action<ValveSubstate> OnSubstateChanged;

    // Interaction lifecycle events
    public event Action OnValveSnapped;      // Snapped to socket
    public event Action OnValveTightened;    // Tightening completed
    public event Action OnValveLoosened;     // Loosening completed  
    public event Action OnValveRemoved;      // Removed from socket

    // Rotation tracking events
    public event Action<float> OnRotationChanged;     // Real-time angle
    public event Action<float> OnTighteningProgress;  // 0-1 progress
    public event Action<float> OnLooseningProgress;   // 0-1 progress
    
    // Error/validation events
    public event Action OnInvalidRotation;   // Wrong direction
    public event Action OnForceRemovalAttempt; // Try to remove when tight
}
```

### Integration with Sequence System
```csharp
// Subscribe to valve events for sequence step completion
valveController.OnValveTightened += () => {
    sequenceController.CompleteStep("tighten_main_valve");
};

valveController.OnValveRemoved += () => {
    sequenceController.CompleteStep("remove_main_valve");
};
```

## Sequence Builder Integration

### New Step Types for Valve Operations
```csharp
public enum InteractionStepType 
{
    // Existing types
    Grab, GrabAndSnap, TurnKnob, WaitForCondition, ShowInstruction,
    
    // New valve-specific types
    ValveInstall,    // Complete forward flow: grab → snap → tighten
    ValveRemove,     // Complete reverse flow: loosen → remove
    ValveTighten,    // Partial: just tighten (assumes already snapped)
    ValveLoosen      // Partial: just loosen (assumes already tight)
}
```

### Step Configuration Properties
```csharp
[System.Serializable]
public class ValveInteractionStep : InteractionStep
{
    [Header("Valve Configuration")]
    public GameObjectReference valveObject;
    public GameObjectReference targetSocket;
    
    [Header("Rotation Settings")]
    public float tightenAngle = 90f;
    public float loosenAngle = 90f;
    public Vector3 rotationAxis = Vector3.up;
    public float angleTolerance = 5f;
    
    [Header("Sequence Integration")]
    public bool waitForCompletion = true;
    public string completionMessage;
    public bool showProgressIndicator = true;
    
    public override bool ValidateStep()
    {
        // Validate valve object has ValveController
        if (!valveObject.GameObject?.GetComponent<ValveController>()) return false;
        
        // Validate socket compatibility
        if (!targetSocket.GameObject?.GetComponent<XRSocketInteractor>()) return false;
        
        // Validate valve-socket compatibility
        var controller = valveObject.GameObject.GetComponent<ValveController>();
        return controller.IsSocketCompatible(targetSocket.GameObject);
    }
}
```

## Visual Feedback System

### State-Based Material Changes
```csharp
private void UpdateVisualFeedback()
{
    var renderer = GetComponent<Renderer>();
    if (renderer == null) return;
    
    switch (currentSubstate)
    {
        case ValveSubstate.Loose:
            renderer.material = profile.looseMaterial; // Red/orange: needs tightening
            break;
            
        case ValveSubstate.Tight:
            renderer.material = profile.tightMaterial; // Green: properly secured
            break;
            
        default:
            renderer.material = originalMaterial; // Default appearance
            break;
    }
}
```

### Progress Indicators
```csharp
private void UpdateProgressIndicator()
{
    float progress = 0f;
    
    if (currentSubstate == ValveSubstate.Loose)
    {
        // Tightening progress: 0 to 1
        progress = Mathf.Clamp01(totalRotation / profile.tightenThreshold);
        OnTighteningProgress?.Invoke(progress);
    }
    else if (currentSubstate == ValveSubstate.Tight)
    {
        // Loosening progress: 0 to 1 (negative rotation mapped to positive progress)
        progress = Mathf.Clamp01(-totalRotation / profile.loosenThreshold);
        OnLooseningProgress?.Invoke(progress);
    }
}
```

## Error Handling & Edge Cases

### Force Removal Prevention
```csharp
private void OnGrabAttempt(SelectEnterEventArgs args)
{
    if (currentState == ValveState.Locked && currentSubstate == ValveSubstate.Tight)
    {
        // Prevent movement, show feedback
        OnForceRemovalAttempt?.Invoke();
        
        // Haptic pulse to indicate valve is locked
        if (profile.hapticIntensity > 0)
        {
            var controller = args.interactorObject as XRBaseControllerInteractor;
            controller?.SendHapticImpulse(profile.hapticIntensity, 0.2f);
        }
    }
}
```

### Socket Compatibility Validation
```csharp
public bool IsSocketCompatible(GameObject socket)
{
    if (!socket.GetComponent<XRSocketInteractor>()) return false;
    
    // Check specific sockets first
    if (profile.requireSpecificSockets)
    {
        return profile.specificCompatibleSockets?.Any(s => s.GameObject == socket) ?? false;
    }
    
    // Check compatible tags
    return profile.compatibleSocketTags?.Any(tag => socket.CompareTag(tag)) ?? false;
}
```

### Incomplete Rotation Handling
```csharp
private void OnGrabReleased(SelectExitEventArgs args)
{
    // If user releases grab before completing rotation, stay in current state
    // Progress is preserved - user can continue from where they left off
    
    if (currentSubstate == ValveSubstate.Loose && totalRotation < profile.tightenThreshold)
    {
        Debug.Log($"Valve tightening incomplete: {totalRotation:F1}° / {profile.tightenThreshold}°");
    }
    else if (currentSubstate == ValveSubstate.Tight && totalRotation > -profile.loosenThreshold)
    {
        Debug.Log($"Valve loosening incomplete: {-totalRotation:F1}° / {profile.loosenThreshold}°");
    }
}
```

## Testing Scenarios & Validation

### Basic Functionality Tests
1. **Initial Grab**: Valve in UNLOCKED state can be grabbed and moved freely
2. **Socket Snap**: Valve transitions to LOCKED-LOOSE when snapped to compatible socket  
3. **Position Lock**: In LOCKED-LOOSE, valve cannot be moved, only rotated
4. **Tightening**: Forward rotation reaches threshold → LOCKED-TIGHT transition
5. **Loosening**: Reverse rotation reaches threshold → socket re-enables
6. **Release to Unlock**: After loosening, grab release → UNLOCKED transition
7. **Final Removal**: UNLOCKED valve can be grabbed out of socket

### Edge Case Tests
1. **Incomplete Tightening**: Release grab before threshold → stays LOCKED-LOOSE
2. **Incomplete Loosening**: Release grab before threshold → stays LOCKED-TIGHT
3. **Wrong Direction**: Rotation in wrong direction has no effect on thresholds
4. **Force Removal**: Cannot grab LOCKED-TIGHT valve for movement (only rotation)
5. **Socket Loss**: If socket destroyed while valve locked → auto-unlock valve

### Integration Tests  
1. **Multiple Valves**: Several valves work independently without interference
2. **Sequence Steps**: ValveInstall and ValveRemove steps complete correctly
3. **Save/Load**: Valve states persist through scene transitions
4. **Performance**: Smooth operation with multiple active valves

### User Experience Tests
1. **Intuitive Feel**: Valve behavior matches real-world expectations
2. **Clear Feedback**: Visual and haptic cues clearly indicate state changes
3. **Error Recovery**: Clear indication when user tries invalid actions
4. **Smooth Transitions**: No jarring state changes or physics glitches
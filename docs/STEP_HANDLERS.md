# VR Training Kit - Step Handler System

## Overview

The Step Handler System is the modular heart of the VR Training Kit. It provides a clean separation between sequence orchestration and interaction-specific logic through the `IStepHandler` interface and `BaseStepHandler` base class.

## Core Architecture

### Interface Contract (`IStepHandler`)

```csharp
public interface IStepHandler
{
    bool CanHandle(InteractionStep.StepType stepType);
    void Initialize(ModularTrainingSequenceController controller);
    void StartStep(InteractionStep step);
    void StopStep(InteractionStep step);
    void Cleanup();
}
```

### Base Handler (`BaseStepHandler`)

Provides common functionality for all handlers:

```csharp
public abstract class BaseStepHandler : MonoBehaviour, IStepHandler
{
    protected ModularTrainingSequenceController controller;
    public event EventHandler<StepCompletionEventArgs> OnStepCompleted;

    // Helper methods: CompleteStep(), LogInfo(), LogDebug(), LogWarning(), LogError()
}
```

## Handler Lifecycle

### 1. **Discovery & Registration**
```csharp
// Auto-discovery in scene
var handlerComponents = FindObjectsOfType<MonoBehaviour>().OfType<IStepHandler>();
foreach (var handler in handlerComponents)
{
    RegisterHandler(handler);
}
```

### 2. **Initialization**
```csharp
public void RegisterHandler(IStepHandler handler)
{
    stepHandlers.Add(handler);
    handler.Initialize(this);

    if (handler is BaseStepHandler baseHandler)
    {
        baseHandler.OnStepCompleted += OnHandlerStepCompleted;
    }
}
```

### 3. **Step Execution**
```csharp
// Find appropriate handler
var handler = stepHandlers.FirstOrDefault(h => h.CanHandle(step.type));

// Start handling
activeStepHandlers[step] = handler;
handler.StartStep(step);
```

### 4. **Completion & Cleanup**
```csharp
// Handler reports completion
protected void CompleteStep(InteractionStep step, string reason)
{
    step.isCompleted = true;
    OnStepCompleted?.Invoke(this, new StepCompletionEventArgs(step, reason));
}

// Controller handles cleanup
handler.StopStep(step);
```

## Common Handler Patterns

### 1. **Component Caching Pattern**

All handlers cache components at initialization to avoid runtime lookups:

```csharp
void Awake()
{
    CacheGrabInteractables();
}

void CacheGrabInteractables()
{
    grabInteractables.Clear();
    var grabInteractableComponents = FindObjectsOfType<XRGrabInteractable>();
    foreach (var grabInteractable in grabInteractableComponents)
    {
        grabInteractables[grabInteractable.gameObject] = grabInteractable;
    }
}
```

### 2. **Event Delegation Pattern**

Handlers store event delegates for proper cleanup:

```csharp
// Dictionary to store delegates
private Dictionary<KnobController, System.Action<float>> knobEventDelegates;

// Subscribe with stored delegate
System.Action<float> angleDelegate = (angle) => OnKnobAngleChanged(step, angle);
knobEventDelegates[knobController] = angleDelegate;
knobController.OnAngleChanged += angleDelegate;

// Cleanup with stored delegate
knobController.OnAngleChanged -= knobEventDelegates[knobController];
knobEventDelegates.Remove(knobController);
```

### 3. **Active Step Tracking Pattern**

Handlers track active steps and their associated components:

```csharp
private Dictionary<InteractionStep, XRGrabInteractable> activeStepGrabs;

// Track active step
activeStepGrabs[step] = grabInteractable;

// Clean up on stop
if (activeStepGrabs.ContainsKey(step))
{
    var grabInteractable = activeStepGrabs[step];
    // Cleanup...
    activeStepGrabs.Remove(step);
}
```

### 4. **Error Handling Pattern**

Consistent error handling across all handlers:

```csharp
public override void StartStep(InteractionStep step)
{
    var targetObject = step.targetObject.GameObject;
    if (targetObject == null)
    {
        LogError($"Target object is null for step: {step.stepName}");
        return;
    }

    if (!grabInteractables.ContainsKey(targetObject))
    {
        LogError($"No grab interactable found for object: {targetObject.name}");
        return;
    }

    // Continue with step handling...
}
```

## Handler Implementations

### 1. **GrabStepHandler** (Simple)

**Purpose**: Handle object grabbing interactions
**Complexity**: Low
**XRI Integration**: Direct `XRGrabInteractable.selectEntered` subscription

```csharp
public override bool CanHandle(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.Grab;
}

void OnObjectGrabbed(InteractionStep step, SelectEnterEventArgs args)
{
    var grabbedObject = args.interactableObject.transform.gameObject;
    var expectedObject = step.targetObject.GameObject;

    if (grabbedObject == expectedObject)
    {
        CompleteStep(step, $"Grabbed {grabbedObject.name}");
    }
}
```

### 2. **SnapStepHandler** (Simple-Medium)

**Purpose**: Handle object snapping to sockets
**Complexity**: Low-Medium
**XRI Integration**: `XRSocketInteractor.selectEntered` subscription

```csharp
public override bool CanHandle(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.GrabAndSnap;
}

void OnObjectSnapped(InteractionStep step, SelectEnterEventArgs args)
{
    var snappedObject = args.interactableObject.transform.gameObject;
    var destinationSocket = args.interactorObject.transform.gameObject;

    if (snappedObject == step.targetObject.GameObject &&
        destinationSocket == step.destination.GameObject)
    {
        CompleteStep(step, $"Snapped {snappedObject.name} to {destinationSocket.name}");
    }
}
```

### 3. **KnobStepHandler** (Medium)

**Purpose**: Handle rotational interactions with angle tracking
**Complexity**: Medium
**Features**: Angle-based completion, parameter application, progress tracking

```csharp
public override bool CanHandle(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.TurnKnob;
}

void OnKnobAngleChanged(InteractionStep step, float currentAngle)
{
    float targetAngle = step.targetAngle;
    float tolerance = step.angleTolerance;

    // Calculate angle difference with proper wrapping
    float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

    if (angleDifference <= tolerance)
    {
        CompleteStep(step, $"Knob rotated to {currentAngle:F1}° (target: {targetAngle}°)");
    }
}
```

### 4. **ValveStepHandler** (Complex)

**Purpose**: Handle complex valve operations with state machine integration
**Complexity**: High
**Features**: State-aware monitoring, coroutines, runtime profile overrides, reflection

```csharp
public override bool CanHandle(InteractionStep.StepType stepType)
{
    return stepType == InteractionStep.StepType.TightenValve ||
           stepType == InteractionStep.StepType.LoosenValve ||
           stepType == InteractionStep.StepType.InstallValve ||
           stepType == InteractionStep.StepType.RemoveValve;
}

// Three-phase monitoring system
IEnumerator MonitorValveStepProgression(InteractionStep step, ValveController valveController)
{
    // PHASE 1: Apply parameter overrides early
    ApplyValveStepParameters(step, valveController);

    // PHASE 2: Wait for appropriate valve state
    yield return WaitForValveReadyState(step, valveController);

    // PHASE 3: Subscribe to completion events
    SubscribeToValveEvents(step, valveController);
}
```

#### Valve State Management

```csharp
IEnumerator WaitForValveReadyState(InteractionStep step, ValveController valveController)
{
    ValveState requiredState;
    ValveSubstate requiredSubstate;

    switch (step.type)
    {
        case InteractionStep.StepType.TightenValve:
            requiredState = ValveState.Locked;
            requiredSubstate = ValveSubstate.Loose;
            break;
        case InteractionStep.StepType.LoosenValve:
            requiredState = ValveState.Locked;
            requiredSubstate = ValveSubstate.Tight;
            break;
    }

    while (valveController.CurrentState != requiredState ||
           valveController.CurrentSubstate != requiredSubstate)
    {
        yield return new WaitForSeconds(0.5f);
    }
}
```

#### Runtime Profile Overrides

```csharp
void ApplyValveStepParameters(InteractionStep step, ValveController valveController)
{
    var currentProfile = GetValveProfile(valveController);

    // Create runtime profile with sequence-specific overrides
    var runtimeProfile = ScriptableObject.CreateInstance<ValveProfile>();

    // Apply selective overrides based on step type
    if (IsTightenStep(step.type) && step.tightenThreshold != currentProfile.tightenThreshold)
    {
        runtimeProfile.tightenThreshold = step.tightenThreshold;
    }

    valveController.Configure(runtimeProfile);
}
```

## Advanced Patterns

### 1. **Coroutine-Based Monitoring**

For complex handlers that need to monitor state over time:

```csharp
private Dictionary<InteractionStep, Coroutine> stepMonitoringCoroutines;

public override void StartStep(InteractionStep step)
{
    var monitoringCoroutine = StartCoroutine(MonitorStepProgression(step));
    stepMonitoringCoroutines[step] = monitoringCoroutine;
}

public override void StopStep(InteractionStep step)
{
    if (stepMonitoringCoroutines.ContainsKey(step))
    {
        StopCoroutine(stepMonitoringCoroutines[step]);
        stepMonitoringCoroutines.Remove(step);
    }
}
```

### 2. **Reflection-Based Profile Access**

For accessing private profile data in controllers:

```csharp
ValveProfile GetValveProfile(ValveController valveController)
{
    var profileField = typeof(ValveController).GetField("profile",
        BindingFlags.NonPublic | BindingFlags.Instance);

    if (profileField != null)
    {
        return (ValveProfile)profileField.GetValue(valveController);
    }

    return null;
}
```

### 3. **Smart Event Cleanup**

Preventing memory leaks and duplicate subscriptions:

```csharp
void SubscribeToValveEvents(InteractionStep step, ValveController valveController)
{
    // Unsubscribe first to prevent duplicates
    UnsubscribeFromValveEvents(valveController);

    switch (step.type)
    {
        case InteractionStep.StepType.TightenValve:
            System.Action tightenDelegate = () => OnValveTightened(step);
            valveTightenedEventDelegates[valveController] = tightenDelegate;
            valveController.OnValveTightened += tightenDelegate;
            break;
    }
}
```

## Creating New Handlers

### 1. **Basic Handler Template**

```csharp
public class CustomStepHandler : BaseStepHandler
{
    // Component cache
    private Dictionary<GameObject, CustomController> customControllers = new Dictionary<GameObject, CustomController>();

    // Active step tracking
    private Dictionary<InteractionStep, CustomController> activeSteps = new Dictionary<InteractionStep, CustomController>();

    void Awake()
    {
        CacheCustomControllers();
    }

    public override bool CanHandle(InteractionStep.StepType stepType)
    {
        return stepType == InteractionStep.StepType.CustomAction;
    }

    public override void Initialize(ModularTrainingSequenceController controller)
    {
        base.Initialize(controller);
        LogInfo("Custom handler initialized");
        CacheCustomControllers();
    }

    public override void StartStep(InteractionStep step)
    {
        // Implementation...
    }

    public override void StopStep(InteractionStep step)
    {
        // Cleanup...
    }

    public override void Cleanup()
    {
        // Full cleanup...
        base.Cleanup();
    }

    void CacheCustomControllers()
    {
        // Cache components...
    }
}
```

### 2. **Handler Complexity Guidelines**

**Simple Handlers** (Grab, Snap):
- Direct XRI event subscription
- Simple object matching
- Immediate completion

**Medium Handlers** (Knob):
- Parameter application
- Value-based completion (angles, distances)
- Progress tracking

**Complex Handlers** (Valve):
- State machine integration
- Coroutine-based monitoring
- Runtime configuration
- Multi-phase operation

## Performance Considerations

### 1. **Component Caching**
- Cache all components at initialization
- Refresh cache only when necessary
- Use dictionaries for O(1) lookups

### 2. **Event Management**
- Store delegates for proper cleanup
- Unsubscribe in reverse order of subscription
- Clear all references in Cleanup()

### 3. **Memory Management**
- Dispose of runtime-created ScriptableObjects
- Stop all coroutines in cleanup
- Clear all dictionaries and collections

## Debugging and Logging

### Consistent Logging Pattern

```csharp
LogInfo("🤏 GrabStepHandler initialized");
LogDebug($"🤏 Starting grab step: {step.stepName}");
LogWarning($"⚠️ No handler found for step: {step.stepName}");
LogError($"Target object is null for step: {step.stepName}");
```

### Debug Configuration

- Use controller's `enableDebugLogging` flag
- Provide detailed state information
- Include emojis for visual identification
- Log timing information for complex operations

The handler system provides a clean, extensible foundation for any type of VR interaction while maintaining consistent patterns and robust error handling.
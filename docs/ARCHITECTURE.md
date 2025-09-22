# VR Training Kit - System Architecture

## Overview

The VR Training Kit is a modular Unity-based system for creating structured VR training experiences. The architecture emphasizes separation of concerns, extensibility, and clean data flow through a handler-based pattern.

## Core Design Principles

### 1. **Modular Handler Architecture**
- **Separation of Concerns**: Sequence orchestration is separate from interaction-specific logic
- **Extensibility**: New interaction types can be added without modifying core controller
- **Event Isolation**: Each handler manages its own events and cleanup
- **Single Responsibility**: Each handler focuses on one interaction type

### 2. **Profile-Based Configuration**
- **ScriptableObject Driven**: All interaction behaviors defined through reusable profiles
- **Runtime Overrides**: Sequence-specific parameters can override profile defaults
- **Validation System**: Built-in validation ensures proper configuration
- **Asset Management**: Profiles stored in Resources for auto-loading

### 3. **Hierarchical Data Structure**
- **Program** → **Module** → **TaskGroup** → **InteractionStep**
- **Scalable**: Supports complex multi-level training programs
- **Flexible**: Optional steps, parallel execution, conditional dependencies
- **Serializable**: Full Unity serialization support with custom property drawers

## System Components

### Core Layer (`Scripts/Core/`)

#### Controllers
- **`ModularTrainingSequenceController`**: Main orchestrator, manages sequence flow
- **`KnobController`**: Handles rotatable object behavior and events
- **`ValveController`**: Complex state machine for valve interactions
- **`ToolController`**: Manages tool-based interactions

#### Interfaces
- **`IStepHandler`**: Contract for all step handlers
- **`BaseStepHandler`**: Common functionality for all handlers

#### Services
- **`InteractionSetupService`**: Scene scanning and profile application

#### Utilities
- **`InteractionLayerManager`**: XR interaction layer management
- **`VRTrainingDebug`**: Centralized logging and debugging

### Handler Layer (`Scripts/StepHandlers/`)

Specialized handlers for each interaction type:
- **`GrabStepHandler`**: Object grabbing interactions
- **`KnobStepHandler`**: Rotational interactions with angle tracking
- **`SnapStepHandler`**: Object snapping to sockets
- **`ValveStepHandler`**: Complex valve operations with state management

### Configuration Layer (`Scripts/Profiles/`)

#### Base
- **`InteractionProfile`**: Abstract base class for all profiles

#### Implementations
- **`GrabProfile`**: Grabbable object configuration
- **`KnobProfile`**: Rotatable object configuration
- **`SnapProfile`**: Socket configuration
- **`ToolProfile`**: Tool-specific configuration
- **`ValveProfile`**: Valve behavior configuration

### Data Layer (`Scripts/SequenceSystem/`)

#### Data Structures
- **`TrainingSequence`**: Core hierarchical data classes
- **`TrainingSequenceAsset`**: ScriptableObject wrapper for serialization
- **`GameObjectReference`**: Safe GameObject references for serialization

#### Validation
- **`SequenceValidator`**: Training sequence validation logic
- **`SnapValidator`**: Socket-specific validation

### Editor Layer (`Scripts/Editor/`)

#### Windows
- **`VRInteractionSetupWindow`**: Main setup and configuration GUI

#### Property Drawers
- **`GameObjectReferenceDrawer`**: Custom inspector for GameObject references

### Legacy Layer (`Scripts/Legacy/`)
- **`LegacySequenceController`**: Previous monolithic implementation (deprecated)

## Data Flow Architecture

```
TrainingSequenceAsset → ModularTrainingSequenceController
                    ↓
            Handler Registration & Discovery
                    ↓
            Task Group Execution Loop
                    ↓
        Step Distribution to Appropriate Handlers
                    ↓
            Component Caching & Event Subscription
                    ↓
            Runtime Monitoring & Completion Detection
                    ↓
            Progress Tracking & Sequence Advancement
```

## Key Patterns

### 1. **Handler Registration Pattern**
```csharp
// Auto-discovery of handlers in scene
var handlerComponents = FindObjectsOfType<MonoBehaviour>().OfType<IStepHandler>();
foreach (var handler in handlerComponents)
{
    RegisterHandler(handler);
}

// Handler capability checking
IStepHandler FindHandlerForStep(InteractionStep step)
{
    return stepHandlers.FirstOrDefault(h => h.CanHandle(step.type));
}
```

### 2. **Component Caching Pattern**
```csharp
// Cache components at initialization to avoid runtime FindObjectsOfType calls
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

### 3. **Event Delegation Pattern**
```csharp
// Store delegates for proper cleanup
private Dictionary<KnobController, System.Action<float>> knobEventDelegates;

// Subscribe with stored delegate
System.Action<float> angleDelegate = (angle) => OnKnobAngleChanged(step, angle);
knobEventDelegates[knobController] = angleDelegate;
knobController.OnAngleChanged += angleDelegate;

// Cleanup with stored delegate
knobController.OnAngleChanged -= knobEventDelegates[knobController];
```

### 4. **Runtime Profile Override Pattern**
```csharp
// Create runtime profile with sequence-specific overrides
var runtimeProfile = ScriptableObject.CreateInstance<ValveProfile>();
// Copy base settings
runtimeProfile.tightenThreshold = step.tightenThreshold != currentProfile.tightenThreshold
    ? step.tightenThreshold
    : currentProfile.tightenThreshold;
// Apply to controller
valveController.Configure(runtimeProfile);
```

### 5. **Safe GameObject Reference Pattern**
```csharp
public class GameObjectReference
{
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private string _gameObjectName;

    public GameObject GameObject
    {
        get
        {
            if (_gameObject != null) return _gameObject;
            if (!string.IsNullOrEmpty(_gameObjectName))
                return GameObject.Find(_gameObjectName); // Fallback
            return null;
        }
    }
}
```

## Architectural Benefits

### 1. **Extensibility**
- New interaction types: Implement `IStepHandler`
- New profiles: Inherit from `InteractionProfile`
- New step types: Add to `InteractionStep.StepType` enum

### 2. **Maintainability**
- Clear separation between sequence logic and interaction logic
- Self-contained handlers with proper cleanup
- Comprehensive validation and error handling

### 3. **Performance**
- Component caching minimizes runtime lookups
- Event-driven completion detection
- Efficient handler capability checking

### 4. **Testability**
- Interface-based design enables mocking
- Clear dependencies and data flow
- Isolated handler logic

## Integration Points

### With Unity XR Interaction Toolkit
- Handlers subscribe to XRI events (selectEntered, etc.)
- Profiles configure XRI components (XRGrabInteractable, XRSocketInteractor)
- Layer management integrates with XRI settings

### With Unity Editor
- Custom property drawers for complex data types
- ScriptableObject integration for asset management
- Editor windows for visual configuration

### With External Systems
- Event system allows UI integration
- Progress tracking enables analytics
- Validation system supports quality assurance

## Naming Conventions

### Files
- Controllers: `[Name]Controller.cs`
- Handlers: `[Type]StepHandler.cs`
- Profiles: `[Type]Profile.cs`
- Data: Descriptive names (`TrainingSequence.cs`)

### Classes
- Interfaces: `I[Name]`
- Handlers: `[Type]StepHandler : BaseStepHandler`
- Profiles: `[Type]Profile : InteractionProfile`

### Methods
- Handler lifecycle: `Initialize`, `StartStep`, `StopStep`, `Cleanup`
- Profile application: `ApplyToGameObject`, `ValidateGameObject`
- Caching: `Cache[ComponentType]s`

### Events
- Completion: `OnStepCompleted`, `OnTaskGroupCompleted`
- State changes: `OnAngleChanged`, `OnValveTightened`

This architecture provides a solid foundation for complex VR training scenarios while maintaining clean code organization and extensibility.
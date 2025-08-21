# VR Training Kit

An automated XR interaction setup system for Unity that streamlines the process of creating VR training scenarios.

## Features

### 🏷️ Tag-Based Object Discovery
- Automatically scans scenes for objects tagged with `grab`, `knob`, or `snap`
- Groups objects by interaction type for batch configuration
- Real-time validation and feedback

### 📋 Scriptable Object Profiles
- **GrabProfile**: Configurable grab interactions with physics settings
- **KnobProfile**: Rotatable objects with hinge joints, limits, and haptic feedback  
- **SnapProfile**: Socket interactions with validation rules and visual feedback

### 🖥️ Editor GUI System
- **Setup Tab**: Scene scanning and object discovery
- **Configure Tab**: Profile selection and creation
- **Sequence Tab**: Training flow state machine setup
- **Validate Tab**: Comprehensive system validation

### 🔄 Sequence Controller
- State-based training progression system
- Condition-driven state transitions
- Visual feedback for locked/available actions
- Event-driven architecture with callbacks

### ⚙️ Advanced Features
- Interaction layer management with custom UI
- Automatic component configuration
- Undo/Redo support
- Asset management and profile discovery
- Comprehensive validation system

## Quick Start

1. **Initialize**: `VR Training > Setup > Initialize VR Training Kit`
2. **Tag Objects**: Apply `grab`, `knob`, or `snap` tags to scene objects
3. **Open Setup Assistant**: `VR Training > Setup Assistant`
4. **Scan Scene**: Click "Scan Scene" in Setup tab
5. **Configure**: Select profiles and apply to objects
6. **Validate**: Check for issues in Validate tab

## Object Types

### Grab Objects (`grab` tag)
- Objects that can be picked up and moved
- Automatically configured with XRGrabInteractable
- Physics and collider setup
- Throw mechanics and dynamic attach

### Knob Objects (`knob` tag)  
- Rotatable objects with constraints (valves, dials)
- Hinge joint configuration with limits
- Spring damping and target positions
- Haptic feedback integration

### Snap Points (`snap` tag)
- Socket interactors for object attachment
- Validation rules for accepted objects
- Visual hover feedback
- Sequence integration callbacks

## Profile Configuration

### GrabProfile Settings
```csharp
- Movement Type: Kinematic/VelocityTracking/Instantaneous
- Track Position/Rotation: Control what movements are tracked
- Throw Settings: Velocity scaling and angular velocity
- Dynamic Attach: Smooth grab transitions
- Collider Type: Box/Sphere/Capsule/Mesh/None
```

### KnobProfile Settings
```csharp
- Rotation Axis: X/Y/Z axis constraint
- Angle Limits: Min/max rotation with bounce settings
- Hinge Joint: Spring, damper, and target configuration
- Haptic Feedback: Intensity and trigger settings
- Snap to Angles: Discrete rotation positions
```

### SnapProfile Settings
```csharp
- Socket Radius: Detection distance
- Accepted Tags: What objects can be snapped
- Specific Objects: Restrict to particular GameObjects
- Hover Materials: Visual feedback configuration
- Validation Rules: Custom acceptance logic
```

## Sequence System

The sequence controller enables complex training scenarios with state-based progression:

### State Groups
- Define training phases with specific allowed/locked actions
- Condition-based activation (object snapped, knob turned, etc.)
- Visual feedback integration

### Validation Components
- **SequenceValidator**: Locks interactions until prerequisites are met
- **SnapValidator**: Custom validation for socket connections
- Real-time visual feedback

### Example AC Leak Testing Sequence
```csharp
Initial Setup State:
- Allow: Hose connections
- Lock: Nitrogen valve operation

System Ready State:
- Activate when: All hoses connected
- Allow: Valve operation, gauge adjustment
```

## Architecture

### Core Components
- **InteractionProfile**: Base class for all profiles
- **InteractionSetupService**: Scene scanning and component application
- **VRInteractionSetupWindow**: Main GUI interface
- **InteractionLayerManager**: Advanced layer management
- **SequenceController**: Training flow state machine

### Helper Components
- **KnobController**: Advanced knob behavior and constraints
- **SnapValidator**: Socket validation and callbacks
- **SequenceValidator**: Training progression enforcement

## Integration

### Unity XR Toolkit
- Built on Unity XR Interaction Toolkit
- Compatible with OpenXR and major VR platforms
- Supports hand tracking and controller input

### VR Builder Integration
- Works alongside VR Builder processes
- Shares interaction components
- Complementary training flow systems

## Best Practices

1. **Object Hierarchy**: Place XR components on parent objects, colliders on mesh children
2. **Layer Management**: Use interaction layers to control what can interact with what  
3. **Profile Reuse**: Create reusable profiles for common object types
4. **Sequence Design**: Break complex training into logical state progressions
5. **Validation**: Always run validation after major changes

## Troubleshooting

### Common Issues
- **Missing Components**: Run validation to identify missing required components
- **Physics Problems**: Ensure rigidbodies are configured correctly for interaction type
- **Layer Conflicts**: Check interaction layer assignments
- **Performance**: Large object counts may require batch processing optimization

### Debug Features
- Console logging for all major operations
- Visual debug UI for sequence controller
- Validation reports with specific issue descriptions
- Scene analysis statistics

## Future Enhancements

Based on the current architecture, potential improvements include:
- Visual scripting integration for complex sequences
- Analytics integration for training effectiveness
- Multi-user collaboration features
- Advanced haptic feedback patterns
- AI-driven hint systems
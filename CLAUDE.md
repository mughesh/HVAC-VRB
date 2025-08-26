# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## VR Training Kit - Reusable Interaction Framework

The VR Training Kit (`Assets/VRTrainingKit/`) is a Unity-based automated XR interaction setup system that streamlines VR training scenario creation through tag-based object discovery and profile-driven configuration.

### Core Architecture

#### Profile-Based Configuration System
The framework uses ScriptableObject profiles to define interaction behaviors:

**Base Architecture:**
- `InteractionProfile.cs` - Abstract base class with `ApplyToGameObject()` and `ValidateGameObject()` methods
- All profiles inherit from this base and implement specific XR Interaction Toolkit component configuration

**Profile Types:**
- `GrabProfile.cs` - Configures grabbable objects with XRGrabInteractable, Rigidbody, and Colliders
- `KnobProfile.cs` - Sets up rotatable objects with XRGrabInteractable, HingeJoint, and KnobController
- `SnapProfile.cs` - Creates socket points with XRSocketInteractor, SphereCollider, and SnapValidator

#### Automated Setup Workflow

**Tag-Based Discovery:**
Objects are tagged with: `grab`, `knob`, or `snap` for automatic detection by `InteractionSetupService.ScanScene()`

**Setup Process:**
1. Scene scanning discovers tagged objects
2. Profile selection defines component configuration
3. Automated component application via `ApplyComponentsToObjects()`
4. Validation ensures proper setup completion

### Key Components Deep Dive

#### InteractionSetupService.cs
Central service class handling the automated setup pipeline:

**Core Methods:**
- `ScanScene()` - Returns `SceneAnalysis` object with categorized tagged objects
- `ApplyComponentsToObjects(List<GameObject> objects, InteractionProfile profile)` - Batch applies profile configurations
- `ValidateSetup()` - Returns list of configuration issues for troubleshooting
- `QuickSetup()` - One-click setup using default profiles from Resources folder
- `CleanupComponents()` - Removes all VR interaction components from tagged objects

**Smart Component Management:**
- Intelligent collider placement (parent vs mesh child selection)
- Automatic Rigidbody configuration based on interaction type
- Component validation and error reporting

#### Profile Implementation Details

**GrabProfile Configuration:**
```csharp
// Movement settings
movementType: VelocityTracking/Kinematic/Instantaneous
trackPosition/trackRotation: Boolean controls
throwOnDetach: Enable physics throwing

// Collider intelligence
addColliderToMeshChild: Automatically finds mesh children for collider placement
FindMeshChild(): Searches component hierarchy for appropriate collider targets
```

**KnobProfile Advanced Features:**
```csharp
// Rotation constraints
rotationAxis: X/Y/Z axis selection
useLimits: Enable angle restrictions with min/max bounds
autoConfigureConnectedAnchor: ENABLED by default for joint stability

// Physics integration
HingeJoint configuration with spring damping
targetPosition and springValue for precise control
Joint limits with bounceMinVelocity for realistic behavior

// Critical settings for joint functionality
movementType: MUST be VelocityTracking
trackPosition: false (rotation only)
trackRotation: true
isKinematic: false (required for HingeJoint)
```

**SnapProfile Validation System:**
```csharp
// Acceptance rules
acceptedTags[]: Array of valid object tags
requireSpecificObjects: Restrict to particular GameObjects
specificAcceptedObjects[]: Explicit object whitelist

// Socket configuration
socketRadius: Detection distance
socketActive: Enable/disable socket
recycleDelayTime: Delay before allowing re-snapping
```

### Advanced Controllers

#### KnobController.cs
Sophisticated rotatable object behavior system:

**Angle Management:**
- Real-time angle tracking with `CurrentAngle` property
- `NormalizedValue` provides 0-1 range based on angle limits
- `SetAngle(float, bool immediate)` for programmatic control with smooth rotation option

**Event System:**
```csharp
event Action<float> OnAngleChanged;  // Fires on rotation updates
event Action<float> OnSnapToAngle;   // Triggers on snap-to-grid behavior
```

**Smart Features:**
- Automatic snap-to-angle on release when `snapToAngles` enabled
- Haptic feedback integration with configurable intensity
- Angle limit enforcement with visual/physical constraints

#### SnapValidator.cs
Comprehensive socket validation and event handling:

**Validation Pipeline:**
- `IsValidForSocket()` checks tags and specific object requirements
- `OnHoverEntered()` provides preview validation feedback
- Invalid object ejection with `EjectInvalidObject()` coroutine

**Sequence Integration:**
- Automatic SequenceController event firing on snap/unsnap
- `OnObjectSnapped(gameObject, snappedObject)` for state transitions
- `OnObjectUnsnapped(gameObject, removedObject)` for state reversal

### Editor Integration

#### VRInteractionSetupWindow.cs
Multi-tab editor window accessed via `VR Training > Setup Assistant`:

**Setup Tab:**
- Real-time scene scanning with object categorization
- Interactive object lists with status indicators (✓ configured, ○ pending)
- Bulk operations: "Apply All Components" and "Clean All"
- Per-object interaction layer management with dropdown controls

**Configure Tab:**
- Profile asset management with auto-discovery
- "Create New [Type] Profile" buttons for custom profile generation
- Available profile listing from project assets
- "Create All Default Profiles" for quick start

**Sequence Tab:**
- SequenceController detection and creation
- Built-in AC Leak Testing template
- Integration guidance and help documentation

**Validate Tab:**
- Comprehensive setup validation with detailed issue reporting
- Component dependency checking (XRGrabInteractable, Rigidbody, Colliders)
- Missing component identification with specific object references

#### InteractionLayerManager.cs
Advanced interaction layer management system:

**Layer Discovery:**
- `GetConfiguredLayerNames()` reads from InteractionLayerSettings asset
- Automatic fallback to `InteractionLayerMask.LayerToName()` if settings unavailable
- `FindNextAvailableLayer()` for automatic layer assignment

**Editor Controls:**
- `DrawLayerMaskDropdown()` creates Unity-style layer mask fields
- Real-time layer conversion between display and actual masks
- "Edit Layers" button for direct settings access

**Programmatic Control:**
- `SetInteractionLayer(GameObject, LayerMask)` with Undo support
- `GetInteractionLayer(GameObject)` for current layer retrieval
- Support for XRGrabInteractable, XRSocketInteractor, and XRSimpleInteractable

### Sequence System Integration

The framework includes a complete sequence management system for training progression:

**SequenceController.cs:**
- State-based training flow with `StateGroup` definitions
- Condition-driven state transitions (ObjectSnapped, AllObjectsSnapped, KnobTurned)
- Visual feedback integration with locked/available action indicators
- Real-time debug UI for development and testing

**SequenceValidator.cs:**
- Object-level sequence requirement enforcement
- `requiredStateGroup` property for state-dependent interactions
- Warning vs blocking modes with `allowWithWarning` flag
- Visual feedback through material/color changes

### Development Commands

#### Setup Operations
- `InteractionSetupService.ScanScene()` - Analyze current scene for tagged objects
- `InteractionSetupService.QuickSetup()` - One-click setup with default profiles
- `InteractionSetupService.ValidateSetup()` - Check for configuration issues

#### Profile Management
- Create profiles via `[CreateAssetMenu]`: VR Training > Grab/Knob/Snap Profile
- Store default profiles in `Assets/VRTrainingKit/Resources/` for auto-loading
- Use `profile.ApplyToGameObject(target)` for programmatic application

#### Editor Window Access
- `Window > VR Training > Setup Assistant` - Main configuration interface
- Setup tab: Scene scanning and object configuration
- Configure tab: Profile selection and creation
- Validate tab: Issue identification and troubleshooting

### Best Practices

#### Object Hierarchy
- Place XR components on parent objects for interaction
- Colliders automatically placed on mesh children when appropriate
- Maintain parent-child relationship for proper physics behavior

#### Profile Design
- Create reusable profiles for common interaction patterns
- Use descriptive `profileName` values for identification
- Test validation logic with `ValidateGameObject()` before application

#### Layer Management
- Assign distinct interaction layers for selective interaction control
- Use layer masks to prevent unwanted grab/snap combinations
- Leverage "Edit Layers" button for quick XRI settings access

#### Validation Workflow
- Always run validation after major configuration changes
- Check console output for detailed error messages
- Use Setup Assistant's Validate tab for comprehensive issue reports

### Framework Extension Points

#### Custom Profiles
Extend `InteractionProfile` base class:
```csharp
public override void ApplyToGameObject(GameObject target) { /* Implementation */ }
public override bool ValidateGameObject(GameObject target) { /* Validation */ }
```

#### Component Integration
Add helper components similar to `KnobController` and `SnapValidator` for specialized behaviors while maintaining profile-driven configuration approach.

#### Sequence Conditions
Extend `SequenceController.StateGroup.Condition` enum for custom training flow triggers and validation logic.
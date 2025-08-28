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

## Hierarchical Training Sequence System

**Status: Phase 3 Complete** (Core Data, Asset System, Read-Only GUI)

The framework includes a comprehensive hierarchical training sequence system for creating structured VR training programs. This system provides a scalable architecture for building complex multi-step training scenarios with proper organization and validation.

### Architecture Overview

The system uses a four-level hierarchy:
- **TrainingProgram** (e.g., "HVAC Training") - Top-level container
- **TrainingModule** (e.g., "Leak Testing") - Major training sections  
- **TaskGroup** (e.g., "Initial Setup") - Related step collections
- **InteractionStep** (e.g., "Remove valve cap") - Individual actions

### Core Components

#### TrainingSequence.cs - Data Structures (Phase 1)
Central data model containing all hierarchical classes:

**GameObjectReference Class:**
- Safe GameObject references that work in both runtime and asset files
- Automatic fallback using GameObject.Find() for broken references
- Scene path tracking for debugging
- Custom property drawer integration for Unity Inspector

**TrainingProgram Class:**
```csharp
public class TrainingProgram
{
    public string programName;
    public string description;
    public List<TrainingModule> modules;
    public bool isExpanded; // UI state management
}
```

**InteractionStep Class:**
- Five step types: `Grab`, `GrabAndSnap`, `TurnKnob`, `WaitForCondition`, `ShowInstruction`
- Built-in validation with `IsValid()` and `GetValidationMessage()`
- Parallel execution support with `allowParallel` flag
- Optional step handling with `isOptional` flag
- Wait conditions for step dependencies

**TrainingSequenceFactory:**
- `CreateHVACLeakTestingProgram()` - Pre-built HVAC template
- `CreateEmptyProgram()` - Starter template with basic structure

#### TrainingSequenceAsset.cs - Asset System (Phase 2)
ScriptableObject integration for save/load functionality:

**Key Features:**
- `[CreateAssetMenu]` integration for asset creation
- Comprehensive validation system with errors/warnings categories
- Statistics tracking with `ProgramStats` class
- Deep copy functionality using JSON serialization
- Automatic dirty marking for Unity asset management

**TrainingSequenceAssetManager:**
- `CreateHVACTemplateAsset()` - Template asset generation
- `SaveAssetToSequencesFolder()` - Automatic folder management
- `LoadAllSequenceAssets()` - Project-wide asset discovery

**Validation System:**
```csharp
public class ValidationResult
{
    public List<string> errors;    // Blocking issues
    public List<string> warnings;  // Non-critical issues
    public bool HasErrors => errors.Count > 0;
    public bool IsValid => !HasErrors;
}
```

#### VRInteractionSetupWindow.cs - GUI System (Phase 3)
Extended Setup Assistant with sequence management:

**Sequence Tab Features:**
- Asset selection dropdown with New/Load/Save operations
- Hierarchical tree view with expandable foldouts
- Real-time validation status indicators (✓ completed, ○ pending, ⚠ invalid)
- Inline error messages and hints display
- Auto-loading of available training sequence assets

**Tree View Implementation:**
- Program-level expansion with description display
- Module foldouts with task group nesting
- Step-level detail view with type and status indicators
- Parallel/Optional step badges
- Validation message inline display

### Phase Implementation Status

#### Phase 1: Core Data Structures ✅ COMPLETED
- All hierarchical classes implemented in `TrainingSequence.cs`
- GameObjectReference system with fallback resolution
- Validation system with detailed error reporting
- Factory methods for common patterns

#### Phase 2: ScriptableObject Asset System ✅ COMPLETED
- `TrainingSequenceAsset.cs` with full Unity integration
- Asset management utilities in `TrainingSequenceAssetManager`
- Sample HVAC template asset creation
- Comprehensive validation with error categorization

#### Phase 3: Basic GUI Display ✅ COMPLETED
- Read-only tree view in VRInteractionSetupWindow
- Asset selection and loading interface
- Status indicators and validation feedback
- Hierarchical display with proper indentation

#### Phase 4: Step Editing Interface 🔄 PLANNED
- Details panel for step property editing
- Add/remove functionality for hierarchy elements
- GameObject pickers and dropdown controls
- In-place editing with immediate validation

#### Phase 5: Runtime Controller 🔄 PLANNED
- `TrainingSequenceController.cs` for XRI event integration
- Step completion detection and progress tracking
- Parallel step execution management
- Runtime validation and error handling

#### Phase 6: Testing & Polish 🔄 PLANNED
- Runtime testing integration in GUI
- Debug UI for step progress monitoring
- Enhanced validation for broken references

### Usage Examples

#### Creating Training Sequences
```csharp
// Method 1: Factory pattern
var program = TrainingSequenceFactory.CreateHVACLeakTestingProgram();

// Method 2: Asset creation
var asset = TrainingSequenceAssetManager.CreateHVACTemplateAsset();
TrainingSequenceAssetManager.SaveAssetToSequencesFolder(asset);

// Method 3: Manual construction
var step = new InteractionStep("Remove valve cap", InteractionStep.StepType.GrabAndSnap)
{
    targetObject = valveCapReference,
    destination = tableReference,
    hint = "Remove the cap and place on table",
    allowParallel = true
};
```

#### Validation Workflow
```csharp
var validation = trainingAsset.ValidateProgram();
if (validation.HasErrors)
{
    foreach (var error in validation.errors)
    {
        Debug.LogError($"Training Sequence Error: {error}");
    }
}
```

#### Statistics Analysis
```csharp
var stats = trainingAsset.GetStats();
Debug.Log($"Program contains {stats.totalSteps} steps across {stats.moduleCount} modules");
Debug.Log($"Step breakdown - Grab: {stats.grabSteps}, Snap: {stats.grabAndSnapSteps}, Knob: {stats.knobSteps}");
```

### File Locations
- **Core Classes:** `Assets/VRTrainingKit/Scripts/TrainingSequence.cs`
- **Asset System:** `Assets/VRTrainingKit/Scripts/TrainingSequenceAsset.cs`
- **GUI Extension:** `Assets/VRTrainingKit/Scripts/VRInteractionSetupWindow.cs`
- **Property Drawer:** `Assets/VRTrainingKit/Scripts/Editor/GameObjectReferenceDrawer.cs`
- **Sample Assets:** `Assets/VRTrainingKit/Sequences/`
- **Testing:** `Assets/VRTrainingKit/Scripts/TestTrainingSequence.cs`

### Integration with Existing Framework
The hierarchical system complements the existing profile-based interaction setup:
- Uses same GameObjects tagged with `grab`, `knob`, `snap`
- InteractionSteps reference objects configured with interaction profiles
- Runtime controller will subscribe to same XRI events as SequenceController
- Validation system checks for proper interaction component setup

### Development Commands
- **Access GUI:** `Window > VR Training > Setup Assistant` → Sequence Tab
- **Create Assets:** Right-click in Project → Create → VR Training → Training Sequence Asset
- **Load Template:** Use "New" button in Sequence tab → Select "HVAC Template"
- **Validate:** Check ⚠ indicators in tree view for validation issues
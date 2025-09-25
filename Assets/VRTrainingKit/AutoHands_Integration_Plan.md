# AutoHands Integration - Revised Framework Isolation Plan

## **Core Philosophy: Framework Isolation**
- **Binary Framework Choice**: Complete XRI or AutoHands mode, no mixing
- **Existing System First**: Make current XRI system framework-aware before adding AutoHands
- **Clean Separation**: XRI mode = XRI only, AutoHands mode = AutoHands only
- **Four Profile Support**: All interaction profiles (Grab, Snap, Knob, Valve) for both frameworks

---

## **Phase 0: Current System Framework Abstraction (1-2 weeks)**
**Goal:** Make existing XRI-dependent code framework-aware (CRITICAL PREREQUISITE)

### **Deliverables:**
1. **Framework Detection System**
   - `VRFrameworkDetector.cs` - Detects XR Origin (XRI) vs AutoHandPlayer (AutoHands)
   - `VRFrameworkManager.cs` - ScriptableObject for framework preference storage
   - Automatic detection based on rig prefabs in scene

2. **Framework-Aware InteractionSetupService Refactor**
   - Replace hardcoded XRI validation in `ValidateSetup()` (lines 164-189)
   - Create `ValidateXRIObjects()` and `ValidateAutoHandsObjects()` methods
   - Framework-specific component checking logic
   - Framework-aware error messages

3. **Framework-Aware Step Handler Interface Extension**
   - Modify `IStepHandler` to include framework awareness
   - Create `BaseXRIStepHandler` and `BaseAutoHandsStepHandler` base classes
   - Framework-specific component caching strategies
   - Event subscription abstraction layer

4. **VRInteractionSetupWindow Framework Tab**
   - Framework detection display (XR Origin vs AutoHandPlayer)
   - Manual framework override option
   - Framework-specific profile filtering
   - Migration warnings for framework mismatches

### **Success Criteria:**
- Existing XRI functionality preserved and framework-isolated
- Current system works identically in "XRI Mode"
- Foundation ready for AutoHands parallel implementation
- Zero breaking changes to existing workflows

---

## **Phase 1: AutoHands Framework Infrastructure (1-2 weeks)**
**Goal:** Establish AutoHands detection and basic infrastructure

### **Deliverables:**
1. **Enhanced Framework Detection**
   - AutoHands rig detection: `AutoHandPlayer` component presence
   - Hand detection: Left/Right `Hand` components under TrackerOffsets
   - Framework validation and conflict detection
   - Automatic framework preference setting

2. **AutoHands Profile Base Classes**
   - `AutoHandsInteractionProfile` base class (inherits from `InteractionProfile`)
   - AutoHands-specific validation patterns
   - Component application utilities for AutoHands components
   - Profile asset menu integration for AutoHands profiles

3. **Framework-Specific Resource Loading**
   - Load XRI profiles when XR Origin detected
   - Load AutoHands profiles when AutoHandPlayer detected
   - Framework-specific default profile discovery
   - "Resources/XRI/" and "Resources/AutoHands/" folder structure

### **Success Criteria:**
- Clean framework detection for both systems
- AutoHands profile infrastructure ready
- Framework-specific resource loading working
- No interference with existing XRI workflows

---

## **Phase 2: All Four AutoHands Profiles (2-3 weeks)**
**Goal:** Create complete AutoHands equivalents for all existing XRI profiles

### **Deliverables:**
1. **AutoHandsGrabProfile.cs**
   - `Grabbable` component configuration
   - Settings: grabType, handType, singleHandOnly, throwPower
   - Rigidbody and Collider automatic setup
   - Equivalent to XRI GrabProfile functionality

2. **AutoHandsSnapProfile.cs**
   - `PlacePoint` component configuration
   - Settings: placeRadius, parentOnPlace, forcePlace
   - Trigger collider automatic setup
   - Object validation and filtering support
   - Equivalent to XRI SnapProfile functionality

3. **AutoHandsKnobProfile.cs**
   - `Grabbable` + physics constraint setup for rotation
   - Angle tracking and constraint configuration
   - AutoHands-compatible rotational physics
   - Integration with existing `KnobController` logic
   - Equivalent to XRI KnobProfile functionality

4. **AutoHandsValveProfile.cs**
   - Combined `Grabbable` + `PlacePoint` setup
   - Multi-component valve configuration
   - State machine integration with AutoHands events
   - Valve-specific physics and constraint setup
   - Equivalent to XRI ValveProfile functionality

### **Success Criteria:**
- All 4 AutoHands profiles functionally equivalent to XRI versions
- Profiles apply components correctly to tagged objects
- CreateAssetMenu integration working
- Default AutoHands profiles in Resources/AutoHands/ folder

---

## **Phase 3: AutoHands Step Handlers (2-3 weeks)**
**Goal:** Create AutoHands step handlers for all interaction types

### **Deliverables:**
1. **AutoHands Step Handler Set**
   - `AutoHandsGrabStepHandler.cs` - Grabbable OnGrab event handling
   - `AutoHandsSnapStepHandler.cs` - PlacePoint OnPlace event handling
   - `AutoHandsKnobStepHandler.cs` - Rotational physics and angle tracking
   - `AutoHandsValveStepHandler.cs` - Complex multi-component valve interactions

2. **AutoHands Event Integration**
   - Event adapters for AutoHands (events have no arguments)
   - Step completion detection using AutoHands event patterns
   - Component caching specific to AutoHands components
   - Error handling for missing AutoHands components

3. **Framework-Specific Handler Registration**
   - `ModularTrainingSequenceController` detects framework and loads appropriate handlers
   - XRI mode: Load XRI step handlers only
   - AutoHands mode: Load AutoHands step handlers only
   - Clean handler lifecycle management

### **Success Criteria:**
- AutoHands step handlers detect step completion correctly
- Training sequences work identically across both frameworks
- Event subscription/unsubscription working properly
- No memory leaks or handler conflicts

---

## **Phase 4: Framework-Aware UI & Validation (1-2 weeks)**
**Goal:** Complete UI integration and framework-specific validation

### **Deliverables:**
1. **Framework-Aware Setup Assistant**
   - Framework status display in Setup tab
   - Profile selection filtered by active framework
   - Framework-specific "Quick Setup" using appropriate default profiles
   - Migration tools for switching between frameworks

2. **Enhanced Validation System**
   - Framework-specific component validation
   - XRI mode: Check for XRGrabInteractable, XRSocketInteractor, etc.
   - AutoHands mode: Check for Grabbable, PlacePoint, etc.
   - Clear error messages with framework-specific recommendations

3. **Framework Migration Tools**
   - Convert XRI scene to AutoHands (remove XRI components, apply AutoHands profiles)
   - Convert AutoHands scene to XRI (remove AutoHands components, apply XRI profiles)
   - Backup and restore functionality for safe migration
   - Batch conversion tools for multiple objects

### **Success Criteria:**
- Setup Assistant completely framework-aware
- Validation system gives framework-appropriate feedback
- Migration tools allow safe framework switching
- UI clearly indicates current framework mode

---

## **Phase 5: Advanced Integration & Controllers (1-2 weeks)**
**Goal:** Integrate existing controllers with AutoHands events

### **Deliverables:**
1. **Controller Integration**
   - Make `KnobController` work with AutoHands Grabbable events
   - Make `ValveController` work with AutoHands PlacePoint events
   - Framework-aware controller initialization
   - Event bridging for controllers that need XRI-style event arguments

2. **Physics Optimization**
   - AutoHands-specific physics settings for HVAC interactions
   - Joint configurations optimized for AutoHands physics system
   - Collision and layer setup for AutoHands workflow
   - Performance testing and optimization

3. **Testing & Polish**
   - Comprehensive testing of all 4 interaction types in AutoHands mode
   - Edge case handling and error recovery
   - Performance comparison between XRI and AutoHands modes
   - Documentation and examples

### **Success Criteria:**
- All existing controllers work with AutoHands components
- Performance comparable to XRI implementation
- Comprehensive testing passed for all interaction types
- Clean, maintainable code with good documentation

---

## **Revised Timeline: 7-12 weeks**

**Phase 0:** Framework abstraction of existing system (1-2 weeks)
**Phase 1:** AutoHands infrastructure (1-2 weeks)
**Phase 2:** All four AutoHands profiles (2-3 weeks)
**Phase 3:** AutoHands step handlers (2-3 weeks)
**Phase 4:** Framework-aware UI (1-2 weeks)
**Phase 5:** Advanced integration (1-2 weeks)

### **Key Architecture Changes:**
- **Phase 0 is CRITICAL**: Must abstract existing XRI dependencies first
- **Complete Profile Parity**: All 4 interaction types for both frameworks
- **Framework Detection**: Based on XR Origin vs AutoHandPlayer components
- **Clean Separation**: Framework choice affects all system behaviors

### **Risk Mitigation:**
- Phase 0 ensures existing functionality is preserved
- Framework detection prevents mixing incompatible components
- Migration tools allow safe framework switching
- Comprehensive testing ensures feature parity

### **Success Metrics:**
- All 4 interaction types working in both XRI and AutoHands modes
- Training sequences identical behavior across frameworks
- Clean framework switching with migration tools
- No performance regression in either mode

---

## **Implementation Notes:**

### **XR Rig Detection Strategy:**
**XRI Rig Detection (Component-based):**
- Look for `XR Origin` GameObject (root of XRI rig)
- Validate presence of `XR Camera` as child
- Check for hand controller GameObjects with `XR Direct Interactor` or `XR Ray Interactor`
- Component validation: `XR Interaction Manager` in scene

**AutoHands Rig Detection (Component-based):**
- Look for `Hand` component instances (left/right hands)
- Check for `HandBase` or derived classes
- Validate `Hand Input` or controller link components
- Component validation: AutoHands-specific input components

**Rig Creation Strategy:**
- **XRI**: Instantiate from Resources or create programmatically using XR Origin prefab
- **AutoHands**: Instantiate AutoHands rig prefab or create from components
- **Conflict Resolution**: Detect mixed rigs and offer cleanup/replacement options

### **Framework Preference Storage:**
- Store in `ProjectSettings` using `EditorPrefs` or ScriptableObject
- Scene-level override option in case project needs mixed framework support
- Clear visual indicators in Setup Assistant showing current framework

### **Testing Strategy:**
Each phase should be tested with:
- Clean Unity scene (no existing components)
- Scene with existing XRI setup
- Scene with existing AutoHands setup
- Mixed framework scenarios (error handling)
- Framework switching workflows
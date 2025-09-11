# Phase 1 Test Setup Guide: Tool-Socket Pairing Validation

## Overview
This guide helps you create a basic test scene to validate the architectural cleanup and tool-socket pairing functionality implemented in Phase 1.

## Test Scene Setup

### Step 1: Create Test Scene
1. Create new scene: `Assets/VRTrainingKit/TestSetup/Phase1_ToolTest.unity`
2. Add basic XR setup (XR Origin, etc.)

### Step 2: Create Test Objects

#### Tool Object Setup
1. **Create Tool GameObject:**
   - Name: `TestWrench`
   - Tag: `tool`
   - Add basic mesh (cube or imported wrench model)
   - Position: `(0, 1, 0)`

2. **Apply Tool Profile:**
   - Create ToolProfile asset: Right-click → Create → VR Training → Tool Profile
   - Name: `TestWrenchProfile`
   - Configure settings:
     - Compatible Socket Tags: `["snap"]`
     - Tighten Threshold: `90`
     - Loosen Threshold: `45`
     - Initial State: `Unlocked`

3. **Apply Profile to Tool:**
   - Select TestWrench in scene
   - Use VR Training → Setup Assistant → Setup Tab
   - Scan Scene → Apply Components

#### Socket Object Setup
1. **Create Socket GameObject:**
   - Name: `TestBoltSocket`
   - Tag: `snap`
   - Add basic mesh (cylinder or imported bolt model)
   - Position: `(2, 1, 0)` (2 meters from tool)

2. **Apply Snap Profile:**
   - Create SnapProfile asset: Right-click → Create → VR Training → Snap Profile
   - Name: `TestBoltSocketProfile`
   - Configure settings:
     - Socket Radius: `0.2`
     - Accepted Tags: `["tool"]`

3. **Apply Profile to Socket:**
   - Select TestBoltSocket in scene
   - Use VR Training → Setup Assistant → Setup Tab
   - Scan Scene → Apply Components

## Validation Checklist

### ✅ Component Verification
After setup, verify these components exist:

**On TestWrench (tool):**
- [ ] XRGrabInteractable
- [ ] Rigidbody
- [ ] Collider
- [ ] ToolController *(NEW)*

**On TestBoltSocket (snap):**
- [ ] XRSocketInteractor  
- [ ] SphereCollider (isTrigger = true)
- [ ] Rigidbody (isKinematic = true)
- [ ] SnapValidator *(NOW SEPARATE FILE)*

### ✅ Architecture Cleanup Verification
Verify refactoring worked correctly:

**File Structure Check:**
- [ ] `SnapValidator.cs` exists as separate file
- [ ] `SequenceValidator.cs` exists as separate file  
- [ ] `ToolController.cs` exists
- [ ] `KnobController.cs` no longer contains SnapValidator/SequenceValidator classes
- [ ] No compilation errors in Console

**Component References:**
- [ ] SnapValidator component works on socket objects
- [ ] SequenceValidator can be added independently
- [ ] All existing knob functionality still works

### ✅ Tool-Socket Pairing Test
Test basic pairing functionality:

**Scene Test:**
1. [ ] **VR Setup**: Can grab TestWrench with VR controllers
2. [ ] **Socket Detection**: ToolController finds TestBoltSocket as compatible
3. [ ] **State Management**: ToolController shows correct initial state (Unlocked)
4. [ ] **Profile Configuration**: ToolController reflects profile settings

**Debug Console Verification:**
Look for these log messages:
```
[ToolProfile] Successfully configured tool: TestWrench
[ToolController] TestWrench Awake() - Initial state: Unlocked
[ToolController] Configure() called for TestWrench: Previous=NULL → New=TestWrenchProfile
```

## Testing Commands

### Manual Testing in Scene
1. **Enter Play Mode**
2. **Grab Tool**: Use VR controller to grab TestWrench
3. **Move Toward Socket**: Bring tool within 50cm of TestBoltSocket
4. **Check Console**: Look for compatibility messages

### Editor Testing
Use VR Training → Setup Assistant:
1. **Setup Tab**: Scan scene, verify objects found correctly
2. **Validate Tab**: Check for any configuration issues
3. **Configure Tab**: Verify profiles load correctly

## Expected Behavior

### ✅ Success Criteria
- Tool can be grabbed and moved
- ToolController detects compatible socket
- No compilation errors
- All refactored components work independently
- Profile settings apply correctly

### ⚠️ Common Issues
1. **Missing Components**: Re-run Setup Assistant if components missing
2. **Tag Issues**: Verify "tool" and "snap" tags exist and are applied
3. **Profile References**: Ensure profiles are properly assigned
4. **XR Setup**: Verify XR Origin and controllers are configured

## Next Steps: Phase 2 Preparation
Once Phase 1 validation passes:
1. Tool-socket pairing works correctly
2. Architecture cleanup complete
3. Ready to implement forward flow (grab → snap → rotate → lock)

## Debug Information

### Component Inspector Fields
**ToolController Debug Fields:**
- Current State: Should show "Unlocked"
- Profile: Should reference TestWrenchProfile
- Is Initialized: Should be true after configuration

**SnapValidator Debug:**
- Should have Configure() method available
- Should reference TestBoltSocketProfile after setup

### Console Debug Output
Enable detailed logging by checking "Enable Debug Logging" on components for verbose output during testing.
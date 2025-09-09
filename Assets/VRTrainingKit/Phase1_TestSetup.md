# Phase 1 Test Setup Instructions

## Test Scene Requirements

### Create ValveTestScene.unity
1. **Create new scene**: `File > New Scene`
2. **Save as**: `Assets/VRTrainingKit/TestScenes/ValveTestScene.unity`
3. **Add XR Rig**: Add XR Origin (VR) from XR Interaction Toolkit

### Test Objects Setup

#### 1. Valve Object
- **Create**: Empty GameObject named "TestValve"
- **Tag**: Set tag to "valve" (create tag if needed)
- **Add Components**: 
  - MeshRenderer with simple mesh (Cube or Cylinder)
  - MeshFilter
- **Position**: Place at (0, 1, 0) for easy grabbing

#### 2. Socket Object  
- **Create**: Empty GameObject named "ValveSocket"
- **Tag**: Set tag to "valve_socket" (create tag if needed)
- **Add Components**:
  - XRSocketInteractor
  - SphereCollider (radius: 0.5, isTrigger: true)
  - SnapValidator component
- **Position**: Place at (2, 1, 0)

### Profile Assets

#### 1. Create Default ValveProfile
- **Right-click** in Project window
- **Create > VR Training > Valve Profile**
- **Name**: "DefaultValveProfile"
- **Configure**:
  - profileName: "Default Valve Profile"
  - rotationAxis: (0, 1, 0) - Y-axis
  - tightenThreshold: 90
  - loosenThreshold: 90
  - compatibleSocketTags: ["valve_socket"]

#### 2. Apply ValveProfile to Valve
- **Method 1**: Use VR Interaction Setup Window
  - Open `Window > VR Training > Setup Assistant`
  - Go to Setup tab
  - Click "Scan Scene"
  - Apply ValveProfile to valve objects
  
- **Method 2**: Manual application
  - Select TestValve object
  - Add ValveController component
  - Assign DefaultValveProfile in inspector

## Phase 1 Validation Tests

### Test 1: Basic Scene Setup
- [ ] Scene contains valve object tagged "valve"
- [ ] Scene contains socket object tagged "valve_socket"
- [ ] VR rig present and functional
- [ ] No console errors on scene load

### Test 2: Profile Creation
- [ ] ValveProfile asset can be created via Create menu
- [ ] ValveProfile shows in inspector with all expected fields
- [ ] Profile validation settings work correctly

### Test 3: Component Application
- [ ] ValveProfile applies to valve object without errors
- [ ] ValveController component added automatically
- [ ] XRGrabInteractable component added
- [ ] Rigidbody component added
- [ ] Appropriate collider added

### Test 4: State Machine Initialization
- [ ] ValveController starts in UNLOCKED state
- [ ] Console shows initialization debug messages
- [ ] No runtime errors in console
- [ ] State properties accessible in inspector

### Test 5: Basic Grab Functionality
- [ ] Valve can be grabbed when UNLOCKED
- [ ] Valve can be moved freely when grabbed
- [ ] Valve responds to VR controller input
- [ ] Grab/release events fire correctly

### Test 6: Socket Snapping
- [ ] Valve snaps to socket when brought nearby
- [ ] Valve transitions to LOCKED-LOOSE state on snap
- [ ] Console shows state transition messages
- [ ] Valve position locks to socket

### Test 7: Interaction Setup Service
- [ ] Scene scan finds valve objects correctly
- [ ] Valve count displayed in debug log
- [ ] Profile application works through service
- [ ] No errors during batch operations

## Success Criteria Checklist

### Functionality Requirements
- [x] ValveProfile ScriptableObject created
- [x] ValveController with state machine implemented  
- [x] ValveState and ValveSubstate enums defined
- [x] InteractionSetupService supports valve objects
- [ ] Basic test scene functions correctly
- [ ] Grab/snap workflow operates smoothly

### Code Quality Requirements
- [x] Clean debug logging throughout system
- [x] Proper error handling and validation
- [x] Consistent naming conventions
- [x] Comprehensive inline documentation

### Integration Requirements
- [x] Extends existing InteractionProfile architecture
- [x] Compatible with current VR Interaction Setup Window
- [x] Works with existing SnapValidator system
- [x] Maintains compatibility with other profiles

## Expected Debug Output

When running Phase 1 tests, you should see console output like:
```
[ValveProfile] ApplyToGameObject() called for: TestValve with profile: Default Valve Profile
[ValveProfile] Added XRGrabInteractable to TestValve
[ValveProfile] Added Rigidbody to TestValve
[ValveProfile] Added Box collider to TestValve
[ValveProfile] Added ValveController to TestValve
[ValveController] TestValve Awake() - Initial state: Unlocked
[ValveController] Configure() called for TestValve: Previous=NULL → New=Default Valve Profile, State=Unlocked, Substate=None
[InteractionSetupService] Found valve object: TestValve (Tag: valve)
[InteractionSetupService] Scene Analysis Complete: 1 interactables found
[InteractionSetupService]   - Valve Objects: 1
```

## Troubleshooting Common Issues

### Issue: Valve tag not recognized
- **Solution**: Create "valve" tag in Tags & Layers settings
- **Path**: Edit > Project Settings > Tags and Layers

### Issue: Socket tag not recognized  
- **Solution**: Create "valve_socket" tag in Tags & Layers settings

### Issue: ValveController not found
- **Solution**: Ensure ValveController.cs is in correct folder and compiles without errors

### Issue: XRSocketInteractor missing
- **Solution**: Ensure XR Interaction Toolkit package is installed and imported

### Issue: Profile not applying
- **Solution**: Check ValveProfile.ValidateGameObject() - object must have "valve" tag

## Next Steps After Phase 1
Once all Phase 1 tests pass:
1. Move to Phase 2: Forward Flow Implementation
2. Add tightening rotation logic
3. Implement LOOSE → TIGHT state transition
4. Test complete forward workflow
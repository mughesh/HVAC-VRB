# VRInteractionSetupWindow Refactoring - Testing Checklist

## Purpose

This checklist helps verify that the modular refactoring maintains all functionality of the original VRInteractionSetupWindow.

## Pre-Testing Setup

### 1. Verify Files Exist
- [ ] `Assets/VRTrainingKit/Scripts/Editor/Windows/VRSetupTabBase.cs`
- [ ] `Assets/VRTrainingKit/Scripts/Editor/Windows/VRProfileManager.cs`
- [ ] `Assets/VRTrainingKit/Scripts/Editor/Windows/VRSetupTab_Setup.cs`
- [ ] `Assets/VRTrainingKit/Scripts/Editor/Windows/VRSetupTab_Configure.cs`
- [ ] `Assets/VRTrainingKit/Scripts/Editor/Windows/VRSetupTab_Validate.cs`
- [ ] `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs` (modified)

### 2. Unity Compilation
- [ ] Open Unity project
- [ ] Wait for compilation to complete
- [ ] No compilation errors in Console
- [ ] No namespace conflicts reported

### 3. Check Toggle State
- [ ] Open `VRInteractionSetupWindow.cs`
- [ ] Verify line ~90: `private bool useModularTabs = true;`
- [ ] Confirm modular tabs are enabled by default

## Functional Testing - Setup Tab

### Scene Scanning
- [ ] Open `Window > VR Training > Setup Assistant`
- [ ] Switch to Setup tab
- [ ] Click "Scan Scene" button
- [ ] Verify objects are categorized correctly
- [ ] Check that counts match actual scene objects

### Profile Selection
- [ ] Verify profile dropdowns appear for each category
- [ ] Select a profile from Configure tab
- [ ] Return to Setup tab
- [ ] Verify selected profile shows in object groups

### Component Application
- [ ] Tag some GameObjects with "grab", "knob", or "snap"
- [ ] Scan scene
- [ ] Select appropriate profiles
- [ ] Click "Configure" on individual object
- [ ] Verify components added to GameObject
- [ ] Check Inspector shows XRGrabInteractable (XRI) or Grabbable (AutoHands)

### Bulk Operations
- [ ] Tag multiple objects
- [ ] Scan scene
- [ ] Click "Apply All Components"
- [ ] Verify all objects configured
- [ ] Check status icons change from ○ to ✓

### Layer Management (XRI only)
- [ ] Configure an XRI object
- [ ] Verify interaction layer dropdown appears
- [ ] Change layer selection
- [ ] Verify layer persists on GameObject

### Clean All
- [ ] Click "Clean All" button
- [ ] Confirm dialog appears
- [ ] Accept cleanup
- [ ] Verify components removed from objects
- [ ] Check status icons change from ✓ to ○

## Functional Testing - Configure Tab

### Profile Display
- [ ] Switch to Configure tab
- [ ] Verify all profile sections visible:
  - [ ] Grab Profile
  - [ ] Knob Profile
  - [ ] Snap Profile
  - [ ] Tool Profile
  - [ ] Valve Profile
  - [ ] Turn Profile
  - [ ] Teleport Profile

### Profile Selection
- [ ] Drag profile asset from Project window to Profile Asset field
- [ ] Verify profile name appears
- [ ] Click "Edit Profile" button
- [ ] Verify profile selected in Project window

### Available Profiles List
- [ ] Clear profile selection (set to None)
- [ ] Verify "Available Profiles" list appears
- [ ] Check framework type shows: [XRI] or [AutoHands]
- [ ] Click "Select" button on a profile
- [ ] Verify profile selected

### Profile Creation
- [ ] Click "Create New [Type] Profile" button
- [ ] Choose save location in dialog
- [ ] Verify profile asset created
- [ ] Check profile appears in Available Profiles list
- [ ] Verify profile auto-selected in Inspector

### Framework Notice
- [ ] Check framework compatibility notice at top
- [ ] Verify current framework detected (XRI or AutoHands)
- [ ] Confirm appropriate message displayed

### Bulk Operations
- [ ] Click "Create All Default Profiles"
- [ ] Verify dialog appears
- [ ] Click "Refresh All Caches"
- [ ] Verify no errors in Console

## Functional Testing - Validate Tab

### Validation Execution
- [ ] Switch to Validate tab
- [ ] Click "Run Validation" button
- [ ] Wait for validation to complete
- [ ] Verify results displayed

### Issues Display
- [ ] If issues found, check:
  - [ ] Issue count shows correctly
  - [ ] Each issue has ⚠ icon
  - [ ] Issue descriptions are readable
  - [ ] Scroll view works for many issues

### Success State
- [ ] Configure all objects properly
- [ ] Run validation again
- [ ] Verify "✓ All checks passed!" message
- [ ] Confirm success styling (green text)

## Functional Testing - Sequence Tab (Legacy)

### Basic Functionality
- [ ] Switch to Sequence tab
- [ ] Verify tab renders (uses old code)
- [ ] Check asset selection works
- [ ] Verify tree view renders
- [ ] Test step property editing
- [ ] Confirm no regression from refactoring

## Functional Testing - Runtime Monitor Tab (Legacy)

### Basic Functionality
- [ ] Switch to Runtime Monitor tab
- [ ] Verify tab renders (uses old code)
- [ ] Check runtime status displays
- [ ] Test monitoring features
- [ ] Confirm no regression from refactoring

## Integration Testing

### Tab Switching
- [ ] Switch between all tabs multiple times
- [ ] Verify no errors in Console
- [ ] Check that each tab renders correctly
- [ ] Confirm no visual artifacts
- [ ] Verify scroll positions maintained

### Window Resize
- [ ] Resize window to various sizes
- [ ] Test minimum window size
- [ ] Maximize window
- [ ] Verify layouts adjust properly
- [ ] Check no UI clipping

### Profile Sharing
- [ ] Select profile in Configure tab
- [ ] Switch to Setup tab
- [ ] Verify profile available in object groups
- [ ] Configure objects
- [ ] Return to Configure tab
- [ ] Verify profile still selected

## Play Mode Testing

### Enter Play Mode
- [ ] Open Setup Assistant
- [ ] Scan scene (if not already scanned)
- [ ] Enter Play Mode
- [ ] Wait for play mode to fully load
- [ ] Check Console for any errors
- [ ] Verify window still accessible

### Exit Play Mode
- [ ] Exit Play Mode
- [ ] Wait for edit mode to fully load
- [ ] Open Setup Assistant (if not open)
- [ ] Verify scene analysis restored
- [ ] Check profile selections maintained
- [ ] Verify no errors in Console

### Play Mode Transitions
- [ ] Set up scene with training sequence
- [ ] Enter and exit play mode 3 times
- [ ] Verify references don't break
- [ ] Check GameObject references remain valid
- [ ] Confirm no memory leaks (check Profiler)

## Framework Testing (XRI)

### XRI Profile Testing
- [ ] Ensure XRI installed in project
- [ ] Open Configure tab
- [ ] Verify XRI profiles load
- [ ] Check [XRI] tag on profiles
- [ ] Configure objects with XRI profiles
- [ ] Verify XRGrabInteractable added
- [ ] Test interaction layers

## Framework Testing (AutoHands)

### AutoHands Profile Testing
- [ ] Ensure AutoHands installed in project
- [ ] Open Configure tab
- [ ] Verify AutoHands profiles load
- [ ] Check [AutoHands] tag on profiles
- [ ] Configure objects with AutoHands profiles
- [ ] Verify Grabbable component added
- [ ] Test PlacePoint configuration

## Performance Testing

### Large Scene
- [ ] Create scene with 100+ tagged objects
- [ ] Scan scene
- [ ] Measure scan time (should be < 2 seconds)
- [ ] Check Setup tab responsiveness
- [ ] Verify no lag when switching tabs

### Profile Caching
- [ ] Clear all profile selections
- [ ] Open Configure tab
- [ ] Note load time for available profiles
- [ ] Close and reopen window
- [ ] Verify profiles load from cache (faster)

### Memory
- [ ] Open Unity Profiler
- [ ] Open Setup Assistant
- [ ] Switch between tabs 20 times
- [ ] Check for memory growth
- [ ] Close window
- [ ] Verify memory released

## Regression Testing

### Compare with Old Implementation
- [ ] Set `useModularTabs = false`
- [ ] Recompile
- [ ] Perform basic operations with old code
- [ ] Set `useModularTabs = true`
- [ ] Recompile
- [ ] Perform same operations with new code
- [ ] Verify identical behavior

### Edge Cases
- [ ] Try with empty scene (no tagged objects)
- [ ] Try with objects tagged incorrectly
- [ ] Try with missing profiles
- [ ] Try with corrupted profile assets
- [ ] Verify graceful error handling

## Error Handling

### Missing Dependencies
- [ ] Remove a profile asset
- [ ] Open Configure tab
- [ ] Verify null check prevents crash
- [ ] Check appropriate warning displayed

### Invalid Operations
- [ ] Try to configure without profile selected
- [ ] Verify dialog warning appears
- [ ] Try to scan with no scene loaded
- [ ] Check error handling works

### Console Messages
- [ ] Review all Console messages
- [ ] Verify no errors
- [ ] Check warnings are appropriate
- [ ] Confirm info messages helpful

## Backward Compatibility

### Toggle Test
- [ ] Set `useModularTabs = false`
- [ ] Recompile and test all features
- [ ] Set `useModularTabs = true`
- [ ] Recompile and test all features
- [ ] Verify both modes work identically

### Fallback Safety
- [ ] Simulate modular tab failure (rename tab file)
- [ ] Recompile
- [ ] Verify fallback to old code works
- [ ] Restore file
- [ ] Verify recovery

## Documentation Review

### Code Comments
- [ ] Review VRSetupTabBase.cs comments
- [ ] Review VRProfileManager.cs comments
- [ ] Review each tab class comments
- [ ] Verify all public methods documented
- [ ] Check inline explanations clear

### Guides
- [ ] Read REFACTORING_GUIDE.md
- [ ] Follow integration steps
- [ ] Verify accuracy
- [ ] Read REFACTORING_SUMMARY.md
- [ ] Check FAQ answers questions

## Final Verification

### Checklist Review
- [ ] All items above completed
- [ ] All issues documented
- [ ] No critical errors remain
- [ ] Performance acceptable
- [ ] Documentation accurate

### Sign-Off
- [ ] Tested by: ___________________________
- [ ] Date: ___________________________
- [ ] Build Version: ___________________________
- [ ] Framework: □ XRI  □ AutoHands  □ Both
- [ ] Status: □ Pass  □ Pass with Issues  □ Fail

### Issues Found
If any issues discovered during testing, document here:

1. **Issue**: _____________________________________________________
   - **Severity**: □ Critical  □ Major  □ Minor
   - **Tab Affected**: _____________________
   - **Steps to Reproduce**: _____________________________________
   - **Workaround**: _____________________________________________

2. **Issue**: _____________________________________________________
   - **Severity**: □ Critical  □ Major  □ Minor
   - **Tab Affected**: _____________________
   - **Steps to Reproduce**: _____________________________________
   - **Workaround**: _____________________________________________

3. **Issue**: _____________________________________________________
   - **Severity**: □ Critical  □ Major  □ Minor
   - **Tab Affected**: _____________________
   - **Steps to Reproduce**: _____________________________________
   - **Workaround**: _____________________________________________

### Notes
Additional observations, suggestions, or comments:
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________

---

**Testing Checklist Version**: 1.0  
**Date Created**: December 2024  
**Refactoring Version**: 1.0

# HVAC Training Sequence - Phase 4 Test Guide

## Overview
This guide walks you through testing the TrainingSequenceController with the HVAC template. The controller will detect XRI interactions and automatically complete training steps.

## Setup Requirements

### 1. Scene Setup
Your scene should contain these GameObjects (you'll create them):
- **ServiceValve** (tagged as `grab`)
- **ServiceValve_Dropped_Pos** (tagged as `snap`) 
- **SuctionValve** (tagged as `grab`)
- **SuctionValve_Dropped_Pos** (tagged as `snap`)

### 2. Component Setup
1. Add **TrainingSequenceController** component to an empty GameObject
2. Assign the **HVAC_LeakTesting_Template** asset to the Training Asset field
3. Enable **Debug Logging** and **Show Step Completions** checkboxes

### 3. XRI Configuration
Make sure your GameObjects have proper XRI components:
- **Grab objects**: XRGrabInteractable, Rigidbody, Collider
- **Snap points**: XRSocketInteractor, SphereCollider

## Test Cases

### Test Case 1: Basic Step Completion
**Objective**: Verify individual step completion detection

**Steps**:
1. Enter Play Mode
2. Look for console message: `"Starting training sequence: HVAC Training"`
3. Look for: `"Starting task group: Initial Setup (Module: Leak Testing)"`
4. Grab the **ServiceValve** with VR controller
5. Snap it to **ServiceValve_Dropped_Pos**

**Expected Results**:
```
✓ Step completed: Remove liquid valve cap - Snapped ServiceValve to ServiceValve_Dropped_Pos
Task group progress: 1/4 steps completed
```

### Test Case 2: Parallel Step Execution
**Objective**: Verify multiple steps can be completed in any order

**Steps**:
1. Continue from Test Case 1
2. Grab the **SuctionValve** 
3. Snap it to **SuctionValve_Dropped_Pos**

**Expected Results**:
```
✓ Step completed: Remove gas valve cap - Snapped SuctionValve to SuctionValve_Dropped_Pos
Task group progress: 2/4 steps completed
```

### Test Case 3: Missing Objects Warning
**Objective**: Verify graceful handling of missing GameObjects

**Expected Results** (at start):
```
[TrainingSequence] Could not find grab interactable for step: Place allen key on liquid valve (target: )
[TrainingSequence] Could not find socket interactor for step: Place allen key on liquid valve (destination: )
```

### Test Case 4: Task Group Completion
**Objective**: Verify task group transitions work

**Note**: Since some steps reference missing objects, the controller will wait. To test progression, you can temporarily modify the HVAC template to only include the first 2 steps.

## Debug Console Messages Reference

### Startup Messages
```
[TrainingSequence] Starting training sequence: HVAC Training
[TrainingSequence] Caching XRI components in scene...
[TrainingSequence] Cached grab interactable: ServiceValve
[TrainingSequence] Cached socket interactor: ServiceValve_Dropped_Pos
[TrainingSequence] Starting task group: Initial Setup (Module: Leak Testing)
```

### Step Completion Messages
```
[TrainingSequence] ✓ Step completed: Remove liquid valve cap - Snapped ServiceValve to ServiceValve_Dropped_Pos
[TrainingSequence] Task group progress: 1/4 steps completed
```

### Task Group Completion
```
[TrainingSequence] ✓ Task group completed: Initial Setup
[TrainingSequence] Starting task group: Hose Connections (Module: Leak Testing)
```

### Full Sequence Completion
```
[TrainingSequence] 🎉 Training sequence completed: HVAC Training
```

## Common Issues & Solutions

### Issue 1: "No training asset assigned"
**Solution**: Make sure to assign HVAC_LeakTesting_Template.asset to the controller

### Issue 2: "Could not find grab interactable"
**Solution**: Ensure GameObjects have matching names and XRGrabInteractable components

### Issue 3: Steps not completing on snap
**Solution**: 
- Check GameObject names match exactly
- Verify XRSocketInteractor is on the destination object
- Ensure snap validation is working

### Issue 4: No console messages
**Solution**: 
- Enable "Debug Logging" and "Show Step Completions" in controller
- Check Console window is visible and not filtered

## Minimal Test Setup

If you want to test with minimal setup:

1. **Create 2 objects**:
   - `ServiceValve` (cube with XRGrabInteractable)
   - `ServiceValve_Dropped_Pos` (empty with XRSocketInteractor)

2. **Edit HVAC template**: Temporarily remove steps 3-4 from "Initial Setup" task group

3. **Test**: Should complete task group after snapping valve

## Advanced Testing

### Progress Tracking
Add this code to get progress information:
```csharp
var controller = FindObjectOfType<TrainingSequenceController>();
var progress = controller.GetProgress();
Debug.Log($"Progress: {progress.currentTaskGroupName} - {progress.completedSteps}/{progress.totalSteps}");
```

### Event Subscription
Subscribe to completion events:
```csharp
controller.OnStepCompleted += (step) => Debug.Log($"Step completed: {step.stepName}");
controller.OnTaskGroupCompleted += (group) => Debug.Log($"Task group completed: {group.groupName}");
```

## Expected Workflow
1. **Scene loads** → Controller initializes
2. **First task group starts** → "Initial Setup" begins
3. **User interacts** → Steps complete based on actions
4. **Task group completes** → Moves to "Hose Connections"
5. **All modules complete** → Sequence finished

This test guide should help you validate that the runtime sequence execution is working correctly!
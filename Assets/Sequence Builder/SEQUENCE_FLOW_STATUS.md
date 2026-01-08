# VR Training Kit - Sequence Flow Restrictions
## Implementation Status & Phase 3 Plan

**Last Updated:** 2025-10-16
**Current Status:** Task Group Level Socket Restrictions ✅ Complete
**Next Phase:** Runtime Monitor Tab UI

---

## 📊 **Current Implementation Status**

### ✅ **Completed: Task Group Socket Restrictions**

**What Works:**
- All sockets in the **current task group** are enabled
- All sockets in **other task groups** are disabled
- Prevents users from placing objects in wrong task group sockets
- Works with both **XRI** and **AutoHands** frameworks
- Automatic PlacePoint/Socket detection and control
- Proper handling of occupied sockets (don't disable if object is snapped)

**Use Case:**
```
Task Group 1: "Initial Setup" (enforceSequentialFlow = true)
├── Step 1: Remove valve cap → Socket1 ✅ ENABLED
├── Step 2: Remove tool → Socket2 ✅ ENABLED
└── Step 3: Place item → Socket3 ✅ ENABLED

Task Group 2: "Hose Connections"
├── Step 4: Connect hose → Socket4 ❌ DISABLED (wrong task group)
└── Step 5: Connect manifold → Socket5 ❌ DISABLED (wrong task group)
```

Users can complete steps in Task Group 1 in any order. Once all steps complete, Task Group 2 activates and its sockets become enabled.

---

## 🏗️ **Architecture Overview**

### **Key Components**

#### 1. **SequenceFlowRestrictionManager.cs**
**Location:** `Assets/VRTrainingKit/Scripts/Core/Controllers/SequenceFlowRestrictionManager.cs`

**Responsibilities:**
- Caches all socket/placepoint components in scene
- Tracks active steps and completed steps
- Enables/disables socket components based on task group
- Works with both XRI (`XRSocketInteractor`) and AutoHands (`PlacePoint`)

**Key Methods:**
- `StartTaskGroup(TaskGroup)` - Initialize for new task group
- `OnStepBecameActive(InteractionStep)` - Track step activation
- `OnStepCompleted(InteractionStep)` - Track step completion
- `UpdateSocketStates()` - Main logic: disable all, enable current group
- `CacheSocketComponents()` - Find all sockets in scene
- `SetSocketEnabled(GameObject, bool)` - Enable/disable specific socket
- `IsSocketOccupied(GameObject)` - Check if socket has object snapped

**Framework Detection:**
```csharp
// XRI Detection
var xriSocket = socketObj.GetComponent<XRSocketInteractor>();

// AutoHands Detection (uses reflection)
var placePoint = socketObj.GetComponents<MonoBehaviour>()
    .FirstOrDefault(c => c.GetType().Name.Contains("PlacePoint"));

// AutoHands Occupied Check
var placedObjectProperty = placePoint.GetType().GetProperty("placedObject");
var placedObject = placedObjectProperty.GetValue(placePoint);
bool isOccupied = placedObject != null;
```

#### 2. **ModularTrainingSequenceController.cs**
**Location:** `Assets/VRTrainingKit/Scripts/Core/Controllers/ModularTrainingSequenceController.cs`

**Integration Points:**
- **Line 51:** Creates `SequenceFlowRestrictionManager` on Start
- **Line 287-290:** Calls `restrictionManager.StartTaskGroup()` when task group begins
- **Line 334-339:** Calls `restrictionManager.OnStepBecameActive()` when step starts
- **Line 245-250:** Calls `restrictionManager.OnStepCompleted()` when step finishes

**Current Behavior:**
- Starts ALL steps in a task group simultaneously (line 309: `StartActiveSteps()`)
- This is intentional - allows free-flow within task groups
- Sequential flow is only enforced BETWEEN task groups, not within them

#### 3. **TrainingSequence.cs**
**Location:** `Assets/VRTrainingKit/Scripts/SequenceSystem/Data/TrainingSequence.cs`

**TaskGroup.enforceSequentialFlow** (Line 240):
```csharp
[Tooltip("Task Group Level Socket Restrictions: Enables all sockets in current task group, disables sockets in other task groups.")]
public bool enforceSequentialFlow = false;
```

#### 4. **VRInteractionSetupWindow.cs**
**Location:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

**UI Integration** (Line 1727-1744):
- Checkbox for `enforceSequentialFlow` in Task Group properties panel
- Help box explaining current behavior
- Updated to reflect task group level restrictions

---

## 🧪 **Testing Checklist**

### **Setup Test Scenario:**
1. Create training sequence with 2 task groups
2. Task Group 1: Enable `enforceSequentialFlow`, add 3 steps with different sockets
3. Task Group 2: Enable `enforceSequentialFlow`, add 2 steps with different sockets

### **Test Procedure:**
- [ ] Enter Play Mode
- [ ] **Verify:** Console shows `"🔒 Sequential flow enforcement ENABLED"`
- [ ] **Verify:** Console shows socket caching (e.g., "Cached 5 socket components")
- [ ] **Verify:** Task Group 1 sockets are enabled (try snapping objects)
- [ ] **Verify:** Task Group 2 sockets are disabled (objects reject/don't snap)
- [ ] Complete all steps in Task Group 1
- [ ] **Verify:** Task Group 2 sockets now enabled
- [ ] **Verify:** Task Group 1 sockets now disabled
- [ ] Complete Task Group 2
- [ ] **Verify:** Sequence completes successfully

### **Expected Console Logs:**
```
[ModularTrainingSequence] 📂 Starting task group: Initial Setup
[ModularTrainingSequence] 🔒 Sequential flow enforcement ENABLED
[SequenceFlowRestriction] 🔒 Starting restriction management for task group: Initial Setup
[SequenceFlowRestriction] 🔍 Scanning scene for socket components...
[SequenceFlowRestriction]    + Found AutoHands PlacePoint: Socket1
[SequenceFlowRestriction] 📍 Cache complete: 0 XRI Sockets, 5 AutoHands PlacePoints
[SequenceFlowRestriction] 🔒 Disabling all sockets...
[SequenceFlowRestriction]    ✓ Disabled 5 sockets, skipped 0 occupied
[SequenceFlowRestriction] 🟢 Step activated: Remove valve cap
[SequenceFlowRestriction] 🔄 Updating socket states...
[SequenceFlowRestriction]    ✓ Enabled 3 step sockets in current task group
```

---

## 📋 **Phase 3 Plan: Runtime Monitor Tab**

### **Goal:**
Create a new tab in `VRInteractionSetupWindow` that shows real-time sequence execution state during Play Mode.

### **UI Design:**

```
┌─ Runtime Monitor ─────────────────────────────────────────┐
│ ⏸️ [Only available in Play Mode]                         │
│                                                             │
│ ┌─ Sequence Status ──────────────────────────────────┐    │
│ │ Current Module: Leak Testing                       │    │
│ │ Current Task Group: Initial Setup                  │    │
│ │ Sequential Flow: ✅ Enabled                        │    │
│ │ Progress: 2/5 steps completed (40%)                │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                             │
│ ┌─ Step Progress ────────────────────────────────────┐    │
│ │ ✅ Remove liquid valve cap (Completed)             │    │
│ │ ✅ Remove gas valve cap (Completed)                │    │
│ │ 🟢 Place allen key (Active - In Progress)          │    │
│ │ ⏸️ Place tool (Pending)                            │    │
│ │ ⏸️ Connect hose (Pending)                          │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                             │
│ ┌─ Socket States ────────────────────────────────────┐    │
│ │ ✅ Table_PlacePoint1 (Enabled)                     │    │
│ │ ✅ Table_PlacePoint2 (Enabled)                     │    │
│ │ ✅ Valve_PlacePoint (Enabled)                      │    │
│ │ ❌ Manifold_Socket (Disabled - Wrong Task Group)   │    │
│ │ ❌ Hose_Socket (Disabled - Wrong Task Group)       │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                             │
│ [Refresh] [Enable All Sockets] [Reset Sequence]            │
└─────────────────────────────────────────────────────────────┘
```

### **Implementation Steps:**

#### **Step 1: Add Runtime Monitor Tab**
**File:** `VRInteractionSetupWindow.cs`

**Changes:**
```csharp
// Add new tab enum value
private enum Tab
{
    Setup,
    Configure,
    Sequence,
    RuntimeMonitor, // NEW
    Validate
}

// Add tab drawing in OnGUI()
case Tab.RuntimeMonitor:
    DrawRuntimeMonitorTab();
    break;
```

#### **Step 2: Implement DrawRuntimeMonitorTab()**

**Features:**
1. **Play Mode Detection:**
   ```csharp
   if (!EditorApplication.isPlaying)
   {
       EditorGUILayout.HelpBox("⏸️ Runtime Monitor is only available in Play Mode", MessageType.Info);
       return;
   }
   ```

2. **Find Active Controller:**
   ```csharp
   var controller = FindObjectOfType<ModularTrainingSequenceController>();
   if (controller == null)
   {
       EditorGUILayout.HelpBox("No ModularTrainingSequenceController found in scene", MessageType.Warning);
       return;
   }
   ```

3. **Get Progress Data:**
   ```csharp
   var progress = controller.GetProgress();
   var currentModule = progress.currentModuleName;
   var currentTaskGroup = progress.currentTaskGroupName;
   var completedSteps = progress.completedSteps;
   var totalSteps = progress.totalSteps;
   ```

4. **Display Step List:**
   ```csharp
   foreach (var step in currentTaskGroup.steps)
   {
       string icon = GetStepIcon(step);
       string status = GetStepStatus(step);
       EditorGUILayout.LabelField($"{icon} {step.stepName} ({status})");
   }
   ```

5. **Display Socket States:**
   ```csharp
   // Access restrictionManager from controller (need to make it public)
   var socketStates = GetSocketStates(controller);
   foreach (var socketState in socketStates)
   {
       string icon = socketState.enabled ? "✅" : "❌";
       string reason = socketState.enabled ? "Enabled" : socketState.disabledReason;
       EditorGUILayout.LabelField($"{icon} {socketState.name} ({reason})");
   }
   ```

#### **Step 3: Make restrictionManager Accessible**

**File:** `ModularTrainingSequenceController.cs`

**Change Line 33:**
```csharp
// OLD:
private SequenceFlowRestrictionManager restrictionManager;

// NEW:
public SequenceFlowRestrictionManager restrictionManager; // Public for editor access
```

#### **Step 4: Add Socket State Query Method**

**File:** `SequenceFlowRestrictionManager.cs`

**Add new method:**
```csharp
/// <summary>
/// Get current socket states for debugging/editor display
/// </summary>
public List<SocketStateInfo> GetSocketStates()
{
    var states = new List<SocketStateInfo>();

    foreach (var kvp in socketComponents)
    {
        var socketObj = kvp.Key;
        var component = kvp.Value;

        bool isEnabled = IsSocketEnabled(socketObj);
        bool isInCurrentGroup = IsSocketInCurrentTaskGroup(socketObj);

        states.Add(new SocketStateInfo
        {
            name = socketObj.name,
            enabled = isEnabled,
            disabledReason = isInCurrentGroup ? "" : "Wrong Task Group"
        });
    }

    return states;
}

public class SocketStateInfo
{
    public string name;
    public bool enabled;
    public string disabledReason;
}

// Helper methods
private bool IsSocketEnabled(GameObject socketObj)
{
    var xriSocket = socketObj.GetComponent<XRSocketInteractor>();
    if (xriSocket != null) return xriSocket.enabled;

    // Check AutoHands PlacePoint enabled state
    var allComps = socketObj.GetComponents<MonoBehaviour>();
    foreach (var comp in allComps)
    {
        if (comp != null && comp.GetType().Name.Contains("PlacePoint"))
            return comp.enabled;
    }

    return false;
}

private bool IsSocketInCurrentTaskGroup(GameObject socketObj)
{
    foreach (var step in activeSteps)
    {
        if (step.destination?.GameObject == socketObj ||
            step.targetSocket?.GameObject == socketObj)
            return true;
    }
    return false;
}
```

### **Estimated Implementation Time:**
- Step 1 (Add tab): 15 minutes
- Step 2 (Basic display): 1 hour
- Step 3 (Controller access): 5 minutes
- Step 4 (Socket state query): 30 minutes
- Testing & polish: 30 minutes

**Total: ~2.5 hours**

---

## 🔮 **Future Enhancements (Not Planned Yet)**

### **Step-by-Step Sequential Flow** (Advanced)
If you need finer control in the future:
- Only enable socket for **current active step** (not all steps in group)
- Respect `allowParallel` flag for parallel execution
- Would require modifying `ModularTrainingSequenceController` to not start all steps at once

### **Grabbable Restrictions** (Phase 4?)
- Disable grabbable components (not just sockets)
- Only current step's target object is grabbable
- More restrictive than current implementation

### **Visual Indicators in Scene** (Phase 5?)
- Highlight enabled sockets with green glow
- Disabled sockets show red indicator
- Visual feedback without looking at editor window

---

## 📁 **File Locations Reference**

### **Core Scripts:**
```
Assets/VRTrainingKit/Scripts/
├── Core/
│   └── Controllers/
│       ├── ModularTrainingSequenceController.cs  (Main controller)
│       └── SequenceFlowRestrictionManager.cs     (Socket restrictions)
├── SequenceSystem/
│   └── Data/
│       └── TrainingSequence.cs                   (Data structures)
└── Editor/
    └── Windows/
        └── VRInteractionSetupWindow.cs           (Editor UI)
```

### **Testing Assets:**
```
Assets/VRTrainingKit/
├── Sequences/
│   └── [Your Training Sequence Assets].asset
└── Scenes/
    └── [Your Test Scenes].unity
```

---

## 🚀 **Getting Started on Phase 3**

### **For New Claude Chat Session:**

1. **Share this file** + context about VR Training Kit architecture
2. **Start with:** "Continue Phase 3: Runtime Monitor Tab implementation from SEQUENCE_FLOW_STATUS.md"
3. **Key info to provide:**
   - Current Unity version
   - Framework in use (XRI / AutoHands / Both)
   - Any specific UI preferences for the monitor tab

### **Quick Reference:**
- Task group restrictions: ✅ Working
- Socket enable/disable: ✅ Working
- XRI + AutoHands support: ✅ Working
- Runtime Monitor Tab: ⏸️ Not started

---

## 📝 **Notes & Decisions**

### **Why Task Group Level?**
- User confirmed: "The task group level restriction works well for my immediate needs"
- Steps within a group can be done in any order (realistic workflow)
- Sequential enforcement between groups (prevents wrong sequence)
- Simpler code = easier to maintain

### **Why No Lookahead?**
- Controller starts all steps simultaneously within a task group
- Lookahead would enable sockets that are already enabled
- Redundant logic removed for clarity
- Can be added later if step-by-step control is needed

### **Architecture Decisions:**
- Socket-only restrictions (grabbables remain active)
- Works with existing controller without major refactoring
- Uses reflection for AutoHands (avoids hard dependencies)
- Service pattern for clean separation of concerns

---

**End of Document**

# Phase 1: Framework Selection & Setup System - Detailed Implementation Plan

## **Overview**
Establish framework choice and validation system with explicit XRI vs AutoHands selection, rig detection, and automated setup.

**Duration:** 1-2 weeks
**Complexity:** Medium (UI + rig management + persistent settings)

---

## **Deliverable 1: VRFrameworkManager.cs (3-4 days)**
**Goal:** Central framework management and preference storage

### **Sub-tasks:**
1. **Framework Type Definition**
   - Create `VRFrameworkType` enum (None, XRI, AutoHands)
   - Framework preference storage in EditorPrefs/ProjectSettings
   - Scene-level framework override option

2. **Rig Detection Utilities**
   - `DetectCurrentFramework()` - analyze scene components
   - `ValidateFrameworkSetup()` - check rig completeness
   - `GetFrameworkCompatibilityIssues()` - detect conflicts

3. **Framework State Management**
   - Get/Set framework preference with persistence
   - Scene validation against chosen framework
   - Migration helpers for framework switching

### **Implementation Details:**
```csharp
public enum VRFrameworkType { None, XRI, AutoHands }

public static class VRFrameworkManager
{
    // Framework preference storage
    public static VRFrameworkType PreferredFramework { get; set; }

    // Rig detection methods
    public static VRFrameworkType DetectSceneFramework();
    public static bool IsXRIRigPresent();
    public static bool IsAutoHandsRigPresent();

    // Validation methods
    public static List<string> ValidateFrameworkSetup(VRFrameworkType framework);
    public static List<string> GetCompatibilityIssues();
}
```

### **Testing Criteria:**
- Framework preference persists across Unity sessions
- Accurate detection of XRI rigs (XR Origin + components)
- Accurate detection of AutoHands rigs (Hand components)
- Clear validation messages for incomplete setups

---

## **Deliverable 2: Rig Detection System (2-3 days)**
**Goal:** Automatic detection of existing XR rigs and validation

### **Sub-tasks:**
1. **XRI Rig Detection**
   - Scan for `XR Origin` GameObject
   - Validate `XR Camera` child presence
   - Check for hand controllers with interaction components
   - Verify `XR Interaction Manager` in scene

2. **AutoHands Rig Detection**
   - Scan for `Hand` component instances
   - Validate left/right hand pair
   - Check for required input components
   - Verify AutoHands-specific scene setup

3. **Rig Completeness Validation**
   - Component dependency checking
   - Missing component identification
   - Configuration validation (layers, tags, etc.)

### **Implementation Details:**
```csharp
public static class RigDetectionUtility
{
    // XRI Detection
    public static XRIRigStatus DetectXRIRig();
    public static bool HasXROrigin();
    public static bool HasXRCamera();
    public static bool HasHandControllers();
    public static bool HasInteractionManager();

    // AutoHands Detection
    public static AutoHandsRigStatus DetectAutoHandsRig();
    public static bool HasHandComponents();
    public static bool HasInputComponents();
    public static bool HasRequiredLayers();
}

public class XRIRigStatus
{
    public bool IsPresent;
    public bool IsComplete;
    public List<string> MissingComponents;
    public GameObject XROrigin;
}

public class AutoHandsRigStatus
{
    public bool IsPresent;
    public bool IsComplete;
    public List<string> MissingComponents;
    public List<Hand> HandComponents;
}
```

### **Testing Criteria:**
- Correct detection in scene with XRI rig
- Correct detection in scene with AutoHands rig
- Accurate "None" detection in empty scene
- Proper missing component identification

---

## **Deliverable 3: Framework Setup Tab UI (2-3 days)**
**Goal:** Add Framework Setup tab to VRInteractionSetupWindow

### **Sub-tasks:**
1. **Tab Structure Addition**
   - Add "Framework" tab to existing tab system
   - Framework selection dropdown UI
   - Current status display section
   - Action buttons section

2. **Status Display Implementation**
   - Current framework preference indicator
   - Scene rig detection results
   - Compatibility warnings/errors
   - Visual status indicators (✓ ○ ⚠️ ❌)

3. **User Action Interface**
   - "Setup XRI Rig" button with progress feedback
   - "Setup AutoHands Rig" button with progress feedback
   - "Remove Conflicting Rig" option
   - Framework preference change with confirmation

### **Implementation Details:**
```csharp
// Extension to existing VRInteractionSetupWindow
private VRFrameworkType selectedFramework;
private XRIRigStatus xriStatus;
private AutoHandsRigStatus autoHandsStatus;

private void DrawFrameworkTab()
{
    // Framework Selection
    DrawFrameworkSelection();

    // Current Status
    DrawCurrentStatus();

    // Actions
    DrawFrameworkActions();

    // Validation Messages
    DrawValidationMessages();
}
```

### **UI Layout:**
```
Framework Setup Tab
├── Framework Selection
│   └── Dropdown: [XRI / AutoHands]
├── Current Scene Status
│   ├── Detected Framework: [XRI/AutoHands/None/Mixed]
│   ├── Rig Completeness: [✓ Complete / ⚠️ Incomplete / ❌ Missing]
│   └── Issues: [List of problems]
├── Actions
│   ├── [Setup XRI Rig] button
│   ├── [Setup AutoHands Rig] button
│   └── [Remove Conflicting Components] button
└── Validation Messages
    └── Framework-specific guidance and warnings
```

### **Testing Criteria:**
- Tab appears correctly in VRInteractionSetupWindow
- Framework selection dropdown works
- Status display updates in real-time
- Action buttons enable/disable appropriately

---

## **Deliverable 4: Automated Rig Setup (3-4 days)**
**Goal:** Automated creation of XRI and AutoHands rigs

### **Sub-tasks:**
1. **XRI Rig Creation**
   - Programmatic XR Origin creation
   - XR Camera setup with proper hierarchy
   - Hand controller creation with interaction components
   - XR Interaction Manager scene setup

2. **AutoHands Rig Creation**
   - Hand component instantiation (left/right pair)
   - Input component configuration
   - Layer and collision setup
   - Required scene dependencies

3. **Rig Cleanup/Replacement**
   - Safe removal of conflicting framework components
   - Component dependency cleanup
   - Hierarchy preservation where possible
   - Undo support for rig operations

### **Implementation Strategy:**

**Option A: Prefab-Based Setup** (if you provide prefabs)
```csharp
public static void SetupXRIRig()
{
    var xriPrefab = LoadXRIRigPrefab();
    var rigInstance = Instantiate(xriPrefab);
    ConfigureXRIRig(rigInstance);
}
```

**Option B: Programmatic Creation** (no prefabs needed)
```csharp
public static void SetupXRIRig()
{
    var xrOrigin = CreateXROrigin();
    var xrCamera = CreateXRCamera(xrOrigin);
    var handControllers = CreateHandControllers(xrOrigin);
    var interactionManager = CreateInteractionManager();
}
```

### **Testing Criteria:**
- XRI rig created correctly in empty scene
- AutoHands rig created correctly in empty scene
- Conflicting rig removal works safely
- Created rigs pass validation checks

---

## **Deliverable 5: Integration Testing (1-2 days)**
**Goal:** End-to-end testing of framework selection workflow

### **Test Scenarios:**
1. **Clean Scene Testing**
   - Start with empty scene
   - Select XRI framework
   - Setup XRI rig via button
   - Validate rig completeness

2. **Framework Switching Testing**
   - Scene with XRI rig
   - Switch to AutoHands preference
   - Handle rig conflict resolution
   - Setup AutoHands rig

3. **Persistence Testing**
   - Set framework preference
   - Close/reopen Unity
   - Verify preference persistence
   - Verify UI state restoration

4. **Error Handling Testing**
   - Incomplete rig scenarios
   - Mixed framework scenarios
   - Missing component scenarios
   - User cancellation scenarios

### **Success Criteria:**
- All test scenarios pass
- No Unity errors during rig operations
- Framework preference persists correctly
- User experience is smooth and intuitive

---

## **Questions for Implementation:**

### **1. XR Rig Prefab Strategy:**
Do you want me to:
- **Option A**: Create rigs programmatically from scratch?
- **Option B**: Use existing prefabs (please specify location)?
- **Option C**: Download/create standard XRI/AutoHands prefabs?

### **2. Framework Preference Storage:**
Where should I store the framework choice:
- **EditorPrefs** (per-machine setting)?
- **ProjectSettings** (per-project setting)?
- **Scene-level** ScriptableObject?

### **3. VRInteractionSetupWindow Location:**
I need to find the existing VRInteractionSetupWindow file. Where is it located?
- `Assets/VRTrainingKit/Scripts/Editor/`?
- `Assets/VRTrainingKit/Scripts/`?
- Different location?

### **4. Conflict Resolution Strategy:**
When both XRI and AutoHands rigs are present, should I:
- **Warn and let user choose** which to keep?
- **Automatically prefer** one framework?
- **Support mixed mode** for advanced users?

---

## **Phase 1 Timeline:**

**Week 1:**
- Days 1-2: VRFrameworkManager implementation
- Days 3-4: Rig detection system
- Day 5: Integration testing

**Week 2:**
- Days 1-2: Framework Setup Tab UI
- Days 3-4: Automated rig setup
- Day 5: End-to-end testing and polish

**Milestone:** Framework selection and rig setup working reliably before proceeding to Phase 2.
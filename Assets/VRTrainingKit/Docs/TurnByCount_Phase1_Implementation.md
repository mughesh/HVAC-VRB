# TurnByCount - Phase 1 Implementation Guide

## Overview

Phase 1 integrates TurnByCountProfile with the tag-based discovery system. After this phase, you'll be able to:
1. Tag objects with "turn" (e.g., allen keys)
2. Scan scene in Setup Assistant
3. See "turn" tagged objects listed
4. Apply TurnByCountProfile to configure them

---

## Step-by-Step Implementation

### **Step 1: Add "turn" Tag to Unity** ✅ USER TASK

**Unity Editor Steps:**
1. **Edit** → **Project Settings** → **Tags and Layers**
2. Under **Tags**, click the **+** button
3. Add new tag: `turn`
4. Click **Save**

**Test:** Create a test GameObject, try to assign "turn" tag - it should appear in dropdown

---

### **Step 2: Update InteractionSetupService.cs** 📝 CODE CHANGE

**File:** `Assets/VRTrainingKit/Scripts/Core/Services/InteractionSetupService.cs`

#### Change 1: Add turnObjects to SceneAnalysis class (Line ~12-18)

**Find this code:**
```csharp
public class SceneAnalysis
{
    public List<GameObject> grabObjects = new List<GameObject>();
    public List<GameObject> knobObjects = new List<GameObject>();
    public List<GameObject> snapObjects = new List<GameObject>();
    public List<GameObject> toolObjects = new List<GameObject>();
    public List<GameObject> valveObjects = new List<GameObject>();
```

**Add after valveObjects:**
```csharp
    public List<GameObject> turnObjects = new List<GameObject>();
```

**Result:**
```csharp
public class SceneAnalysis
{
    public List<GameObject> grabObjects = new List<GameObject>();
    public List<GameObject> knobObjects = new List<GameObject>();
    public List<GameObject> snapObjects = new List<GameObject>();
    public List<GameObject> toolObjects = new List<GameObject>();
    public List<GameObject> valveObjects = new List<GameObject>();
    public List<GameObject> turnObjects = new List<GameObject>();  // ← NEW
```

---

#### Change 2: Add "turn" tag scanning (Line ~57-61)

**Find this code:**
```csharp
                else if (obj.CompareTag("valve"))
                {
                    analysis.valveObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found valve object: {obj.name} (Tag: {obj.tag})");
                }
            }  // ← End of foreach loop
```

**Add BEFORE the closing brace `}`:**
```csharp
                else if (obj.CompareTag("turn"))
                {
                    analysis.turnObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found turn object: {obj.name} (Tag: {obj.tag})");
                }
```

**Result:**
```csharp
                else if (obj.CompareTag("valve"))
                {
                    analysis.valveObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found valve object: {obj.name} (Tag: {obj.tag})");
                }
                else if (obj.CompareTag("turn"))  // ← NEW
                {
                    analysis.turnObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found turn object: {obj.name} (Tag: {obj.tag})");
                }
            }
```

---

#### Change 3: Update TotalInteractables property (Line ~20-26)

**Find this code:**
```csharp
            public int TotalInteractables => grabObjects.Count +
                                              knobObjects.Count +
                                              snapObjects.Count +
                                              toolObjects.Count +
                                              valveObjects.Count;
```

**Add `+ turnObjects.Count`:**
```csharp
            public int TotalInteractables => grabObjects.Count +
                                              knobObjects.Count +
                                              snapObjects.Count +
                                              toolObjects.Count +
                                              valveObjects.Count +
                                              turnObjects.Count;  // ← NEW
```

---

#### Change 4: Add debug logging (Line ~64-70)

**Find this code:**
```csharp
            Debug.Log($"Scene Analysis Complete: {analysis.TotalInteractables} interactables found");
            Debug.Log($"  - Grab Objects: {analysis.grabObjects.Count}");
            Debug.Log($"  - Knob Objects: {analysis.knobObjects.Count}");
            Debug.Log($"  - Snap Points: {analysis.snapObjects.Count}");
            Debug.Log($"  - Tool Objects: {analysis.toolObjects.Count}");
            Debug.Log($"  - Valve Objects: {analysis.valveObjects.Count}");
```

**Add after Valve Objects:**
```csharp
            Debug.Log($"  - Turn Objects: {analysis.turnObjects.Count}");
```

---

### **Step 3: Update VRInteractionSetupWindow.cs** 📝 CODE CHANGE

**File:** `Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

We need to add "Turn Objects" section in the Setup tab UI.

#### Change 1: Find the Setup Tab Drawing Code (Search for "Grab Objects")

**Search for:** `"Grab Objects"` in the file (around line ~725-750)

You'll see a pattern like this:
```csharp
// Grab Objects
DrawObjectList(sceneAnalysis.grabObjects, "Grab Objects", "grab", selectedGrabProfile);

// Knob Objects
DrawObjectList(sceneAnalysis.knobObjects, "Knob Objects", "knob", selectedKnobProfile);

// etc...
```

#### Change 2: Add Turn Objects Section

**Find where Valve Objects are drawn** (after Tool Objects, before Snap Points):
```csharp
// Valve Objects
DrawObjectList(sceneAnalysis.valveObjects, "Valve Objects", "valve", selectedValveProfile);

// Snap Points (usually last)
DrawObjectList(sceneAnalysis.snapObjects, "Snap Points", "snap", selectedSnapProfile);
```

**Add BETWEEN Valve and Snap:**
```csharp
// Valve Objects
DrawObjectList(sceneAnalysis.valveObjects, "Valve Objects", "valve", selectedValveProfile);

// Turn Objects - NEW SECTION
EditorGUILayout.Space(10);
DrawObjectList(sceneAnalysis.turnObjects, "Turn Objects", "turn", selectedTurnProfile);

// Snap Points
DrawObjectList(sceneAnalysis.snapObjects, "Snap Points", "snap", selectedSnapProfile);
```

**Note:** We need to add `selectedTurnProfile` variable. Let me show you where...

---

#### Change 3: Add selectedTurnProfile Variable

**Search for:** `private InteractionProfile selected` (around line ~60-70)

**Find this code:**
```csharp
    private InteractionProfile selectedGrabProfile;
    private InteractionProfile selectedKnobProfile;
    private InteractionProfile selectedSnapProfile;
    private InteractionProfile selectedToolProfile;
    private InteractionProfile selectedValveProfile;
```

**Add:**
```csharp
    private InteractionProfile selectedTurnProfile;  // ← NEW
```

---

#### Change 4: Add Turn Profile Loading in Configure Tab

**Search for:** `LoadProfiles()` method (around line ~950-1000)

**Find this code:**
```csharp
    private void LoadProfiles()
    {
        grabProfiles = LoadProfilesOfType<GrabProfile>("GrabProfile");
        knobProfiles = LoadProfilesOfType<KnobProfile>("KnobProfile");
        snapProfiles = LoadProfilesOfType<SnapProfile>("SnapProfile");
        toolProfiles = LoadProfilesOfType<ToolProfile>("ToolProfile");
        valveProfiles = LoadProfilesOfType<ValveProfile>("ValveProfile");
```

**Add:**
```csharp
        turnProfiles = LoadProfilesOfType<TurnByCountProfile>("TurnByCountProfile");  // ← NEW
```

---

#### Change 5: Add Turn Profile List Variable

**Search for:** `private List<InteractionProfile>` (around line ~75-85)

**Find:**
```csharp
    private List<InteractionProfile> grabProfiles;
    private List<InteractionProfile> knobProfiles;
    private List<InteractionProfile> snapProfiles;
    private List<InteractionProfile> toolProfiles;
    private List<InteractionProfile> valveProfiles;
```

**Add:**
```csharp
    private List<InteractionProfile> turnProfiles;  // ← NEW
```

---

#### Change 6: Add Turn Profile UI in Configure Tab

**Search for:** `"Valve Profile"` in Configure tab rendering (around line ~1100-1150)

**Find this section:**
```csharp
// Valve Profile
EditorGUILayout.Space(10);
EditorGUILayout.LabelField("Valve Profile", EditorStyles.boldLabel);
DrawProfileSelector(valveProfiles, ref selectedValveProfile, () => CreateNewProfile<ValveProfile>("NewValveProfile"));
```

**Add AFTER Valve Profile:**
```csharp
// Turn By Count Profile - NEW SECTION
EditorGUILayout.Space(10);
EditorGUILayout.LabelField("Turn By Count Profile", EditorStyles.boldLabel);
DrawProfileSelector(turnProfiles, ref selectedTurnProfile, () => CreateNewProfile<TurnByCountProfile>("NewTurnByCountProfile"));
```

---

### **Step 4: Update DrawObjectList() Validation** 📝 CODE CHANGE (Optional but Recommended)

This makes configured "turn" objects show green checkmark.

**Search for:** `if (tag == "grab" || tag == "knob"` in `DrawObjectList` method (around line ~780-800)

**Find this code:**
```csharp
if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve")
{
    var grabbable = obj.GetComponent<Autohand.Grabbable>();
    isConfigured = grabbable != null;
}
```

**Change to:**
```csharp
if (tag == "grab" || tag == "knob" || tag == "tool" || tag == "valve" || tag == "turn")  // ← Added "turn"
{
    var grabbable = obj.GetComponent<Autohand.Grabbable>();
    isConfigured = grabbable != null;
}
```

---

## Phase 1 Testing Checklist

After making all changes:

### Test 1: Compile Check
- [ ] No compilation errors in Unity
- [ ] Console shows no red errors

### Test 2: Tag Check
- [ ] "turn" tag exists in Tag Manager
- [ ] Can assign "turn" tag to test GameObject

### Test 3: Scene Scanning
- [ ] Create test GameObject, tag it with "turn"
- [ ] Open Setup Assistant → Setup Tab
- [ ] Click "Scan Scene"
- [ ] **Expected:** Console shows: `Found turn object: [YourObjectName] (Tag: turn)`
- [ ] **Expected:** "Turn Objects" section appears with your object listed

### Test 4: Profile Creation
- [ ] Right-click in Project → Create → VR Training → Turn By Count Profile
- [ ] Asset created successfully
- [ ] Can configure turnCount, direction, etc. in Inspector

### Test 5: Configure Tab
- [ ] Open Setup Assistant → Configure Tab
- [ ] **Expected:** "Turn By Count Profile" section visible
- [ ] Can select/create turn profiles

### Test 6: Profile Application (Basic)
- [ ] Setup tab → Select your "turn" tagged object
- [ ] Select a TurnByCountProfile in Configure tab
- [ ] Click "Configure" button for the object
- [ ] **Expected:** Object shows green checkmark ✓
- [ ] **Expected:** Object has Grabbable component added
- [ ] **Expected:** Object has Rigidbody added
- [ ] **Expected:** Object has Collider added

---

## Summary of Files Modified

| File | Changes | Lines Changed |
|------|---------|---------------|
| `InteractionSetupService.cs` | Add turnObjects list, scanning, logging | ~10 lines |
| `VRInteractionSetupWindow.cs` | Add UI sections, variables, profile handling | ~15 lines |
| `TurnByCountProfile.cs` | Already created ✓ | - |

---

## Next Steps

After Phase 1 is complete and tested:
- **Phase 2:** Create AutoHandsTurnByCountController.cs (the runtime component)
- **Phase 3:** Create AutoHandsTurnByCountStepHandler.cs (handles sequence steps)
- **Phase 4:** Integration testing with full workflow

---

## Troubleshooting

### Problem: "turn" tag not found
**Solution:** Make sure you added "turn" tag in Tag Manager (Step 1)

### Problem: "Turn Objects" section doesn't appear
**Solution:** Check if you added DrawObjectList call in VRInteractionSetupWindow

### Problem: Objects don't show as configured (no green checkmark)
**Solution:** Make sure you updated the validation code to include `|| tag == "turn"`

### Problem: TurnByCountProfile not found in Create menu
**Solution:** Check if `[CreateAssetMenu]` attribute exists in TurnByCountProfile.cs

---

**Status:** Ready for implementation
**Estimated Time:** 20-30 minutes

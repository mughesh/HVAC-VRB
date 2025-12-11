# VRInteractionSetupWindow Refactoring Plan

## Overview

**Goal:** Refactor `VRInteractionSetupWindow.cs` from 3,385 lines to ~800 lines through modular extraction.

**Status:** Phase 1 in progress

## Problem Statement

The `VRInteractionSetupWindow.cs` file has grown beyond maintainable limits:
- 3,385 lines total
- ~35% repetitive/duplicated code
- 5 tabs with mixed responsibilities
- 7 nearly identical profile cache methods
- 7 nearly identical profile selection UI sections

## Current Structure Analysis

| Section | Lines | Complexity | Notes |
|---------|-------|------------|-------|
| Setup Tab | ~83 | Low | Scene scanning UI |
| Configure Tab | ~483 | High | 7 repetitive profile sections |
| Sequence Tab | ~1,175 | Very High | Tree view + properties panel |
| Runtime Monitor Tab | ~263 | Medium | Live monitoring |
| Validate Tab | ~32 | Low | Simple validation |
| Profile Cache Methods | ~174 | High | 7 identical methods |
| Utilities | ~417 | Medium | Various helpers |

## Phased Implementation Plan

### Phase 1: Infrastructure & Shared Resources (Current)

**Objective:** Create base infrastructure for state and styles sharing.

**Files to Create:**
1. `Editor/Core/VRTrainingWindowState.cs` - Shared state container
2. `Editor/UI/EditorStyles/VRTrainingEditorStyles.cs` - Centralized GUI styles

**State Fields to Extract:**
```csharp
// Tab state
Tab currentTab

// Setup tab
SceneAnalysis sceneAnalysis
Vector2 setupScrollPos

// Configure tab
InteractionProfile selectedGrabProfile, selectedKnobProfile, selectedSnapProfile
InteractionProfile selectedToolProfile, selectedValveProfile, selectedTurnProfile
InteractionProfile selectedTeleportProfile
List<InteractionProfile> cachedGrabProfiles, cachedKnobProfiles, cachedSnapProfiles
List<InteractionProfile> cachedToolProfiles, cachedValveProfiles, cachedTurnProfiles
List<InteractionProfile> cachedTeleportProfiles
Vector2 configScrollPos

// Sequence tab
TrainingSequenceAsset currentTrainingAsset
TrainingProgram currentProgram
TrainingSequenceAsset[] availableAssets
int selectedAssetIndex
bool assetsLoaded
Vector2 treeViewScrollPos, detailsPanelScrollPos
object selectedHierarchyItem
string selectedItemType
bool showAddMenu
float splitterPosition

// Validate tab
List<string> validationIssues
Vector2 validateScrollPos
```

**Styles to Extract:**
```csharp
GUIStyle headerStyle      // 14pt, Bold, White
GUIStyle subHeaderStyle   // 12pt, Bold, White
GUIStyle successStyle     // Green text
GUIStyle warningStyle     // Yellow text
GUIStyle errorStyle       // Red text
```

**Testing Checklist:**
- [ ] Window opens without errors
- [ ] Styles render correctly on all tabs
- [ ] State persists across tab switches
- [ ] No null reference exceptions

---

### Phase 2: Profile Cache Manager

**Objective:** Replace 7 repetitive cache refresh methods with generic system.

**File to Create:**
- `Editor/Managers/ProfileCacheManager.cs`

**Methods to Consolidate:**
- `RefreshGrabProfileCache()` -> `RefreshCache(ProfileType.Grab)`
- `RefreshKnobProfileCache()` -> `RefreshCache(ProfileType.Knob)`
- `RefreshSnapProfileCache()` -> `RefreshCache(ProfileType.Snap)`
- `RefreshToolProfileCache()` -> `RefreshCache(ProfileType.Tool)`
- `RefreshValveProfileCache()` -> `RefreshCache(ProfileType.Valve)`
- `RefreshTurnProfileCache()` -> `RefreshCache(ProfileType.Turn)`
- `RefreshTeleportProfileCache()` -> `RefreshCache(ProfileType.Teleport)`

**Pattern to Implement:**
```csharp
public enum ProfileType { Grab, Knob, Snap, Tool, Valve, Turn, Teleport }

public void RefreshCache(ProfileType type)
{
    var profiles = new List<InteractionProfile>();

    // XRI profiles
    foreach (var guid in AssetDatabase.FindAssets($"t:{GetXRITypeName(type)}"))
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
        if (profile != null && IsValidProfile(profile, type))
            profiles.Add(profile);
    }

    // AutoHands profiles
    foreach (var guid in AssetDatabase.FindAssets($"t:{GetAutoHandsTypeName(type)}"))
    {
        // Same pattern...
    }

    _caches[type] = profiles;
}
```

**Testing Checklist:**
- [ ] All 7 profile types cache correctly
- [ ] Both XRI and AutoHands profiles detected
- [ ] Refresh buttons work in Configure tab
- [ ] Profile validation still works

---

### Phase 3: Configure Tab Extraction

**Objective:** Extract Configure tab with generic profile section drawing.

**File to Create:**
- `Editor/UI/Tabs/ConfigureTabDrawer.cs`

**Pattern to Implement:**
```csharp
public class ConfigureTabDrawer
{
    public void Draw(VRTrainingWindowState state, ProfileCacheManager cache)
    {
        // Framework notice
        DrawFrameworkNotice();

        // Generic profile sections
        DrawProfileSection("Grab Profile", ProfileType.Grab);
        DrawProfileSection("Knob Profile", ProfileType.Knob);
        DrawProfileSection("Snap Profile", ProfileType.Snap);
        // ... etc
    }

    private void DrawProfileSection(string label, ProfileType type)
    {
        // One method instead of 7 copy-paste sections
    }
}
```

**Testing Checklist:**
- [ ] All 7 profile sections render correctly
- [ ] Profile selection works for each type
- [ ] "Create New Profile" buttons function
- [ ] "Edit Profile" selects asset in Project
- [ ] "Create All Default Profiles" works

---

### Phase 4: Sequence Tab - Tree View

**Objective:** Extract the left panel (hierarchy tree view).

**Files to Create:**
1. `Editor/UI/SequenceEditor/SequenceTreeView.cs`
2. `Editor/UI/SequenceEditor/ISequenceTreeViewCallbacks.cs`

**Methods to Extract:**
- `DrawTreeViewContent()`
- `DrawProgramTreeItem()`
- `DrawModuleTreeItem()`
- `DrawTaskGroupTreeItem()`
- `DrawStepTreeItem()`
- `GetStepTypeIcon()`
- All Add/Delete menu methods

**Testing Checklist:**
- [ ] Tree view renders program hierarchy
- [ ] Foldouts expand/collapse correctly
- [ ] Selection highlighting works
- [ ] Add buttons show context menus
- [ ] Delete buttons show confirmation
- [ ] New items auto-select after creation
- [ ] Auto-save triggers on changes

---

### Phase 5: Sequence Tab - Properties Panel

**Objective:** Extract the right panel including DrawStepProperties (333 lines).

**Files to Create:**
1. `Editor/UI/SequenceEditor/SequencePropertiesPanel.cs`
2. `Editor/UI/SequenceEditor/StepPropertiesDrawer.cs`

**DrawStepProperties Breakdown:**
| Sub-section | Method |
|-------------|--------|
| Basic properties | `DrawBasicProperties()` |
| Target objects | `DrawTargetObjectFields()` |
| WaitForScriptCondition | `DrawWaitConditionInfo()` |
| Knob settings | `DrawKnobSettings()` |
| Valve settings | `DrawValveSettings()` |
| Teleport settings | `DrawTeleportSettings()` |
| Execution settings | `DrawExecutionSettings()` |
| Guidance arrows | `DrawGuidanceArrows()` |
| Validation | `DrawValidationStatus()` |

**Testing Checklist:**
- [ ] All 11 step types render correctly
- [ ] Guidance arrow fields work
- [ ] Changes trigger auto-save

---

### Phase 6: Remaining Tabs & Final Integration

**Objective:** Extract remaining tabs, clean up main window.

**Files to Create:**
1. `Editor/UI/Tabs/SetupTabDrawer.cs`
2. `Editor/UI/Tabs/ValidateTabDrawer.cs`
3. `Editor/UI/Tabs/RuntimeMonitorTabDrawer.cs`
4. `Editor/Managers/TrainingAssetManager.cs`

**Final Main Window Target (~800 lines):**
```csharp
public class VRInteractionSetupWindow : EditorWindow
{
    // Managers
    private VRTrainingWindowState _state;
    private ProfileCacheManager _profileCache;
    private TrainingAssetManager _assetManager;

    // Tab drawers
    private SetupTabDrawer _setupTab;
    private ConfigureTabDrawer _configureTab;
    private SequenceTreeView _sequenceTree;
    private SequencePropertiesPanel _sequenceProperties;
    private RuntimeMonitorTabDrawer _runtimeTab;
    private ValidateTabDrawer _validateTab;

    private void OnEnable() { /* Initialize managers & drawers */ }
    private void OnGUI() { /* Tab dispatch only */ }
}
```

---

## Final Directory Structure

```
Assets/VRTrainingKit/Scripts/Editor/
├── Core/
│   └── VRTrainingWindowState.cs
├── Managers/
│   ├── ProfileCacheManager.cs
│   └── TrainingAssetManager.cs
├── UI/
│   ├── EditorStyles/
│   │   └── VRTrainingEditorStyles.cs
│   ├── SequenceEditor/
│   │   ├── ISequenceTreeViewCallbacks.cs
│   │   ├── SequenceTreeView.cs
│   │   ├── SequencePropertiesPanel.cs
│   │   └── StepPropertiesDrawer.cs
│   └── Tabs/
│       ├── SetupTabDrawer.cs
│       ├── ConfigureTabDrawer.cs
│       ├── ValidateTabDrawer.cs
│       └── RuntimeMonitorTabDrawer.cs
└── Windows/
    └── VRInteractionSetupWindow.cs (~800 lines)
```

---

## Git Workflow

**Base Branch:** `UI-script-Refactor`

**Phase Branches:**
- `refactor-phase-1` - Infrastructure
- `refactor-phase-2` - Profile Cache
- `refactor-phase-3` - Configure Tab
- `refactor-phase-4` - Tree View
- `refactor-phase-5` - Properties Panel
- `refactor-phase-6` - Final Integration

**Workflow:**
```bash
# Before each phase
git checkout UI-script-Refactor
git checkout -b refactor-phase-X

# After phase complete and tested
git add -A && git commit -m "Phase X: [Description] complete"
git checkout UI-script-Refactor
git merge refactor-phase-X
```

**Rollback:** `git checkout refactor-phase-X` or `git checkout UI-script-Refactor`

---

## Risk Assessment

| Phase | Risk | Mitigation |
|-------|------|------------|
| 1 | Style timing | Lazy init with null checks |
| 2 | Generic type handling | String-based fallback |
| 3 | UI layout changes | Visual comparison |
| 4 | Selection state sync | Event-based callbacks |
| 5 | Step type handling | Test all 11 types |
| 6 | Integration bugs | End-to-end testing |

---

## Progress Tracking

- [x] Phase 0: Planning complete
- [ ] Phase 1: Infrastructure (in progress)
- [ ] Phase 2: Profile Cache Manager
- [ ] Phase 3: Configure Tab
- [ ] Phase 4: Tree View
- [ ] Phase 5: Properties Panel
- [ ] Phase 6: Final Integration

**Result Target:** 3,385 lines -> ~800 lines (76% reduction)

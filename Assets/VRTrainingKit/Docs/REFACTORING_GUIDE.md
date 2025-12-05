# VRInteractionSetupWindow Refactoring Guide

## Overview

This document explains the modular refactoring of VRInteractionSetupWindow.cs (3,385 lines) into smaller, maintainable components.

## Architecture

### Before Refactoring
- **Single File**: VRInteractionSetupWindow.cs (3,385 lines)
- **Monolithic Structure**: All tab logic in one class
- **Code Duplication**: Repetitive patterns across tabs
- **Hard to Maintain**: Changes affect multiple concerns

### After Refactoring
- **Modular Structure**: Separate files for each major component
- **Base Class Pattern**: VRSetupTabBase for shared functionality
- **Utility Classes**: VRProfileManager for profile operations
- **Clear Separation**: Each tab is self-contained

## File Structure

### Core Components

```
Assets/VRTrainingKit/Scripts/Editor/Windows/
├── VRInteractionSetupWindow.cs        (Main window - coordinator)
├── VRSetupTabBase.cs                  (Abstract base for tabs)
├── VRProfileManager.cs                (Profile management utility)
├── VRSetupTab_Setup.cs                (Setup tab implementation)
├── VRSetupTab_Configure.cs            (Configure tab implementation)
├── VRSetupTab_Validate.cs             (Validate tab implementation)
├── VRSetupTab_RuntimeMonitor.cs       (TODO: Runtime monitor tab)
└── VRSetupTab_Sequence.cs             (TODO: Sequence tab - largest)
```

### Class Responsibilities

#### VRInteractionSetupWindow (Main Coordinator)
- Window lifecycle management
- Tab selection UI
- Tab instance creation and management
- Event delegation to active tabs

#### VRSetupTabBase (Abstract Base Class)
```csharp
public abstract class VRSetupTabBase
{
    protected VRInteractionSetupWindow window;
    protected VRProfileManager profileManager;
    protected GUIStyle headerStyle, subHeaderStyle, successStyle, warningStyle, errorStyle;
    
    public abstract void DrawTab();
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnPlayModeStateChanged(PlayModeStateChange state) { }
    public virtual void Cleanup() { }
}
```

#### VRProfileManager (Utility Class)
- Loads and caches interaction profiles
- Framework detection (XRI vs AutoHands)
- Profile validation helpers
- Reduces AssetDatabase queries

#### Individual Tab Classes
- Self-contained tab implementation
- Manages own state (scroll positions, selections)
- Accesses profiles through VRProfileManager
- Communicates with window when needed

## Integration Steps

### Step 1: Initialize VRProfileManager

In VRInteractionSetupWindow.cs, add field:
```csharp
private VRProfileManager profileManager;
```

In OnEnable(), initialize:
```csharp
private void OnEnable()
{
    InitializeStyles();
    
    // NEW: Create profile manager
    profileManager = new VRProfileManager();
    profileManager.LoadDefaultProfiles();
    profileManager.RefreshProfileCaches();
    
    // ... rest of OnEnable
}
```

### Step 2: Create Tab Instances

Add fields for tab instances:
```csharp
private VRSetupTab_Setup setupTab;
private VRSetupTab_Configure configureTab;
private VRSetupTab_Validate validateTab;
// TODO: Add RuntimeMonitor and Sequence tabs
```

Initialize in OnEnable():
```csharp
private void OnEnable()
{
    // ... existing code ...
    
    // Create tab instances
    setupTab = new VRSetupTab_Setup(this, profileManager);
    configureTab = new VRSetupTab_Configure(this, profileManager);
    validateTab = new VRSetupTab_Validate(this);
    
    // Call OnEnable on tabs
    setupTab.OnEnable();
    configureTab.OnEnable();
    validateTab.OnEnable();
}
```

### Step 3: Update OnGUI() Switch Statement

Replace DrawXXXTab() calls with tab.DrawTab():
```csharp
private void OnGUI()
{
    HandleKeyboardShortcuts();

    // Tab selection UI
    GUILayout.BeginHorizontal();
    if (GUILayout.Toggle(currentTab == Tab.Setup, "Setup", "Button"))
        currentTab = Tab.Setup;
    if (GUILayout.Toggle(currentTab == Tab.Configure, "Configure", "Button"))
        currentTab = Tab.Configure;
    if (GUILayout.Toggle(currentTab == Tab.Sequence, "Sequence", "Button"))
        currentTab = Tab.Sequence;
    if (GUILayout.Toggle(currentTab == Tab.RuntimeMonitor, "Runtime Monitor", "Button"))
        currentTab = Tab.RuntimeMonitor;
    if (GUILayout.Toggle(currentTab == Tab.Validate, "Validate", "Button"))
        currentTab = Tab.Validate;
    GUILayout.EndHorizontal();
    
    EditorGUILayout.Space(10);
    
    // Draw current tab using new modular classes
    switch (currentTab)
    {
        case Tab.Setup:
            setupTab.DrawTab();        // NEW
            break;
        case Tab.Configure:
            configureTab.DrawTab();    // NEW
            break;
        case Tab.Sequence:
            DrawSequenceTab();         // TODO: Create VRSetupTab_Sequence
            break;
        case Tab.RuntimeMonitor:
            DrawRuntimeMonitorTab();   // TODO: Create VRSetupTab_RuntimeMonitor
            break;
        case Tab.Validate:
            validateTab.DrawTab();     // NEW
            break;
    }
}
```

### Step 4: Forward Lifecycle Events

Update OnDisable():
```csharp
private void OnDisable()
{
    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    
    // Forward to tabs
    if (setupTab != null) setupTab.OnDisable();
    if (configureTab != null) configureTab.OnDisable();
    if (validateTab != null) validateTab.OnDisable();
    
    // Save state as before
    if (sceneAnalysis != null)
    {
        EditorPrefs.SetBool("VRTrainingKit_LastSceneAnalysisValid", true);
    }
}
```

Update OnPlayModeStateChanged():
```csharp
private void OnPlayModeStateChanged(PlayModeStateChange state)
{
    // Forward to tabs
    if (setupTab != null) setupTab.OnPlayModeStateChanged(state);
    if (configureTab != null) configureTab.OnPlayModeStateChanged(state);
    if (validateTab != null) validateTab.OnPlayModeStateChanged(state);
    
    // Existing sequence asset refresh logic
    if (state == PlayModeStateChange.EnteredEditMode)
    {
        RefreshTrainingAssetReferences();
    }
}
```

### Step 5: Remove Old Code (After Testing)

Once tabs are verified to work:

1. Remove old DrawSetupTab() method
2. Remove old DrawConfigureTab() method
3. Remove old DrawValidateTab() method
4. Remove old profile loading methods (now in VRProfileManager)
5. Remove old profile caching methods (now in VRProfileManager)
6. Remove profile field declarations (now in VRProfileManager)

**Keep:**
- DrawSequenceTab() and related code (until VRSetupTab_Sequence is created)
- DrawRuntimeMonitorTab() and related code (until VRSetupTab_RuntimeMonitor is created)
- Utility methods used by multiple tabs

## Remaining Work

### Priority 1: Create Runtime Monitor Tab

File: `VRSetupTab_RuntimeMonitor.cs`

Extract DrawRuntimeMonitorTab() (~295 lines) into:
```csharp
public class VRSetupTab_RuntimeMonitor : VRSetupTabBase
{
    // State management
    private RuntimeMonitorSettings settings;
    private Vector2 scrollPos;
    
    public VRSetupTab_RuntimeMonitor(VRInteractionSetupWindow window) : base(window)
    {
    }
    
    public override void DrawTab()
    {
        // Implementation from DrawRuntimeMonitorTab()
    }
}
```

### Priority 2: Create Sequence Tab (Largest and Most Complex)

File: `VRSetupTab_Sequence.cs`

This is the biggest refactoring (~1,176 lines). Consider breaking into sub-components:

```csharp
public class VRSetupTab_Sequence : VRSetupTabBase
{
    // Asset management
    private TrainingSequenceAsset currentTrainingAsset;
    private TrainingProgram currentProgram;
    
    // UI state
    private Vector2 treeViewScrollPos;
    private Vector2 detailsPanelScrollPos;
    private object selectedHierarchyItem;
    private string selectedItemType;
    
    // Sub-components (consider further modularization)
    private SequenceTreeViewRenderer treeViewRenderer;
    private SequenceDetailsRenderer detailsRenderer;
    private SequenceAssetManager assetManager;
    
    public override void DrawTab()
    {
        DrawAssetSelectionBar();
        DrawTwoPanelLayout();
    }
    
    private void DrawTwoPanelLayout()
    {
        // Two-panel splitter logic
        // Left: Tree view
        // Right: Properties panel
    }
}
```

**Consider Further Modularization:**
- `SequenceTreeViewRenderer.cs` - Handles tree view rendering
- `SequenceDetailsRenderer.cs` - Handles property panel rendering
- `SequenceAssetManager.cs` - Handles asset loading/saving

## Benefits of Modular Approach

### 1. Easier Maintenance
- Changes to Setup tab don't affect Validate tab
- Each tab file is manageable size (< 400 lines)

### 2. Better Code Reuse
- VRProfileManager eliminates duplication
- VRSetupTabBase provides shared infrastructure
- Helper methods in Configure tab reduce repetition

### 3. Clearer Responsibilities
- VRInteractionSetupWindow: Coordination
- VRProfileManager: Profile operations
- Tab classes: UI rendering and state management

### 4. Easier Testing
- Each tab can be tested independently
- Profile manager can be mocked for testing
- Reduced coupling between components

### 5. Easier Onboarding
- New developers can understand one tab at a time
- Clear file structure shows system organization
- Less cognitive load per file

## Migration Strategy

### Phase 1: Foundation (✓ COMPLETED)
- [x] Create VRSetupTabBase
- [x] Create VRProfileManager
- [x] Extract simple tabs (Setup, Configure, Validate)

### Phase 2: Integration (IN PROGRESS)
- [ ] Update VRInteractionSetupWindow to use new tabs
- [ ] Test each tab thoroughly
- [ ] Keep old code commented out as backup

### Phase 3: Remaining Tabs
- [ ] Create VRSetupTab_RuntimeMonitor
- [ ] Create VRSetupTab_Sequence (with sub-components)
- [ ] Test integration

### Phase 4: Cleanup
- [ ] Remove old code from VRInteractionSetupWindow
- [ ] Verify all functionality works
- [ ] Update documentation
- [ ] Final testing pass

## Testing Checklist

After integration, verify:

- [ ] Setup tab: Scene scanning works
- [ ] Setup tab: Component application works
- [ ] Setup tab: Layer mask dropdowns work
- [ ] Configure tab: Profile selection works
- [ ] Configure tab: Profile creation works
- [ ] Configure tab: All profile types load correctly
- [ ] Validate tab: Validation runs
- [ ] Validate tab: Issues display correctly
- [ ] All tabs: Play mode transitions work
- [ ] All tabs: Window resizing works
- [ ] All tabs: Styles render correctly

## Troubleshooting

### Issue: "Type not found" errors
**Solution**: Ensure all new files are in the Editor folder and have `#if UNITY_EDITOR` directives

### Issue: Profiles not loading
**Solution**: Check VRProfileManager initialization in OnEnable()

### Issue: Tabs not rendering
**Solution**: Verify tab instances are created before DrawTab() is called

### Issue: State not persisting
**Solution**: Ensure OnEnable/OnDisable are called on tabs

## Future Enhancements

### Potential Further Refactoring
1. **Sequence Sub-Components**: Break Sequence tab into tree view and details renderers
2. **Shared Utilities**: Extract common UI patterns (e.g., collapsible sections)
3. **Data Models**: Separate data from rendering logic
4. **Command Pattern**: Implement undo/redo for sequence editing

### Extension Points
- Custom tabs can inherit from VRSetupTabBase
- Profile manager can support custom profile types
- Tab registration system for plugins

## Conclusion

This modular refactoring transforms a monolithic 3,385-line file into manageable, focused components while maintaining all existing functionality. The approach prioritizes:

1. **Safety**: Keep old code until new code is verified
2. **Incremental**: Migrate one tab at a time
3. **Testing**: Verify each component thoroughly
4. **Documentation**: Clear guidance for future work

---

**Document Version**: 1.0  
**Date**: December 2024  
**Author**: VR Training Kit Refactoring Team

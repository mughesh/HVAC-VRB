# Plan: Add Drag-and-Drop Reordering to Sequence Tree Tab

## Overview
Add Unity-style drag-and-drop reordering (like ScriptableObject inspector) to the VRInteractionSetupWindow Sequence Tree tab for **all levels**: Modules, Task Groups, and Steps.

---

## Approach: Unity ReorderableList Style

Using Unity's built-in `UnityEditorInternal.ReorderableList` pattern:
- Drag handles (≡) on the left of each item
- Smooth drag-and-drop reordering
- Visual feedback during drag
- Familiar Unity editor experience

---

## Reliability Assessment: **Very High**

| Aspect | Assessment |
|--------|------------|
| **Safety** | Uses standard List<T> swaps - no data corruption risk |
| **Persistence** | Auto-save already built-in |
| **Side Effects** | None - only changes list order |
| **Runtime Impact** | Intended - sequences execute in new order |

---

## Implementation Strategy: Iterative Phases

We'll implement in 3 phases, testing after each:

### Phase 1: Steps Reordering (Simplest)
### Phase 2: Task Groups Reordering
### Phase 3: Modules Reordering

---

## File to Modify
`Assets/VRTrainingKit/Scripts/Editor/Windows/VRInteractionSetupWindow.cs`

---

## Phase 1: Steps Reordering

### 1.1 Add ReorderableList Fields
Location: Near top of class with other fields (~line 36)

```csharp
using UnityEditorInternal;

// Add field
private Dictionary<TaskGroup, ReorderableList> stepReorderableLists = new Dictionary<TaskGroup, ReorderableList>();
```

### 1.2 Create Helper Method to Get/Create ReorderableList
Location: After existing helper methods (~line 3300)

```csharp
private ReorderableList GetStepReorderableList(TaskGroup taskGroup)
{
    if (!stepReorderableLists.ContainsKey(taskGroup) || stepReorderableLists[taskGroup] == null)
    {
        var list = new ReorderableList(taskGroup.steps, typeof(InteractionStep), true, false, false, false);

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var step = taskGroup.steps[index];
            string icon = GetStepTypeIcon(step.type);
            string status = step.IsValid() ? "✅" : "⚠️";
            EditorGUI.LabelField(rect, $"{icon} {step.stepName} {status}");
        };

        list.onReorderCallback = (ReorderableList l) =>
        {
            AutoSaveCurrentAsset();
        };

        list.elementHeight = EditorGUIUtility.singleLineHeight + 2;

        stepReorderableLists[taskGroup] = list;
    }
    return stepReorderableLists[taskGroup];
}
```

### 1.3 Modify DrawTaskGroupTreeItem to Use ReorderableList for Steps
Location: DrawTaskGroupTreeItem() (~line 1705)

Replace the step iteration loop with ReorderableList drawing when expanded.

### 1.4 Clear Cache on Asset Change
When loading a new asset, clear the cached ReorderableLists.

**TEST POINT 1**: Test step reordering within task groups

---

## Phase 2: Task Groups Reordering

### 2.1 Add ReorderableList Field
```csharp
private Dictionary<TrainingModule, ReorderableList> taskGroupReorderableLists = new Dictionary<TrainingModule, ReorderableList>();
```

### 2.2 Create Helper Method
```csharp
private ReorderableList GetTaskGroupReorderableList(TrainingModule module)
{
    // Similar pattern to steps
}
```

### 2.3 Modify DrawModuleTreeItem to Use ReorderableList

**TEST POINT 2**: Test task group reordering within modules

---

## Phase 3: Modules Reordering

### 3.1 Add ReorderableList Field
```csharp
private ReorderableList moduleReorderableList;
```

### 3.2 Create/Update in DrawTreeViewContent
```csharp
private ReorderableList GetModuleReorderableList()
{
    // Similar pattern
}
```

**TEST POINT 3**: Test module reordering at program level

---

## Alternative Simpler Approach: Up/Down Buttons

If ReorderableList integration proves complex with the existing tree structure, we can fall back to simple ↑↓ buttons:

```
📁 Task Group 1  ↑ ↓ ➕ ❌
  ✋ Step A      ↑ ↓ ❌
  🔗 Step B      ↑ ↓ ❌
```

This is:
- Faster to implement (~30 min)
- 100% reliable
- Less elegant but fully functional

---

## Key Integration Points

### Current Tree Drawing Methods:
- `DrawModuleTreeItem()` - line ~1646
- `DrawTaskGroupTreeItem()` - line ~1705
- `DrawStepTreeItem()` - line ~1762

### Current Add/Delete Methods (for reference):
- `DeleteModule()` - line ~2451
- `DeleteTaskGroup()` - line ~2465
- `DeleteStep()` - line ~2479

### Auto-Save:
- `AutoSaveCurrentAsset()` - already handles persistence

---

## Testing Checklist

### Phase 1 (Steps):
- [ ] Drag step up within task group
- [ ] Drag step down within task group
- [ ] Drag step to middle position
- [ ] Changes persist after window close
- [ ] Changes persist after Unity restart
- [ ] Selection still works after reorder

### Phase 2 (Task Groups):
- [ ] Same tests for task groups within modules

### Phase 3 (Modules):
- [ ] Same tests for modules within program

---

## Implementation Consideration

The current tree view uses custom rendering with `EditorGUI.indentLevel` and manual selection handling. Integrating `ReorderableList` requires adapting to its drawing callbacks.

**Recommended Strategy**: Start with Phase 1 (Steps only) using ReorderableList. If integration proves too complex with the existing tree structure, we have a **fallback option** of simple ↑↓ buttons that integrate perfectly with the current code.

---

## Detailed Phase 1 Implementation

### Step 1.1: Add Import and Fields
Location: Top of VRInteractionSetupWindow.cs (~line 10)

```csharp
using UnityEditorInternal;
```

Location: After existing fields (~line 50)
```csharp
// Reorderable list caches
private Dictionary<TaskGroup, ReorderableList> stepReorderableLists =
    new Dictionary<TaskGroup, ReorderableList>();
```

### Step 1.2: Add Cache Clear Method
Location: Where asset is loaded/changed (~line 270)

```csharp
private void ClearReorderableListCaches()
{
    stepReorderableLists.Clear();
}
```
Call this in `LoadAsset()` or when `currentTrainingAsset` changes.

### Step 1.3: Add ReorderableList Creator
Location: Near end of file (~line 3320)

```csharp
private ReorderableList GetOrCreateStepReorderableList(TaskGroup taskGroup)
{
    if (taskGroup?.steps == null) return null;

    if (!stepReorderableLists.TryGetValue(taskGroup, out var list) || list == null)
    {
        list = new ReorderableList(
            taskGroup.steps,
            typeof(InteractionStep),
            true,   // draggable
            false,  // displayHeader
            false,  // displayAddButton
            false   // displayRemoveButton
        );

        // Draw each step element
        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index >= taskGroup.steps.Count) return;
            var step = taskGroup.steps[index];

            string statusIcon = step.IsValid() ? "✅" : "⚠️";
            string typeIcon = GetStepTypeIcon(step.type);

            // Leave space for drag handle (built-in)
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 30, rect.height);
            Rect deleteRect = new Rect(rect.x + rect.width - 25, rect.y, 25, rect.height);

            // Draw step info
            EditorGUI.LabelField(labelRect, $"{statusIcon} {typeIcon} {step.stepName}");

            // Delete button
            if (GUI.Button(deleteRect, "❌"))
            {
                if (EditorUtility.DisplayDialog("Delete Step",
                    $"Delete step '{step.stepName}'?", "Delete", "Cancel"))
                {
                    taskGroup.steps.RemoveAt(index);
                    AutoSaveCurrentAsset();
                }
            }
        };

        // Handle selection
        list.onSelectCallback = (ReorderableList l) =>
        {
            if (l.index >= 0 && l.index < taskGroup.steps.Count)
            {
                SelectItem(taskGroup.steps[l.index], "step");
            }
        };

        // Handle reorder - auto-save
        list.onReorderCallback = (ReorderableList l) =>
        {
            AutoSaveCurrentAsset();
        };

        list.elementHeight = EditorGUIUtility.singleLineHeight + 4;

        stepReorderableLists[taskGroup] = list;
    }

    return list;
}
```

### Step 1.4: Modify DrawTaskGroupTreeItem
Location: Line ~1747-1754

Replace:
```csharp
// Draw steps
if (taskGroup.isExpanded && taskGroup.steps != null)
{
    for (int stepIndex = 0; stepIndex < taskGroup.steps.Count; stepIndex++)
    {
        DrawStepTreeItem(taskGroup.steps[stepIndex], taskGroup, stepIndex);
    }
}
```

With:
```csharp
// Draw steps with reorderable list
if (taskGroup.isExpanded && taskGroup.steps != null)
{
    EditorGUI.indentLevel++;
    var stepList = GetOrCreateStepReorderableList(taskGroup);
    if (stepList != null)
    {
        stepList.DoLayoutList();
    }
    EditorGUI.indentLevel--;
}
```

### TEST POINT 1
After Phase 1, test:
- [ ] Steps show with drag handles
- [ ] Drag reordering works
- [ ] Selection works on click
- [ ] Delete button works
- [ ] Changes persist

---

## Fallback: Simple ↑↓ Buttons

If ReorderableList integration is problematic, add buttons instead:

### In DrawStepTreeItem (~line 1782):
```csharp
// Move buttons
GUI.enabled = stepIndex > 0;
if (GUILayout.Button("↑", GUILayout.Width(20)))
{
    SwapSteps(parentTaskGroup, stepIndex, stepIndex - 1);
}
GUI.enabled = stepIndex < parentTaskGroup.steps.Count - 1;
if (GUILayout.Button("↓", GUILayout.Width(20)))
{
    SwapSteps(parentTaskGroup, stepIndex, stepIndex + 1);
}
GUI.enabled = true;

// Existing delete button...
```

### Add helper method:
```csharp
private void SwapSteps(TaskGroup taskGroup, int indexA, int indexB)
{
    var temp = taskGroup.steps[indexA];
    taskGroup.steps[indexA] = taskGroup.steps[indexB];
    taskGroup.steps[indexB] = temp;
    AutoSaveCurrentAsset();
}
```

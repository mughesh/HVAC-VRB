# Runtime Monitor Tab - Visibility Control

## Quick Setup Guide

The Runtime Monitor tab can be hidden/shown using a simple GameObject component.

---

## ✅ How to ENABLE Runtime Monitor Tab (Show the tab)

### Step 1: Add the Component
1. In your Unity scene, select **any GameObject** (or create a new empty GameObject)
2. In the Inspector, click **Add Component**
3. Search for `RuntimeMonitorSettings`
4. Add the component

### Step 2: Enable the Checkbox
1. In the `RuntimeMonitorSettings` component inspector
2. Check the box: ☑ **Show Runtime Monitor Tab**

### Result:
✅ The "Runtime Monitor" tab will now appear in: **Sequence Builder → Setup Assistant**

---

## ❌ How to DISABLE Runtime Monitor Tab (Hide the tab)

Choose **ONE** of these methods:

### Method 1: Uncheck the Checkbox (Temporary)
1. Find the GameObject with `RuntimeMonitorSettings` component
2. In the Inspector, **uncheck**: ☐ Show Runtime Monitor Tab

### Method 2: Disable the Component (Temporary)
1. Find the GameObject with `RuntimeMonitorSettings` component
2. **Uncheck the component** itself (checkbox next to component name)

### Method 3: Remove the Component (Permanent)
1. Find the GameObject with `RuntimeMonitorSettings` component
2. Right-click on the component header
3. Select **Remove Component**

### Method 4: Delete the GameObject (Permanent)
1. Find the GameObject with `RuntimeMonitorSettings` component
2. Delete the entire GameObject from the hierarchy

### Result:
❌ The "Runtime Monitor" tab will be **hidden** from the Setup Assistant window

---

## 📍 Recommended Setup

**For Development (Now):**
```
GameObject: "DevelopmentSettings" (or any name)
├── RuntimeMonitorSettings
    └── ☐ Show Runtime Monitor Tab = UNCHECKED
```
**Result:** Tab is hidden until you're ready to show it

**For Production (Later):**
```
Option 1: Remove the RuntimeMonitorSettings component entirely
Option 2: Check the checkbox to show the tab
```

---

## 🔧 Technical Details

**Files Involved:**
- `RuntimeMonitorSettings.cs` - Controls visibility (RuntimeMonitorSettings.cs)
- `VRInteractionSetupWindow.cs` - Checks settings before showing tab (VRInteractionSetupWindow.cs:620-630, 2960-2964)

**How it Works:**
- The Setup Assistant window checks for `RuntimeMonitorSettings` in the scene
- If found and enabled → Tab is visible
- If not found or disabled → Tab is hidden
- All Runtime Monitor logic remains intact even when hidden

**When to Remove (Future):**
When you want Runtime Monitor always visible:
1. Delete `RuntimeMonitorSettings.cs` script file
2. Remove the conditional check in `VRInteractionSetupWindow.cs` (lines 619-630)
3. Revert tab button to simple version (remove the `if (IsRuntimeMonitorTabEnabled())` wrapper)

---

## ⚙️ Component Settings Explained

```csharp
[Show Runtime Monitor Tab]  ← Main toggle
  ☑ Checked = Tab visible
  ☐ Unchecked = Tab hidden

[Info]  ← Read-only description
  Explains how the component works
```

---

## 💡 Tips

1. **Best Practice:** Create a GameObject named "RuntimeMonitorSettings" so it's easy to find
2. **Quick Toggle:** Just check/uncheck the checkbox to show/hide the tab instantly
3. **Per-Scene:** Each scene needs its own `RuntimeMonitorSettings` if you want the tab visible in that scene
4. **Prefab:** You can create a prefab with this component and add it to scenes as needed

---

## 🚀 Example Workflow

**Week 1-3 (Development):**
- Add `RuntimeMonitorSettings` to scene
- Keep checkbox **UNCHECKED** ☐
- Tab stays hidden from team

**Week 4 (Testing):**
- Find `RuntimeMonitorSettings` GameObject
- **CHECK** the checkbox ☑
- Tab appears for testing

**Week 5 (Release):**
- Either keep the checkbox checked
- Or remove the component/GameObject entirely
- Or delete `RuntimeMonitorSettings.cs` script for permanent visibility

---

## 📝 Quick Reference

| Action | Method | Reversible? |
|--------|--------|-------------|
| Hide tab temporarily | Uncheck checkbox | ✅ Yes |
| Hide tab temporarily | Disable component | ✅ Yes |
| Hide tab permanently | Remove component | ✅ Yes (re-add) |
| Show tab | Add component + check box | ✅ Yes |
| Always show tab | Delete settings script | ❌ No (need code changes) |

---

**That's it!** The Runtime Monitor tab functionality is complete and can be controlled with a simple checkbox.

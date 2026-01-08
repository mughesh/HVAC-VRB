# Package Export - Quick Reference Card

## Simple Export (2 Minutes)

**When to use:** Internal sharing, team collaboration, open-source

```
┌─────────────────────────────────────────────────────┐
│  In Unity Editor:                                   │
│                                                     │
│  1. Right-click Assets/VRTrainingKit/ folder        │
│  2. Select "Export Package..."                      │
│  3. Verify all files checked                        │
│  4. Click "Export..."                               │
│  5. Save as: VRTrainingKit.unitypackage             │
│                                                     │
│  ✅ Done! Share the .unitypackage file             │
└─────────────────────────────────────────────────────┘
```

**Result:**
- File size: ~5-20 MB
- Protection: ❌ None (full source visible)
- Users can: Modify everything

---

## Protected Export (15 Minutes)

**When to use:** Commercial distribution, Asset Store, IP protection

```
┌─────────────────────────────────────────────────────┐
│  STEP 1: Get DLLs                                   │
│  ───────────────                                    │
│  Navigate to:                                       │
│  E:\Unity Projects\HVAC-VRB\Library\ScriptAssemblies│
│                                                     │
│  Copy these files:                                  │
│  • VRTrainingKit.Runtime.dll                        │
│  • VRTrainingKit.Runtime.pdb                        │
│  • VRTrainingKit.Editor.dll                         │
│  • VRTrainingKit.Editor.pdb                         │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  STEP 2: Create Distribution Folder                 │
│  ───────────────────────────────                    │
│  On Desktop, create:                                │
│  VRTrainingKit_Distribution/                        │
│    VRTrainingKit/                                   │
│      Plugins/         (paste DLLs here)             │
│      Scripts/                                       │
│        Profiles/      (copy profile .cs files)      │
│      Resources/       (copy .asset files)           │
│      Documentation/   (copy guides)                 │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  STEP 3: Test & Export                              │
│  ──────────────────                                 │
│  1. Create NEW Unity project                        │
│  2. Copy VRTrainingKit/ folder into Assets/         │
│  3. Unity imports automatically                     │
│  4. Test: Sequence Builder menu should appear       │
│  5. Right-click VRTrainingKit/ > Export Package     │
│  6. Save as: VRTrainingKit_v1.0_Protected.unitypackage │
└─────────────────────────────────────────────────────┘
```

**Result:**
- File size: ~1-10 MB
- Protection: ✅ Strong (DLL)
- Users can: Extend profiles, use functionality

---

## File Locations Cheat Sheet

```
┌─────────────────────────────────────────────────────┐
│ YOUR PROJECT (Source):                              │
│ ───────────────────────                             │
│ E:\Unity Projects\HVAC-VRB\                         │
│   Assets/VRTrainingKit/          (your code)        │
│   Library/ScriptAssemblies/      (auto-compiled DLLs)│
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ DISTRIBUTION (What users get):                      │
│ ───────────────────────────────                     │
│ Desktop/VRTrainingKit_Distribution/                 │
│   VRTrainingKit/                                    │
│     Plugins/              (DLLs - protected)        │
│     Scripts/Profiles/     (source - extensible)     │
│     Resources/            (assets)                  │
│     Documentation/        (guides)                  │
└─────────────────────────────────────────────────────┘
```

---

## What Gets Protected?

### DLL (Protected) ✅
- Core Services (InteractionSetupService, VRFrameworkDetector)
- Runtime Controllers (AutoHandsScrewController, KnobController)
- Sequence System (ModularTrainingSequenceController)
- Editor Windows (VRInteractionSetupWindow, VRFrameworkValidatorWindow)

### Source Code (Visible) 📄
- All Profiles (GrabProfile, KnobProfile, ScrewProfile, etc.)
- Profile base classes (InteractionProfile, TurnByCountProfile)
- Documentation and guides

---

## Common Commands

**Find DLLs automatically compiled by Unity:**
```
File Explorer:
E:\Unity Projects\HVAC-VRB\Library\ScriptAssemblies\
```

**Force Unity to recompile (if needed):**
```
Unity Menu:
Assets > Reimport All
```

**Export current project as package:**
```
Unity Menu:
Right-click Assets/VRTrainingKit/ > Export Package...
```

---

## Version Naming Convention

```
✅ Good:
VRTrainingKit_v1.0.0.unitypackage
VRTrainingKit_v1.1.0_Protected.unitypackage
VRTrainingKit_v2.0.0_Beta.unitypackage

❌ Bad:
VRTrainingKit.unitypackage
VRTrainingKit_Final.unitypackage
package.unitypackage
```

**Version format:** `Major.Minor.Patch`
- v1.0.0 → Initial release
- v1.1.0 → New features added
- v1.0.1 → Bug fixes only
- v2.0.0 → Breaking changes

---

## Troubleshooting Quick Fixes

**Problem:** Can't find DLL files
```
Solution: Unity hasn't compiled yet
→ Open Unity project
→ Assets > Reimport All
→ Check Library/ScriptAssemblies/ again
```

**Problem:** Menu doesn't appear after import
```
Solution: Unity needs refresh
→ Close and reopen Unity
→ Or: Assets > Refresh
```

**Problem:** Package size is huge (>50 MB)
```
Solution: Excluding unnecessary files
→ Uncheck TestSetup/ folder
→ Uncheck Examples/ folder
→ Only include essentials
```

---

## Distribution Checklist

Before sharing your package:

- [ ] Tested in fresh Unity project
- [ ] No errors in Console
- [ ] Sequence Builder menu appears
- [ ] Profiles work correctly
- [ ] Documentation included
- [ ] Version number in filename
- [ ] README.md with quick start
- [ ] LICENSE.txt included

---

## Support Resources

**Full Guide:** `Documentation/PACKAGE_EXPORT_GUIDE.md`
**DLL Explanation:** `Plans/purring-enchanting-knuth.md`
**Architecture:** `Documentation/ARCHITECTURE.md`

---

## Quick Comparison

| Aspect | Simple Export | Protected Export |
|--------|---------------|------------------|
| **Time** | 2 min | 15 min |
| **Protection** | None | Strong |
| **File Size** | 5-20 MB | 1-10 MB |
| **Use Case** | Internal | Commercial |
| **User Access** | Full source | DLL only |

---

**Last Updated:** 2025-12-31
**VR Training Kit Version:** 1.0+

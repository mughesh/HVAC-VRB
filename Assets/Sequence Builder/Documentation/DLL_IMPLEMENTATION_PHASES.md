# DLL Implementation - Phased Plan

## Overview

This document outlines the step-by-step phases for implementing Assembly Definitions and DLL protection for VR Training Kit.

**Goal:** Organize code with .asmdef files to enable DLL distribution while keeping profiles as source code.

**Total Time:** ~1-2 hours (spread across testing phases)

---

## Phase 0: Pre-Implementation Scan ✋ STOP HERE

**Goal:** Understand current project structure

**What happens:** Claude scans your project to map out files

**Who does it:** Claude (automated)

**Time:** 2 minutes

**What you do:** Review the scan results and confirm structure looks correct

**What to verify:**
- [ ] File structure report generated
- [ ] Core, Editor, and Profiles folders identified
- [ ] No major issues detected

**Stop here for confirmation before proceeding**

---

## Phase 1: Create Assembly Definition Files ✋ STOP HERE

**Goal:** Create .asmdef files to organize code into assemblies

**What happens:** Claude creates 3 .asmdef files in appropriate folders

**Who does it:** Claude (automated)

**Files created:**
1. `Assets/VRTrainingKit/Scripts/Core/VRTrainingKit.Runtime.asmdef`
2. `Assets/VRTrainingKit/Scripts/Editor/VRTrainingKit.Editor.asmdef`
3. `Assets/VRTrainingKit/Scripts/Profiles/VRTrainingKit.Profiles.asmdef`

**Time:** 1 minute

**What you do:**
- Let Claude create the files
- Unity will auto-recompile (wait for it to finish)
- Check Unity Console for errors

**What to verify:**
- [ ] 3 .asmdef files created in correct locations
- [ ] Unity recompiled successfully (no spinning wheel)
- [ ] Check Console window - should be EMPTY (no errors)

**If errors appear:** Don't panic - we'll fix them in Phase 2

**Stop here for confirmation before proceeding**

---

## Phase 2: Fix Compilation Errors ✋ STOP HERE

**Goal:** Resolve any assembly reference issues

**What happens:** Unity may show errors about missing assembly references

**Who does it:** Claude + You (collaborative)

**Common errors:**
- "The type or namespace 'X' could not be found"
- "Assembly 'Y' will not be loaded due to errors"
- Missing references between assemblies

**Time:** 5-30 minutes (depends on errors)

**What you do:**
1. Check Unity Console for errors
2. Share error messages with Claude
3. Claude updates .asmdef files with missing references
4. You verify errors are gone

**What to verify:**
- [ ] Unity Console shows 0 errors
- [ ] All scripts compile successfully
- [ ] No yellow warnings about assemblies

**Stop here for confirmation before proceeding**

---

## Phase 3: Verify DLL Generation ✋ STOP HERE

**Goal:** Confirm Unity is creating separate DLL files

**What happens:** Check that Unity created custom DLL files

**Who does it:** You (with Claude's guidance)

**Time:** 2 minutes

**What you do:**
1. In File Explorer (not Unity), navigate to:
   ```
   E:\Unity Projects\HVAC -VRBuilder\HVAC-VRB\Library\ScriptAssemblies\
   ```

2. Look for these files:
   - `VRTrainingKit.Runtime.dll`
   - `VRTrainingKit.Runtime.pdb`
   - `VRTrainingKit.Editor.dll`
   - `VRTrainingKit.Editor.pdb`
   - `VRTrainingKit.Profiles.dll`
   - `VRTrainingKit.Profiles.pdb`

3. Screenshot or list what you see

**What to verify:**
- [ ] All 6 DLL files exist
- [ ] File sizes are reasonable (not 0 KB)
- [ ] Timestamps are recent (just compiled)

**If files missing:** Claude will help diagnose and fix

**Stop here for confirmation before proceeding**

---

## Phase 4: Test Functionality ✋ STOP HERE

**Goal:** Ensure everything still works after adding .asmdef files

**What happens:** Test all major features

**Who does it:** You (with test checklist)

**Time:** 10-15 minutes

**What you do:**

**Test Checklist:**

1. **Menu System:**
   - [ ] Sequence Builder menu appears
   - [ ] Setup Assistant opens
   - [ ] Framework Validator opens

2. **Editor Window:**
   - [ ] Setup tab works (scan scene)
   - [ ] Configure tab shows profiles
   - [ ] Sequence tab loads assets
   - [ ] Validate tab works

3. **Play Mode Test:**
   - [ ] Create test object, tag it "grab"
   - [ ] Apply GrabProfile to it
   - [ ] Enter Play Mode
   - [ ] Object has correct components
   - [ ] No errors in Console

4. **Profile System:**
   - [ ] Can create new profile assets (Right-click > Create > Sequence Builder)
   - [ ] Profiles apply to objects correctly
   - [ ] Custom profile creation works (if you have custom ones)

**What to verify:**
- [ ] All tests pass
- [ ] No functionality broken
- [ ] Performance feels the same

**If issues found:** Claude will help fix them

**Stop here for confirmation before proceeding**

---

## Phase 5: Mark Classes as Internal (Optional) ✋ STOP HERE

**Goal:** Hide implementation details from users (even with source code)

**What happens:** Claude marks internal classes/methods as `internal` instead of `public`

**Who does it:** Claude (if you want this)

**Time:** 15-30 minutes

**What this does:**
- Classes marked `internal` are only accessible within their assembly
- Users can still see source code but can't directly use internal classes
- Cleaner public API

**What you do:**
- Decide if you want this step
- Review which classes should be public vs internal
- Claude makes the changes
- Test that everything still works

**What to verify:**
- [ ] Public API classes still accessible
- [ ] Internal classes not visible to external code
- [ ] No compilation errors

**This phase is OPTIONAL** - you can skip it and do it later

**Stop here for confirmation before proceeding**

---

## Phase 6: Documentation Update ✋ STOP HERE

**Goal:** Update docs to reflect assembly structure

**What happens:** Claude updates documentation with assembly info

**Who does it:** Claude (automated)

**Time:** 5 minutes

**What you do:** Review updated documentation

**Files updated:**
- `CLAUDE.md` - Add assembly definitions section
- `ARCHITECTURE.md` - Update with assembly structure
- `API_REFERENCE.md` - Document public API

**What to verify:**
- [ ] Documentation mentions assemblies
- [ ] Public API clearly documented
- [ ] Extension points explained

**Stop here for confirmation**

---

## Phase 7: Create Test Distribution Package (Practice Run) ✋ STOP HERE

**Goal:** Practice creating a protected package (without sharing it)

**What happens:** You follow the export guide to create a test package

**Who does it:** You (following PACKAGE_EXPORT_GUIDE.md)

**Time:** 20 minutes

**What you do:**
1. Copy DLLs from Library/ScriptAssemblies/
2. Create distribution folder structure
3. Import into test Unity project
4. Export as .unitypackage
5. Test the package

**What to verify:**
- [ ] Test package imports successfully
- [ ] DLLs load correctly
- [ ] Profiles visible as source
- [ ] Menu works
- [ ] No errors

**This is just practice** - you won't share this yet

**Stop here - Phase 1 Complete! 🎉**

---

## Future: Phase 8 (When Ready to Distribute)

**Goal:** Create and share actual distribution package

**When:** Only when you're ready to share with others

**What happens:** Same as Phase 7 but with proper versioning and release

**Not implemented now** - just keeping development structure

---

## Summary: What Each Phase Accomplishes

| Phase | Goal | Time | Your Involvement |
|-------|------|------|------------------|
| 0 | Scan structure | 2 min | Review results |
| 1 | Create .asmdef | 1 min | Watch Unity compile |
| 2 | Fix errors | 5-30 min | Share errors, test fixes |
| 3 | Verify DLLs | 2 min | Check files exist |
| 4 | Test features | 10-15 min | Run test checklist |
| 5 | Mark internal | 15-30 min | Optional - review changes |
| 6 | Update docs | 5 min | Review updates |
| 7 | Test package | 20 min | Follow guide |

**Total Active Time:** ~1-2 hours (can spread over multiple days)

---

## Important Notes

### During Development (After Phase 4):
- ✅ Continue editing .cs files normally
- ✅ Unity auto-compiles to DLLs
- ✅ Debug with breakpoints as usual
- ✅ No manual DLL compilation needed
- ✅ Everything works the same as before

### For Distribution (Phase 7+):
- Only create packages when releasing
- Copy DLLs from Library/ScriptAssemblies/
- Follow PACKAGE_EXPORT_GUIDE.md
- Test before sharing

---

## Rollback Plan (If Something Goes Wrong)

If at any phase you want to undo:

1. Delete all .asmdef files:
   - Core/VRTrainingKit.Runtime.asmdef
   - Editor/VRTrainingKit.Editor.asmdef
   - Profiles/VRTrainingKit.Profiles.asmdef

2. Unity recompiles back to default Assembly-CSharp.dll

3. Everything returns to original state

**No code is modified** - only .asmdef files added/removed

---

## Current Status

**Phase:** Not started
**Last Updated:** 2025-12-31
**Ready to begin:** Phase 0

---

## Next Steps

1. Review this phased plan
2. Confirm you're ready to start
3. Begin Phase 0 (project scan)
4. Stop and confirm after each phase

Let's go! 🚀

# VR Training Kit - Package Export Guide

**Complete step-by-step guide for sharing VR Training Kit with others**

---

## Table of Contents
1. [Simple Export (Current State - No DLLs)](#simple-export-current-state)
2. [Protected Export (With DLLs - Advanced)](#protected-export-with-dlls)
3. [What Users Receive](#what-users-receive)
4. [Troubleshooting](#troubleshooting)

---

# Simple Export (Current State)

**Use this method for:**
- Quick sharing with team members
- Internal testing
- Development collaboration
- When IP protection isn't critical

**Time required:** 2-3 minutes

---

## Step 1: Locate Your VRTrainingKit Folder

**In Unity Editor:**

1. Click the **Project** window (bottom panel)
2. Navigate to **Assets** folder
3. Find **VRTrainingKit** folder

**What you'll see:**
```
Project Window:
Assets/
  ├─ Scenes/
  ├─ TextMesh Pro/
  ├─ VRTrainingKit/        ← This is what you need
  │   ├─ Documentation/
  │   ├─ Editor/
  │   ├─ Resources/
  │   ├─ Scripts/
  │   └─ ...
  └─ XR/
```

**Visual Reference:**
```
┌─────────────────────────────────────┐
│  Project                        ⚙️  │
├─────────────────────────────────────┤
│  📁 Assets                          │
│    📁 Scenes                        │
│    📁 TextMesh Pro                  │
│  ▶ 📁 VRTrainingKit     ← RIGHT-CLICK THIS
│    📁 XR                            │
└─────────────────────────────────────┘
```

---

## Step 2: Right-Click and Export Package

**Actions:**

1. **Right-click** on `VRTrainingKit` folder
2. From context menu, select **"Export Package..."**

**What the menu looks like:**
```
┌─────────────────────────────┐
│  Show in Explorer           │
│  Open                       │
│  Delete                     │
│  Rename                     │
│ ──────────────────────────  │
│  Import New Asset...        │
│  Import Package          ▶  │
│ ▶ Export Package...        │ ← CLICK THIS
│  Open C# Project            │
│  Refresh                    │
│ ──────────────────────────  │
│  Reimport                   │
│  Reimport All               │
└─────────────────────────────┘
```

---

## Step 3: Review Export Package Window

**Unity will open the "Exporting package" window**

**What you'll see:**

```
┌────────────────────────────────────────────────┐
│  Exporting package                         ✕   │
├────────────────────────────────────────────────┤
│                                                │
│  Items to Export:                              │
│  ┌──────────────────────────────────────────┐ │
│  │ ☑ VRTrainingKit                          │ │
│  │   ☑ Documentation                        │ │
│  │     ☑ ARCHITECTURE.md                    │ │
│  │     ☑ SETUP_GUIDE.md                     │ │
│  │   ☑ Editor                               │ │
│  │     ☑ VRTrainingKitInit.cs               │ │
│  │     ☑ Windows                            │ │
│  │       ☑ VRInteractionSetupWindow.cs     │ │
│  │       ☑ VRFrameworkValidatorWindow.cs   │ │
│  │   ☑ Resources                            │ │
│  │     ☑ GrabProfile.asset                  │ │
│  │     ☑ KnobProfile.asset                  │ │
│  │     ☑ SnapProfile.asset                  │ │
│  │   ☑ Scripts                              │ │
│  │     ☑ Core                               │ │
│  │     ☑ Editor                             │ │
│  │     ☑ Profiles                           │ │
│  │     ☑ SequenceSystem                     │ │
│  │   (... and more ...)                     │ │
│  └──────────────────────────────────────────┘ │
│                                                │
│  ☐ include dependencies                       │
│  ☐ include package manifest                   │
│                                                │
│            [ Export... ]        [ Cancel ]     │
└────────────────────────────────────────────────┘
```

**What to check:**

✅ **All checkboxes should be checked** (Unity does this by default)
- This ensures all files are included
- Scroll through the list to see what's being exported

❌ **DON'T check "include dependencies"** unless you want to include Unity packages
- Leaving unchecked is fine for most cases

❌ **DON'T check "include package manifest"** (not needed for simple export)

---

## Step 4: Click Export and Choose Location

**Actions:**

1. Click **"Export..."** button (bottom right)
2. Windows File Explorer opens
3. Navigate to where you want to save the package
4. Enter filename: `VRTrainingKit.unitypackage`
5. Click **"Save"**

**Recommended save locations:**

```
Option 1: Desktop (easy to find)
C:\Users\YourName\Desktop\VRTrainingKit.unitypackage

Option 2: Google Drive (for sharing)
C:\Users\YourName\Google Drive\Packages\VRTrainingKit.unitypackage

Option 3: Project Exports folder
E:\Unity Projects\Exports\VRTrainingKit.unitypackage
```

**File Explorer window:**
```
┌──────────────────────────────────────────────┐
│  Save As                                  ✕  │
├──────────────────────────────────────────────┤
│  📁 Desktop                               ▼  │
├──────────────────────────────────────────────┤
│  File name: VRTrainingKit.unitypackage      │
│  Save as type: Unity Package (*.unitypackage)│
│                                              │
│              [ Save ]        [ Cancel ]      │
└──────────────────────────────────────────────┘
```

---

## Step 5: Wait for Export (Progress Bar)

**What happens:**

Unity shows a progress bar while creating the package:

```
┌────────────────────────────────────┐
│  Exporting Package                 │
├────────────────────────────────────┤
│  ████████████████░░░░░░  67%      │
│                                    │
│  Processing: Scripts/Core/         │
│  InteractionSetupService.cs        │
└────────────────────────────────────┘
```

**Time:** Usually 10-30 seconds depending on project size

---

## Step 6: Export Complete!

**What you get:**

A single file ready to share:
```
📦 VRTrainingKit.unitypackage
   Size: ~5-20 MB (varies based on content)
```

**This file contains:**
- ✅ All scripts (.cs files)
- ✅ All editor windows
- ✅ All profiles and controllers
- ✅ Documentation
- ✅ Default profile assets (.asset files)
- ✅ Folder structure preserved

**What to do with it:**
- Email to colleagues
- Upload to Google Drive / Dropbox
- Share via Slack / Discord
- Publish on GitHub (Releases section)
- Submit to Unity Asset Store

---

## Step 7: How Others Import Your Package

**What they do:**

1. Open their Unity project
2. Click `Assets > Import Package > Custom Package...`
3. Select your `VRTrainingKit.unitypackage` file
4. Click "Open"

**Import Package window appears:**
```
┌────────────────────────────────────────────┐
│  Importing package                     ✕   │
├────────────────────────────────────────────┤
│  Items to Import:                          │
│  ┌──────────────────────────────────────┐ │
│  │ ☑ VRTrainingKit                      │ │
│  │   ☑ Documentation                    │ │
│  │   ☑ Editor                           │ │
│  │   ☑ Resources                        │ │
│  │   ☑ Scripts                          │ │
│  │   (... all your files ...)           │ │
│  └──────────────────────────────────────┘ │
│                                            │
│            [ Import ]      [ Cancel ]      │
└────────────────────────────────────────────┘
```

5. They click **"Import"**
6. VRTrainingKit folder appears in their `Assets/` folder
7. They can now use Sequence Builder menu

---

# Protected Export (With DLLs)

**Use this method for:**
- Commercial distribution
- Asset Store submission
- IP protection
- Client delivery with licensing

**Time required:** 15-20 minutes (first time)

---

## Prerequisites

Before starting, ensure you have:
- ✅ Assembly Definition files (.asmdef) created
- ✅ Your project compiles without errors
- ✅ Tested that everything works

---

## Phase 1: Get Compiled DLLs from Unity

### Step 1A: Locate Unity's Compiled DLLs (Automatic Method)

**What Unity does automatically:**
Every time you save a .cs file, Unity compiles it to a DLL in the background.

**Where to find them:**

1. In File Explorer (NOT Unity Editor), navigate to:
   ```
   E:\Unity Projects\HVAC -VRBuilder\HVAC-VRB\Library\ScriptAssemblies\
   ```

2. Look for files named:
   ```
   VRTrainingKit.Runtime.dll
   VRTrainingKit.Runtime.pdb
   VRTrainingKit.Editor.dll
   VRTrainingKit.Editor.pdb
   VRTrainingKit.Profiles.dll
   VRTrainingKit.Profiles.pdb
   ```

**What you'll see in Windows Explorer:**
```
📁 Library
  └─ 📁 ScriptAssemblies
       ├─ Assembly-CSharp.dll
       ├─ Assembly-CSharp-Editor.dll
       ├─ 📄 VRTrainingKit.Runtime.dll     ← Copy this
       ├─ 📄 VRTrainingKit.Runtime.pdb     ← Copy this (debug symbols)
       ├─ 📄 VRTrainingKit.Editor.dll      ← Copy this
       ├─ 📄 VRTrainingKit.Editor.pdb      ← Copy this
       ├─ 📄 VRTrainingKit.Profiles.dll    ← DON'T copy (keep as source)
       └─ (many other Unity DLLs...)
```

**Note:** If you don't see these files, you need to create .asmdef files first (see Prerequisites).

---

### Step 1B: Force Recompile (Optional - For Safety)

**If you want to ensure DLLs are 100% up-to-date:**

**Method 1: Reimport All (Recommended)**
1. In Unity Editor
2. Click `Assets > Reimport All`
3. Wait for Unity to recompile (2-5 minutes)
4. Check `Library/ScriptAssemblies/` again

**Method 2: Dummy Build (Alternative)**
1. Click `File > Build Settings`
2. Select **Windows** platform (or any platform)
3. Click **"Build"**
4. Choose temporary folder: `C:\Temp\DummyBuild\`
5. Click "Select Folder"
6. Wait for "Compiling scripts..." to finish
7. **You can close/cancel after compilation** - you don't need the actual build
8. DLLs in `Library/ScriptAssemblies/` are now guaranteed fresh

**Build Settings window:**
```
┌──────────────────────────────────────────┐
│  Build Settings                      ✕   │
├──────────────────────────────────────────┤
│  Platform:                               │
│  ☑ PC, Mac & Linux Standalone    [icon] │
│    iOS                            [icon] │
│    Android                        [icon] │
│    WebGL                          [icon] │
│                                          │
│  Target Platform: Windows                │
│  Architecture: x86_64                    │
│                                          │
│  [ Switch Platform ]   [ Build ]  ← Click│
└──────────────────────────────────────────┘
```

---

## Phase 2: Create Distribution Folder Structure

### Step 2: Create Clean Folder for Distribution

**In File Explorer, create this structure:**

1. Navigate to a convenient location (e.g., Desktop)
2. Create new folder: `VRTrainingKit_Distribution`
3. Inside, create this structure:

```
📁 C:\Users\YourName\Desktop\VRTrainingKit_Distribution\
  └─ 📁 VRTrainingKit\
       ├─ 📁 Plugins\           (create this)
       ├─ 📁 Scripts\           (create this)
       │    └─ 📁 Profiles\     (create this)
       ├─ 📁 Resources\         (create this)
       └─ 📁 Documentation\     (create this)
```

**How to create folders in Windows:**
- Right-click → New → Folder
- Name it exactly as shown above

---

### Step 3: Copy DLL Files to Plugins Folder

**From:** `E:\Unity Projects\HVAC -VRBuilder\HVAC-VRB\Library\ScriptAssemblies\`

**To:** `C:\Users\YourName\Desktop\VRTrainingKit_Distribution\VRTrainingKit\Plugins\`

**Files to copy:**
1. `VRTrainingKit.Runtime.dll`
2. `VRTrainingKit.Runtime.pdb`
3. `VRTrainingKit.Editor.dll`
4. `VRTrainingKit.Editor.pdb`

**Result:**
```
📁 VRTrainingKit_Distribution\VRTrainingKit\
  └─ 📁 Plugins\
       ├─ 📄 VRTrainingKit.Runtime.dll
       ├─ 📄 VRTrainingKit.Runtime.pdb
       ├─ 📄 VRTrainingKit.Editor.dll
       └─ 📄 VRTrainingKit.Editor.pdb
```

**Why include .pdb files?**
- They contain debugging symbols
- Users get meaningful error messages with line numbers
- They CANNOT see your source code from .pdb files
- Makes your package more professional

---

### Step 4: Copy Profile Source Code

**From:** `E:\Unity Projects\HVAC -VRBuilder\HVAC-VRB\Assets\VRTrainingKit\Scripts\Profiles\`

**To:** `C:\Users\YourName\Desktop\VRTrainingKit_Distribution\VRTrainingKit\Scripts\Profiles\`

**Copy these files/folders:**
```
📁 Profiles\
  ├─ 📁 Base\
  │    ├─ 📄 InteractionProfile.cs      ← Copy
  │    └─ 📄 TurnByCountProfile.cs      ← Copy
  ├─ 📁 Implementations\
  │    ├─ 📁 AutoHands\
  │    │    ├─ 📄 AutoHandsGrabProfile.cs        ← Copy
  │    │    ├─ 📄 AutoHandsKnobProfile.cs        ← Copy
  │    │    ├─ 📄 AutoHandsScrewProfile.cs       ← Copy
  │    │    ├─ 📄 AutoHandsSnapProfile.cs        ← Copy
  │    │    └─ (all AutoHands profiles...)       ← Copy
  │    ├─ 📄 GrabProfile.cs                       ← Copy
  │    ├─ 📄 KnobProfile.cs                       ← Copy
  │    ├─ 📄 SnapProfile.cs                       ← Copy
  │    ├─ 📄 ScrewProfile.cs                      ← Copy
  │    └─ (all XRI profiles...)                   ← Copy
```

**Why keep profiles as source?**
Users can:
- Read and understand how profiles work
- Create custom profiles by inheriting from `InteractionProfile`
- Extend your framework for their specific needs
- Example: Create `SliderProfile` for sliding doors

---

### Step 5: Copy Resources (Profile Assets)

**From:** `E:\Unity Projects\HVAC -VRBuilder\HVAC-VRB\Assets\VRTrainingKit\Resources\`

**To:** `C:\Users\YourName\Desktop\VRTrainingKit_Distribution\VRTrainingKit\Resources\`

**Files to copy:**
```
📁 Resources\
  ├─ 📄 GrabProfile.asset
  ├─ 📄 KnobProfile.asset
  ├─ 📄 SnapProfile.asset
  └─ (any other default .asset files)
```

**Why include these?**
- Pre-configured default profiles
- Users can use them immediately
- Good examples for creating custom profiles

---

### Step 6: Copy Documentation

**From:** `E:\Unity Projects\HVAC -VRBuilder\HVAC-VRB\Assets\VRTrainingKit\Documentation\`

**To:** `C:\Users\YourName\Desktop\VRTrainingKit_Distribution\VRTrainingKit\Documentation\`

**Files to copy:**
```
📁 Documentation\
  ├─ 📄 README.md
  ├─ 📄 ARCHITECTURE.md
  ├─ 📄 SETUP_GUIDE.md
  ├─ 📄 API_REFERENCE.md
  └─ (any guides you want to share)
```

**What NOT to copy:**
- Internal development notes
- TODO lists
- Private client information
- Experimental features documentation

---

### Step 7: Verify Distribution Folder

**Final structure should look like:**

```
📁 VRTrainingKit_Distribution\
  └─ 📁 VRTrainingKit\
       ├─ 📁 Plugins\
       │    ├─ VRTrainingKit.Runtime.dll
       │    ├─ VRTrainingKit.Runtime.pdb
       │    ├─ VRTrainingKit.Editor.dll
       │    └─ VRTrainingKit.Editor.pdb
       ├─ 📁 Scripts\
       │    └─ 📁 Profiles\
       │         ├─ 📁 Base\
       │         │    ├─ InteractionProfile.cs
       │         │    └─ TurnByCountProfile.cs
       │         └─ 📁 Implementations\
       │              ├─ 📁 AutoHands\
       │              │    └─ (all AutoHands profiles)
       │              ├─ GrabProfile.cs
       │              ├─ KnobProfile.cs
       │              └─ (all XRI profiles)
       ├─ 📁 Resources\
       │    ├─ GrabProfile.asset
       │    ├─ KnobProfile.asset
       │    └─ SnapProfile.asset
       └─ 📁 Documentation\
            ├─ README.md
            └─ (other docs)
```

**Total file count:** ~50-100 files
**Total size:** ~2-10 MB

---

## Phase 3: Create Unity Package from Distribution

### Step 8: Import Distribution Folder into Clean Unity Project

**Why do this?**
- Test that package works correctly
- Ensure folder structure is correct
- Verify DLLs load properly

**Steps:**

1. **Create new Unity project:**
   - Open Unity Hub
   - Click "New Project"
   - Choose "3D" template
   - Name: `VRTrainingKit_PackageTest`
   - Click "Create"

2. **Wait for Unity to initialize** (1-2 minutes)

3. **Copy your distribution folder into the project:**
   - In File Explorer, open:
     ```
     C:\Users\YourName\Desktop\VRTrainingKit_Distribution\
     ```
   - Copy the `VRTrainingKit\` folder
   - Paste into:
     ```
     [New Unity Project Path]\Assets\
     ```

4. **Unity auto-imports everything**

---

### Step 9: Verify DLLs Loaded Correctly

**In the test Unity project:**

1. Open Project window
2. Navigate to `Assets/VRTrainingKit/Plugins/`
3. You should see DLL files with Unity icons:

```
Project Window:
📁 Assets
  └─ 📁 VRTrainingKit
       ├─ 📁 Plugins
       │    ├─ 🔌 VRTrainingKit.Runtime.dll    ← Unity recognized it
       │    ├─ 📋 VRTrainingKit.Runtime.pdb
       │    ├─ 🔌 VRTrainingKit.Editor.dll     ← Unity recognized it
       │    └─ 📋 VRTrainingKit.Editor.pdb
       └─ 📁 Scripts
            └─ 📁 Profiles
                 └─ 📄 InteractionProfile.cs    ← Source code visible
```

4. **Test the menu:**
   - Click `Sequence Builder` menu (top menu bar)
   - Should show:
     - Setup Assistant
     - Framework Validator
     - Setup submenu

**If menu doesn't appear:**
- Check Unity Console for errors
- Verify DLLs are in `Plugins/` folder
- Check .pdb files are alongside .dll files

---

### Step 10: Export Unity Package

**Now create the distributable .unitypackage:**

1. In Project window, **right-click** `Assets/VRTrainingKit/` folder
2. Select **"Export Package..."**

3. **Export Package window appears:**
   ```
   ┌────────────────────────────────────────────┐
   │  Exporting package                     ✕   │
   ├────────────────────────────────────────────┤
   │  Items to Export:                          │
   │  ┌──────────────────────────────────────┐ │
   │  │ ☑ VRTrainingKit                      │ │
   │  │   ☑ Plugins                          │ │
   │  │     ☑ VRTrainingKit.Runtime.dll      │ │
   │  │     ☑ VRTrainingKit.Runtime.pdb      │ │
   │  │     ☑ VRTrainingKit.Editor.dll       │ │
   │  │     ☑ VRTrainingKit.Editor.pdb       │ │
   │  │   ☑ Scripts                          │ │
   │  │     ☑ Profiles                       │ │
   │  │       ☑ Base                         │ │
   │  │       ☑ Implementations              │ │
   │  │   ☑ Resources                        │ │
   │  │   ☑ Documentation                    │ │
   │  └──────────────────────────────────────┘ │
   │                                            │
   │            [ Export... ]      [ Cancel ]   │
   └────────────────────────────────────────────┘
   ```

4. **Verify all items are checked** ✅

5. Click **"Export..."**

6. **Save as:**
   ```
   Filename: VRTrainingKit_v1.0_Protected.unitypackage
   Location: Desktop (or your preferred location)
   ```

7. Click **"Save"**

---

### Step 11: Test the Protected Package

**Final verification:**

1. Create ANOTHER new Unity project (or use existing one)
2. Import your `VRTrainingKit_v1.0_Protected.unitypackage`
3. Verify:
   - ✅ DLLs in Plugins folder
   - ✅ Profile scripts visible and editable
   - ✅ Sequence Builder menu works
   - ✅ Setup Assistant opens
   - ✅ Framework Validator opens

**Try to verify IP protection:**
- Navigate to `Assets/VRTrainingKit/Plugins/`
- Double-click `VRTrainingKit.Runtime.dll`
- Unity should say: "Can't open DLL files"
- ✅ Source code is protected!

---

## Phase 4: Distribution

### Step 12: Share the Package

**You now have:**
```
📦 VRTrainingKit_v1.0_Protected.unitypackage
   Size: ~1-10 MB (smaller than source version!)

Contains:
   ✅ DLLs (protected core logic)
   ✅ Profile source code (extensible)
   ✅ Resources (default profiles)
   ✅ Documentation
```

**Distribution methods:**

**Option 1: Direct sharing**
- Email to clients
- Upload to Google Drive / Dropbox
- Share via company file server

**Option 2: GitHub Release**
1. Create GitHub repository
2. Go to "Releases" section
3. Click "Create a new release"
4. Upload .unitypackage as release asset
5. Share release URL

**Option 3: Unity Asset Store**
1. Create Unity Publisher account
2. Submit package through Publisher Portal
3. Include DLLs + documentation
4. Set pricing / licensing

**Option 4: Your own website**
- Host on your server
- Provide download link
- Implement licensing system if needed

---

# What Users Receive

## Simple Export (Source Code) Package Contents

When users import `VRTrainingKit.unitypackage` (simple version):

**They get:**
```
Assets/VRTrainingKit/
  ├─ Documentation/      (all docs)
  ├─ Editor/             (all editor scripts - SOURCE)
  ├─ Resources/          (default profiles)
  └─ Scripts/
       ├─ Core/          (all core logic - SOURCE)
       ├─ Editor/        (editor utilities - SOURCE)
       ├─ Profiles/      (all profiles - SOURCE)
       └─ SequenceSystem/ (sequence system - SOURCE)
```

**They can:**
- ✅ Read ALL source code
- ✅ Modify anything
- ✅ Learn from your code
- ✅ Copy/fork for their projects
- ✅ Debug everything with breakpoints

**IP Protection:** ❌ None - full source exposed

**Best for:**
- Open-source projects
- Team collaboration
- Educational purposes
- Internal tools

---

## Protected Export (DLL) Package Contents

When users import `VRTrainingKit_v1.0_Protected.unitypackage`:

**They get:**
```
Assets/VRTrainingKit/
  ├─ Plugins/
  │    ├─ VRTrainingKit.Runtime.dll      (PROTECTED)
  │    ├─ VRTrainingKit.Runtime.pdb
  │    ├─ VRTrainingKit.Editor.dll       (PROTECTED)
  │    └─ VRTrainingKit.Editor.pdb
  ├─ Scripts/
  │    └─ Profiles/                       (SOURCE - extensible)
  │         ├─ InteractionProfile.cs
  │         ├─ GrabProfile.cs
  │         └─ (all profiles)
  ├─ Resources/                           (profile assets)
  └─ Documentation/                       (guides)
```

**They can:**
- ✅ Use all functionality
- ✅ Create custom profiles (inherit from InteractionProfile)
- ✅ Read profile source code as examples
- ✅ See error messages with line numbers (.pdb files)
- ❌ **Cannot** see core implementation
- ❌ **Cannot** modify protected logic
- ❌ **Cannot** easily reverse-engineer DLLs

**IP Protection:** ✅ Strong - core logic protected

**Best for:**
- Commercial Asset Store packages
- Client deliveries
- Proprietary tools
- Products with licensing

---

## Comparison Table

| Feature | Simple Export | Protected Export |
|---------|--------------|------------------|
| **Core logic visible** | ✅ Yes | ❌ No (DLL) |
| **Profiles visible** | ✅ Yes | ✅ Yes |
| **Users can extend** | ✅ Yes | ✅ Yes (profiles only) |
| **Users can debug** | ✅ Full access | ⚠️ Limited (with .pdb) |
| **IP protection** | ❌ None | ✅ Strong |
| **Package size** | 5-20 MB | 1-10 MB |
| **Export time** | 2 min | 15-20 min |
| **Maintenance** | Easy | Moderate |

---

# Troubleshooting

## Common Issues During Export

### Issue 1: "Export Package" is Grayed Out

**Symptom:** Can't click "Export Package" in menu

**Cause:** No folder/asset selected

**Solution:**
1. Click away from VRTrainingKit folder
2. Click back on VRTrainingKit folder
3. Wait 1 second
4. Right-click again

---

### Issue 2: Export Window is Empty

**Symptom:** Export window shows no files to export

**Cause:** Folder selection lost during right-click

**Solution:**
1. Close export window
2. Single-click VRTrainingKit folder (make it blue/highlighted)
3. Right-click and Export Package again

---

### Issue 3: Can't Find Library/ScriptAssemblies Folder

**Symptom:** Library folder is hidden or doesn't exist

**Solution:**

**Make hidden files visible:**
1. Open File Explorer
2. Click "View" tab (top)
3. Check "Hidden items" checkbox
4. Library folder should appear

**If Library folder doesn't exist:**
- Your Unity project hasn't compiled yet
- Open Unity project
- Let it compile for a few minutes
- Check again

---

### Issue 4: DLL Files Not Found in ScriptAssemblies

**Symptom:** `VRTrainingKit.Runtime.dll` doesn't exist

**Cause:** No Assembly Definition files created

**Solution:**
1. You need to create `.asmdef` files first
2. See Assembly Definitions guide
3. Or use Simple Export method instead

**Files you're looking for:**
- `VRTrainingKit.Runtime.dll` (runtime scripts)
- `VRTrainingKit.Editor.dll` (editor scripts)

**If you only see:**
- `Assembly-CSharp.dll` → No .asmdef files created
- Use Simple Export method for now

---

### Issue 5: Import Package Shows Warnings

**Symptom:** After import, Unity Console shows warnings

**Common warnings and fixes:**

**"Shader error..."**
- Not related to VR Training Kit
- Probably user's project settings
- Can be ignored

**"Missing script reference..."**
- Some ScriptableObject references broken
- User may need to reassign profile assets
- Include setup guide in documentation

**"Assembly definition conflict..."**
- User has conflicting .asmdef files
- Ask them to check their assembly setup
- May need to adjust assembly references

---

### Issue 6: Menu Doesn't Appear After Import

**Symptom:** "Sequence Builder" menu missing after import

**Possible causes:**

**Cause 1: Unity needs refresh**
- Solution: `Assets > Refresh` or restart Unity

**Cause 2: DLLs didn't load**
- Check Unity Console for errors
- Verify DLLs are in Plugins folder
- Check .pdb files are with .dll files

**Cause 3: Script compilation errors**
- Check Console for red errors
- Fix any compilation issues
- Menu appears after successful compile

**Cause 4: Wrong Unity version**
- VR Training Kit may require specific Unity version
- Check user has compatible version
- Include version requirements in README

---

### Issue 7: Package File Size is Too Large

**Symptom:** .unitypackage file is 100+ MB

**Cause:** Including unnecessary files

**Solution:**

**Check what's being included:**
1. During export, expand all folders
2. Look for:
   - ❌ Test scenes
   - ❌ Example assets (textures, models)
   - ❌ Temp files
   - ❌ .meta.meta files (corruption)

**Uncheck large unnecessary items:**
- Uncheck Examples folder
- Uncheck TestSetup folder
- Only include essential files

**Expected sizes:**
- Simple export: 5-20 MB
- Protected export: 1-10 MB (DLLs are smaller)
- If >50 MB, something's wrong

---

### Issue 8: Users Report Missing Dependencies

**Symptom:** Users get errors about missing packages

**Cause:** Your package depends on other Unity packages

**Solutions:**

**Option 1: Check "include dependencies"**
- In Export Package window
- Check "include dependencies" checkbox
- Unity includes required packages
- Warning: Makes package larger

**Option 2: Document dependencies**
- In README.md, list required packages:
  ```
  Required Unity Packages:
  - XR Interaction Toolkit 2.0+
  - TextMeshPro
  - Input System 1.4+
  ```
- Users install manually before importing

**Option 3: Include Package Manifest**
- Check "include package manifest"
- Unity auto-installs dependencies
- Recommended for complex packages

---

## Best Practices

### Version Naming

Use semantic versioning in filenames:

```
✅ Good:
VRTrainingKit_v1.0.0.unitypackage
VRTrainingKit_v1.1.0_Protected.unitypackage
VRTrainingKit_v2.0.0_Beta.unitypackage

❌ Bad:
VRTrainingKit.unitypackage
VRTrainingKit_Final.unitypackage
VRTrainingKit_FINAL_FINAL_v2.unitypackage
```

**Version scheme:**
- `v1.0.0` - Major.Minor.Patch
- `v1.0.0` - Initial release
- `v1.1.0` - New features
- `v1.0.1` - Bug fixes
- `v2.0.0` - Breaking changes

---

### Include README in Package

**Always include a README.md in the package root:**

```markdown
# VR Training Kit v1.0.0

## Quick Start
1. Open Sequence Builder > Setup Assistant
2. Tag objects (grab, knob, snap)
3. Scan scene and apply profiles

## Documentation
See Documentation/ folder for full guides

## Support
Email: support@yourcompany.com
Discord: discord.gg/yourserver

## License
[Your license terms]
```

---

### Test Before Distributing

**Checklist before sharing:**

- [ ] Export package to desktop
- [ ] Create NEW Unity project
- [ ] Import package
- [ ] Check Console for errors
- [ ] Test all menu items
- [ ] Create test scene
- [ ] Tag objects and test setup
- [ ] Verify profiles work
- [ ] Test sequence system
- [ ] Check documentation opens
- [ ] Try example workflows

**Only distribute after all tests pass!**

---

### Provide Support Documentation

**Include these files in Documentation/ folder:**

```
📁 Documentation/
  ├─ README.md              (quick start)
  ├─ SETUP_GUIDE.md         (installation)
  ├─ USER_GUIDE.md          (how to use)
  ├─ API_REFERENCE.md       (for developers)
  ├─ TROUBLESHOOTING.md     (common issues)
  ├─ CHANGELOG.md           (version history)
  └─ LICENSE.txt            (legal terms)
```

---

## Summary

### Simple Export (3 steps):
1. Right-click `Assets/VRTrainingKit/` folder
2. Select "Export Package..."
3. Save as `VRTrainingKit.unitypackage`

**Time:** 2 minutes
**Protection:** None
**Use case:** Internal sharing, open-source

---

### Protected Export (11 steps):
1. Locate DLLs in `Library/ScriptAssemblies/`
2. Create distribution folder structure
3. Copy DLLs to `Plugins/`
4. Copy Profile source to `Scripts/Profiles/`
5. Copy Resources and Documentation
6. Create new Unity test project
7. Import distribution folder
8. Verify DLLs loaded correctly
9. Right-click folder and Export Package
10. Save as `VRTrainingKit_v1.0_Protected.unitypackage`
11. Test in fresh Unity project

**Time:** 15-20 minutes
**Protection:** Strong (DLL)
**Use case:** Commercial, Asset Store, clients

---

## Next Steps

**You're now ready to:**
- ✅ Export VR Training Kit for distribution
- ✅ Choose protection level (simple vs. protected)
- ✅ Share with colleagues/clients
- ✅ Publish to Asset Store
- ✅ Support users with documentation

**For updates:**
- Make changes to source code
- Test thoroughly
- Increment version number
- Re-export package
- Distribute to users

---

**Questions?**
Refer to this guide anytime you need to export the package!

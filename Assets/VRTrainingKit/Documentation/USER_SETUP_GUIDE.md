# VR Training Kit - User Setup Guide

## Quick Start for Users

After importing VR Training Kit into your Unity project, follow these steps to set up your first training scene.

---

## Prerequisites

Before using VR Training Kit, install these Unity packages via Package Manager:

1. **XR Interaction Toolkit** - `Window > Package Manager > Unity Registry > XR Interaction Toolkit`
2. **Input System** - Included with XR Interaction Toolkit
3. **TextMeshPro** - Usually auto-imported
4. **AutoHands** - Import from wherever you obtained it

---

## Step 1: Create Sequence Manager

The Sequence Manager is the runtime controller that executes your training sequences.

### Create via Menu (Recommended):

1. In Unity, go to: `Sequence Builder > Create Sequence Manager`
2. This creates a GameObject with all necessary components
3. The object appears in your Hierarchy as "Sequence Manager"

### Configure the Sequence Manager:

1. Select the "Sequence Manager" in Hierarchy
2. In the Inspector, find the `Modular Training Sequence Controller` component
3. Assign a Training Sequence Asset to the "Training Asset" field
4. Configure debug settings as needed

---

## Step 2: Setup VR Interaction Objects

Use the Setup Assistant to configure your scene objects for VR interaction:

1. Go to: `Window > Sequence Builder > Setup Assistant`
2. In the **Setup Tab**:
   - Tag objects in your scene with: `grab`, `knob`, or `snap`
   - Click "Scan Scene" to detect tagged objects
   - Select objects and click "Apply Profile" to add VR components

3. In the **Configure Tab**:
   - Create custom profiles if needed
   - Manage existing profiles

4. In the **Validate Tab**:
   - Check for setup issues
   - Fix any errors before testing

---

## Step 3: Create Training Sequence

1. Create a new Training Sequence Asset:
   - Right-click in Project window
   - `Create > VR Training > Training Sequence Asset`

2. Open the Setup Assistant: `Window > Sequence Builder > Setup Assistant`

3. Go to **Sequence Tab**:
   - Click "New" to create a new program
   - Or load an existing asset
   - Use the tree view to organize: Programs > Modules > Task Groups > Steps
   - Configure each step with target objects and instructions

---

## Step 4: Test Your Training

1. Ensure you have a VR rig in your scene (XR Origin or AutoHands rig)
2. Enter Play Mode
3. The Sequence Manager will automatically start executing your training sequence
4. Follow the on-screen instructions and arrows

---

## Common Issues

### "Sequence Manager prefab has missing scripts"

**Solution:** Don't use the prefab! Instead, use the menu item:
- `Sequence Builder > Create Sequence Manager`

This creates a fresh GameObject with all correct DLL references.

### "Scripts are missing or not compiling"

**Solution:** Make sure all required packages are installed:
- XR Interaction Toolkit
- AutoHands framework
- TextMeshPro
- Input System

### "Setup Assistant doesn't open"

**Solution:** Check the Console for errors. Make sure:
- VR Training Kit is fully imported
- No compilation errors exist
- Unity version is compatible (2021.3+)

---

## Creating Custom Profiles

You can extend VR Training Kit by creating custom interaction profiles:

1. Create a new C# script in your project (NOT in VRTrainingKit folder)

2. Inherit from `InteractionProfile`:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "VR Training/My Custom Profile")]
public class MyCustomProfile : InteractionProfile
{
    public override void ApplyToGameObject(GameObject target)
    {
        // Add your custom components here
        var myComponent = target.AddComponent<MyCustomComponent>();
        // Configure it...
    }

    public override bool ValidateGameObject(GameObject target)
    {
        // Return true if target is valid for this profile
        return target != null;
    }
}
```

3. Create profile assets:
   - Right-click in Project
   - `Create > VR Training > My Custom Profile`

4. Use in Setup Assistant like built-in profiles

---

## API Reference

### Key Classes (Available via DLLs):

- `ModularTrainingSequenceController` - Main runtime controller
- `InteractionProfile` - Base class for custom profiles
- `TrainingSequenceAsset` - ScriptableObject containing training data
- `InteractionSetupService` - Utility for applying profiles to objects
- `VRFrameworkDetector` - Detects current VR framework (XRI/AutoHands)

### Public Methods:

See the Profiles source code in `Assets/VRTrainingKit/Scripts/Profiles/` for examples and API documentation.

---

## Support

For issues or questions:
1. Check the Console for detailed error messages
2. Use `Sequence Builder > Setup Assistant > Validate Tab`
3. Refer to code examples in the Profiles folder

---

## Advanced: Multiple Training Sequences

To switch between different training programs:

1. Create multiple Training Sequence Assets
2. On the Sequence Manager, change the "Training Asset" reference
3. Use code to switch at runtime:

```csharp
var controller = FindObjectOfType<ModularTrainingSequenceController>();
controller.trainingAsset = myOtherSequence;
// Restart controller if needed
```

---

## Best Practices

✅ **Do:**
- Use the "Create Sequence Manager" menu item (not prefabs)
- Test in Play mode frequently
- Use validation tools before building
- Create reusable profiles for common interactions

❌ **Don't:**
- Modify files in the VRTrainingKit folder (create your own scripts instead)
- Use the old "Sequence Manager" prefab (it has missing scripts)
- Forget to assign Training Sequence Assets

---

## Version Information

Check your VR Training Kit version in the DLLs folder:
- `Assets/VRTrainingKit/Plugins/VRTrainingKit.*.dll`


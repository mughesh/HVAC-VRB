// VRTrainingKitInit.cs
// Initialization and setup utility for VR Training Kit

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Initialization utilities for VR Training Kit setup
/// Handles project setup, tag creation, and default configuration
/// </summary>
public static class VRTrainingKitInit
{
    private const string MENU_PATH = "VR Training/Setup/";
    private const string VR_TRAINING_KIT_PATH = "Assets/VRTrainingKit";
    private const string RESOURCES_PATH = "Assets/VRTrainingKit/Resources";
    
    // Required tags for the system
    private static readonly string[] REQUIRED_TAGS = { "grab", "knob", "snap" };
    
    [MenuItem(MENU_PATH + "Initialize VR Training Kit", false, 0)]
    public static void InitializeVRTrainingKit()
    {
        bool needsInitialization = false;
        
        // Check if setup is needed
        if (!AssetDatabase.IsValidFolder(VR_TRAINING_KIT_PATH))
            needsInitialization = true;
        
        if (!HasRequiredTags())
            needsInitialization = true;
        
        if (needsInitialization)
        {
            if (EditorUtility.DisplayDialog("VR Training Kit Setup", 
                "Initialize VR Training Kit?\n\nThis will:\n• Create required tags\n• Set up folder structure\n• Create default profiles", 
                "Initialize", "Cancel"))
            {
                PerformInitialization();
            }
        }
        else
        {
            EditorUtility.DisplayDialog("VR Training Kit", 
                "VR Training Kit is already initialized!\n\nUse the VR Training Setup Assistant to configure your scene.", 
                "OK");
        }
    }
    
    [MenuItem(MENU_PATH + "Create Required Tags", false, 20)]
    public static void CreateRequiredTags()
    {
        CreateTags();
        EditorUtility.DisplayDialog("Tags Created", 
            "Required tags (grab, knob, snap) have been created.", "OK");
    }
    
    [MenuItem(MENU_PATH + "Create Default Profiles", false, 21)]
    public static void CreateDefaultProfilesOnly()
    {
        EnsureFolderStructure();
        CreateDefaultProfiles();
        EditorUtility.DisplayDialog("Profiles Created", 
            "Default interaction profiles have been created in VRTrainingKit/Resources/", "OK");
    }
    
    [MenuItem(MENU_PATH + "Validate Setup", false, 40)]
    public static void ValidateSetup()
    {
        bool isValid = true;
        string issues = "";
        
        // Check folder structure
        if (!AssetDatabase.IsValidFolder(VR_TRAINING_KIT_PATH))
        {
            isValid = false;
            issues += "• VRTrainingKit folder missing\n";
        }
        
        if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
        {
            isValid = false;
            issues += "• Resources folder missing\n";
        }
        
        // Check tags
        foreach (string tag in REQUIRED_TAGS)
        {
            if (!HasTag(tag))
            {
                isValid = false;
                issues += $"• Tag '{tag}' missing\n";
            }
        }
        
        // Check profiles
        if (!HasDefaultProfiles())
        {
            isValid = false;
            issues += "• Default profiles missing\n";
        }
        
        if (isValid)
        {
            EditorUtility.DisplayDialog("Setup Validation", 
                "✓ VR Training Kit setup is complete and valid!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Setup Issues Found", 
                "Issues found:\n\n" + issues + "\nRun 'Initialize VR Training Kit' to fix.", "OK");
        }
    }
    
    [MenuItem(MENU_PATH + "Open Setup Window", false, 60)]
    public static void OpenSetupWindow()
    {
        VRInteractionSetupWindow.ShowWindow();
    }
    
    private static void PerformInitialization()
    {
        Debug.Log("[VR Training Kit] Starting initialization...");
        
        // Create folder structure
        EnsureFolderStructure();
        
        // Create required tags
        CreateTags();
        
        // Create default profiles
        CreateDefaultProfiles();
        
        // Create interaction layers if needed
        CreateDefaultInteractionLayers();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[VR Training Kit] Initialization complete!");
        EditorUtility.DisplayDialog("Initialization Complete", 
            "VR Training Kit has been successfully initialized!\n\nNext steps:\n" +
            "1. Tag your objects with 'grab', 'knob', or 'snap'\n" +
            "2. Open the Setup Assistant (VR Training > Setup Assistant)\n" +
            "3. Scan your scene and apply configurations", "OK");
    }
    
    private static void EnsureFolderStructure()
    {
        // Create main folder
        if (!AssetDatabase.IsValidFolder("Assets/VRTrainingKit"))
        {
            AssetDatabase.CreateFolder("Assets", "VRTrainingKit");
            Debug.Log("[VR Training Kit] Created VRTrainingKit folder");
        }
        
        // Create Resources folder
        if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
        {
            AssetDatabase.CreateFolder("Assets/VRTrainingKit", "Resources");
            Debug.Log("[VR Training Kit] Created Resources folder");
        }
    }
    
    private static void CreateTags()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        foreach (string tag in REQUIRED_TAGS)
        {
            if (!HasTag(tag))
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = tag;
                Debug.Log($"[VR Training Kit] Created tag: {tag}");
            }
        }
        
        tagManager.ApplyModifiedProperties();
    }
    
    private static bool HasRequiredTags()
    {
        foreach (string tag in REQUIRED_TAGS)
        {
            if (!HasTag(tag)) return false;
        }
        return true;
    }
    
    private static bool HasTag(string tag)
    {
        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
        {
            if (UnityEditorInternal.InternalEditorUtility.tags[i] == tag)
                return true;
        }
        return false;
    }
    
    private static void CreateDefaultProfiles()
    {
        // Create Grab Profile
        string grabPath = $"{RESOURCES_PATH}/GrabProfile.asset";
        if (!File.Exists(grabPath))
        {
            GrabProfile grabProfile = ScriptableObject.CreateInstance<GrabProfile>();
            grabProfile.profileName = "Default Grab Profile";
            grabProfile.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
            grabProfile.trackPosition = true;
            grabProfile.trackRotation = true;
            grabProfile.throwOnDetach = true;
            grabProfile.colliderType = ColliderType.Mesh;
            
            AssetDatabase.CreateAsset(grabProfile, grabPath);
            Debug.Log("[VR Training Kit] Created default GrabProfile");
        }
        
        // Create Knob Profile
        string knobPath = $"{RESOURCES_PATH}/KnobProfile.asset";
        if (!File.Exists(knobPath))
        {
            KnobProfile knobProfile = ScriptableObject.CreateInstance<KnobProfile>();
            knobProfile.profileName = "Default Knob Profile";
            knobProfile.rotationAxis = KnobProfile.RotationAxis.Y;
            knobProfile.useLimits = true;
            knobProfile.minAngle = -90f;
            knobProfile.maxAngle = 180f;
            knobProfile.useSpring = true;
            knobProfile.springValue = 0f;
            knobProfile.damper = 1f;
            knobProfile.useHapticFeedback = true;
            knobProfile.colliderType = ColliderType.Box;
            
            AssetDatabase.CreateAsset(knobProfile, knobPath);
            Debug.Log("[VR Training Kit] Created default KnobProfile");
        }
        
        // Create Snap Profile
        string snapPath = $"{RESOURCES_PATH}/SnapProfile.asset";
        if (!File.Exists(snapPath))
        {
            SnapProfile snapProfile = ScriptableObject.CreateInstance<SnapProfile>();
            snapProfile.profileName = "Default Snap Profile";
            snapProfile.socketRadius = 0.1f;
            snapProfile.socketActive = true;
            snapProfile.showInteractableHoverMeshes = true;
            snapProfile.acceptedTags = new string[] { "grab" };
            
            AssetDatabase.CreateAsset(snapProfile, snapPath);
            Debug.Log("[VR Training Kit] Created default SnapProfile");
        }
    }
    
    private static bool HasDefaultProfiles()
    {
        return File.Exists($"{RESOURCES_PATH}/GrabProfile.asset") &&
               File.Exists($"{RESOURCES_PATH}/KnobProfile.asset") &&
               File.Exists($"{RESOURCES_PATH}/SnapProfile.asset");
    }
    
    private static void CreateDefaultInteractionLayers()
    {
        // This would create commonly used interaction layers
        // Implementation would depend on your specific requirements
        
        var commonLayers = new string[] 
        { 
            "Service Valve", 
            "Teleport", 
            "UI",
            "Grabbable Objects",
            "Snap Points"
        };
        
        foreach (string layerName in commonLayers)
        {
            InteractionLayerManager.AddInteractionLayer(layerName);
        }
        
        Debug.Log("[VR Training Kit] Created default interaction layers");
    }
    
    [MenuItem(MENU_PATH + "Documentation", false, 80)]
    public static void OpenDocumentation()
    {
        string docText = @"VR Training Kit - Quick Start Guide

SETUP:
1. Initialize the toolkit: VR Training > Setup > Initialize VR Training Kit
2. Tag your objects: 'grab', 'knob', or 'snap'
3. Open Setup Assistant: VR Training > Setup Assistant

WORKFLOW:
1. Setup Tab: Scan scene for tagged objects
2. Configure Tab: Select/create interaction profiles  
3. Apply components to objects
4. Sequence Tab: Set up training flow (optional)
5. Validate Tab: Check for issues

OBJECT TAGGING:
• 'grab' - Objects that can be picked up and moved
• 'knob' - Rotatable objects with constraints (valves, dials)
• 'snap' - Socket points where objects can be attached

PROFILES:
• GrabProfile - Physics, collider, and grab settings
• KnobProfile - Rotation limits, joint configuration, haptics
• SnapProfile - Socket radius, validation rules, feedback

SEQUENCE SYSTEM:
• State-based training progression
• Condition-driven state transitions
• Visual feedback for locked/available actions
• Integration with validation components

For detailed documentation, see the script comments in VRTrainingKit/Scripts/";

        EditorUtility.DisplayDialog("VR Training Kit Documentation", docText, "OK");
    }
}
#endif
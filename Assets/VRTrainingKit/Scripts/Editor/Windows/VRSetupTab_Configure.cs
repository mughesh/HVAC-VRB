// VRSetupTab_Configure.cs
// Configure tab for VR Training Setup window - Profile management and selection

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Configure tab - Handles interaction profile selection and creation
/// </summary>
public class VRSetupTab_Configure : VRSetupTabBase
{
    private VRProfileManager profileManager;
    private Vector2 configScrollPos;
    
    public VRSetupTab_Configure(VRInteractionSetupWindow window, VRProfileManager profileManager) : base(window)
    {
        this.profileManager = profileManager;
    }
    
    public override void DrawTab()
    {
        EditorGUILayout.LabelField("Profile Configuration", headerStyle);
        EditorGUILayout.Space(5);

        // Framework compatibility notice
        DrawConfigureFrameworkNotice();
        EditorGUILayout.Space(5);

        configScrollPos = EditorGUILayout.BeginScrollView(configScrollPos);
        
        // Draw profile sections using helper method to reduce code duplication
        DrawProfileSection("Grab Profile", 
            ref profileManager.selectedGrabProfile, 
            profileManager.cachedGrabProfiles,
            VRProfileManager.IsGrabProfile,
            () => profileManager.RefreshGrabProfileCache(),
            typeof(GrabProfile));
            
        EditorGUILayout.Space(10);
        
        DrawProfileSection("Knob Profile",
            ref profileManager.selectedKnobProfile,
            profileManager.cachedKnobProfiles,
            VRProfileManager.IsKnobProfile,
            () => profileManager.RefreshKnobProfileCache(),
            typeof(KnobProfile));
            
        EditorGUILayout.Space(10);
        
        DrawProfileSection("Snap Profile",
            ref profileManager.selectedSnapProfile,
            profileManager.cachedSnapProfiles,
            VRProfileManager.IsSnapProfile,
            () => profileManager.RefreshSnapProfileCache(),
            typeof(SnapProfile));
            
        EditorGUILayout.Space(10);
        
        DrawProfileSection("Tool Profile",
            ref profileManager.selectedToolProfile,
            profileManager.cachedToolProfiles,
            VRProfileManager.IsToolProfile,
            () => profileManager.RefreshToolProfileCache(),
            typeof(ToolProfile));
            
        EditorGUILayout.Space(10);
        
        DrawProfileSection("Valve Profile",
            ref profileManager.selectedValveProfile,
            profileManager.cachedValveProfiles,
            VRProfileManager.IsValveProfile,
            () => profileManager.RefreshValveProfileCache(),
            typeof(ValveProfile));
            
        EditorGUILayout.Space(10);
        
        DrawProfileSection("Turn Profile",
            ref profileManager.selectedTurnProfile,
            profileManager.cachedTurnProfiles,
            VRProfileManager.IsTurnProfile,
            () => profileManager.RefreshTurnProfileCache(),
            typeof(AutoHandsTurnByCountProfile));
            
        EditorGUILayout.Space(10);
        
        DrawProfileSection("🚀 Teleport Profile",
            ref profileManager.selectedTeleportProfile,
            profileManager.cachedTeleportProfiles,
            VRProfileManager.IsTeleportProfile,
            () => profileManager.RefreshTeleportProfileCache(),
            typeof(AutoHandsTeleportProfile));

        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // Quick actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create All Default Profiles", GUILayout.Height(30)))
        {
            CreateDefaultProfiles();
        }
        if (GUILayout.Button("Refresh All Caches", GUILayout.Height(30)))
        {
            profileManager.RefreshProfileCaches();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawProfileSection(
        string title,
        ref InteractionProfile selectedProfile,
        System.Collections.Generic.List<InteractionProfile> cachedProfiles,
        Func<InteractionProfile, bool> validator,
        Action refreshAction,
        Type profileType)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, subHeaderStyle);

        // Profile object field with validation
        var tempProfile = EditorGUILayout.ObjectField(
            "Profile Asset", selectedProfile, typeof(InteractionProfile), false) as InteractionProfile;

        if (tempProfile != null && validator(tempProfile))
        {
            selectedProfile = tempProfile;
        }
        else if (tempProfile != null)
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{tempProfile.name}' is not a {title.ToLower()}.", "OK");
        }
        
        if (selectedProfile == null)
        {
            // Show available profiles from cache
            if (cachedProfiles != null && cachedProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedProfiles)
                {
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string frameworkType = VRProfileManager.GetProfileFrameworkType(profile);
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            selectedProfile = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"No {title.ToLower()}s found. Create one below.", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Create New {title}"))
            {
                CreateNewProfile(profileType, profileType.Name);
                refreshAction();
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                refreshAction();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = selectedProfile;
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void CreateNewProfile(Type profileType, string defaultName)
    {
        try
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create New {profileType.Name}",
                defaultName,
                "asset",
                $"Choose location for new {profileType.Name}");
                
            if (!string.IsNullOrEmpty(path))
            {
                var profile = ScriptableObject.CreateInstance(profileType) as InteractionProfile;
                if (profile != null)
                {
                    profile.profileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    AssetDatabase.CreateAsset(profile, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    Selection.activeObject = profile;
                    EditorGUIUtility.PingObject(profile);
                    
                    Debug.Log($"[VRSetupTab_Configure] Created new {profileType.Name} at {path}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VRSetupTab_Configure] Failed to create profile: {e.Message}");
        }
    }
    
    private void CreateDefaultProfiles()
    {
        // This would create a default set of profiles
        EditorUtility.DisplayDialog("Create Default Profiles", 
            "This feature will create a complete set of default profiles for your VR framework.", "OK");
        // Implementation would create standard profiles in Resources folder
    }
    
    private void DrawConfigureFrameworkNotice()
    {
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        
        EditorGUILayout.BeginVertical("box");
        string frameworkName = VRFrameworkDetector.GetFrameworkDisplayName(currentFramework);
        
        if (currentFramework == VRFramework.None)
        {
            EditorGUILayout.HelpBox(
                "⚠️ No VR framework detected. Please install XR Interaction Toolkit or AutoHands.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField($"Current Framework: {frameworkName}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Showing {frameworkName} profiles. Profiles must match your VR framework.",
                MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
}

#endif

// VRProfileManager.cs
// Utility class for managing interaction profiles across tabs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Manages interaction profile loading, caching, and validation
/// </summary>
public class VRProfileManager
{
    // Selected profiles - Support both XRI and AutoHands
    public InteractionProfile selectedGrabProfile;
    public InteractionProfile selectedKnobProfile;
    public InteractionProfile selectedSnapProfile;
    public InteractionProfile selectedToolProfile;
    public InteractionProfile selectedValveProfile;
    public InteractionProfile selectedTurnProfile;
    public InteractionProfile selectedTeleportProfile;
    
    // Cache available profiles to avoid performance issues
    public List<InteractionProfile> cachedGrabProfiles;
    public List<InteractionProfile> cachedKnobProfiles;
    public List<InteractionProfile> cachedSnapProfiles;
    public List<InteractionProfile> cachedToolProfiles;
    public List<InteractionProfile> cachedValveProfiles;
    public List<InteractionProfile> cachedTurnProfiles;
    public List<InteractionProfile> cachedTeleportProfiles;
    
    public void LoadDefaultProfiles()
    {
        // Detect current framework and load appropriate profiles
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();

        Debug.Log($"[VRProfileManager] Loading profiles for framework: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");

        // Load framework-appropriate profiles
        if (currentFramework == VRFramework.AutoHands)
        {
            LoadAutoHandsProfiles();
        }
        else
        {
            LoadXRIProfilesInternal();
        }
    }
    
    private void LoadXRIProfilesInternal()
    {
        // Try to load default profiles from Resources (framework-specific paths first, then fallback)
        selectedGrabProfile = Resources.Load<InteractionProfile>("XRI/DefaultGrabProfile") ?? Resources.Load<InteractionProfile>("DefaultGrabProfile");
        selectedKnobProfile = Resources.Load<InteractionProfile>("XRI/DefaultKnobProfile") ?? Resources.Load<InteractionProfile>("DefaultKnobProfile");
        selectedSnapProfile = Resources.Load<InteractionProfile>("XRI/DefaultSnapProfile") ?? Resources.Load<InteractionProfile>("DefaultSnapProfile");
        selectedToolProfile = Resources.Load<InteractionProfile>("XRI/DefaultToolProfile") ?? Resources.Load<InteractionProfile>("DefaultToolProfile");
        selectedValveProfile = Resources.Load<InteractionProfile>("XRI/DefaultValveProfile") ?? Resources.Load<InteractionProfile>("DefaultValveProfile");

        // If not found in Resources, search in Assets for XRI profiles
        if (selectedGrabProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:GrabProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedGrabProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedKnobProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:KnobProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedKnobProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedSnapProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SnapProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedSnapProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedToolProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ToolProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedToolProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedValveProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ValveProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedValveProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }
    }
    
    private void LoadAutoHandsProfiles()
    {
        // Try to load default profiles from Resources (framework-specific paths first)
        selectedGrabProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsGrabProfile");
        selectedKnobProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsKnobProfile");
        selectedSnapProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsSnapProfile");
        selectedToolProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsToolProfile");
        selectedValveProfile = Resources.Load<InteractionProfile>("AutoHands/DefaultAutoHandsValveProfile");

        // If not found in Resources, search in Assets for AutoHands profiles
        if (selectedGrabProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsGrabProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedGrabProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedKnobProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsKnobProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedKnobProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedSnapProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsSnapProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedSnapProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedToolProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsToolProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedToolProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }

        if (selectedValveProfile == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AutoHandsValveProfile");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                selectedValveProfile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            }
        }
    }
    
    public void RefreshProfileCaches()
    {
        RefreshGrabProfileCache();
        RefreshKnobProfileCache();
        RefreshSnapProfileCache();
        RefreshToolProfileCache();
        RefreshValveProfileCache();
        RefreshTurnProfileCache();
        RefreshTeleportProfileCache();
    }

    private void RefreshGrabProfileCache()
    {
        cachedGrabProfiles = new List<InteractionProfile>();

        // Find XRI GrabProfile
        string[] xriGrabGuids = AssetDatabase.FindAssets("t:GrabProfile");
        foreach (string guid in xriGrabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsGrabProfile(profile))
            {
                cachedGrabProfiles.Add(profile);
            }
        }

        // Find AutoHands GrabProfile
        string[] autoHandsGrabGuids = AssetDatabase.FindAssets("t:AutoHandsGrabProfile");
        foreach (string guid in autoHandsGrabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsGrabProfile(profile))
            {
                cachedGrabProfiles.Add(profile);
            }
        }
    }

    private void RefreshKnobProfileCache()
    {
        cachedKnobProfiles = new List<InteractionProfile>();

        string[] xriKnobGuids = AssetDatabase.FindAssets("t:KnobProfile");
        foreach (string guid in xriKnobGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsKnobProfile(profile))
            {
                cachedKnobProfiles.Add(profile);
            }
        }

        string[] autoHandsKnobGuids = AssetDatabase.FindAssets("t:AutoHandsKnobProfile");
        foreach (string guid in autoHandsKnobGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsKnobProfile(profile))
            {
                cachedKnobProfiles.Add(profile);
            }
        }
    }

    private void RefreshSnapProfileCache()
    {
        cachedSnapProfiles = new List<InteractionProfile>();

        string[] xriSnapGuids = AssetDatabase.FindAssets("t:SnapProfile");
        foreach (string guid in xriSnapGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsSnapProfile(profile))
            {
                cachedSnapProfiles.Add(profile);
            }
        }

        string[] autoHandsSnapGuids = AssetDatabase.FindAssets("t:AutoHandsSnapProfile");
        foreach (string guid in autoHandsSnapGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsSnapProfile(profile))
            {
                cachedSnapProfiles.Add(profile);
            }
        }
    }

    private void RefreshToolProfileCache()
    {
        cachedToolProfiles = new List<InteractionProfile>();

        string[] xriToolGuids = AssetDatabase.FindAssets("t:ToolProfile");
        foreach (string guid in xriToolGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsToolProfile(profile))
            {
                cachedToolProfiles.Add(profile);
            }
        }

        string[] autoHandsToolGuids = AssetDatabase.FindAssets("t:AutoHandsToolProfile");
        foreach (string guid in autoHandsToolGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsToolProfile(profile))
            {
                cachedToolProfiles.Add(profile);
            }
        }
    }

    private void RefreshValveProfileCache()
    {
        cachedValveProfiles = new List<InteractionProfile>();

        string[] xriValveGuids = AssetDatabase.FindAssets("t:ValveProfile");
        foreach (string guid in xriValveGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsValveProfile(profile))
            {
                cachedValveProfiles.Add(profile);
            }
        }

        string[] autoHandsValveGuids = AssetDatabase.FindAssets("t:AutoHandsValveProfile");
        foreach (string guid in autoHandsValveGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsValveProfile(profile))
            {
                cachedValveProfiles.Add(profile);
            }
        }
    }

    private void RefreshTurnProfileCache()
    {
        cachedTurnProfiles = new List<InteractionProfile>();

        // Find AutoHands TurnByCountProfile (only AutoHands version exists)
        string[] autoHandsTurnGuids = AssetDatabase.FindAssets("t:AutoHandsTurnByCountProfile");
        foreach (string guid in autoHandsTurnGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsTurnProfile(profile))
            {
                cachedTurnProfiles.Add(profile);
            }
        }
    }

    private void RefreshTeleportProfileCache()
    {
        cachedTeleportProfiles = new List<InteractionProfile>();

        // Find AutoHands TeleportProfile (only AutoHands version exists)
        string[] autoHandsTeleportGuids = AssetDatabase.FindAssets("t:AutoHandsTeleportProfile");
        foreach (string guid in autoHandsTeleportGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
            if (profile != null && IsTeleportProfile(profile))
            {
                cachedTeleportProfiles.Add(profile);
            }
        }
    }
    
    // Profile type checking helpers
    public static bool IsGrabProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Grab") || profile.profileName.Contains("Grab"));
    }
    
    public static bool IsKnobProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Knob") || profile.profileName.Contains("Knob"));
    }
    
    public static bool IsSnapProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Snap") || profile.profileName.Contains("Snap"));
    }
    
    public static bool IsToolProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Tool") || profile.profileName.Contains("Tool"));
    }
    
    public static bool IsValveProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Valve") || profile.profileName.Contains("Valve"));
    }
    
    public static bool IsTurnProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Turn") || profile.profileName.Contains("Turn"));
    }
    
    public static bool IsTeleportProfile(InteractionProfile profile)
    {
        return profile != null && (profile.GetType().Name.Contains("Teleport") || profile.profileName.Contains("Teleport"));
    }
    
    public static string GetProfileFrameworkType(InteractionProfile profile)
    {
        if (profile == null) return "";
        
        string typeName = profile.GetType().Name;
        if (typeName.Contains("AutoHands"))
            return "(AutoHands)";
        else if (typeName.Contains("XRI") || !typeName.Contains("AutoHands"))
            return "(XRI)";
        
        return "";
    }
}

#endif

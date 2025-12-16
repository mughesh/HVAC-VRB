// ProfileCacheManager.cs
// Centralized profile caching system for VR Training Kit
// Part of Phase 2: Profile Cache Manager refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages caching and validation of interaction profiles.
/// Replaces 6 nearly identical RefreshXxxProfileCache() methods with a single generic system.
///
/// Usage:
/// - Call RefreshAllCaches() on window enable
/// - Use GetCachedProfiles(ProfileType) to get profile lists
/// - Use IsValidProfile(profile, ProfileType) for validation
/// </summary>
public class ProfileCacheManager
{
    /// <summary>
    /// Supported profile types
    /// </summary>
    public enum ProfileType
    {
        Grab,
        Knob,
        Snap,
        Tool,
        Valve,
        Screw,
        Turn,
        Teleport
    }

    /// <summary>
    /// Profile type configuration - defines XRI and AutoHands type names for each profile type
    /// </summary>
    private class ProfileTypeConfig
    {
        public string XRITypeName { get; set; }
        public string AutoHandsTypeName { get; set; }
        public Func<InteractionProfile, bool> Validator { get; set; }
    }

    // Configuration for each profile type
    private readonly Dictionary<ProfileType, ProfileTypeConfig> _typeConfigs;

    // Cached profiles by type
    private readonly Dictionary<ProfileType, List<InteractionProfile>> _caches;

    public ProfileCacheManager()
    {
        _caches = new Dictionary<ProfileType, List<InteractionProfile>>();
        _typeConfigs = new Dictionary<ProfileType, ProfileTypeConfig>
        {
            {
                ProfileType.Grab, new ProfileTypeConfig
                {
                    XRITypeName = "GrabProfile",
                    AutoHandsTypeName = "AutoHandsGrabProfile",
                    Validator = IsGrabProfile
                }
            },
            {
                ProfileType.Knob, new ProfileTypeConfig
                {
                    XRITypeName = "KnobProfile",
                    AutoHandsTypeName = "AutoHandsKnobProfile",
                    Validator = IsKnobProfile
                }
            },
            {
                ProfileType.Snap, new ProfileTypeConfig
                {
                    XRITypeName = "SnapProfile",
                    AutoHandsTypeName = "AutoHandsSnapProfile",
                    Validator = IsSnapProfile
                }
            },
            {
                ProfileType.Tool, new ProfileTypeConfig
                {
                    XRITypeName = "ToolProfile",
                    AutoHandsTypeName = "AutoHandsToolProfile",
                    Validator = IsToolProfile
                }
            },
            {
                ProfileType.Valve, new ProfileTypeConfig
                {
                    XRITypeName = "ValveProfile",
                    AutoHandsTypeName = "AutoHandsValveProfile",
                    Validator = IsValveProfile
                }
            },
            {
                ProfileType.Screw, new ProfileTypeConfig
                {
                    XRITypeName = "ScrewProfile",
                    AutoHandsTypeName = "AutoHandsScrewProfile",
                    Validator = IsScrewProfile
                }
            },
            {
                ProfileType.Turn, new ProfileTypeConfig
                {
                    XRITypeName = null, // No XRI version exists
                    AutoHandsTypeName = "AutoHandsTurnByCountProfile",
                    Validator = IsTurnProfile
                }
            },
            {
                ProfileType.Teleport, new ProfileTypeConfig
                {
                    XRITypeName = null, // No XRI version exists
                    AutoHandsTypeName = "AutoHandsTeleportProfile",
                    Validator = IsTeleportProfile
                }
            }
        };

        // Initialize empty caches
        foreach (ProfileType type in Enum.GetValues(typeof(ProfileType)))
        {
            _caches[type] = new List<InteractionProfile>();
        }
    }

    /// <summary>
    /// Refreshes all profile caches
    /// </summary>
    public void RefreshAllCaches()
    {
        foreach (ProfileType type in Enum.GetValues(typeof(ProfileType)))
        {
            RefreshCache(type);
        }
    }

    /// <summary>
    /// Refreshes cache for a specific profile type
    /// </summary>
    public void RefreshCache(ProfileType type)
    {
        var profiles = new List<InteractionProfile>();
        var config = _typeConfigs[type];

        // Find XRI profiles (if type has XRI version)
        if (!string.IsNullOrEmpty(config.XRITypeName))
        {
            string[] xriGuids = AssetDatabase.FindAssets($"t:{config.XRITypeName}");
            foreach (string guid in xriGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
                if (profile != null && config.Validator(profile))
                {
                    profiles.Add(profile);
                }
            }
        }

        // Find AutoHands profiles
        if (!string.IsNullOrEmpty(config.AutoHandsTypeName))
        {
            string[] autoHandsGuids = AssetDatabase.FindAssets($"t:{config.AutoHandsTypeName}");
            foreach (string guid in autoHandsGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<InteractionProfile>(path);
                if (profile != null && config.Validator(profile))
                {
                    profiles.Add(profile);
                }
            }
        }

        _caches[type] = profiles;
    }

    /// <summary>
    /// Gets cached profiles for a specific type
    /// </summary>
    public List<InteractionProfile> GetCachedProfiles(ProfileType type)
    {
        if (_caches.TryGetValue(type, out var profiles))
        {
            return profiles;
        }
        return new List<InteractionProfile>();
    }

    /// <summary>
    /// Validates if a profile matches the expected type
    /// </summary>
    public bool IsValidProfile(InteractionProfile profile, ProfileType type)
    {
        if (profile == null) return false;

        if (_typeConfigs.TryGetValue(type, out var config))
        {
            return config.Validator(profile);
        }
        return false;
    }

    /// <summary>
    /// Gets the count of cached profiles for a specific type
    /// </summary>
    public int GetCacheCount(ProfileType type)
    {
        if (_caches.TryGetValue(type, out var profiles))
        {
            return profiles.Count;
        }
        return 0;
    }

    /// <summary>
    /// Clears all caches
    /// </summary>
    public void ClearAllCaches()
    {
        foreach (var cache in _caches.Values)
        {
            cache.Clear();
        }
    }

    // ==========================================
    // Profile Type Validators
    // ==========================================

    private static bool IsGrabProfile(InteractionProfile profile)
    {
        return profile is GrabProfile ||
               (profile != null && profile.GetType().Name.Contains("Grab"));
    }

    private static bool IsKnobProfile(InteractionProfile profile)
    {
        return profile is KnobProfile ||
               (profile != null && profile.GetType().Name.Contains("Knob"));
    }

    private static bool IsSnapProfile(InteractionProfile profile)
    {
        return profile is SnapProfile ||
               (profile != null && profile.GetType().Name.Contains("Snap"));
    }

    private static bool IsToolProfile(InteractionProfile profile)
    {
        return profile is ToolProfile ||
               (profile != null && profile.GetType().Name.Contains("Tool"));
    }

    private static bool IsValveProfile(InteractionProfile profile)
    {
        return profile is ValveProfile ||
               (profile != null && profile.GetType().Name.Contains("Valve"));
    }

    private static bool IsScrewProfile(InteractionProfile profile)
    {
        return profile is ScrewProfile ||
               (profile != null && profile.GetType().Name.Contains("Screw"));
    }

    private static bool IsTurnProfile(InteractionProfile profile)
    {
        return profile != null && profile.GetType().Name.Contains("Turn");
    }

    private static bool IsTeleportProfile(InteractionProfile profile)
    {
        return profile != null && profile.GetType().Name.Contains("Teleport");
    }
}
#endif

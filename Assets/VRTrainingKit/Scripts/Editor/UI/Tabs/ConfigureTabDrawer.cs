// ConfigureTabDrawer.cs
// Extracted Configure tab UI for VR Training Kit editor window
// Part of Phase 3: Configure Tab Extraction refactoring

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Draws the Configure tab UI for profile selection and management.
/// Consolidates 7 nearly identical profile sections into a single generic method.
/// </summary>
public class ConfigureTabDrawer
{
    // Dependencies
    private ProfileCacheManager _profileCacheManager;

    // Profile selection state - using dictionary for generic access
    private Dictionary<ProfileCacheManager.ProfileType, InteractionProfile> _selectedProfiles;

    // Scroll position
    private Vector2 _scrollPosition;

    /// <summary>
    /// Configuration for each profile section
    /// </summary>
    private class ProfileSectionConfig
    {
        public string Label { get; set; }
        public string CreateButtonText { get; set; }
        public string EmptyMessage { get; set; }
        public string InvalidTypeMessage { get; set; }
        public Type CreateProfileType { get; set; }
        public string DefaultAssetName { get; set; }
        public bool IsAutoHandsOnly { get; set; }
        public string Icon { get; set; }
    }

    private readonly Dictionary<ProfileCacheManager.ProfileType, ProfileSectionConfig> _sectionConfigs;

    public ConfigureTabDrawer(ProfileCacheManager profileCacheManager)
    {
        _profileCacheManager = profileCacheManager;
        _selectedProfiles = new Dictionary<ProfileCacheManager.ProfileType, InteractionProfile>();

        // Initialize all profile types to null
        foreach (ProfileCacheManager.ProfileType type in Enum.GetValues(typeof(ProfileCacheManager.ProfileType)))
        {
            _selectedProfiles[type] = null;
        }

        // Configure each profile section
        _sectionConfigs = new Dictionary<ProfileCacheManager.ProfileType, ProfileSectionConfig>
        {
            {
                ProfileCacheManager.ProfileType.Grab, new ProfileSectionConfig
                {
                    Label = "Grab Profile",
                    CreateButtonText = "Create New Grab Profile",
                    EmptyMessage = "No grab profiles found. Create one below.",
                    InvalidTypeMessage = "is not a grab-type profile",
                    CreateProfileType = typeof(GrabProfile),
                    DefaultAssetName = "GrabProfile",
                    IsAutoHandsOnly = false,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Knob, new ProfileSectionConfig
                {
                    Label = "Knob Profile",
                    CreateButtonText = "Create New Knob Profile",
                    EmptyMessage = "No knob profiles found. Create one below.",
                    InvalidTypeMessage = "is not a knob-type profile",
                    CreateProfileType = typeof(KnobProfile),
                    DefaultAssetName = "KnobProfile",
                    IsAutoHandsOnly = false,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Snap, new ProfileSectionConfig
                {
                    Label = "Snap Profile",
                    CreateButtonText = "Create New Snap Profile",
                    EmptyMessage = "No snap profiles found. Create one below.",
                    InvalidTypeMessage = "is not a snap-type profile",
                    CreateProfileType = typeof(SnapProfile),
                    DefaultAssetName = "SnapProfile",
                    IsAutoHandsOnly = false,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Tool, new ProfileSectionConfig
                {
                    Label = "Tool Profile",
                    CreateButtonText = "Create New Tool Profile",
                    EmptyMessage = "No tool profiles found. Create one below.",
                    InvalidTypeMessage = "is not a tool-type profile",
                    CreateProfileType = typeof(ToolProfile),
                    DefaultAssetName = "ToolProfile",
                    IsAutoHandsOnly = false,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Valve, new ProfileSectionConfig
                {
                    Label = "Valve Profile",
                    CreateButtonText = "Create New Valve Profile",
                    EmptyMessage = "No valve profiles found. Create one below.",
                    InvalidTypeMessage = "is not a valve-type profile",
                    CreateProfileType = typeof(ValveProfile),
                    DefaultAssetName = "ValveProfile",
                    IsAutoHandsOnly = false,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Screw, new ProfileSectionConfig
                {
                    Label = "Screw Profile",
                    CreateButtonText = "Create New Screw Profile",
                    EmptyMessage = "No screw profiles found. Create one below.",
                    InvalidTypeMessage = "is not a screw-type profile",
                    CreateProfileType = typeof(ScrewProfile),
                    DefaultAssetName = "ScrewProfile",
                    IsAutoHandsOnly = false,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Turn, new ProfileSectionConfig
                {
                    Label = "Turn By Count Profile",
                    CreateButtonText = "Create New Turn By Count Profile",
                    EmptyMessage = "No turn-by-count profiles found. Create one below.",
                    InvalidTypeMessage = "is not a turn-by-count-type profile",
                    CreateProfileType = typeof(AutoHandsTurnByCountProfile),
                    DefaultAssetName = "TurnByCountProfile",
                    IsAutoHandsOnly = true,
                    Icon = ""
                }
            },
            {
                ProfileCacheManager.ProfileType.Teleport, new ProfileSectionConfig
                {
                    Label = "Teleport Profile",
                    CreateButtonText = "Create New Teleport Profile",
                    EmptyMessage = "No teleport profiles found in project.",
                    InvalidTypeMessage = "is not a teleport-type profile",
                    CreateProfileType = typeof(AutoHandsTeleportProfile),
                    DefaultAssetName = "TeleportProfile",
                    IsAutoHandsOnly = true,
                    Icon = "🚀 "
                }
            }
        };
    }

    /// <summary>
    /// Gets or sets the selected profile for a given type
    /// </summary>
    public InteractionProfile GetSelectedProfile(ProfileCacheManager.ProfileType type)
    {
        return _selectedProfiles.TryGetValue(type, out var profile) ? profile : null;
    }

    public void SetSelectedProfile(ProfileCacheManager.ProfileType type, InteractionProfile profile)
    {
        _selectedProfiles[type] = profile;
    }

    /// <summary>
    /// Gets the scroll position for external state persistence
    /// </summary>
    public Vector2 ScrollPosition
    {
        get => _scrollPosition;
        set => _scrollPosition = value;
    }

    /// <summary>
    /// Main draw method for the Configure tab
    /// </summary>
    public void Draw()
    {
        EditorGUILayout.LabelField("Profile Configuration", VRTrainingEditorStyles.HeaderStyle);
        EditorGUILayout.Space(5);

        // Framework compatibility notice
        DrawFrameworkNotice();
        EditorGUILayout.Space(5);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // Draw all profile sections using the generic method
        DrawProfileSection(ProfileCacheManager.ProfileType.Grab);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Knob);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Snap);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Tool);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Valve);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Screw);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Turn);
        EditorGUILayout.Space(10);

        DrawProfileSection(ProfileCacheManager.ProfileType.Teleport);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // Create default profiles button
        if (GUILayout.Button("Create All Default Profiles", GUILayout.Height(30)))
        {
            CreateDefaultProfiles();
        }
    }

    /// <summary>
    /// Generic profile section drawing - consolidates 7 nearly identical sections into one method
    /// </summary>
    private void DrawProfileSection(ProfileCacheManager.ProfileType profileType)
    {
        if (!_sectionConfigs.TryGetValue(profileType, out var config))
        {
            EditorGUILayout.HelpBox($"Unknown profile type: {profileType}", MessageType.Error);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"{config.Icon}{config.Label}", VRTrainingEditorStyles.SubHeaderStyle);

        var currentProfile = _selectedProfiles[profileType];

        // Profile ObjectField
        var newProfile = EditorGUILayout.ObjectField(
            "Profile Asset", currentProfile, typeof(InteractionProfile), false) as InteractionProfile;

        // Validate the selected profile
        if (newProfile != null && _profileCacheManager.IsValidProfile(newProfile, profileType))
        {
            _selectedProfiles[profileType] = newProfile;
        }
        else if (newProfile != null && !_profileCacheManager.IsValidProfile(newProfile, profileType))
        {
            EditorUtility.DisplayDialog("Invalid Profile Type",
                $"The selected profile '{newProfile.name}' {config.InvalidTypeMessage}.", "OK");
        }

        // Show profile selection UI if no profile is selected
        if (_selectedProfiles[profileType] == null)
        {
            var cachedProfiles = _profileCacheManager.GetCachedProfiles(profileType);

            if (cachedProfiles != null && cachedProfiles.Count > 0)
            {
                EditorGUILayout.LabelField("Available Profiles:", EditorStyles.miniLabel);
                foreach (var profile in cachedProfiles)
                {
                    if (profile != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string frameworkType = GetProfileFrameworkType(profile, config.IsAutoHandsOnly);
                        EditorGUILayout.LabelField($"  • {profile.name} {frameworkType}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            _selectedProfiles[profileType] = profile;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                if (config.IsAutoHandsOnly && profileType == ProfileCacheManager.ProfileType.Teleport)
                {
                    EditorGUILayout.HelpBox(config.EmptyMessage, MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField(config.EmptyMessage, EditorStyles.miniLabel);
                }
            }

            // Create and Refresh buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(config.CreateButtonText))
            {
                CreateNewProfile(config.CreateProfileType, config.DefaultAssetName, profileType);
            }
            if (GUILayout.Button("Refresh List", GUILayout.Width(80)))
            {
                _profileCacheManager?.RefreshCache(profileType);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // Edit button when profile is selected
            if (GUILayout.Button("Edit Profile"))
            {
                Selection.activeObject = _selectedProfiles[profileType];
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Gets the framework type display string for a profile
    /// </summary>
    private string GetProfileFrameworkType(InteractionProfile profile, bool isAutoHandsOnly)
    {
        if (profile == null) return "[Unknown]";

        if (isAutoHandsOnly)
            return "[AutoHands]";

        string typeName = profile.GetType().Name;
        if (typeName.Contains("AutoHands"))
            return "[AutoHands]";
        else if (typeName.Contains("XRI") ||
                 profile is GrabProfile ||
                 profile is KnobProfile ||
                 profile is SnapProfile ||
                 profile is ToolProfile ||
                 profile is ValveProfile ||
                 profile is ScrewProfile)
            return "[XRI]";
        else
            return "[Unknown]";
    }

    /// <summary>
    /// Creates a new profile asset
    /// </summary>
    private void CreateNewProfile(Type profileType, string defaultName, ProfileCacheManager.ProfileType cacheType)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Profile",
            defaultName,
            "asset",
            "Save the interaction profile asset");

        if (!string.IsNullOrEmpty(path))
        {
            var profile = ScriptableObject.CreateInstance(profileType) as InteractionProfile;
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();

            // Auto-select the newly created profile
            _selectedProfiles[cacheType] = profile;
            Selection.activeObject = profile;

            // Refresh the cache to include the new profile
            _profileCacheManager?.RefreshCache(cacheType);
        }
    }

    /// <summary>
    /// Creates all default profiles in the Resources folder
    /// </summary>
    private void CreateDefaultProfiles()
    {
        string folderPath = "Assets/VRTrainingKit/Resources";

        // Create folders if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/VRTrainingKit"))
        {
            AssetDatabase.CreateFolder("Assets", "VRTrainingKit");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/VRTrainingKit", "Resources");
        }

        // Create Grab Profile
        if (_selectedProfiles[ProfileCacheManager.ProfileType.Grab] == null)
        {
            GrabProfile grabProfile = ScriptableObject.CreateInstance<GrabProfile>();
            grabProfile.profileName = "Default Grab";
            grabProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabProfile.trackPosition = true;
            grabProfile.trackRotation = true;
            grabProfile.throwOnDetach = true;

            AssetDatabase.CreateAsset(grabProfile, $"{folderPath}/DefaultGrabProfile.asset");
            _selectedProfiles[ProfileCacheManager.ProfileType.Grab] = grabProfile;
        }

        // Create Knob Profile
        if (_selectedProfiles[ProfileCacheManager.ProfileType.Knob] == null)
        {
            KnobProfile knobProfile = ScriptableObject.CreateInstance<KnobProfile>();
            knobProfile.profileName = "Default Knob";
            knobProfile.rotationAxis = KnobProfile.RotationAxis.Y;
            knobProfile.useLimits = true;
            knobProfile.minAngle = -90f;
            knobProfile.maxAngle = 180f;
            knobProfile.useSpring = true;
            knobProfile.springValue = 0f;
            knobProfile.damper = 1f;
            knobProfile.targetPosition = 0f;
            knobProfile.bounceMinVelocity = 0.2f;
            knobProfile.contactDistance = 0f;
            knobProfile.useHapticFeedback = true;
            knobProfile.colliderType = ColliderType.Box;

            AssetDatabase.CreateAsset(knobProfile, $"{folderPath}/DefaultKnobProfile.asset");
            _selectedProfiles[ProfileCacheManager.ProfileType.Knob] = knobProfile;
        }

        // Create Snap Profile
        if (_selectedProfiles[ProfileCacheManager.ProfileType.Snap] == null)
        {
            SnapProfile snapProfile = ScriptableObject.CreateInstance<SnapProfile>();
            snapProfile.profileName = "Default Snap";
            snapProfile.socketRadius = 0.1f;
            snapProfile.socketActive = true;
            snapProfile.showInteractableHoverMeshes = true;

            AssetDatabase.CreateAsset(snapProfile, $"{folderPath}/DefaultSnapProfile.asset");
            _selectedProfiles[ProfileCacheManager.ProfileType.Snap] = snapProfile;
        }

        // Create Tool Profile
        if (_selectedProfiles[ProfileCacheManager.ProfileType.Tool] == null)
        {
            ToolProfile toolProfile = ScriptableObject.CreateInstance<ToolProfile>();
            toolProfile.profileName = "Default Tool";
            toolProfile.rotationAxis = Vector3.up;
            toolProfile.tightenAngle = 90f;
            toolProfile.loosenAngle = 90f;
            toolProfile.tightenThreshold = 90f;
            toolProfile.loosenThreshold = 45f;
            toolProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            toolProfile.trackPosition = true;
            toolProfile.trackRotation = true;

            AssetDatabase.CreateAsset(toolProfile, $"{folderPath}/DefaultToolProfile.asset");
            _selectedProfiles[ProfileCacheManager.ProfileType.Tool] = toolProfile;
        }

        // Create Valve Profile
        if (_selectedProfiles[ProfileCacheManager.ProfileType.Valve] == null)
        {
            ValveProfile valveProfile = ScriptableObject.CreateInstance<ValveProfile>();
            valveProfile.profileName = "Default Valve";
            valveProfile.rotationAxis = Vector3.up;
            valveProfile.tightenThreshold = 180f;
            valveProfile.loosenThreshold = 180f;
            valveProfile.angleTolerance = 10f;
            valveProfile.compatibleSocketTags = new string[] { "valve_socket" };
            valveProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            valveProfile.trackPosition = true;
            valveProfile.trackRotation = true;
            valveProfile.rotationDampening = 3f;
            valveProfile.dampeningSpeed = 8f;

            AssetDatabase.CreateAsset(valveProfile, $"{folderPath}/DefaultValveProfile.asset");
            _selectedProfiles[ProfileCacheManager.ProfileType.Valve] = valveProfile;
        }

        // Create Screw Profile
        if (_selectedProfiles[ProfileCacheManager.ProfileType.Screw] == null)
        {
            ScrewProfile screwProfile = ScriptableObject.CreateInstance<ScrewProfile>();
            screwProfile.profileName = "Default Screw";
            screwProfile.rotationAxis = Vector3.up;
            screwProfile.tightenThreshold = 180f;
            screwProfile.loosenThreshold = 180f;
            screwProfile.angleTolerance = 10f;
            screwProfile.compatibleSocketTags = new string[] { "valve_socket", "screw_socket" };
            screwProfile.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            screwProfile.trackPosition = true;
            screwProfile.trackRotation = true;
            screwProfile.rotationDampening = 3f;
            screwProfile.dampeningSpeed = 8f;

            AssetDatabase.CreateAsset(screwProfile, $"{folderPath}/DefaultScrewProfile.asset");
            _selectedProfiles[ProfileCacheManager.ProfileType.Screw] = screwProfile;
        }

        AssetDatabase.SaveAssets();

        // Refresh all caches
        _profileCacheManager?.RefreshAllCaches();

        EditorUtility.DisplayDialog("Profiles Created",
            "Default profiles have been created in Assets/VRTrainingKit/Resources", "OK");
    }

    /// <summary>
    /// Draws framework compatibility notice
    /// </summary>
    private void DrawFrameworkNotice()
    {
        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var frameworkDisplayName = VRFrameworkDetector.GetFrameworkDisplayName(detectedFramework);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var originalColor = GUI.color;

        switch (detectedFramework)
        {
            case VRFramework.XRI:
                EditorGUILayout.LabelField("✅ XRI Framework Detected", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Current profiles are compatible with your XRI setup.", EditorStyles.wordWrappedLabel);
                break;

            case VRFramework.AutoHands:
                GUI.color = new Color(1f, 0.8f, 0f); // Orange
                EditorGUILayout.LabelField("⚠️ AutoHands Framework Detected", EditorStyles.boldLabel);
                GUI.color = originalColor;

                EditorGUILayout.LabelField(
                    "Current profiles are XRI-based. AutoHands profiles will be available in Phase 2.",
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Use VR Training > Framework Validator for detailed framework analysis.", MessageType.Info);
                break;

            case VRFramework.None:
                GUI.color = new Color(1f, 0.5f, 0.5f); // Light red
                EditorGUILayout.LabelField("❌ No VR Framework Detected", EditorStyles.boldLabel);
                GUI.color = originalColor;

                EditorGUILayout.LabelField(
                    "No VR framework found in scene. Add an XR Origin (XRI) or AutoHandPlayer (AutoHands) to use profiles.",
                    EditorStyles.wordWrappedLabel);
                break;

            default:
                EditorGUILayout.LabelField($"Framework: {frameworkDisplayName}", EditorStyles.boldLabel);
                break;
        }

        EditorGUILayout.EndVertical();
    }
}
#endif

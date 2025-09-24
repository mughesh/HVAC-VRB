// VRFrameworkManager.cs
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// ScriptableObject for managing VR framework preferences and settings
/// Provides centralized framework selection and configuration
/// </summary>
[CreateAssetMenu(fileName = "VRFrameworkManager", menuName = "VR Training/Framework Manager", order = 1)]
public class VRFrameworkManager : ScriptableObject
{
    [Header("Framework Settings")]
    [Tooltip("Preferred VR framework for this project")]
    public VRFramework preferredFramework = VRFramework.XRI;

    [Tooltip("Automatically detect framework based on scene components")]
    public bool autoDetect = true;

    [Header("Framework Override")]
    [Tooltip("Force a specific framework regardless of detection")]
    public bool forceFramework = false;

    [Tooltip("Framework to force when forceFramework is enabled")]
    public VRFramework forcedFramework = VRFramework.XRI;

    [Header("Validation Settings")]
    [Tooltip("Show warnings when framework mismatch is detected")]
    public bool showFrameworkMismatchWarnings = true;

    [Tooltip("Automatically validate framework setup on scene load")]
    public bool validateOnSceneLoad = true;

    /// <summary>
    /// Singleton instance of the VRFrameworkManager
    /// </summary>
    private static VRFrameworkManager _instance;

    /// <summary>
    /// Gets the singleton instance of VRFrameworkManager
    /// Creates a default instance if none exists
    /// </summary>
    public static VRFrameworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to load from Resources first
                _instance = Resources.Load<VRFrameworkManager>("VRFrameworkManager");

                if (_instance == null)
                {
                    // Create a default instance if none found
                    _instance = CreateInstance<VRFrameworkManager>();
                    _instance.preferredFramework = VRFramework.XRI;
                    _instance.autoDetect = true;

                    Debug.LogWarning("[VRFrameworkManager] No VRFrameworkManager found in Resources. Using default settings. " +
                                   "Create one via: Assets > Create > VR Training > Framework Manager");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Gets the active VR framework based on current settings
    /// </summary>
    /// <returns>The active VR framework</returns>
    public VRFramework GetActiveFramework()
    {
        // If force framework is enabled, return the forced framework
        if (forceFramework)
        {
            Debug.Log($"[VRFrameworkManager] Using forced framework: {GetFrameworkDisplayName(forcedFramework)}");
            return forcedFramework;
        }

        // If auto-detect is enabled, detect from scene
        if (autoDetect)
        {
            var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();

            // If no framework detected, fall back to preferred framework
            if (detectedFramework == VRFramework.None)
            {
                Debug.LogWarning($"[VRFrameworkManager] No framework detected in scene. Using preferred framework: {GetFrameworkDisplayName(preferredFramework)}");
                return preferredFramework;
            }

            Debug.Log($"[VRFrameworkManager] Auto-detected framework: {GetFrameworkDisplayName(detectedFramework)}");
            return detectedFramework;
        }

        // Use preferred framework
        Debug.Log($"[VRFrameworkManager] Using preferred framework: {GetFrameworkDisplayName(preferredFramework)}");
        return preferredFramework;
    }

    /// <summary>
    /// Checks if there's a mismatch between preferred and detected frameworks
    /// </summary>
    /// <returns>True if there's a mismatch</returns>
    public bool HasFrameworkMismatch()
    {
        if (!autoDetect) return false;

        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var activeFramework = GetActiveFramework();

        return detectedFramework != VRFramework.None && detectedFramework != activeFramework;
    }

    /// <summary>
    /// Gets framework mismatch information
    /// </summary>
    /// <returns>Mismatch details or null if no mismatch</returns>
    public string GetFrameworkMismatchInfo()
    {
        if (!HasFrameworkMismatch()) return null;

        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var activeFramework = GetActiveFramework();

        return $"Framework mismatch detected!\n" +
               $"Scene contains: {GetFrameworkDisplayName(detectedFramework)}\n" +
               $"Manager using: {GetFrameworkDisplayName(activeFramework)}";
    }

    /// <summary>
    /// Validates the current framework setup
    /// </summary>
    /// <returns>Validation result with details</returns>
    public FrameworkValidationResult ValidateCurrentSetup()
    {
        var activeFramework = GetActiveFramework();
        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var isValid = VRFrameworkDetector.ValidateFrameworkSetup();
        var hasMismatch = HasFrameworkMismatch();

        var result = new FrameworkValidationResult
        {
            activeFramework = activeFramework,
            detectedFramework = detectedFramework,
            isValid = isValid,
            hasMismatch = hasMismatch,
            frameworkInfo = VRFrameworkDetector.GetFrameworkInfo()
        };

        // Add warnings and errors
        if (hasMismatch && showFrameworkMismatchWarnings)
        {
            result.warnings.Add(GetFrameworkMismatchInfo());
        }

        if (!isValid)
        {
            result.errors.Add($"Invalid {GetFrameworkDisplayName(detectedFramework)} setup detected");
        }

        if (detectedFramework == VRFramework.None)
        {
            result.warnings.Add("No VR framework detected in scene. Some features may not work.");
        }

        return result;
    }

    /// <summary>
    /// Sets the preferred framework and saves the asset
    /// </summary>
    /// <param name="framework">Framework to set as preferred</param>
    public void SetPreferredFramework(VRFramework framework)
    {
        preferredFramework = framework;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif

        Debug.Log($"[VRFrameworkManager] Preferred framework set to: {GetFrameworkDisplayName(framework)}");
    }

    /// <summary>
    /// Helper method to get framework display name
    /// </summary>
    private string GetFrameworkDisplayName(VRFramework framework)
    {
        return VRFrameworkDetector.GetFrameworkDisplayName(framework);
    }

    /// <summary>
    /// Called when the asset is loaded
    /// </summary>
    private void OnEnable()
    {
        if (validateOnSceneLoad)
        {
            // Validate setup when manager is loaded
            var validation = ValidateCurrentSetup();
            if (validation.errors.Count > 0)
            {
                Debug.LogError($"[VRFrameworkManager] Framework validation errors: {string.Join(", ", validation.errors)}");
            }
            if (validation.warnings.Count > 0)
            {
                Debug.LogWarning($"[VRFrameworkManager] Framework validation warnings: {string.Join(", ", validation.warnings)}");
            }
        }
    }
}

/// <summary>
/// Result of framework validation
/// </summary>
[System.Serializable]
public class FrameworkValidationResult
{
    public VRFramework activeFramework;
    public VRFramework detectedFramework;
    public bool isValid;
    public bool hasMismatch;
    public string frameworkInfo;
    public System.Collections.Generic.List<string> errors = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();

    public bool HasIssues => errors.Count > 0 || warnings.Count > 0;
}
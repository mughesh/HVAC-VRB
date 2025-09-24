// VRFrameworkDetector.cs
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Enumeration of supported VR frameworks
/// </summary>
public enum VRFramework
{
    None,
    XRI,
    AutoHands
}

/// <summary>
/// Static utility class for detecting the active VR framework in the scene
/// Based on rig component presence: XR Origin (XRI) vs AutoHandPlayer (AutoHands)
/// </summary>
public static class VRFrameworkDetector
{
    /// <summary>
    /// Detects the current VR framework based on components present in the scene
    /// </summary>
    /// <returns>The detected VR framework</returns>
    public static VRFramework DetectCurrentFramework()
    {
        // Check for XRI framework - look for XR Origin component
        if (Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>() != null)
        {
            Debug.Log("[VRFrameworkDetector] XRI framework detected (XR Origin found)");
            return VRFramework.XRI;
        }

        // Check for AutoHands framework - look for AutoHandPlayer component
        var autoHandPlayer = Object.FindObjectOfType<MonoBehaviour>();
        if (autoHandPlayer != null && autoHandPlayer.GetType().Name == "AutoHandPlayer")
        {
            Debug.Log("[VRFrameworkDetector] AutoHands framework detected (AutoHandPlayer found)");
            return VRFramework.AutoHands;
        }

        Debug.Log("[VRFrameworkDetector] No VR framework detected");
        return VRFramework.None;
    }

    /// <summary>
    /// Gets a user-friendly name for the framework
    /// </summary>
    /// <param name="framework">The framework enum value</param>
    /// <returns>Human-readable framework name</returns>
    public static string GetFrameworkDisplayName(VRFramework framework)
    {
        return framework switch
        {
            VRFramework.XRI => "XR Interaction Toolkit",
            VRFramework.AutoHands => "Auto Hand",
            VRFramework.None => "No Framework Detected",
            _ => "Unknown Framework"
        };
    }

    /// <summary>
    /// Checks if the specified framework is currently available in the scene
    /// </summary>
    /// <param name="framework">Framework to check for</param>
    /// <returns>True if the framework is detected</returns>
    public static bool IsFrameworkAvailable(VRFramework framework)
    {
        return DetectCurrentFramework() == framework;
    }

    /// <summary>
    /// Gets detailed information about the current framework setup
    /// </summary>
    /// <returns>Detailed framework information</returns>
    public static string GetFrameworkInfo()
    {
        var currentFramework = DetectCurrentFramework();
        var displayName = GetFrameworkDisplayName(currentFramework);

        switch (currentFramework)
        {
            case VRFramework.XRI:
                var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                return $"{displayName}\nXR Origin: {xrOrigin?.name ?? "Not found"}";

            case VRFramework.AutoHands:
                var autoHandPlayer = Object.FindObjectOfType<MonoBehaviour>();
                if (autoHandPlayer != null && autoHandPlayer.GetType().Name == "AutoHandPlayer")
                {
                    return $"{displayName}\nAutoHandPlayer: {autoHandPlayer.name}";
                }
                return $"{displayName}\nAutoHandPlayer: Not found";

            case VRFramework.None:
                return $"{displayName}\nNo XR Origin or AutoHandPlayer components found in scene.";

            default:
                return $"{displayName}\nUnknown framework state.";
        }
    }

    /// <summary>
    /// Validates that the detected framework has all required components
    /// </summary>
    /// <returns>True if framework setup is valid</returns>
    public static bool ValidateFrameworkSetup()
    {
        var framework = DetectCurrentFramework();

        switch (framework)
        {
            case VRFramework.XRI:
                return ValidateXRISetup();

            case VRFramework.AutoHands:
                return ValidateAutoHandsSetup();

            case VRFramework.None:
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Validates XRI framework setup
    /// </summary>
    private static bool ValidateXRISetup()
    {
        var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null) return false;

        Debug.Log("[VRFrameworkDetector] XRI framework setup is valid - XR Origin found");
        return true;
    }

    /// <summary>
    /// Validates AutoHands framework setup
    /// </summary>
    private static bool ValidateAutoHandsSetup()
    {
        // Basic validation - check for AutoHandPlayer
        var autoHandPlayer = Object.FindObjectOfType<MonoBehaviour>();
        if (autoHandPlayer == null || autoHandPlayer.GetType().Name != "AutoHandPlayer")
        {
            return false;
        }

        // TODO: Add more comprehensive AutoHands validation when we have access to components
        Debug.Log("[VRFrameworkDetector] AutoHands framework setup is valid (basic check)");
        return true;
    }
}
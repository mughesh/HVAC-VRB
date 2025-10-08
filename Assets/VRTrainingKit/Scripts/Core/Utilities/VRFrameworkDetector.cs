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
    /// Detects the current VR framework based on component hierarchy analysis
    /// Uses multi-layered detection for accurate framework identification
    /// </summary>
    /// <returns>The detected VR framework</returns>
    public static VRFramework DetectCurrentFramework()
    {
        // Layer 1: Check for AutoHandPlayer (most reliable AutoHands identifier)
        if (HasAutoHandPlayerComponent())
        {
//            Debug.Log("[VRFrameworkDetector] AutoHands framework detected (AutoHandPlayer component found)");
            return VRFramework.AutoHands;
        }

        // Layer 2: Check for AutoHands Hand components (secondary AutoHands identifier)
        if (HasAutoHandsHandComponents())
        {
            Debug.Log("[VRFrameworkDetector] AutoHands framework detected (Hand components found)");
            return VRFramework.AutoHands;
        }

        // Layer 3: Check for XRI components
        if (HasXRIComponents())
        {
            Debug.Log("[VRFrameworkDetector] XRI framework detected (XRI components found)");
            return VRFramework.XRI;
        }

        Debug.Log("[VRFrameworkDetector] No VR framework detected");
        return VRFramework.None;
    }

    /// <summary>
    /// Checks for AutoHandPlayer component (primary AutoHands identifier)
    /// </summary>
    private static bool HasAutoHandPlayerComponent()
    {
        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        foreach (var component in allMonoBehaviours)
        {
            if (component != null && component.GetType().Name == "AutoHandPlayer")
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks for AutoHands Hand components (secondary AutoHands identifier)
    /// </summary>
    private static bool HasAutoHandsHandComponents()
    {
        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        int handComponentCount = 0;
        int handFollowCount = 0;

        foreach (var component in allMonoBehaviours)
        {
            if (component == null) continue;

            var typeName = component.GetType().Name;
            if (typeName == "Hand")
            {
                handComponentCount++;
            }
            else if (typeName == "HandFollow")
            {
                handFollowCount++;
            }

            // If we find multiple AutoHands-specific components, it's likely AutoHands
            if (handComponentCount >= 2 && handFollowCount >= 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks for XRI framework components
    /// </summary>
    private static bool HasXRIComponents()
    {
        // Check for XR Origin
        var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null) return false;

        // Verify ANY XR-related components exist alongside XR Origin
        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        bool hasXRInteractionComponents = false;

        foreach (var component in allMonoBehaviours)
        {
            if (component == null) continue;

            var typeName = component.GetType().Name;
            // Loosened check: Any XR Interactor, XRController, or XRI components
            if (typeName.Contains("XRInteractor") ||       // Catches XRDirectInteractor, XRRayInteractor, etc.
                typeName.Contains("XRController") ||       // Catches ActionBasedController, etc.
                typeName.Contains("XRInteractionGroup") ||
                typeName.Contains("XRGrab"))               // Catches XRGrabInteractable
            {
                hasXRInteractionComponents = true;
                Debug.Log($"[VRFrameworkDetector] Found XRI component: {typeName} on {component.name}");
                break;
            }
        }

        return hasXRInteractionComponents;
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
            case VRFramework.AutoHands:
                var info = $"{displayName}\n";

                // Check AutoHandPlayer
                var autoHandPlayer = FindAutoHandPlayerComponent();
                if (autoHandPlayer != null)
                {
                    info += $"AutoHandPlayer: {autoHandPlayer.name}\n";
                }

                // Check Hand components
                var handComponents = GetAutoHandsHandComponentInfo();
                if (handComponents.handCount > 0)
                {
                    info += $"Hand Components: {handComponents.handCount} found\n";
                    info += $"HandFollow Components: {handComponents.handFollowCount} found\n";
                }

                return info.TrimEnd();

            case VRFramework.XRI:
                var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                var xriInfo = $"{displayName}\nXR Origin: {xrOrigin?.name ?? "Not found"}\n";

                // Check XRI interaction components
                var xriComponentCount = GetXRIComponentCount();
                xriInfo += $"XRI Interaction Components: {xriComponentCount} found";

                return xriInfo;

            case VRFramework.None:
                return $"{displayName}\nNo AutoHandPlayer, Hand, or XRI components found in scene.";

            default:
                return $"{displayName}\nUnknown framework state.";
        }
    }

    /// <summary>
    /// Finds the AutoHandPlayer component in the scene
    /// </summary>
    private static MonoBehaviour FindAutoHandPlayerComponent()
    {
        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        foreach (var component in allMonoBehaviours)
        {
            if (component != null && component.GetType().Name == "AutoHandPlayer")
            {
                return component;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets information about AutoHands Hand components
    /// </summary>
    private static (int handCount, int handFollowCount) GetAutoHandsHandComponentInfo()
    {
        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        int handCount = 0;
        int handFollowCount = 0;

        foreach (var component in allMonoBehaviours)
        {
            if (component == null) continue;

            var typeName = component.GetType().Name;
            if (typeName == "Hand")
            {
                handCount++;
            }
            else if (typeName == "HandFollow")
            {
                handFollowCount++;
            }
        }

        return (handCount, handFollowCount);
    }

    /// <summary>
    /// Counts XRI interaction components
    /// </summary>
    private static int GetXRIComponentCount()
    {
        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        int count = 0;

        foreach (var component in allMonoBehaviours)
        {
            if (component == null) continue;

            var typeName = component.GetType().Name;
            if (typeName.Contains("XRPokeInteractor") ||
                typeName.Contains("NearFarInteractor") ||
                typeName.Contains("XRInteractionGroup") ||
                typeName.Contains("ControllerInputActionManager"))
            {
                count++;
            }
        }

        return count;
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
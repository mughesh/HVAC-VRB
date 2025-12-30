// RuntimeMonitorSettings.cs
// Simple settings script to control Runtime Monitor tab visibility
// Add this to any GameObject in your scene to enable/disable the Runtime Monitor tab

using UnityEngine;

/// <summary>
/// Controls visibility of the Runtime Monitor tab in Sequence Builder Setup Assistant
/// Add this component to any GameObject in your scene to enable the tab
/// Remove or disable this component to hide the tab
/// </summary>
public class RuntimeMonitorSettings : MonoBehaviour
{
    [Header("Runtime Monitor Visibility")]
    [Tooltip("Enable this to show the Runtime Monitor tab in the Setup Assistant window")]
    public bool showRuntimeMonitorTab = true;

    [Header("Info")]
    [TextArea(3, 5)]
    public string info = "The Runtime Monitor tab will be visible in the Sequence Builder Setup Assistant window only when this component exists in the scene and 'showRuntimeMonitorTab' is enabled.";

    // Singleton pattern for easy access from editor
    private static RuntimeMonitorSettings instance;

    public static RuntimeMonitorSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RuntimeMonitorSettings>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Ensure only one instance exists
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("[RuntimeMonitorSettings] Multiple RuntimeMonitorSettings detected. Only one should exist in the scene.");
        }
    }

    /// <summary>
    /// Check if Runtime Monitor tab should be visible
    /// Called from VRInteractionSetupWindow editor script
    /// </summary>
    public static bool IsRuntimeMonitorEnabled()
    {
        var settings = Instance;
        return settings != null && settings.enabled && settings.showRuntimeMonitorTab;
    }
}

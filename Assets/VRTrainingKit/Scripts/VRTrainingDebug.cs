// VRTrainingDebug.cs
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Simple debug control for VR Training Kit components
/// </summary>
public class VRTrainingDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enable debug logging for all VR Training Kit components")]
    public bool enableDebugLogging = false;
    
    [Tooltip("Enable detailed validation logging")]
    public bool enableValidationLogging = false;
    
    [Tooltip("Enable event flow logging")]
    public bool enableEventLogging = false;
    
    // Static instance for easy access
    private static VRTrainingDebug _instance;
    public static VRTrainingDebug Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<VRTrainingDebug>();
                if (_instance == null)
                {
                    // Create a default instance if none exists
                    GameObject debugObj = new GameObject("VRTrainingDebug");
                    _instance = debugObj.AddComponent<VRTrainingDebug>();
                    _instance.enableDebugLogging = false; // Default to off
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // Ensure singleton
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        
        // Persist across scenes if needed
        // DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Log general debug messages
    /// </summary>
    public static void Log(string message)
    {
        if (Instance.enableDebugLogging)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Log validation-specific messages
    /// </summary>
    public static void LogValidation(string message)
    {
        if (Instance.enableValidationLogging)
        {
            Debug.Log($"[VALIDATION] {message}");
        }
    }
    
    /// <summary>
    /// Log event flow messages
    /// </summary>
    public static void LogEvent(string message)
    {
        if (Instance.enableEventLogging)
        {
            Debug.Log($"[EVENT] {message}");
        }
    }
    
    /// <summary>
    /// Always log warnings (can't be disabled)
    /// </summary>
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }
    
    /// <summary>
    /// Always log errors (can't be disabled)
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}
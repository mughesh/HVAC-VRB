// VRSetupTabBase.cs
// Base class for VR Training Setup window tabs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Base class for VR Training Setup window tabs
/// Provides common lifecycle methods and styling access
/// </summary>
public abstract class VRSetupTabBase
{
    protected VRInteractionSetupWindow window;
    
    // Common styles
    protected GUIStyle headerStyle;
    protected GUIStyle subHeaderStyle;
    protected GUIStyle successStyle;
    protected GUIStyle warningStyle;
    protected GUIStyle errorStyle;
    
    public VRSetupTabBase(VRInteractionSetupWindow window)
    {
        this.window = window;
        InitializeStyles();
    }
    
    /// <summary>
    /// Initialize GUI styles for this tab
    /// </summary>
    protected virtual void InitializeStyles()
    {
        headerStyle = new GUIStyle()
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 5, 5)
        };
        
        subHeaderStyle = new GUIStyle()
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 3, 3)
        };
        
        successStyle = new GUIStyle()
        {
            normal = { textColor = new Color(0.3f, 1f, 0.3f) },
            fontStyle = FontStyle.Bold
        };
        
        warningStyle = new GUIStyle()
        {
            normal = { textColor = new Color(1f, 0.7f, 0.2f) },
            fontStyle = FontStyle.Bold
        };
        
        errorStyle = new GUIStyle()
        {
            normal = { textColor = new Color(1f, 0.3f, 0.3f) },
            fontStyle = FontStyle.Bold
        };
    }
    
    /// <summary>
    /// Called when tab is enabled/shown
    /// </summary>
    public virtual void OnEnable()
    {
    }
    
    /// <summary>
    /// Called when tab is disabled/hidden
    /// </summary>
    public virtual void OnDisable()
    {
    }
    
    /// <summary>
    /// Called when entering/exiting play mode
    /// </summary>
    public virtual void OnPlayModeStateChanged(PlayModeStateChange state)
    {
    }
    
    /// <summary>
    /// Draw the tab GUI
    /// </summary>
    public abstract void DrawTab();
    
    /// <summary>
    /// Cleanup resources when window is closed
    /// </summary>
    public virtual void Cleanup()
    {
    }
}

#endif

// VRTrainingEditorStyles.cs
// Centralized GUI styles for VR Training Kit editor windows
// Part of Phase 1: Infrastructure refactoring

#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Provides centralized, lazily-initialized GUI styles for VR Training Kit editor windows.
/// This class extracts style definitions from VRInteractionSetupWindow to enable sharing across multiple UI classes.
/// </summary>
public static class VRTrainingEditorStyles
{
    // Cached style instances
    private static GUIStyle _headerStyle;
    private static GUIStyle _subHeaderStyle;
    private static GUIStyle _successStyle;
    private static GUIStyle _warningStyle;
    private static GUIStyle _errorStyle;

    // Track initialization state
    private static bool _initialized = false;

    /// <summary>
    /// Large bold header style (14pt, bold, white text)
    /// Used for main section headers
    /// </summary>
    public static GUIStyle HeaderStyle
    {
        get
        {
            EnsureInitialized();
            return _headerStyle;
        }
    }

    /// <summary>
    /// Medium bold header style (12pt, bold, white text)
    /// Used for subsection headers
    /// </summary>
    public static GUIStyle SubHeaderStyle
    {
        get
        {
            EnsureInitialized();
            return _subHeaderStyle;
        }
    }

    /// <summary>
    /// Success message style (green text)
    /// Used for positive status indicators
    /// </summary>
    public static GUIStyle SuccessStyle
    {
        get
        {
            EnsureInitialized();
            return _successStyle;
        }
    }

    /// <summary>
    /// Warning message style (yellow text)
    /// Used for warning status indicators
    /// </summary>
    public static GUIStyle WarningStyle
    {
        get
        {
            EnsureInitialized();
            return _warningStyle;
        }
    }

    /// <summary>
    /// Error message style (red text)
    /// Used for error status indicators
    /// </summary>
    public static GUIStyle ErrorStyle
    {
        get
        {
            EnsureInitialized();
            return _errorStyle;
        }
    }

    /// <summary>
    /// Ensures styles are initialized. Call this at the start of OnGUI if you need
    /// to guarantee styles are ready, or access properties directly which auto-initialize.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized) return;

        InitializeStyles();
        _initialized = true;
    }

    /// <summary>
    /// Forces re-initialization of all styles.
    /// Useful if Unity's skin changes or after domain reload.
    /// </summary>
    public static void Reinitialize()
    {
        _initialized = false;
        EnsureInitialized();
    }

    private static void InitializeStyles()
    {
        _headerStyle = new GUIStyle()
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 5, 5)
        };

        _subHeaderStyle = new GUIStyle()
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            padding = new RectOffset(5, 5, 3, 3)
        };

        _successStyle = new GUIStyle()
        {
            normal = { textColor = Color.green },
            padding = new RectOffset(5, 5, 2, 2)
        };

        _warningStyle = new GUIStyle()
        {
            normal = { textColor = Color.yellow },
            padding = new RectOffset(5, 5, 2, 2)
        };

        _errorStyle = new GUIStyle()
        {
            normal = { textColor = Color.red },
            padding = new RectOffset(5, 5, 2, 2)
        };
    }
}
#endif

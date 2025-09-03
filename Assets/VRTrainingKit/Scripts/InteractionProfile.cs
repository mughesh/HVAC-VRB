// InteractionProfile.cs
using UnityEngine;

/// <summary>
/// Hand system types for interaction profiles
/// </summary>
public enum HandSystemType
{
    XRI,        // Unity XR Interaction Toolkit (default)
    AutoHand,   // AutoHand physics-based system
    Auto        // Automatically detect based on project setup
}

/// <summary>
/// Base class for all interaction profiles
/// Enhanced to support both XRI and AutoHand systems
/// </summary>
public abstract class InteractionProfile : ScriptableObject
{
    [Header("Base Settings")]
    public string profileName = "New Profile";
    public Color gizmoColor = Color.cyan;
    
    [Header("Hand System")]
    [Tooltip("Which hand interaction system to use - XRI (abstract) or AutoHand (physics-based)")]
    public HandSystemType handSystem = HandSystemType.XRI;
    
    [Header("AutoHand Settings")]
    [Tooltip("Only used when Hand System is AutoHand")]
    public bool showAutoHandSettings = false;
    
    /// <summary>
    /// Main method - applies components based on selected hand system
    /// </summary>
    public void ApplyToGameObject(GameObject target)
    {
        switch (handSystem)
        {
            case HandSystemType.XRI:
                ApplyXRIComponents(target);
                break;
            case HandSystemType.AutoHand:
                ApplyAutoHandComponents(target);
                break;
            case HandSystemType.Auto:
                // Auto-detect available systems and choose best option
                if (IsAutoHandAvailable())
                    ApplyAutoHandComponents(target);
                else
                    ApplyXRIComponents(target);
                break;
        }
        
        // Always apply common components (Rigidbody, Colliders, etc.)
        ApplyCommonComponents(target);
    }
    
    /// <summary>
    /// Validates GameObject for the selected hand system
    /// </summary>
    public bool ValidateGameObject(GameObject target)
    {
        switch (handSystem)
        {
            case HandSystemType.XRI:
                return ValidateForXRI(target);
            case HandSystemType.AutoHand:
                return ValidateForAutoHand(target);
            case HandSystemType.Auto:
                return ValidateForXRI(target) || ValidateForAutoHand(target);
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Apply XRI-specific components (default behavior)
    /// </summary>
    protected abstract void ApplyXRIComponents(GameObject target);
    
    /// <summary>
    /// Apply AutoHand-specific components (new functionality)
    /// </summary>
    protected abstract void ApplyAutoHandComponents(GameObject target);
    
    /// <summary>
    /// Apply common components needed by both systems
    /// </summary>
    protected virtual void ApplyCommonComponents(GameObject target) { }
    
    /// <summary>
    /// Validate GameObject for XRI system
    /// </summary>
    protected abstract bool ValidateForXRI(GameObject target);
    
    /// <summary>
    /// Validate GameObject for AutoHand system
    /// </summary>
    protected abstract bool ValidateForAutoHand(GameObject target);
    
    /// <summary>
    /// Check if AutoHand is available in the project
    /// </summary>
    public virtual bool IsAutoHandAvailable()
    {
        // Try multiple methods to detect AutoHand
        
        // Method 1: Try exact namespace and assembly
        var autoHandType = System.Type.GetType("Autohand.Hand, Assembly-CSharp");
        if (autoHandType != null)
        {
            Debug.Log("[InteractionProfile] Found AutoHand via Assembly-CSharp");
            return true;
        }
        
        // Method 2: Try without specifying assembly
        autoHandType = System.Type.GetType("Autohand.Hand");
        if (autoHandType != null)
        {
            Debug.Log("[InteractionProfile] Found AutoHand via default assembly");
            return true;
        }
        
        // Method 3: Search through all loaded assemblies
        try
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "Hand" && type.Namespace == "Autohand")
                    {
                        Debug.Log($"[InteractionProfile] Found AutoHand Hand class in assembly: {assembly.GetName().Name}");
                        return true;
                    }
                    if (type.Name == "Grabbable" && type.Namespace == "Autohand")
                    {
                        Debug.Log($"[InteractionProfile] Found AutoHand Grabbable class in assembly: {assembly.GetName().Name}");
                        return true;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[InteractionProfile] Error searching assemblies for AutoHand: {e.Message}");
        }
        
        // Method 4: Try UnityEngine assembly (in case AutoHand is there)
        autoHandType = System.Type.GetType("Autohand.Hand, UnityEngine");
        if (autoHandType != null)
        {
            Debug.Log("[InteractionProfile] Found AutoHand via UnityEngine assembly");
            return true;
        }
        
        Debug.LogWarning("[InteractionProfile] AutoHand not detected - tried multiple detection methods");
        return false;
    }
    
    /// <summary>
    /// Get AutoHand type by name, searching through all assemblies
    /// </summary>
    public virtual System.Type GetAutoHandType(string typeName)
    {
        // Method 1: Try exact namespace and assembly
        var type = System.Type.GetType($"Autohand.{typeName}, Assembly-CSharp");
        if (type != null) return type;
        
        // Method 2: Try without specifying assembly
        type = System.Type.GetType($"Autohand.{typeName}");
        if (type != null) return type;
        
        // Method 3: Search through all loaded assemblies
        try
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var assemblyType in assembly.GetTypes())
                {
                    if (assemblyType.Name == typeName && assemblyType.Namespace == "Autohand")
                    {
                        Debug.Log($"[InteractionProfile] Found {typeName} in assembly: {assembly.GetName().Name}");
                        return assemblyType;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[InteractionProfile] Error searching for {typeName}: {e.Message}");
        }
        
        Debug.LogError($"[InteractionProfile] Could not find AutoHand type: {typeName}");
        return null;
    }
    
    /// <summary>
    /// Get display name for current hand system
    /// </summary>
    public string GetHandSystemDisplayName()
    {
        switch (handSystem)
        {
            case HandSystemType.XRI: return "XR Interaction Toolkit";
            case HandSystemType.AutoHand: return "AutoHand Physics";
            case HandSystemType.Auto: return "Auto-Detect";
            default: return "Unknown";
        }
    }
}

// Helper enum for collider types (shared across profiles)
public enum ColliderType
{
    Box,
    Sphere,
    Capsule,
    Mesh,
    None
}
// InteractionProfile.cs
using UnityEngine;

/// <summary>
/// Base class for all interaction profiles
/// </summary>
public abstract class InteractionProfile : ScriptableObject
{
    [Header("Base Settings")]
    public string profileName = "New Profile";
    public Color gizmoColor = Color.cyan;
    
    public abstract void ApplyToGameObject(GameObject target);
    public abstract bool ValidateGameObject(GameObject target);
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
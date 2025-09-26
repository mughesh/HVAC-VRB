// AutoHandsInteractionProfile.cs
// Base class for all AutoHands interaction profiles
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Abstract base class for all AutoHands interaction profiles
/// Provides framework-specific utilities and validation for AutoHands components
/// </summary>
public abstract class AutoHandsInteractionProfile : InteractionProfile
{
    [Header("AutoHands Framework Settings")]
    [Tooltip("Enable detailed AutoHands component logging")]
    public bool enableAutoHandsDebugLogging = false;

    /// <summary>
    /// Override to ensure this profile only works with AutoHands framework
    /// </summary>
    public sealed override bool ValidateGameObject(GameObject target)
    {
        if (target == null)
        {
            LogError("Target GameObject is null");
            return false;
        }

        // Check if AutoHands framework is active
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        if (currentFramework != VRFramework.AutoHands)
        {
            LogWarning($"AutoHands profile used but current framework is: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");
        }

        // Call AutoHands-specific validation
        return ValidateAutoHandsGameObject(target);
    }

    /// <summary>
    /// AutoHands-specific validation - implement in derived classes
    /// </summary>
    /// <param name="target">Target GameObject to validate</param>
    /// <returns>True if valid for AutoHands interaction</returns>
    protected abstract bool ValidateAutoHandsGameObject(GameObject target);

    /// <summary>
    /// Helper method to check if a GameObject has a specific AutoHands component
    /// Uses reflection-based detection to avoid assembly dependencies
    /// </summary>
    /// <param name="obj">GameObject to check</param>
    /// <param name="componentName">Name of AutoHands component (e.g. "Grabbable", "PlacePoint")</param>
    /// <returns>True if component found</returns>
    protected bool HasAutoHandsComponent(GameObject obj, string componentName)
    {
        if (obj == null) return false;

        var components = obj.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component != null && component.GetType().Name == componentName)
            {
                LogDebug($"✅ Found {componentName} component on {obj.name}");
                return true;
            }
        }

        LogDebug($"❌ No {componentName} component found on {obj.name}");
        return false;
    }

    /// <summary>
    /// Helper method to add AutoHands component by name using reflection
    /// Returns the added component or null if failed
    /// </summary>
    /// <param name="obj">GameObject to add component to</param>
    /// <param name="componentName">Name of AutoHands component to add</param>
    /// <returns>Added component or null if failed</returns>
    protected Component AddAutoHandsComponent(GameObject obj, string componentName)
    {
        if (obj == null)
        {
            LogError($"Cannot add {componentName} - GameObject is null");
            return null;
        }

        // Try to find the component type
        var componentType = System.Type.GetType(componentName);
        if (componentType == null)
        {
            // Try common AutoHands namespaces
            string[] namespaces = { "Autohand", "AutoHand" };
            foreach (var ns in namespaces)
            {
                componentType = System.Type.GetType($"{ns}.{componentName}");
                if (componentType != null) break;
            }
        }

        if (componentType == null)
        {
            LogError($"Could not find AutoHands component type: {componentName}");
            return null;
        }

        var addedComponent = obj.AddComponent(componentType);
        LogDebug($"✅ Added {componentName} component to {obj.name}");
        return addedComponent;
    }

    /// <summary>
    /// Helper method to ensure Rigidbody exists with AutoHands-appropriate settings
    /// </summary>
    /// <param name="obj">Target GameObject</param>
    /// <param name="isKinematic">Whether rigidbody should be kinematic</param>
    /// <returns>The Rigidbody component</returns>
    protected Rigidbody EnsureRigidbody(GameObject obj, bool isKinematic = false)
    {
        if (obj == null) return null;

        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            LogDebug($"✅ Added Rigidbody to {obj.name}");
        }

        // AutoHands typically works better with these settings
        rb.isKinematic = isKinematic;
        rb.useGravity = !isKinematic;

        return rb;
    }

    /// <summary>
    /// Helper method to ensure Collider exists
    /// </summary>
    /// <param name="obj">Target GameObject</param>
    /// <param name="colliderType">Type of collider to create if none exists</param>
    /// <returns>True if collider exists or was created</returns>
    protected bool EnsureCollider(GameObject obj, ColliderType colliderType = ColliderType.Box)
    {
        if (obj == null) return false;

        var existingCollider = obj.GetComponent<Collider>();
        if (existingCollider != null)
        {
            LogDebug($"✅ Collider already exists on {obj.name}");
            return true;
        }

        return AddCollider(obj, colliderType);
    }

    /// <summary>
    /// Helper method to add specific collider type
    /// </summary>
    private bool AddCollider(GameObject obj, ColliderType type)
    {
        if (obj == null) return false;

        switch (type)
        {
            case ColliderType.Box:
                var boxCollider = obj.AddComponent<BoxCollider>();
                LogDebug($"✅ Added BoxCollider to {obj.name}");
                return true;

            case ColliderType.Sphere:
                var sphereCollider = obj.AddComponent<SphereCollider>();
                LogDebug($"✅ Added SphereCollider to {obj.name}");
                return true;

            case ColliderType.Capsule:
                var capsuleCollider = obj.AddComponent<CapsuleCollider>();
                LogDebug($"✅ Added CapsuleCollider to {obj.name}");
                return true;

            case ColliderType.Mesh:
                var meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.convex = true; // Required for AutoHands physics
                LogDebug($"✅ Added MeshCollider (convex) to {obj.name}");
                return true;

            case ColliderType.None:
                LogDebug($"No collider added to {obj.name} (None specified)");
                return true;

            default:
                LogError($"Unknown collider type: {type}");
                return false;
        }
    }

    /// <summary>
    /// Logging helper methods with AutoHands prefix
    /// </summary>
    protected void LogDebug(string message)
    {
        if (enableAutoHandsDebugLogging)
        {
            Debug.Log($"[AutoHands-{GetType().Name}] {message}");
        }
    }

    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[AutoHands-{GetType().Name}] {message}");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"[AutoHands-{GetType().Name}] {message}");
    }
}
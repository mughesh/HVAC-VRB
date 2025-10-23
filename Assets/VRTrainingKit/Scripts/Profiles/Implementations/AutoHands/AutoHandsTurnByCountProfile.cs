// AutoHandsTurnByCountProfile.cs
// AutoHands implementation for turn-by-count interactions (e.g., allen keys)
// Workflow: grab → snap to socket → turn N times → complete
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for turn-by-count interactions with tools like allen keys
/// Requires object to be snapped to socket first, then tracks rotation count
/// Uses Grabbable for grabbing and PlacePoint for socket snapping
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsTurnByCountProfile", menuName = "VR Training/AutoHands/Turn By Count Profile", order = 5)]
public class AutoHandsTurnByCountProfile : AutoHandsInteractionProfile
{
    [Header("Turn Requirements")]
    [Tooltip("Number of full 360° turns required (e.g., 0.5 = 180°, 1.0 = 360°, 2.5 = 900°)")]
    [Range(0.1f, 10f)]
    public float requiredTurnCount = 0.5f;

    [Tooltip("Axis around which object rotates (e.g., Vector3.forward for Z-axis)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Tolerance in degrees for completion (±)")]
    [Range(1f, 45f)]
    public float angleTolerance = 10f;

    [Tooltip("Direction of rotation required")]
    public RotationDirection rotationDirection = RotationDirection.Clockwise;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this tool can work with")]
    public string[] compatibleSocketTags = {"turn_socket"};

    [Tooltip("Specific socket objects this tool works with")]
    public GameObjectReference[] specificCompatibleSockets;

    [Tooltip("Use specific socket objects instead of tag-based matching")]
    public bool requireSpecificSockets = false;

    [Header("AutoHands Grab Settings")]
    [Tooltip("Grab behavior type")]
    public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;

    [Tooltip("Which hands can grab")]
    public Autohand.HandType handType = Autohand.HandType.both;

    [Tooltip("Only one hand can grab at a time")]
    public bool singleHandOnly = false;

    [Tooltip("Throw power multiplier")]
    public float throwPower = 1.5f;

    [Tooltip("Joint break force")]
    public float jointBreakForce = 3500f;

    [Header("Physics Settings")]
    [Tooltip("Angular drag applied when released")]
    [Range(0f, 10f)]
    public float rotationDampening = 5f;

    [Header("HingeJoint Settings (Applied at Runtime when Snapped)")]
    [Tooltip("Auto configure connected anchor (recommended)")]
    public bool autoConfigureConnectedAnchor = true;

    [Tooltip("Use spring for smooth rotation resistance")]
    public bool useSpring = false;

    [Tooltip("Spring force value (only used if useSpring is true)")]
    [Range(0f, 1000f)]
    public float springValue = 0f;

    [Tooltip("Spring damper for smooth motion (friction/resistance)")]
    [Range(0f, 100f)]
    public float springDamper = 0.1f;

    [Tooltip("Bounce minimum velocity for joint limits")]
    [Range(0f, 10f)]
    public float bounceMinVelocity = 0.2f;

    [Tooltip("Contact distance for joint limits")]
    [Range(0f, 1f)]
    public float contactDistance = 0f;

    [Header("Socket Positioning")]
    [Tooltip("Maximum distance from socket center to consider positioning complete")]
    [Range(0.001f, 0.1f)]
    public float positionTolerance = 0.01f;

    [Tooltip("Maximum velocity to consider object stabilized in socket")]
    [Range(0.001f, 0.5f)]
    public float velocityThreshold = 0.05f;

    [Tooltip("Maximum time to wait for socket positioning before timeout")]
    [Range(1f, 10f)]
    public float positioningTimeout = 3f;

    [Header("Interaction Feel")]
    [Tooltip("Haptic feedback intensity during rotation")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.3f;

    [Tooltip("Show progress indicator during turning")]
    public bool showProgressIndicator = true;

    [Header("Collider Settings")]
    [Tooltip("Type of collider to add")]
    public ColliderType colliderType = ColliderType.Box;

    /// <summary>
    /// Rotation direction options
    /// </summary>
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise,
        Either
    }

    /// <summary>
    /// Apply AutoHands Grabbable component for turn-by-count interaction
    /// Note: The controller component (AutoHandsTurnByCountController) will be added in Phase 2
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying AutoHands TurnByCount components to: {target.name}");

        // 1. Add Grabbable component (for AutoHands grabbing)
        var grabbable = target.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Autohand.Grabbable>();
            LogDebug($"✅ Added Grabbable component to {target.name}");
        }

        // Configure Grabbable for turn interaction
        ConfigureGrabbableComponent(grabbable);

        // 2. Add Rigidbody (required for physics, non-kinematic)
        Rigidbody rb = EnsureRigidbody(target, false);
        if (rb != null)
        {
            rb.useGravity = true;
            rb.angularDamping = rotationDampening;
            LogDebug($"✅ Configured Rigidbody (isKinematic=false, useGravity=true, angularDrag={rotationDampening}) on {target.name}");
        }

        // 3. Add collider (find mesh child or add to parent)
        GameObject meshChild = FindMeshChild(target);
        if (meshChild != null && meshChild.GetComponent<Collider>() == null)
        {
            AddCollider(meshChild, colliderType);
        }
        else if (meshChild == null && target.GetComponent<Collider>() == null)
        {
            AddCollider(target, colliderType);
        }

        // NOTE: AutoHandsTurnByCountController will be added in Phase 2
        // For now, we just configure the basic grabbable object

        LogDebug($"✅ Successfully configured AutoHands turn-by-count object on {target.name}");
        LogDebug($"✅ {target.name} is ready for grab→snap→turn workflow (controller to be added in Phase 2)");
    }

    /// <summary>
    /// Configure Grabbable component for turn interaction
    /// </summary>
    private void ConfigureGrabbableComponent(Autohand.Grabbable grabbable)
    {
        LogDebug($"Configuring Grabbable for turn interaction");

        // Core grab settings
        grabbable.grabType = grabType;
        grabbable.handType = handType;
        grabbable.singleHandOnly = singleHandOnly;

        // Physics settings
        SetPropertySafely(grabbable, "throwPower", throwPower);
        SetPropertySafely(grabbable, "jointBreakForce", jointBreakForce);

        // Allow both position and rotation tracking
        SetPropertySafely(grabbable, "instantGrab", false);

        LogDebug($"✅ Configured Grabbable: grabType={grabType}, handType={handType}");
    }

    /// <summary>
    /// Helper to safely set Grabbable properties via reflection
    /// </summary>
    private void SetPropertySafely<T>(Autohand.Grabbable grabbable, string propertyName, T value)
    {
        try
        {
            var property = grabbable.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(grabbable, value);
                LogDebug($"✅ Set {propertyName} = {value}");
                return;
            }

            var field = grabbable.GetType().GetField(propertyName);
            if (field != null)
            {
                field.SetValue(grabbable, value);
                LogDebug($"✅ Set {propertyName} = {value}");
                return;
            }

            LogDebug($"⚠️ Property/field '{propertyName}' not found on Grabbable");
        }
        catch (System.Exception ex)
        {
            LogWarning($"⚠️ Could not set {propertyName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Find mesh child for collider placement
    /// </summary>
    private GameObject FindMeshChild(GameObject parent)
    {
        MeshRenderer meshRenderer = parent.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.gameObject != parent)
        {
            return meshRenderer.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Add collider to target
    /// </summary>
    private void AddCollider(GameObject target, ColliderType type)
    {
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        Bounds bounds = renderer != null ? renderer.bounds : new Bounds(Vector3.zero, Vector3.one);

        switch (type)
        {
            case ColliderType.Box:
                BoxCollider boxCol = target.AddComponent<BoxCollider>();
                if (renderer != null)
                {
                    boxCol.center = target.transform.InverseTransformPoint(bounds.center);
                    boxCol.size = bounds.size;
                }
                LogDebug($"✅ Added BoxCollider to {target.name}");
                break;

            case ColliderType.Sphere:
                SphereCollider sphereCol = target.AddComponent<SphereCollider>();
                if (renderer != null)
                {
                    sphereCol.center = target.transform.InverseTransformPoint(bounds.center);
                    sphereCol.radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / 2f;
                }
                LogDebug($"✅ Added SphereCollider to {target.name}");
                break;

            case ColliderType.Capsule:
                CapsuleCollider capsuleCol = target.AddComponent<CapsuleCollider>();
                if (renderer != null)
                {
                    capsuleCol.center = target.transform.InverseTransformPoint(bounds.center);
                    capsuleCol.height = bounds.size.y;
                    capsuleCol.radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
                }
                LogDebug($"✅ Added CapsuleCollider to {target.name}");
                break;

            case ColliderType.Mesh:
                MeshCollider meshCol = target.AddComponent<MeshCollider>();
                MeshFilter meshFilter = target.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshCol.sharedMesh = meshFilter.sharedMesh;
                    meshCol.convex = true;
                }
                LogDebug($"✅ Added MeshCollider (convex) to {target.name}");
                break;
        }
    }

    /// <summary>
    /// Validate that target object is suitable for AutoHands turn-by-count interaction
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        if (!target.CompareTag("turn"))
        {
            LogError($"GameObject {target.name} must have 'turn' tag for AutoHandsTurnByCountProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands turn-by-count interaction");
        return true;
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure turn count is reasonable
        requiredTurnCount = Mathf.Clamp(requiredTurnCount, 0.1f, 10f);
        angleTolerance = Mathf.Clamp(angleTolerance, 1f, 45f);

        // Ensure tolerance is reasonable compared to turn requirements
        float totalDegrees = requiredTurnCount * 360f;
        if (angleTolerance >= totalDegrees * 0.5f)
            angleTolerance = totalDegrees * 0.2f;
    }
    #endif
}

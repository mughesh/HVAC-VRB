// AutoHandsScrewProfile.cs (formerly AutoHandsValveProfile.cs)
// AutoHands implementation of screw interactions using Grabbable + AutoHandsScrewController
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for screw interactions with complex state machine
/// Workflow: grab → snap → tighten → loosen → remove
/// Uses Grabbable for grabbing and PlacePoint for socket snapping
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsScrewProfile", menuName = "VR Training/AutoHands/Screw Profile", order = 4)]
public class AutoHandsScrewProfile : AutoHandsInteractionProfile
{
    [Header("Screw Mechanics")]
    [Tooltip("Axis around which screw rotates (e.g., Vector3.up for Y-axis)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Degrees of rotation required to tighten screw")]
    [Range(10f, 360f)]
    public float tightenThreshold = 50f;

    [Tooltip("Degrees of reverse rotation required to loosen screw")]
    [Range(10f, 360f)]
    public float loosenThreshold = 90f;

    [Tooltip("Angle tolerance for threshold completion")]
    [Range(1f, 15f)]
    public float angleTolerance = 5f;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this screw can work with (supports both 'valve_socket' and 'screw_socket' for backwards compatibility)")]
    public string[] compatibleSocketTags = {"valve_socket", "screw_socket"};

    [Tooltip("Specific socket objects this screw works with")]
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
    [Tooltip("Angular drag applied when screw is released to stop spinning")]
    [Range(0f, 10f)]
    public float rotationDampening = 5f;

    [Tooltip("How quickly to apply dampening (higher = more responsive)")]
    [Range(1f, 20f)]
    public float dampeningSpeed = 10f;

    [Header("HingeJoint Settings")]
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

    [Tooltip("Target position for spring (rotation angle)")]
    [Range(-180f, 180f)]
    public float springTargetPosition = 0f;

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
    [Tooltip("Rotation speed multiplier when screw is locked")]
    [Range(0.1f, 3.0f)]
    public float lockedRotationSpeed = 1.0f;

    [Tooltip("Haptic feedback intensity during rotation")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Material when screw is in loose state (needs tightening)")]
    public Material looseMaterial;

    [Tooltip("Material when screw is in tight state (properly secured)")]
    public Material tightMaterial;

    [Tooltip("Show progress indicator during rotation")]
    public bool showProgressIndicator = true;

    [Header("Collider Settings")]
    [Tooltip("Type of collider to add")]
    public ColliderType colliderType = ColliderType.Box;

    /// <summary>
    /// Apply AutoHands Grabbable + AutoHandsScrewController for screw interaction
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying AutoHands Screw components to: {target.name}");

        // 1. Add Grabbable component (for AutoHands grabbing)
        var grabbable = target.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Autohand.Grabbable>();
            LogDebug($"✅ Added Grabbable component to {target.name}");
        }

        // Configure Grabbable for screw interaction
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

        // 4. Add AutoHandsScrewControllerV2 (Clean HingeJoint-based screw controller)
        AutoHandsScrewControllerV2 screwController = target.GetComponent<AutoHandsScrewControllerV2>();
        if (screwController == null)
        {
            screwController = target.AddComponent<AutoHandsScrewControllerV2>();
            LogDebug($"✅ Added AutoHandsScrewControllerV2 to {target.name}");
        }

        // Configure AutoHandsScrewControllerV2 with this profile
        ConfigureScrewController(screwController);

        LogDebug($"✅ Successfully configured AutoHands screw on {target.name}");
        LogDebug($"✅ Screw {target.name} is ready for grab→snap→tighten→loosen→remove workflow");
    }

    /// <summary>
    /// Configure Grabbable component for screw interaction
    /// </summary>
    private void ConfigureGrabbableComponent(Autohand.Grabbable grabbable)
    {
        LogDebug($"Configuring Grabbable for screw interaction");

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
    /// Configure AutoHandsScrewControllerV2 with a temporary ScrewProfile
    /// We create a ScrewProfile ScriptableObject at runtime to pass settings to AutoHandsScrewControllerV2
    /// </summary>
    private void ConfigureScrewController(AutoHandsScrewControllerV2 screwController)
    {
        // Create a temporary ScrewProfile to pass to AutoHandsScrewController
        ScrewProfile tempProfile = ScriptableObject.CreateInstance<ScrewProfile>();

        // Copy settings from AutoHandsScrewProfile to ScrewProfile
        tempProfile.rotationAxis = rotationAxis;
        tempProfile.tightenThreshold = tightenThreshold;
        tempProfile.loosenThreshold = loosenThreshold;
        tempProfile.angleTolerance = angleTolerance;
        tempProfile.compatibleSocketTags = compatibleSocketTags;
        tempProfile.specificCompatibleSockets = specificCompatibleSockets;
        tempProfile.requireSpecificSockets = requireSpecificSockets;
        tempProfile.rotationDampening = rotationDampening;
        tempProfile.dampeningSpeed = dampeningSpeed;
        tempProfile.positionTolerance = positionTolerance;
        tempProfile.velocityThreshold = velocityThreshold;
        tempProfile.positioningTimeout = positioningTimeout;
        tempProfile.lockedRotationSpeed = lockedRotationSpeed;
        tempProfile.hapticIntensity = hapticIntensity;
        tempProfile.looseMaterial = looseMaterial;
        tempProfile.tightMaterial = tightMaterial;
        tempProfile.showProgressIndicator = showProgressIndicator;
        tempProfile.colliderType = colliderType;

        // Copy HingeJoint settings
        tempProfile.autoConfigureConnectedAnchor = autoConfigureConnectedAnchor;
        tempProfile.useSpring = useSpring;
        tempProfile.springValue = springValue;
        tempProfile.springDamper = springDamper;
        tempProfile.springTargetPosition = springTargetPosition;
        tempProfile.bounceMinVelocity = bounceMinVelocity;
        tempProfile.contactDistance = contactDistance;

        // Configure AutoHandsScrewControllerV2 with the temp profile
        screwController.Configure(tempProfile);

        LogDebug($"✅ Configured AutoHandsScrewControllerV2 with settings from AutoHandsScrewProfile");
        LogDebug($"   - Rotation Axis: {rotationAxis}");
        LogDebug($"   - Tighten Threshold: {tightenThreshold}°");
        LogDebug($"   - Loosen Threshold: {loosenThreshold}°");
        LogDebug($"   - Angle Tolerance: {angleTolerance}°");
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
    /// Validate that target object is suitable for AutoHands screw interaction
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        // Dual tag support: accepts both "valve" and "screw" for backwards compatibility
        if (!target.CompareTag("valve") && !target.CompareTag("screw"))
        {
            LogError($"GameObject {target.name} must have 'valve' or 'screw' tag for AutoHandsScrewProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands screw interaction");
        return true;
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure thresholds are reasonable
        tightenThreshold = Mathf.Clamp(tightenThreshold, 10f, 360f);
        loosenThreshold = Mathf.Clamp(loosenThreshold, 10f, 360f);
        angleTolerance = Mathf.Clamp(angleTolerance, 1f, 15f);

        // Ensure tolerance is smaller than thresholds
        if (angleTolerance >= tightenThreshold * 0.5f)
            angleTolerance = tightenThreshold * 0.2f;
        if (angleTolerance >= loosenThreshold * 0.5f)
            angleTolerance = loosenThreshold * 0.2f;
    }
    #endif
}

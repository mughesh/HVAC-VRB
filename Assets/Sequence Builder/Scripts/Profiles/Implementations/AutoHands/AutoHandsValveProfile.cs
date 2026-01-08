// AutoHandsValveProfile.cs
// AutoHands implementation of valve interactions using Grabbable + AutoHandsValveController
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for valve interactions with complex state machine
/// Workflow: grab → snap → tighten → loosen → remove
/// Uses Grabbable for grabbing and PlacePoint for socket snapping
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsScrewProfile", menuName = "Sequence Builder/AutoHands/Screw Profile", order = 4)]
public class AutoHandsValveProfile : AutoHandsInteractionProfile
{
    [Header("Valve Mechanics")]
    [Tooltip("Axis around which valve rotates (e.g., Vector3.up for Y-axis)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Degrees of rotation required to tighten valve")]
    [Range(10f, 360f)]
    public float tightenThreshold = 50f;

    [Tooltip("Degrees of reverse rotation required to loosen valve")]
    [Range(10f, 360f)]
    public float loosenThreshold = 90f;

    [Tooltip("Angle tolerance for threshold completion")]
    [Range(1f, 15f)]
    public float angleTolerance = 5f;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this valve can work with")]
    public string[] compatibleSocketTags = { "valve_socket" };

    [Tooltip("Specific socket objects this valve works with")]
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
    [Tooltip("Angular drag applied when valve is released to stop spinning")]
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
    [Tooltip("Rotation speed multiplier when valve is locked")]
    [Range(0.1f, 3.0f)]
    public float lockedRotationSpeed = 1.0f;

    [Tooltip("Haptic feedback intensity during rotation")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Material when valve is in loose state (needs tightening)")]
    public Material looseMaterial;

    [Tooltip("Material when valve is in tight state (properly secured)")]
    public Material tightMaterial;

    [Tooltip("Show progress indicator during rotation")]
    public bool showProgressIndicator = true;

    [Header("Collider Settings")]
    [Tooltip("Type of collider to add")]
    public ColliderType colliderType = ColliderType.Box;

    /// <summary>
    /// Apply AutoHands Grabbable + AutoHandsValveController for valve interaction
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying AutoHands Valve components to: {target.name}");

        // 1. Add Grabbable component (for AutoHands grabbing)
        var grabbable = target.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Autohand.Grabbable>();
            LogDebug($"✅ Added Grabbable component to {target.name}");
        }

        // Configure Grabbable for valve interaction
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

        // 4. Add AutoHandsScrewControllerV2 (Clean HingeJoint-based screw controller used for valve compatibility)
        AutoHandsScrewControllerV2 valveController = target.GetComponent<AutoHandsScrewControllerV2>();
        if (valveController == null)
        {
            valveController = target.AddComponent<AutoHandsScrewControllerV2>();
            LogDebug($"✅ Added AutoHandsScrewControllerV2 to {target.name}");
        }

        // Configure AutoHandsScrewControllerV2 with this profile
        ConfigureValveController(valveController);

        LogDebug($"✅ Successfully configured AutoHands valve on {target.name}");
        LogDebug($"✅ Valve {target.name} is ready for grab→snap→tighten→loosen→remove workflow");
    }

    /// <summary>
    /// Configure Grabbable component for valve interaction
    /// </summary>
    private void ConfigureGrabbableComponent(Autohand.Grabbable grabbable)
    {
        LogDebug($"Configuring Grabbable for valve interaction");

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
    /// NOTE: This profile must persist as the controller stores a reference to it for runtime use
    /// </summary>
    private void ConfigureValveController(AutoHandsScrewControllerV2 valveController)
    {
        // Create a ScrewProfile instance to pass to AutoHandsScrewController
        // This profile will be stored by the controller and used at runtime
        ScrewProfile tempProfile = ScriptableObject.CreateInstance<ScrewProfile>();
        tempProfile.name = $"{valveController.gameObject.name}_ScrewProfile";

        // Copy settings from AutoHandsValveProfile to ScrewProfile
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
        // The controller will store this reference and use it at runtime
        valveController.Configure(tempProfile);

        LogDebug($"✅ Configured AutoHandsScrewControllerV2 with settings from AutoHandsValveProfile");
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
    /// Add collider to target (only if no collider exists)
    /// </summary>
    private void AddCollider(GameObject target, ColliderType type)
    {
        // BUGFIX: Check for existing collider to prevent stacking
        Collider existingCollider = target.GetComponent<Collider>();
        if (existingCollider != null)
        {
            LogDebug($"✅ Collider already exists on {target.name}, skipping collider creation");
            return;
        }

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
    /// Validate that target object is suitable for AutoHands valve interaction
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        // Support both 'valve' and 'screw' tags for backward compatibility
        if (!target.CompareTag("valve") && !target.CompareTag("screw"))
        {
            LogError($"GameObject {target.name} must have 'valve' or 'screw' tag for AutoHandsValveProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands valve interaction");
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

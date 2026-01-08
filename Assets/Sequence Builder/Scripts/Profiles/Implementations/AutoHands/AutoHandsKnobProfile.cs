// AutoHandsKnobProfile.cs
// AutoHands implementation of knob interactions using Grabbable + HingeJoint + KnobController
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for knob interactions using Grabbable with HingeJoint rotation constraints
/// Reuses XRI KnobController for angle tracking (framework-agnostic component)
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsKnobProfile", menuName = "Sequence Builder/AutoHands/Knob Profile", order = 3)]
public class AutoHandsKnobProfile : AutoHandsInteractionProfile
{
    public enum RotationAxis { X, Y, Z }

    [Header("Knob Rotation Settings")]
    [Tooltip("Axis of rotation for the knob")]
    public RotationAxis rotationAxis = RotationAxis.Y;

    [Tooltip("Minimum rotation angle in degrees")]
    public float minAngle = -90f;

    [Tooltip("Maximum rotation angle in degrees")]
    public float maxAngle = 180f;

    [Tooltip("Use angle limits to constrain rotation")]
    public bool useLimits = true;

    [Header("HingeJoint Settings")]
    [Tooltip("Auto configure connected anchor (recommended: enabled)")]
    public bool autoConfigureConnectedAnchor = true;

    [Tooltip("Use spring to return knob to target position")]
    public bool useSpring = true;

    [Tooltip("Spring force strength")]
    public float springValue = 0f;

    [Tooltip("Spring damping")]
    public float damper = 0.1f;

    [Tooltip("Target spring position")]
    public float targetPosition = 0f;

    [Header("Joint Limits")]
    [Tooltip("Minimum velocity for bounce on limits")]
    public float bounceMinVelocity = 0.2f;

    [Tooltip("Contact distance for limits")]
    public float contactDistance = 0f;

    [Header("AutoHands Grab Settings")]
    [Tooltip("Grab behavior type")]
    public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;

    [Tooltip("Which hands can grab")]
    public Autohand.HandType handType = Autohand.HandType.both;

    [Tooltip("Only one hand can grab at a time")]
    public bool singleHandOnly = false;

    [Tooltip("Throw power multiplier")]
    public float throwPower = 1f;

    [Tooltip("Joint break force")]
    public float jointBreakForce = 3500f;

    [Header("Interaction Settings")]
    [Tooltip("Rotation speed multiplier")]
    public float rotationSpeed = 1.0f;

    [Tooltip("Snap to angle increments on release")]
    public bool snapToAngles = false;

    [Tooltip("Angle increment for snapping (degrees)")]
    public float snapAngleIncrement = 15f;

    [Header("Feedback")]
    [Tooltip("Enable haptic feedback during rotation")]
    public bool useHapticFeedback = true;

    [Tooltip("Haptic feedback intensity (0-1)")]
    public float hapticIntensity = 0.3f;

    [Header("Collider Settings")]
    [Tooltip("Type of collider to add")]
    public ColliderType colliderType = ColliderType.Box;

    /// <summary>
    /// Apply AutoHands Grabbable + HingeJoint + KnobController for rotatable knob
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying AutoHands Knob components to: {target.name}");

        // 1. Add Grabbable component (for AutoHands grabbing)
        var grabbable = target.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Autohand.Grabbable>();
            LogDebug($"✅ Added Grabbable component to {target.name}");
        }

        // Configure Grabbable for knob interaction
        ConfigureGrabbableComponent(grabbable);

        // 2. Add Rigidbody (required for HingeJoint, must be non-kinematic)
        Rigidbody rb = EnsureRigidbody(target, false);
        if (rb != null)
        {
            rb.useGravity = true; // Gravity helps with joint stability
            LogDebug($"✅ Configured Rigidbody (isKinematic=false, useGravity=true) on {target.name}");
        }

        // 3. Add HingeJoint
        HingeJoint joint = target.GetComponent<HingeJoint>();
        if (joint == null)
        {
            joint = target.AddComponent<HingeJoint>();
            LogDebug($"✅ Added HingeJoint to {target.name}");
        }
        ConfigureHingeJoint(joint);

        // 4. Add collider (find mesh child or add to parent)
        GameObject meshChild = FindMeshChild(target);
        if (meshChild != null && meshChild.GetComponent<Collider>() == null)
        {
            AddCollider(meshChild, colliderType);
        }
        else if (meshChild == null && target.GetComponent<Collider>() == null)
        {
            AddCollider(target, colliderType);
        }

        // 5. Add AutoHandsKnobController (AutoHands-specific controller)
        AutoHandsKnobController knobController = target.GetComponent<AutoHandsKnobController>();
        if (knobController == null)
        {
            knobController = target.AddComponent<AutoHandsKnobController>();
            LogDebug($"✅ Added AutoHandsKnobController to {target.name}");
        }

        // Configure AutoHandsKnobController with this profile
        ConfigureKnobController(knobController);

        LogDebug($"✅ Successfully configured AutoHands knob on {target.name}");
    }

    /// <summary>
    /// Configure Grabbable component for knob interaction
    /// </summary>
    private void ConfigureGrabbableComponent(Autohand.Grabbable grabbable)
    {
        LogDebug($"Configuring Grabbable for knob interaction");

        // Core grab settings
        grabbable.grabType = grabType;
        grabbable.handType = handType;
        grabbable.singleHandOnly = singleHandOnly;

        // Physics settings
        SetPropertySafely(grabbable, "throwPower", throwPower);
        SetPropertySafely(grabbable, "jointBreakForce", jointBreakForce);

        // Knob-specific: we want rotation only, no translation
        SetPropertySafely(grabbable, "instantGrab", false);

        LogDebug($"✅ Configured Grabbable: grabType={grabType}, handType={handType}");
    }

    /// <summary>
    /// Configure HingeJoint (same logic as XRI KnobProfile)
    /// </summary>
    private void ConfigureHingeJoint(HingeJoint joint)
    {
        // Set axis based on rotation axis
        switch (rotationAxis)
        {
            case RotationAxis.X:
                joint.axis = Vector3.right;
                break;
            case RotationAxis.Y:
                joint.axis = Vector3.up;
                break;
            case RotationAxis.Z:
                joint.axis = Vector3.forward;
                break;
        }

        // Auto configure connected anchor
        joint.autoConfigureConnectedAnchor = autoConfigureConnectedAnchor;
        if (!autoConfigureConnectedAnchor)
        {
            joint.connectedAnchor = Vector3.zero;
        }

        // Configure spring
        joint.useSpring = useSpring;
        if (useSpring)
        {
            JointSpring spring = new JointSpring();
            spring.spring = springValue;
            spring.damper = damper;
            spring.targetPosition = targetPosition;
            joint.spring = spring;
        }

        // Configure limits
        joint.useLimits = useLimits;
        if (useLimits)
        {
            JointLimits limits = new JointLimits();
            limits.min = minAngle;
            limits.max = maxAngle;
            limits.bounceMinVelocity = bounceMinVelocity;
            limits.contactDistance = contactDistance;
            joint.limits = limits;
        }

        // Enable preprocessing for stability
        joint.enablePreprocessing = true;

        LogDebug($"✅ Configured HingeJoint: axis={rotationAxis}, limits=[{minAngle}° to {maxAngle}°], spring={useSpring}");
    }

    /// <summary>
    /// Configure AutoHandsKnobController with a temporary KnobProfile
    /// We create a KnobProfile ScriptableObject at runtime to pass settings to AutoHandsKnobController
    /// </summary>
    private void ConfigureKnobController(AutoHandsKnobController knobController)
    {
        // Create a temporary KnobProfile to pass to AutoHandsKnobController
        // AutoHandsKnobController expects KnobProfile, so we create one on the fly
        KnobProfile tempProfile = ScriptableObject.CreateInstance<KnobProfile>();

        // Copy settings from AutoHandsKnobProfile to KnobProfile
        tempProfile.rotationAxis = (KnobProfile.RotationAxis)((int)rotationAxis);
        tempProfile.minAngle = minAngle;
        tempProfile.maxAngle = maxAngle;
        tempProfile.useLimits = useLimits;
        tempProfile.autoConfigureConnectedAnchor = autoConfigureConnectedAnchor;
        tempProfile.useSpring = useSpring;
        tempProfile.springValue = springValue;
        tempProfile.damper = damper;
        tempProfile.targetPosition = targetPosition;
        tempProfile.bounceMinVelocity = bounceMinVelocity;
        tempProfile.contactDistance = contactDistance;
        tempProfile.rotationSpeed = rotationSpeed;
        tempProfile.snapToAngles = snapToAngles;
        tempProfile.snapAngleIncrement = snapAngleIncrement;
        tempProfile.useHapticFeedback = useHapticFeedback;
        tempProfile.hapticIntensity = hapticIntensity;
        tempProfile.colliderType = colliderType;

        // Configure AutoHandsKnobController with the temp profile
        knobController.Configure(tempProfile);

        LogDebug($"✅ Configured AutoHandsKnobController with settings from AutoHandsKnobProfile");
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
    /// Add collider to target (reuse from XRI KnobProfile logic)
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
    /// Validate that target object is suitable for AutoHands knob interaction
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        if (!target.CompareTag("knob"))
        {
            LogError($"GameObject {target.name} must have 'knob' tag for AutoHandsKnobProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands knob interaction");
        return true;
    }
}
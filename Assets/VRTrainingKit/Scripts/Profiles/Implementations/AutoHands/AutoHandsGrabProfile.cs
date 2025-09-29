// AutoHandsGrabProfile.cs
// AutoHands implementation of grab interactions using Grabbable component
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for grab interactions using Grabbable component
/// Phase 2 Implementation: Will configure AutoHands Grabbable with proper settings
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsGrabProfile", menuName = "VR Training/AutoHands/Grab Profile", order = 1)]
public class AutoHandsGrabProfile : AutoHandsInteractionProfile
{
    [Header("AutoHands Grab Settings")]
    [Tooltip("Grab behavior type - Default, HandToGrabbable, GrabbableToHand")]
    public Autohand.HandGrabType grabType = Autohand.HandGrabType.Default;

    [Tooltip("Grab pose type - Grab, Pinch")]
    public Autohand.HandGrabPoseType grabPoseType = Autohand.HandGrabPoseType.Grab;

    [Tooltip("Which hands can grab this object - both, left, right")]
    public Autohand.HandType handType = Autohand.HandType.both;

    [Tooltip("Whether only one hand can grab at a time")]
    public bool singleHandOnly = false;

    [Tooltip("Make children objects grabbable")]
    public bool makeChildrenGrabbable = false;

    [Header("Grab Behavior")]
    [Tooltip("Instant grab without transition")]
    public bool instantGrab = false;

    [Tooltip("Allow switching between hands while held")]
    public bool allowHeldSwapping = true;

    [Tooltip("Use gentle grab return behavior")]
    public bool useGentleGrab = false;

    [Tooltip("Maintain original grab position offset")]
    public bool maintainGrabOffset = true;

    [Tooltip("Ignore object weight when held")]
    public bool ignoreWeight = false;

    [Tooltip("Parent object to hand when grabbed")]
    public bool parentOnGrab = false;

    [Header("Hold Settings")]
    [Tooltip("Hold object without friction when grabbed")]
    public bool holdNoFriction = true;

    [Header("Hold Position/Rotation Offsets")]
    [Tooltip("Position offset while held")]
    public Vector3 holdPositionOffset = Vector3.zero;

    [Tooltip("Rotation offset while held")]
    public Vector3 holdRotationOffset = Vector3.zero;

    [Header("Physics Settings")]
    [Tooltip("Throw power multiplier when releasing")]
    public float throwPower = 1f;

    [Tooltip("Force required to break the grab joint")]
    public float jointBreakForce = 3500f;

    [Header("Advanced Settings")]
    [Tooltip("Grab priority when multiple objects available")]
    public float grabPriorityWeight = 1f;

    [Tooltip("Time to ignore release after grab")]
    public float ignoreReleaseTime = 0.5f;

    [Tooltip("Minimum drag while held")]
    public float minHeldDrag = 1.5f;

    [Tooltip("Minimum angular drag while held")]
    public float minHeldAngleDrag = 3f;

    [Tooltip("Minimum mass while held")]
    public float minHeldMass = 0.1f;

    [Tooltip("Maximum velocity while held")]
    public float maxHeldVelocity = 10f;

    [Header("Collider Settings")]
    [Tooltip("Type of collider to add if none exists")]
    public ColliderType colliderType = ColliderType.Box;

    /// <summary>
    /// Apply AutoHands Grabbable component and configure all settings
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying AutoHands Grabbable to: {target.name}");

        // Ensure Rigidbody exists (required for AutoHands physics)
        Rigidbody rb = EnsureRigidbody(target, false);

        // Ensure Collider exists (required for grab detection)
        EnsureCollider(target, colliderType);

        // Add or get Grabbable component using direct type reference
        var grabbable = target.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Autohand.Grabbable>();
            LogDebug($"✅ Added Grabbable component to {target.name}");
        }

        if (grabbable != null)
        {
            ConfigureGrabbableComponent(grabbable);
            LogDebug($"✅ Successfully configured Grabbable on {target.name}");
        }
        else
        {
            LogError($"❌ Failed to add Grabbable component to {target.name}");
        }
    }

    /// <summary>
    /// Configure the Grabbable component with profile settings using direct property assignment
    /// </summary>
    private void ConfigureGrabbableComponent(Autohand.Grabbable grabbable)
    {
        LogDebug($"Configuring Grabbable component: {grabbable.GetType().Name}");

        // Configure core grab settings - direct property assignment (no reflection needed!)
        grabbable.grabType = grabType;
        grabbable.grabPoseType = grabPoseType;
        grabbable.handType = handType;
        grabbable.singleHandOnly = singleHandOnly;

        // Configure grab behavior
        grabbable.instantGrab = instantGrab;
        grabbable.useGentleGrab = useGentleGrab;

        // Configure physics and advanced settings that we can safely set
        TrySetProperty(() => grabbable.jointBreakForce = jointBreakForce, nameof(grabbable.jointBreakForce));
        TrySetProperty(() => grabbable.throwPower = throwPower, nameof(grabbable.throwPower));

        // Set other properties using safe property setting (in case some don't exist in this version)
        SetPropertySafely(grabbable, "makeChildrenGrabbable", makeChildrenGrabbable);
        SetPropertySafely(grabbable, "allowHeldSwapping", allowHeldSwapping);
        SetPropertySafely(grabbable, "maintainGrabOffset", maintainGrabOffset);
        SetPropertySafely(grabbable, "ignoreWeight", ignoreWeight);
        SetPropertySafely(grabbable, "parentOnGrab", parentOnGrab);
        SetPropertySafely(grabbable, "holdNoFriction", holdNoFriction);
        SetPropertySafely(grabbable, "holdPositionOffset", holdPositionOffset);
        SetPropertySafely(grabbable, "holdRotationOffset", holdRotationOffset);
        SetPropertySafely(grabbable, "grabPriorityWeight", grabPriorityWeight);
        SetPropertySafely(grabbable, "ignoreReleaseTime", ignoreReleaseTime);
        SetPropertySafely(grabbable, "minHeldDrag", minHeldDrag);
        SetPropertySafely(grabbable, "minHeldAngleDrag", minHeldAngleDrag);
        SetPropertySafely(grabbable, "minHeldMass", minHeldMass);
        SetPropertySafely(grabbable, "maxHeldVelocity", maxHeldVelocity);

        LogDebug($"✅ Configured Grabbable: grabType={grabType}, poseType={grabPoseType}, handType={handType}");
    }

    /// <summary>
    /// Helper method to safely try setting a property with lambda
    /// </summary>
    private void TrySetProperty(System.Action setter, string propertyName)
    {
        try
        {
            setter();
            LogDebug($"✅ Set {propertyName}");
        }
        catch (System.Exception ex)
        {
            LogWarning($"⚠️ Could not set {propertyName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to safely set property using reflection as fallback
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

            LogDebug($"⚠️ Property/field '{propertyName}' not found (might not exist in this AutoHands version)");
        }
        catch (System.Exception ex)
        {
            LogWarning($"⚠️ Could not set {propertyName}: {ex.Message}");
        }
    }


    /// <summary>
    /// Validate that target object is suitable for AutoHands grab interaction
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        if (!target.CompareTag("grab"))
        {
            LogError($"GameObject {target.name} must have 'grab' tag for AutoHandsGrabProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands grab interaction");
        return true;
    }
}
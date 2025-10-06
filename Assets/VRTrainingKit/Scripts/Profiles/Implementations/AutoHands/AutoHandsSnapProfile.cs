// AutoHandsSnapProfile.cs
// AutoHands implementation of snap interactions using PlacePoint component
using UnityEngine;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for snap interactions using PlacePoint component
/// Configures PlacePoint component with validation, placement behavior, and events
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsSnapProfile", menuName = "VR Training/AutoHands/Snap Profile", order = 2)]
public class AutoHandsSnapProfile : AutoHandsInteractionProfile
{
    [Header("PlacePoint Shape Settings")]
    [Tooltip("Shape of the place point detection area - Sphere or Box")]
    public Autohand.PlacePointShape shapeType = Autohand.PlacePointShape.Sphere;

    [Tooltip("Radius of sphere detection area (relative to scale)")]
    public float placeRadius = 0.15f;

    [Tooltip("Size of box detection area (relative to scale)")]
    public Vector3 placeSize = new Vector3(0.15f, 0.15f, 0.15f);

    [Tooltip("Local offset of the detection area")]
    public Vector3 shapeOffset = Vector3.zero;

    [Header("Place Behavior")]
    [Tooltip("This will make the place point itself targetable for grab instead of just the object inside")]
    public bool grabbablePlacePoint = true;

    [Tooltip("Place object as soon as it enters radius (vs on release)")]
    public bool forcePlace = false;

    [Tooltip("Force hand to release on place when force place is called")]
    public bool forceHandRelease = true;

    [Tooltip("Parent placed object under this PlacePoint")]
    public bool parentOnPlace = true;

    [Tooltip("Match position when placing")]
    public bool matchPosition = true;

    [Tooltip("Match rotation when placing")]
    public bool matchRotation = true;

    [Header("Place Requirements")]
    [Tooltip("Only allow placement while object is being held")]
    public bool heldPlaceOnly = false;

    [Tooltip("Compare by object name or tag")]
    public Autohand.PlacePointNameType nameCompareType = Autohand.PlacePointNameType.tag;

    [Tooltip("Array of names/tags to allow (leave empty for any)")]
    public string[] placeNames = new string[] { "grab" };

    [Tooltip("Array of names/tags to prevent")]
    public string[] blacklistNames = new string[0];

    [Header("Advanced Settings")]
    [Tooltip("Make placed object kinematic")]
    public bool makePlacedKinematic = true;

    [Tooltip("Disable rigidbody on placed object")]
    public bool disableRigidbodyOnPlace = false;

    [Tooltip("Disable grab on placed object")]
    public bool disableGrabOnPlace = false;

    [Tooltip("Disable this place point after placement")]
    public bool disablePlacePointOnPlace = false;

    [Tooltip("Destroy object after placement")]
    public bool destroyObjectOnPlace = false;

    [Tooltip("Joint break force if using placedJointLink")]
    public float jointBreakForce = 1000f;

    [Header("Collider Settings")]
    [Tooltip("Type of trigger collider to create")]
    public ColliderType colliderType = ColliderType.Sphere;

    /// <summary>
    /// Apply AutoHands PlacePoint component and configure all settings
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying AutoHands PlacePoint to: {target.name}");

        // Ensure trigger collider exists for detection
        Collider triggerCollider = EnsureCollider(target, colliderType) ? target.GetComponent<Collider>() : null;
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            LogDebug($"✅ Configured trigger collider on {target.name}");
        }

        // Ensure Rigidbody exists (required for trigger detection, must be kinematic for snap points)
        Rigidbody rb = EnsureRigidbody(target, true);
        if (rb != null)
        {
            rb.useGravity = false;
            LogDebug($"✅ Configured kinematic Rigidbody on {target.name}");
        }

        // Add PlacePoint component using reflection (AutoHands component)
        Component placePointComponent = AddAutoHandsComponent(target, "PlacePoint");

        if (placePointComponent != null)
        {
            ConfigurePlacePointComponent(placePointComponent);
            LogDebug($"✅ Successfully configured PlacePoint on {target.name}");
        }
        else
        {
            LogError($"❌ Failed to add PlacePoint component to {target.name}");
        }
    }

    /// <summary>
    /// Configure the PlacePoint component with profile settings using reflection
    /// </summary>
    private void ConfigurePlacePointComponent(Component placePoint)
    {
        LogDebug($"Configuring PlacePoint component: {placePoint.GetType().Name}");

        // Configure shape settings
        SetPropertySafely(placePoint, "shapeType", shapeType);
        SetPropertySafely(placePoint, "placeRadius", placeRadius);
        SetPropertySafely(placePoint, "placeSize", placeSize);
        SetPropertySafely(placePoint, "shapeOffset", shapeOffset);

        // Configure place behavior
        SetPropertySafely(placePoint, "grabbablePlacePoint", grabbablePlacePoint);
        SetPropertySafely(placePoint, "forcePlace", forcePlace);
        SetPropertySafely(placePoint, "forceHandRelease", forceHandRelease);
        SetPropertySafely(placePoint, "parentOnPlace", parentOnPlace);
        SetPropertySafely(placePoint, "matchPosition", matchPosition);
        SetPropertySafely(placePoint, "matchRotation", matchRotation);

        // Configure place requirements
        SetPropertySafely(placePoint, "heldPlaceOnly", heldPlaceOnly);
        SetPropertySafely(placePoint, "nameCompareType", nameCompareType);
        SetPropertySafely(placePoint, "placeNames", placeNames);
        SetPropertySafely(placePoint, "blacklistNames", blacklistNames);

        // Configure advanced settings
        SetPropertySafely(placePoint, "makePlacedKinematic", makePlacedKinematic);
        SetPropertySafely(placePoint, "disableRigidbodyOnPlace", disableRigidbodyOnPlace);
        SetPropertySafely(placePoint, "disableGrabOnPlace", disableGrabOnPlace);
        SetPropertySafely(placePoint, "disablePlacePointOnPlace", disablePlacePointOnPlace);
        SetPropertySafely(placePoint, "destroyObjectOnPlace", destroyObjectOnPlace);
        SetPropertySafely(placePoint, "jointBreakForce", jointBreakForce);

        LogDebug($"✅ Configured PlacePoint: shape={shapeType}, radius={placeRadius}, placeNames={string.Join(",", placeNames)}");
    }

    /// <summary>
    /// Helper method to safely set property using reflection
    /// </summary>
    private void SetPropertySafely<T>(Component component, string propertyName, T value)
    {
        try
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value);
                LogDebug($"✅ Set {propertyName} = {value}");
                return;
            }

            var field = component.GetType().GetField(propertyName);
            if (field != null)
            {
                field.SetValue(component, value);
                LogDebug($"✅ Set {propertyName} = {value}");
                return;
            }

            LogDebug($"⚠️ Property/field '{propertyName}' not found on {component.GetType().Name}");
        }
        catch (System.Exception ex)
        {
            LogWarning($"⚠️ Could not set {propertyName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate that target object is suitable for AutoHands snap interaction
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        if (!target.CompareTag("snap"))
        {
            LogError($"GameObject {target.name} must have 'snap' tag for AutoHandsSnapProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands snap interaction");
        return true;
    }
}
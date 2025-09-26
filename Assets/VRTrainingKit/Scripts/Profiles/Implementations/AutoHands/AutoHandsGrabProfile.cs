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
    [Tooltip("Will be implemented in Phase 2")]
    public bool implementationPending = true;

    [Space]
    [Header("Planned Implementation (Phase 2)")]
    [Tooltip("Grab type for AutoHands Grabbable component")]
    public string plannedGrabType = "Default/HandToGrabbable/GrabbableToHand";

    [Tooltip("Physics settings for grabbable objects")]
    public string plannedPhysicsSettings = "Mass, Drag, Joint Break Force";

    [Tooltip("Collider configuration for grab detection")]
    public ColliderType plannedColliderType = ColliderType.Box;

    /// <summary>
    /// Phase 2 Implementation: Apply AutoHands Grabbable component and settings
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogWarning("AutoHandsGrabProfile.ApplyToGameObject() - Implementation pending (Phase 2)");

        // Phase 1: Just log what we would do
        LogDebug($"Would apply AutoHands Grabbable to: {target.name}");
        LogDebug($"Would ensure Rigidbody exists");
        LogDebug($"Would ensure Collider exists (type: {plannedColliderType})");
        LogDebug($"Would configure grab settings and physics");

        // Placeholder validation that components are ready
        if (!HasAutoHandsComponent(target, "Grabbable"))
        {
            LogDebug($"Would add Grabbable component to {target.name}");
        }

        EnsureRigidbody(target, false);
        EnsureCollider(target, plannedColliderType);
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
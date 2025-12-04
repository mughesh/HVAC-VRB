// AutoHandsTeleportProfile.cs
// AutoHands implementation for teleport destination points
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// AutoHands profile for configuring teleport destination points
/// Applies TeleportController component with position/rotation metadata
/// </summary>
[CreateAssetMenu(fileName = "AutoHandsTeleportProfile", menuName = "VR Training/AutoHands/Teleport Profile", order = 5)]
public class AutoHandsTeleportProfile : AutoHandsInteractionProfile
{
    [Header("Recentering Settings")]
    [Tooltip("Enable XRInput subsystem recentering after teleport")]
    public bool enableRecentering = true;

    [Tooltip("Delay before triggering recentering (seconds)")]
    [Range(0f, 2f)]
    public float recenteringDelay = 0.5f;

    [Header("Teleport Behavior")]
    [Tooltip("Visual fade duration during teleport (0 = instant)")]
    [Range(0f, 1f)]
    public float fadeDuration = 0.3f;

    [Tooltip("Maintain player's current Y rotation after teleport")]
    public bool preserveYRotation = false;

    [Header("Position/Rotation Offsets")]
    [Tooltip("Position offset from destination point")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Rotation offset from destination point (euler angles)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Visual Feedback")]
    [Tooltip("Show destination preview indicator in scene")]
    public bool showDestinationPreview = true;

    [Tooltip("Preview indicator color")]
    public Color previewColor = new Color(0, 1, 0, 0.5f); // Green semi-transparent

    [Tooltip("Preview indicator radius")]
    [Range(0.1f, 2f)]
    public float previewRadius = 0.5f;

    /// <summary>
    /// Apply TeleportController component to destination point
    /// </summary>
    public override void ApplyToGameObject(GameObject target)
    {
        LogDebug($"Applying TeleportController to: {target.name}");

        // Add TeleportController component
        var controller = target.GetComponent<TeleportController>();
        if (controller == null)
        {
            controller = target.AddComponent<TeleportController>();
            LogDebug($"✅ Added TeleportController to {target.name}");
        }

        // Configure controller with profile settings
        // Note: autoHandPlayerReference is now set directly on TeleportController component in scene
        controller.enableRecentering = enableRecentering;
        controller.recenteringDelay = recenteringDelay;
        controller.fadeDuration = fadeDuration;
        controller.preserveYRotation = preserveYRotation;
        controller.positionOffset = positionOffset;
        controller.rotationOffset = rotationOffset;
        controller.showDestinationPreview = showDestinationPreview;
        controller.previewColor = previewColor;
        controller.previewRadius = previewRadius;

        LogDebug($"✅ Configured TeleportController on {target.name}");
    }

    /// <summary>
    /// Validate teleport destination point
    /// </summary>
    protected override bool ValidateAutoHandsGameObject(GameObject target)
    {
        if (!target.CompareTag("teleportPoint"))
        {
            LogError($"GameObject {target.name} must have 'teleportPoint' tag for AutoHandsTeleportProfile");
            return false;
        }

        LogDebug($"✅ {target.name} is valid for AutoHands teleport interaction");
        return true;
    }
}

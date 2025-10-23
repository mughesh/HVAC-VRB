// TurnByCountProfile.cs
// Base profile for turn-by-count interactions (shared between XRI and AutoHands)
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Base profile for turn-by-count interactions (e.g., allen keys)
/// Used by AutoHandsTurnByCountController for runtime configuration
/// </summary>
[CreateAssetMenu(fileName = "TurnByCountProfile", menuName = "VR Training/Turn By Count Profile (Base)", order = 100)]
public class TurnByCountProfile : ScriptableObject
{
    [Header("Turn Requirements")]
    [Tooltip("Degrees of rotation that count as one 'turn' (e.g., 30° per turn, 45° per turn, etc.)")]
    [Range(5f, 360f)]
    public float degreesPerTurn = 30f;

    [Tooltip("Number of turns required (will be multiplied by degreesPerTurn)")]
    [Range(1, 100)]
    public int requiredTurnCount = 6;

    [Tooltip("Axis around which object rotates (e.g., Vector3.forward for Z-axis)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Tolerance in degrees for completion (±)")]
    [Range(1f, 45f)]
    public float angleTolerance = 5f;

    [Tooltip("Direction of rotation required")]
    public RotationDirection rotationDirection = RotationDirection.Clockwise;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this tool can work with")]
    public string[] compatibleSocketTags = {"turn_socket"};

    [Tooltip("Specific socket objects this tool works with")]
    public GameObjectReference[] specificCompatibleSockets;

    [Tooltip("Use specific socket objects instead of tag-based matching")]
    public bool requireSpecificSockets = false;

    [Header("Physics Settings")]
    [Tooltip("Angular drag applied when released")]
    [Range(0f, 10f)]
    public float rotationDampening = 5f;

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
    /// Calculate total degrees required (degreesPerTurn * requiredTurnCount)
    /// </summary>
    public float TotalDegreesRequired => degreesPerTurn * requiredTurnCount;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure values are reasonable
        degreesPerTurn = Mathf.Clamp(degreesPerTurn, 5f, 360f);
        requiredTurnCount = Mathf.Clamp(requiredTurnCount, 1, 100);
        angleTolerance = Mathf.Clamp(angleTolerance, 1f, 45f);

        // Ensure tolerance is reasonable compared to turn requirements
        float totalDegrees = TotalDegreesRequired;
        if (angleTolerance >= totalDegrees * 0.5f)
            angleTolerance = Mathf.Max(1f, totalDegrees * 0.2f);
    }
    #endif
}

// TeleportController.cs
// Runtime component for teleport destination points
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Controller component for teleport destination points
/// Stores configuration and provides teleport execution logic
/// Handles AutoHandPlayer positioning and XRInput recentering
/// </summary>
public class TeleportController : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Reference to AutoHandPlayer in scene (set by profile)")]
    public GameObject autoHandPlayerReference;

    [Header("Recentering Settings")]
    public bool enableRecentering = true;
    [Range(0f, 2f)]
    public float recenteringDelay = 0.5f;

    [Header("Teleport Behavior")]
    [Range(0f, 1f)]
    public float fadeDuration = 0.3f;
    public bool preserveYRotation = false;

    [Header("Offsets")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Visual Feedback")]
    public bool showDestinationPreview = true;
    public Color previewColor = new Color(0, 1, 0, 0.5f);
    [Range(0.1f, 2f)]
    public float previewRadius = 0.5f;

    // Runtime state
    private GameObject previewIndicator;
    private Coroutine recenteringCoroutine;

    void Start()
    {
        // Create preview indicator if enabled
        if (showDestinationPreview)
        {
            CreatePreviewIndicator();
        }

        // Validate AutoHandPlayer reference
        if (autoHandPlayerReference == null)
        {
            Debug.LogError($"[TeleportController] {name}: AutoHandPlayer reference is null! Teleport will not work.");
        }
    }

    void OnDestroy()
    {
        // Cleanup preview indicator
        if (previewIndicator != null)
        {
            Destroy(previewIndicator);
        }
    }

    /// <summary>
    /// Execute teleport to this destination point
    /// </summary>
    public void ExecuteTeleport()
    {
        if (autoHandPlayerReference == null)
        {
            Debug.LogError($"[TeleportController] Cannot teleport - AutoHandPlayer reference is null");
            return;
        }

        // Calculate final position/rotation
        Vector3 finalPosition = transform.position + transform.TransformDirection(positionOffset);
        Quaternion finalRotation = transform.rotation * Quaternion.Euler(rotationOffset);

        // Preserve Y rotation if enabled
        if (preserveYRotation)
        {
            float currentYRotation = autoHandPlayerReference.transform.eulerAngles.y;
            Vector3 eulerAngles = finalRotation.eulerAngles;
            eulerAngles.y = currentYRotation;
            finalRotation = Quaternion.Euler(eulerAngles);
        }

        Debug.Log($"[TeleportController] Teleporting to {name} at position {finalPosition}");

        // Execute teleport via AutoHandPlayer.SetPositionAndRotation()
        autoHandPlayerReference.transform.SetPositionAndRotation(finalPosition, finalRotation);

        // Trigger recentering if enabled
        if (enableRecentering)
        {
            if (recenteringCoroutine != null)
            {
                StopCoroutine(recenteringCoroutine);
            }
            recenteringCoroutine = StartCoroutine(HandleRecentering());
        }
    }

    /// <summary>
    /// XRInput subsystem recentering logic (from PlayerTeleport.cs reference)
    /// </summary>
    private IEnumerator HandleRecentering()
    {
        Debug.Log($"[TeleportController] Starting recentering sequence (delay: {recenteringDelay}s)");

        yield return new WaitForSeconds(recenteringDelay);

#if UNITY_XR_MANAGEMENT
        // Get XRInputSubsystem from XR Management
        var xrInputSubsystems = new List<UnityEngine.XR.XRInputSubsystem>();

        if (UnityEngine.XR.Management.XRGeneralSettings.Instance != null &&
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null)
        {
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.GetSubsystems(xrInputSubsystems);
        }

        foreach (var subsystem in xrInputSubsystems)
        {
            if (subsystem != null && subsystem.running)
            {
                Debug.Log($"[TeleportController] Found running XRInputSubsystem, attempting recenter...");

                // Step 1: Switch to Device tracking mode
                subsystem.TrySetTrackingOriginMode(UnityEngine.XR.TrackingOriginModeFlags.Device);
                yield return new WaitForSeconds(0.1f);

                // Step 2: Recenter
                bool recenterSuccess = subsystem.TryRecenter();
                Debug.Log($"[TeleportController] TryRecenter() result: {recenterSuccess}");
                yield return new WaitForSeconds(0.1f);

                // Step 3: Switch back to Floor tracking mode
                subsystem.TrySetTrackingOriginMode(UnityEngine.XR.TrackingOriginModeFlags.Floor);
                yield return new WaitForSeconds(0.1f);

                Debug.Log($"[TeleportController] Recentering sequence complete");
                break;
            }
        }

        if (xrInputSubsystems.Count == 0)
        {
            Debug.LogWarning("[TeleportController] No XRInputSubsystem found - recentering skipped");
        }
#else
        Debug.LogWarning("[TeleportController] XR Management package not available - recentering skipped");
#endif
    }

    /// <summary>
    /// Create visual preview indicator for destination
    /// </summary>
    private void CreatePreviewIndicator()
    {
        // Create cylinder as preview indicator
        previewIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        previewIndicator.name = $"{name}_Preview";
        previewIndicator.transform.SetParent(transform);
        previewIndicator.transform.localPosition = Vector3.zero;
        previewIndicator.transform.localRotation = Quaternion.identity;
        previewIndicator.transform.localScale = new Vector3(previewRadius * 2, 0.01f, previewRadius * 2);

        // Configure material
        var renderer = previewIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = previewColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }

        // Remove collider (preview only)
        var collider = previewIndicator.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw position indicator
        Gizmos.color = previewColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw forward direction arrow
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward * 0.5f;
        Gizmos.DrawRay(transform.position, forward);

        // Draw label
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        Handles.Label(transform.position + Vector3.up * 0.5f, $"Teleport: {name}", style);
    }
#endif
}

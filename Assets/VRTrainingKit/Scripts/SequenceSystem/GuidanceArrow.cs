// GuidanceArrow.cs
// Visual guidance arrow for training sequence steps
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Guidance arrow component for visual step indication
/// Attach to arrow GameObjects (tagged "GuidanceArrow") as children of interactive objects
/// Animations controlled via Unity Animator - this component just handles enable/disable
/// </summary>
public class GuidanceArrow : MonoBehaviour
{
    public enum ArrowType
    {
        Straight,   // For grab, snap, button press
        Rotating,   // For knobs, valves
        Pointing,   // For observation steps (static pointing)
        None        // Disable arrow
    }

    [Header("Arrow Settings")]
    [Tooltip("Type of arrow - determines visual style")]
    public ArrowType arrowType = ArrowType.Straight;

    [Tooltip("Auto-play animator when arrow is enabled")]
    public bool autoPlayAnimator = true;

    [Tooltip("Start hidden (will be shown by sequence controller)")]
    public bool startHidden = true;

    [Header("References")]
    [Tooltip("Animator component (optional - for animated arrows)")]
    public Animator animator;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLog = false;

    private bool isVisible = false;

    void Awake()
    {
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Validate tag
        if (!gameObject.CompareTag("GuidanceArrow"))
        {
            LogWarning($"GameObject '{gameObject.name}' has GuidanceArrow component but no 'GuidanceArrow' tag! Add tag for automatic discovery.");
        }
    }

    void Start()
    {
        // Set initial visibility
        if (startHidden)
        {
            HideArrow();
        }
        else
        {
            ShowArrow();
        }

        LogDebug($"GuidanceArrow initialized - Type: {arrowType}, Hidden: {startHidden}");
    }

    /// <summary>
    /// Show the arrow (called by sequence controller)
    /// </summary>
    public void ShowArrow()
    {
        if (isVisible) return;

        gameObject.SetActive(true);
        isVisible = true;

        // Start animator if available
        if (autoPlayAnimator && animator != null)
        {
            animator.enabled = true;

            // Trigger animation based on arrow type
            switch (arrowType)
            {
                case ArrowType.Rotating:
                    animator.SetBool("IsRotating", true);
                    break;
                case ArrowType.Straight:
                    animator.SetTrigger("Pulse");
                    break;
            }
        }

        LogDebug($"Arrow shown - Type: {arrowType}");
    }

    /// <summary>
    /// Hide the arrow (called by sequence controller)
    /// </summary>
    public void HideArrow()
    {
        if (!isVisible && !gameObject.activeSelf) return;

        isVisible = false;

        // Stop animator
        if (animator != null)
        {
            animator.enabled = false;
        }

        gameObject.SetActive(false);
        LogDebug("Arrow hidden");
    }

    /// <summary>
    /// Toggle arrow visibility
    /// </summary>
    public void ToggleArrow()
    {
        if (isVisible)
            HideArrow();
        else
            ShowArrow();
    }

    /// <summary>
    /// Check if arrow is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible && gameObject.activeSelf;
    }

    /// <summary>
    /// Change arrow type at runtime
    /// </summary>
    public void SetArrowType(ArrowType newType)
    {
        arrowType = newType;

        // Update animator parameters if visible
        if (isVisible && animator != null)
        {
            // Reset previous state
            animator.SetBool("IsRotating", false);

            // Set new state
            if (newType == ArrowType.Rotating)
            {
                animator.SetBool("IsRotating", true);
            }
        }
    }

    // Debug logging
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GuidanceArrow:{gameObject.name}] {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[GuidanceArrow:{gameObject.name}] {message}", this);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Ensure tag is set
        if (!gameObject.CompareTag("GuidanceArrow") && !gameObject.CompareTag("Untagged"))
        {
            Debug.LogWarning($"[GuidanceArrow] '{gameObject.name}' should have 'GuidanceArrow' tag for automatic discovery", this);
        }
    }

    void OnDrawGizmos()
    {
        // Draw arrow indicator in scene view
        if (isVisible || !Application.isPlaying)
        {
            Gizmos.color = arrowType == ArrowType.Rotating ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.02f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.05f);
        }
    }
#endif
}

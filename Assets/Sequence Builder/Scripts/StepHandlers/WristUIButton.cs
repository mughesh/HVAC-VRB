using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Simple trigger-based button for wrist UI
/// Only responds to specific colliders (like right hand fingertips)
/// Designed to be a child of the hand without physics conflicts
/// </summary>
public class WristUIButton : MonoBehaviour
{
    [Header("Allowed Interaction Colliders")]
    [Tooltip("Only these colliders can press this button (e.g., right hand fingertips)")]
    [SerializeField] private List<Collider> allowedColliders = new List<Collider>();

    [Header("Button Settings")]
    [Tooltip("Instant press on touch (no hold required). Recommended for quick interactions.")]
    [SerializeField] private bool instantPress = true;

    [Tooltip("How long collider must stay in trigger to activate (seconds) - only used if instantPress is false")]
    [SerializeField] private float pressTime = 0.2f;

    [Tooltip("Cooldown between button presses (seconds)")]
    [SerializeField] private float cooldown = 0.5f;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Material to apply when button is being pressed")]
    [SerializeField] private Material pressedMaterial;
    [SerializeField] private MeshRenderer buttonRenderer;

    [Header("Events")]
    public UnityEvent OnButtonPressed;

    private Material originalMaterial;
    private float pressTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isPressed = false;
    private HashSet<Collider> currentTouching = new HashSet<Collider>();

    private void Start()
    {
        // Store original material
        if (buttonRenderer != null && pressedMaterial != null)
        {
            originalMaterial = buttonRenderer.material;
        }

        // Ensure this has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"WristUIButton on {gameObject.name}: No collider found! Add a collider with 'Is Trigger' enabled.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"WristUIButton on {gameObject.name}: Collider 'Is Trigger' is not enabled! Enabling it now.");
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        // Update cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        // Check if any allowed collider is touching
        bool isTouching = currentTouching.Count > 0;

        // Skip Update loop logic if using instant press mode
        if (instantPress)
        {
            // Only handle visual feedback and reset
            if (!isTouching && isPressed)
            {
                ResetButton();
            }
            return;
        }

        // Original hold-to-press logic (only when instantPress is false)
        if (isTouching)
        {
            pressTimer += Time.deltaTime;

            // Visual feedback while pressing
            if (!isPressed && buttonRenderer != null && pressedMaterial != null)
            {
                buttonRenderer.material = pressedMaterial;
            }

            // Check if press time reached
            if (pressTimer >= pressTime && !isPressed)
            {
                ActivateButton();
            }
        }
        else
        {
            // Reset if no longer touching
            if (pressTimer > 0f || isPressed)
            {
                ResetButton();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only respond to allowed colliders
        if (IsAllowedCollider(other))
        {
            currentTouching.Add(other);
            Debug.Log($"[WristUIButton] Allowed collider entered - {other.name}");

            // Instant press mode - activate immediately on touch
            if (instantPress && cooldownTimer <= 0f && !isPressed)
            {
                ActivateButton();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Remove from touching set
        if (currentTouching.Contains(other))
        {
            currentTouching.Remove(other);
            Debug.Log($"[WristUIButton] Allowed collider exited - {other.name}");
        }
    }

    private bool IsAllowedCollider(Collider col)
    {
        return allowedColliders.Contains(col);
    }

    private void ActivateButton()
    {
        isPressed = true;
        Debug.Log($"[WristUIButton] Button pressed on {gameObject.name}");

        OnButtonPressed?.Invoke();

        // Start cooldown
        cooldownTimer = cooldown;
    }

    private void ResetButton()
    {
        pressTimer = 0f;
        isPressed = false;

        // Reset visual
        if (buttonRenderer != null && originalMaterial != null)
        {
            buttonRenderer.material = originalMaterial;
        }
    }

    // Public method to add allowed colliders at runtime
    public void AddAllowedCollider(Collider col)
    {
        if (!allowedColliders.Contains(col))
        {
            allowedColliders.Add(col);
        }
    }

    // Public method to clear all touching state (useful when UI hides)
    public void ClearTouchingState()
    {
        currentTouching.Clear();
        ResetButton();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-set trigger on collider in editor
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }
#endif
}

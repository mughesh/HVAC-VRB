// ValveSocketController.cs
// Minimal socket for valve snapping - AutoHands & XRI compatible
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Simple socket trigger for valve snapping
/// Detects when valve is released in trigger area and fires snap event
/// Valve controller handles all physics/constraints
/// </summary>
public class ValveSocketController : MonoBehaviour
{
    [Header("Socket Settings")]
    [Tooltip("Only accept objects with this tag")]
    [SerializeField] private string acceptedTag = "valve";

    [Tooltip("Socket is enabled and can accept objects")]
    [SerializeField] private bool socketActive = true;

    [Header("Attach Transform")]
    [Tooltip("Optional: Attach transform for object alignment. Leave null to use socket position/rotation")]
    [SerializeField] private Transform attachTransform;

    [Tooltip("Auto-detect first child as attach transform if not set")]
    [SerializeField] private bool autoDetectAttachTransform = true;

    [Header("Detection")]
    [Tooltip("Detection radius for trigger collider")]
    [SerializeField] private float detectionRadius = 0.15f;

    // Runtime state
    private GameObject currentSnappedObject;
    private SphereCollider triggerCollider;

    // Events
    public event Action<GameObject> OnObjectSnapped;
    public event Action<GameObject> OnObjectRemoved;

    // Public API
    public bool HasObject => currentSnappedObject != null;
    public GameObject SnappedObject => currentSnappedObject;
    public bool SocketActive { get => socketActive; set => socketActive = value; }

    public Vector3 SnapPosition => attachTransform != null ? attachTransform.position : transform.position;
    public Quaternion SnapRotation => attachTransform != null ? attachTransform.rotation : transform.rotation;

    private void Awake()
    {
        // Auto-detect attach transform from children
        if (attachTransform == null && autoDetectAttachTransform && transform.childCount > 0)
        {
            // Try to find child named "AttachTransform"
            attachTransform = transform.Find("AttachTransform");

            // Otherwise use first child
            if (attachTransform == null)
            {
                attachTransform = transform.GetChild(0);
                Debug.Log($"[ValveSocket] {gameObject.name} auto-detected attach transform: {attachTransform.name}");
            }
        }

        // Setup or validate trigger collider
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = detectionRadius;
            Debug.Log($"[ValveSocket] {gameObject.name} auto-created SphereCollider trigger (radius={detectionRadius})");
        }
        else
        {
            triggerCollider.isTrigger = true;
            Debug.Log($"[ValveSocket] {gameObject.name} using existing SphereCollider (radius={triggerCollider.radius})");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(MonitorForReleaseSnap());
    }

    /// <summary>
    /// Monitor objects in trigger area for release → snap
    /// Detects both AutoHands Grabbable and XRI XRGrabInteractable
    /// </summary>
    private IEnumerator MonitorForReleaseSnap()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1s

            if (!socketActive || HasObject)
                continue;

            // Find all colliders in trigger radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, triggerCollider.radius);

            foreach (var col in colliders)
            {
                GameObject obj = col.gameObject;

                // Check tag
                if (!obj.CompareTag(acceptedTag))
                    continue;

                // Check if object is released (not being grabbed)
                if (IsObjectReleased(obj))
                {
                    TrySnapObject(obj);
                    break; // Only snap one object
                }
            }
        }
    }

    /// <summary>
    /// Check if object is released (not being grabbed by hand)
    /// Supports AutoHands and XRI frameworks
    /// </summary>
    private bool IsObjectReleased(GameObject obj)
    {
        // AutoHands check
        var grabbable = obj.GetComponent<Autohand.Grabbable>();
        if (grabbable != null)
        {
            // Check both isGrabbing (this object is grabbing something) and BeingGrabbed (being held)
            return !grabbable.BeingGrabbed();
        }

        // XRI check (for future)
        var xrGrabbable = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (xrGrabbable != null)
        {
            return !xrGrabbable.isSelected;
        }

        // No grabbable component - assume released
        return true;
    }

    /// <summary>
    /// Try to snap object to socket
    /// Only sets transform - object handles its own physics
    /// </summary>
    private void TrySnapObject(GameObject obj)
    {
        if (!socketActive || HasObject)
            return;

        currentSnappedObject = obj;

        // Snap to attach transform position/rotation
        obj.transform.position = SnapPosition;
        obj.transform.rotation = SnapRotation;

        Debug.Log($"[ValveSocket] {gameObject.name} snapped object: {obj.name} at {SnapPosition}");

        // Fire event - valve controller will handle locking constraints
        OnObjectSnapped?.Invoke(obj);
    }

    /// <summary>
    /// Remove object from socket
    /// Called by valve controller when unlocking
    /// </summary>
    public void RemoveObject()
    {
        if (currentSnappedObject == null)
            return;

        GameObject removedObject = currentSnappedObject;
        currentSnappedObject = null;

        Debug.Log($"[ValveSocket] {gameObject.name} removed object: {removedObject.name}");

        // Fire event
        OnObjectRemoved?.Invoke(removedObject);
    }

    /// <summary>
    /// Eject current object (for manual removal)
    /// </summary>
    public void EjectObject()
    {
        RemoveObject();
    }
}

// ValveProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Profile for valve interactions with forward/reverse flow (grab → snap → tighten → loosen → remove)
/// Handles complex state management with LOCKED substates (LOOSE/TIGHT)
/// </summary>
[CreateAssetMenu(fileName = "ValveProfile", menuName = "VR Training/Valve Profile")]
public class ValveProfile : InteractionProfile
{
    [Header("Valve Mechanics")]
    [Tooltip("Axis around which valve rotates (e.g., Vector3.up for Y-axis)")]
    public Vector3 rotationAxis = Vector3.up;
    
    [Tooltip("Degrees of rotation required to tighten valve")]
    [Range(10f, 360f)]
    public float tightenThreshold = 90f;
    
    [Tooltip("Degrees of reverse rotation required to loosen valve")]
    [Range(10f, 360f)]
    public float loosenThreshold = 90f;
    
    [Tooltip("Angle tolerance for threshold completion")]
    [Range(1f, 15f)]
    public float angleTolerance = 5f;

    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this valve can work with")]
    public string[] compatibleSocketTags = {"valve_socket"};
    
    [Tooltip("Specific socket objects this valve works with")]
    public GameObjectReference[] specificCompatibleSockets;
    
    [Tooltip("Use specific socket objects instead of tag-based matching")]
    public bool requireSpecificSockets = false;

    [Header("Grab Settings")]
    public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.VelocityTracking;
    public bool trackPosition = true;
    public bool trackRotation = true;
    public bool throwOnDetach = true;

    [Header("Physics Settings")]
    public float throwVelocityScale = 1.5f;
    public float throwAngularVelocityScale = 1.0f;

    [Header("Attach Settings")]
    public bool useDynamicAttach = true;
    public float attachEaseInTime = 0.15f;

    [Header("Collider Settings")]
    public ColliderType colliderType = ColliderType.Box;
    public bool addColliderToMeshChild = true;

    [Header("Interaction Feel")]
    [Tooltip("Rotation speed multiplier when valve is locked")]
    [Range(0.1f, 3.0f)]
    public float lockedRotationSpeed = 1.0f;
    
    [Tooltip("Haptic feedback intensity during rotation")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Material when valve is in loose state (needs tightening)")]
    public Material looseMaterial;
    
    [Tooltip("Material when valve is in tight state (properly secured)")]
    public Material tightMaterial;
    
    [Tooltip("Show progress indicator during rotation")]
    public bool showProgressIndicator = true;

    public override void ApplyToGameObject(GameObject target)
    {
        Debug.Log($"[ValveProfile] ApplyToGameObject() called for: {target.name} with profile: {profileName}");
        
        // Add XRGrabInteractable
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
            Debug.Log($"[ValveProfile] Added XRGrabInteractable to {target.name}");
        }
        
        // Apply grab settings
        grabInteractable.movementType = movementType;
        grabInteractable.trackPosition = trackPosition;
        grabInteractable.trackRotation = trackRotation;
        grabInteractable.throwOnDetach = throwOnDetach;
        grabInteractable.throwVelocityScale = throwVelocityScale;
        grabInteractable.throwAngularVelocityScale = throwAngularVelocityScale;
        grabInteractable.useDynamicAttach = useDynamicAttach;
        grabInteractable.attachEaseInTime = attachEaseInTime;
        
        // Add Rigidbody
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
            Debug.Log($"[ValveProfile] Added Rigidbody to {target.name}");
        }
        rb.useGravity = true;
        rb.isKinematic = (movementType == XRBaseInteractable.MovementType.Kinematic);
        
        // Handle collider - find appropriate target
        GameObject colliderTarget = target;
        if (addColliderToMeshChild)
        {
            colliderTarget = FindMeshChild(target) ?? target;
        }
        
        // Ensure Collider exists on appropriate object
        if (colliderTarget.GetComponent<Collider>() == null && colliderType != ColliderType.None)
        {
            AddCollider(colliderTarget, colliderType);
            Debug.Log($"[ValveProfile] Added {colliderType} collider to {colliderTarget.name}");
        }
        
        // Add ValveController for complex valve behavior
        ValveController valveController = target.GetComponent<ValveController>();
        if (valveController == null)
        {
            valveController = target.AddComponent<ValveController>();
            Debug.Log($"[ValveProfile] Added ValveController to {target.name}");
        }
        
        // Configure the valve controller with this profile
        valveController.Configure(this);
        
        Debug.Log($"[ValveProfile] Successfully configured valve: {target.name}");
        Debug.Log($"[ValveProfile] Valve {target.name} is now ready for grab→snap→tighten→loosen→remove interactions");
    }
    
    private GameObject FindMeshChild(GameObject parent)
    {
        // First check direct children
        MeshRenderer meshRenderer = parent.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.gameObject != parent)
        {
            return meshRenderer.gameObject;
        }
        
        // If no mesh renderer found in children, return null
        return null;
    }
    
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
                break;
                
            case ColliderType.Sphere:
                SphereCollider sphereCol = target.AddComponent<SphereCollider>();
                if (renderer != null)
                {
                    sphereCol.center = target.transform.InverseTransformPoint(bounds.center);
                    sphereCol.radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / 2f;
                }
                break;
                
            case ColliderType.Capsule:
                CapsuleCollider capsuleCol = target.AddComponent<CapsuleCollider>();
                if (renderer != null)
                {
                    capsuleCol.center = target.transform.InverseTransformPoint(bounds.center);
                    capsuleCol.height = bounds.size.y;
                    capsuleCol.radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
                }
                break;
                
            case ColliderType.Mesh:
                MeshCollider meshCol = target.AddComponent<MeshCollider>();
                MeshFilter meshFilter = target.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshCol.sharedMesh = meshFilter.sharedMesh;
                    meshCol.convex = true; // Required for physics interactions
                }
                break;
        }
    }
    
    public override bool ValidateGameObject(GameObject target)
    {
        return target != null && target.CompareTag("valve");
    }
    
    /// <summary>
    /// Check if a socket is compatible with this valve
    /// </summary>
    public bool IsSocketCompatible(GameObject socket)
    {
        if (socket == null) return false;
        
        // Check if socket has XRSocketInteractor (required for snapping)
        if (socket.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>() == null) 
            return false;
        
        // Check specific sockets first
        if (requireSpecificSockets && specificCompatibleSockets != null)
        {
            foreach (var socketRef in specificCompatibleSockets)
            {
                if (socketRef.GameObject == socket)
                    return true;
            }
            return false;
        }
        
        // Check compatible tags
        if (compatibleSocketTags != null && compatibleSocketTags.Length > 0)
        {
            foreach (string tag in compatibleSocketTags)
            {
                if (socket.CompareTag(tag))
                    return true;
            }
        }
        
        return false;
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure thresholds are reasonable
        tightenThreshold = Mathf.Clamp(tightenThreshold, 10f, 360f);
        loosenThreshold = Mathf.Clamp(loosenThreshold, 10f, 360f);
        angleTolerance = Mathf.Clamp(angleTolerance, 1f, 15f);
        
        // Ensure tolerance is smaller than thresholds
        if (angleTolerance >= tightenThreshold * 0.5f)
            angleTolerance = tightenThreshold * 0.2f;
        if (angleTolerance >= loosenThreshold * 0.5f)
            angleTolerance = loosenThreshold * 0.2f;
    }
    #endif
}
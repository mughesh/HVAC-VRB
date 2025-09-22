// ToolProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Tool operation states for complex interactions
/// </summary>
public enum ToolState
{
    Unlocked,    // Tool can be grabbed and moved freely
    Snapped,     // Tool is snapped to socket but not locked
    Locked       // Tool is locked to socket position, only rotation allowed
}

/// <summary>
/// Profile for tool interactions (grab + snap + rotate)
/// </summary>
[CreateAssetMenu(fileName = "ToolProfile", menuName = "VR Training/Tool Profile")]
public class ToolProfile : InteractionProfile
{
    [Header("Tool Settings")]
    public Vector3 rotationAxis = Vector3.up;
    public float tightenAngle = 180f;
    public float loosenAngle = 90f;
    
    [Header("Socket Compatibility")]
    [Tooltip("Tags of sockets this tool can work with")]
    public string[] compatibleSocketTags = new string[] { "snap" };
    
    [Tooltip("Specific socket objects this tool works with (if empty, uses tags)")]
    public GameObjectReference[] specificCompatibleSockets;
    
    [Tooltip("Use specific socket objects instead of tag-based matching")]
    public bool requireSpecificSockets = false;
    
    [Header("Tool Operation Settings")]
    [Tooltip("Rotation threshold (degrees) to consider tool tightened")]
    public float tightenThreshold = 50f;
    
    [Tooltip("Rotation threshold (degrees) to consider tool loosened")]
    public float loosenThreshold = 45f;
    
    [Tooltip("Tolerance for angle completion (degrees)")]
    public float angleTolerance = 5f;
    
    [Tooltip("Speed multiplier for rotation when tool is locked in socket")]
    public float lockedRotationSpeed = 1.0f;
    
    [Header("State Management")]
    [Tooltip("Initial state of the tool (Unlocked for forward flow, Locked for reverse flow)")]
    public ToolState initialState = ToolState.Unlocked;
    
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
    
    public override void ApplyToGameObject(GameObject target)
    {
        Debug.Log($"[ToolProfile] ApplyToGameObject() called for: {target.name} with profile: {profileName}");
        
        // Add XRGrabInteractable
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
            Debug.Log($"[ToolProfile] Added XRGrabInteractable to {target.name}");
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
            Debug.Log($"[ToolProfile] Added Rigidbody to {target.name}");
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
            Debug.Log($"[ToolProfile] Added {colliderType} collider to {colliderTarget.name}");
        }
        
        // Add ToolController for complex tool behavior
        ToolController toolController = target.GetComponent<ToolController>();
        if (toolController == null)
        {
            toolController = target.AddComponent<ToolController>();
            Debug.Log($"[ToolProfile] Added ToolController to {target.name}");
        }
        
        // Configure the tool controller
        toolController.Configure(this);
        
        Debug.Log($"[ToolProfile] Successfully configured tool: {target.name}");
        Debug.Log($"[ToolProfile] Tool {target.name} is now ready for complex grab→snap→rotate interactions");
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
        return target != null && target.CompareTag("tool");
    }
}
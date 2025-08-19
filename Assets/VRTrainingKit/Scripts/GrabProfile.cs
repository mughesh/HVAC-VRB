// GrabProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Profile for grab interactions
/// </summary>
[CreateAssetMenu(fileName = "GrabProfile", menuName = "VR Training/Grab Profile")]
public class GrabProfile : InteractionProfile
{
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
        // Add or get XRGrabInteractable on parent
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
        }
        
        // Apply settings
        grabInteractable.movementType = movementType;
        grabInteractable.trackPosition = trackPosition;
        grabInteractable.trackRotation = trackRotation;
        grabInteractable.throwOnDetach = throwOnDetach;
        grabInteractable.throwVelocityScale = throwVelocityScale;
        grabInteractable.throwAngularVelocityScale = throwAngularVelocityScale;
        grabInteractable.useDynamicAttach = useDynamicAttach;
        grabInteractable.attachEaseInTime = attachEaseInTime;
        
        // Ensure Rigidbody exists on parent
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = (movementType == XRBaseInteractable.MovementType.Kinematic);
        }
        
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
        }
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
        return target != null && target.CompareTag("grab");
    }
}
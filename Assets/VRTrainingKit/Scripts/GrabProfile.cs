// GrabProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Profile for grab interactions - supports both XRI and AutoHand systems
/// </summary>
[CreateAssetMenu(fileName = "GrabProfile", menuName = "VR Training/Grab Profile")]
public class GrabProfile : InteractionProfile
{
    [Header("XRI Grab Settings")]
    [Tooltip("Only used when Hand System is XRI")]
    public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.VelocityTracking;
    public bool trackPosition = true;
    public bool trackRotation = true;
    public bool throwOnDetach = true;
    
    [Header("XRI Physics Settings")]
    [Tooltip("Only used when Hand System is XRI")]
    public float throwVelocityScale = 1.5f;
    public float throwAngularVelocityScale = 1.0f;
    
    [Header("XRI Attach Settings")]
    [Tooltip("Only used when Hand System is XRI")]
    public bool useDynamicAttach = true;
    public float attachEaseInTime = 0.15f;
    
    [Header("AutoHand Grab Settings")]
    [Tooltip("Only used when Hand System is AutoHand")]
    public bool instantGrab = false;
    [Tooltip("Good for heavy objects - hand returns gently during movement")]
    public bool useGentleGrab = false;
    [Tooltip("Creates offset grab so hand doesn't return to object - good for jointed objects")]
    public bool maintainGrabOffset = false;
    [Tooltip("Parent object under hand on grab - recommended for most objects")]
    public bool parentOnGrab = true;
    
    [Header("AutoHand Physics Settings")]
    [Tooltip("Only used when Hand System is AutoHand")]
    [Range(0f, 2f)]
    public float throwPower = 1.0f;
    [Tooltip("Force required to break grab connection - higher values = stronger grip")]
    public float jointBreakForce = 3500f;
    [Tooltip("Priority when multiple objects can be grabbed - higher is better")]
    [Range(0.1f, 3f)]
    public float grabPriorityWeight = 1.0f;
    
    [Header("Collider Settings")]
    public ColliderType colliderType = ColliderType.Box;
    public bool addColliderToMeshChild = true;
    
    /// <summary>
    /// Apply XRI-specific components to the target GameObject
    /// </summary>
    protected override void ApplyXRIComponents(GameObject target)
    {
        Debug.Log($"[GrabProfile] Applying XRI components to {target.name}");
        
        // Add or get XRGrabInteractable on parent
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
        }
        
        // Apply XRI settings
        grabInteractable.movementType = movementType;
        grabInteractable.trackPosition = trackPosition;
        grabInteractable.trackRotation = trackRotation;
        grabInteractable.throwOnDetach = throwOnDetach;
        grabInteractable.throwVelocityScale = throwVelocityScale;
        grabInteractable.throwAngularVelocityScale = throwAngularVelocityScale;
        grabInteractable.useDynamicAttach = useDynamicAttach;
        grabInteractable.attachEaseInTime = attachEaseInTime;
    }
    
    /// <summary>
    /// Apply AutoHand-specific components to the target GameObject
    /// </summary>
    protected override void ApplyAutoHandComponents(GameObject target)
    {
        Debug.Log($"[GrabProfile] Applying AutoHand components to {target.name}");
        
        // Remove any existing XRI components to avoid conflicts
        var existingXRI = target.GetComponent<XRGrabInteractable>();
        if (existingXRI != null)
        {
            Debug.Log($"[GrabProfile] Removing existing XRGrabInteractable from {target.name}");
            #if UNITY_EDITOR
            if (Application.isPlaying)
                Object.Destroy(existingXRI);
            else
                Object.DestroyImmediate(existingXRI);
            #else
            Object.Destroy(existingXRI);
            #endif
        }
        
        // Check if AutoHand is available
        if (!IsAutoHandAvailable())
        {
            Debug.LogError("[GrabProfile] AutoHand components not found in project. Please ensure AutoHand asset is imported.");
            return;
        }
        
        // Add AutoHand Grabbable component using robust type detection
        try
        {
            var grabbableType = GetAutoHandType("Grabbable");
            if (grabbableType == null)
            {
                Debug.LogError("[GrabProfile] Autohand.Grabbable type not found. Please check AutoHand installation.");
                return;
            }
            
            var grabbable = target.GetComponent(grabbableType);
            if (grabbable == null)
            {
                grabbable = target.AddComponent(grabbableType);
                Debug.Log($"[GrabProfile] Added Grabbable component to {target.name}");
            }
            
            // Set AutoHand properties using reflection
            SetAutoHandGrabbableProperties(grabbable, grabbableType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GrabProfile] Failed to add AutoHand components: {e.Message}");
        }
    }
    
    /// <summary>
    /// Apply common components needed by both XRI and AutoHand systems
    /// </summary>
    protected override void ApplyCommonComponents(GameObject target)
    {
        // Ensure Rigidbody exists on parent
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
            rb.useGravity = true;
            
            // Set kinematic based on system type
            if (handSystem == HandSystemType.XRI)
            {
                rb.isKinematic = (movementType == XRBaseInteractable.MovementType.Kinematic);
            }
            else if (handSystem == HandSystemType.AutoHand)
            {
                rb.isKinematic = false; // AutoHand typically uses non-kinematic rigidbodies
            }
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
    
    /// <summary>
    /// Set AutoHand Grabbable properties using reflection
    /// </summary>
    private void SetAutoHandGrabbableProperties(Component grabbable, System.Type grabbableType)
    {
        try
        {
            // Set basic AutoHand properties
            SetFieldValue(grabbable, grabbableType, "instantGrab", instantGrab);
            SetFieldValue(grabbable, grabbableType, "useGentleGrab", useGentleGrab);
            SetFieldValue(grabbable, grabbableType, "maintainGrabOffset", maintainGrabOffset);
            SetFieldValue(grabbable, grabbableType, "parentOnGrab", parentOnGrab);
            SetFieldValue(grabbable, grabbableType, "throwPower", throwPower);
            SetFieldValue(grabbable, grabbableType, "jointBreakForce", jointBreakForce);
            SetFieldValue(grabbable, grabbableType, "grabPriorityWeight", grabPriorityWeight);
            
            Debug.Log($"[GrabProfile] Successfully configured AutoHand properties for {grabbable.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GrabProfile] Could not set all AutoHand properties: {e.Message}");
        }
    }
    
    /// <summary>
    /// Helper method to set field values using reflection
    /// </summary>
    private void SetFieldValue(Component component, System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName);
        if (field != null)
        {
            field.SetValue(component, value);
        }
        else
        {
            Debug.LogWarning($"[GrabProfile] Field '{fieldName}' not found in AutoHand Grabbable");
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
    
    /// <summary>
    /// Validate GameObject for XRI system
    /// </summary>
    protected override bool ValidateForXRI(GameObject target)
    {
        return target != null && target.CompareTag("grab");
    }
    
    /// <summary>
    /// Validate GameObject for AutoHand system  
    /// </summary>
    protected override bool ValidateForAutoHand(GameObject target)
    {
        if (target == null || !target.CompareTag("grab"))
            return false;
            
        // Additional AutoHand-specific validation
        if (!IsAutoHandAvailable())
        {
            Debug.LogWarning("[GrabProfile] AutoHand not available in project");
            return false;
        }
        
        return true;
    }
}
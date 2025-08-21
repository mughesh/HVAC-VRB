// KnobProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Profile for knob/rotatable interactions
/// </summary>
[CreateAssetMenu(fileName = "KnobProfile", menuName = "VR Training/Knob Profile")]
public class KnobProfile : InteractionProfile
{
    public enum RotationAxis { X, Y, Z }
    
    [Header("Knob Settings")]
    public RotationAxis rotationAxis = RotationAxis.Y;
    public float minAngle = -90f;
    public float maxAngle = 180f;
    public bool useLimits = true;
    
    [Header("Hinge Joint Settings")]
    public bool autoConfigureConnectedAnchor = true;  // ENABLED by default as you requested
    public bool useSpring = true;
    public float springValue = 0f;
    public float damper = 0.1f;
    public float targetPosition = 0f;
    
    [Header("Joint Limits")]
    public float bounceMinVelocity = 0.2f;
    public float contactDistance = 0f;
    
    [Header("Interaction Settings")]
    public float rotationSpeed = 1.0f;
    public bool snapToAngles = false;
    public float snapAngleIncrement = 15f;
    
    [Header("Feedback")]
    public bool useHapticFeedback = true;
    public float hapticIntensity = 0.3f;
    
    [Header("Collider Settings")]
    public ColliderType colliderType = ColliderType.Box;
    
    public override void ApplyToGameObject(GameObject target)
    {
        // Add XRGrabInteractable to parent
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
        }
        
        // Configure for knob - MUST use these settings for joint to work
        grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = true;
        grabInteractable.useDynamicAttach = true;
        
        // Add or configure Rigidbody on parent
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false; // Must be false for HingeJoint
        rb.useGravity = true; // Gravity can help with joint stability
        
        // Add HingeJoint to parent
        HingeJoint joint = target.GetComponent<HingeJoint>();
        if (joint == null)
        {
            joint = target.AddComponent<HingeJoint>();
        }
        ConfigureHingeJoint(joint);
        
        // Find mesh child for collider
        GameObject meshChild = FindMeshChild(target);
        if (meshChild != null && meshChild.GetComponent<Collider>() == null)
        {
            AddCollider(meshChild, colliderType);
        }
        else if (meshChild == null && target.GetComponent<Collider>() == null)
        {
            // Fallback: add to parent if no mesh child found
            AddCollider(target, colliderType);
        }
        
        // Add KnobController for additional behavior
        KnobController knobController = target.GetComponent<KnobController>();
        if (knobController == null)
        {
            knobController = target.AddComponent<KnobController>();
        }
        knobController.Configure(this);
    }
    
    private void ConfigureHingeJoint(HingeJoint joint)
    {
        // Set axis based on rotation axis
        switch (rotationAxis)
        {
            case RotationAxis.X:
                joint.axis = Vector3.right;
                break;
            case RotationAxis.Y:
                joint.axis = Vector3.up;
                break;
            case RotationAxis.Z:
                joint.axis = Vector3.forward;
                break;
        }
        
        // Auto configure connected anchor - ENABLED 
        joint.autoConfigureConnectedAnchor = autoConfigureConnectedAnchor;
        if (!autoConfigureConnectedAnchor)
        {
            joint.connectedAnchor = Vector3.zero;
        }
        
        // Configure spring 
        joint.useSpring = useSpring;
        if (useSpring)
        {
            JointSpring spring = new JointSpring();
            spring.spring = springValue;
            spring.damper = damper;
            spring.targetPosition = targetPosition;
            joint.spring = spring;
        }
        
        // Configure limits
        joint.useLimits = useLimits;
        if (useLimits)
        {
            JointLimits limits = new JointLimits();
            limits.min = minAngle;
            limits.max = maxAngle;
            limits.bounceMinVelocity = bounceMinVelocity;
            limits.contactDistance = contactDistance;
            joint.limits = limits;
        }
        
        // Enable preprocessing (for stability)
        joint.enablePreprocessing = true;
    }
    
    private GameObject FindMeshChild(GameObject parent)
    {
        MeshRenderer meshRenderer = parent.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.gameObject != parent)
        {
            return meshRenderer.gameObject;
        }
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
                    meshCol.convex = true;
                }
                break;
        }
    }
    
    public override bool ValidateGameObject(GameObject target)
    {
        return target != null && target.CompareTag("knob");
    }
}
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
    
    /// <summary>
    /// Apply XRI-specific components for knob interactions
    /// </summary>
    protected override void ApplyXRIComponents(GameObject target)
    {
        Debug.Log($"[KnobProfile] ApplyToGameObject() called for: {target.name} with profile: {profileName}");
        
        // Add XRGrabInteractable to parent
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
            Debug.Log($"[KnobProfile] Added XRGrabInteractable to {target.name}");
        }
        else
        {
            Debug.Log($"[KnobProfile] Found existing XRGrabInteractable on {target.name}");
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
            Debug.Log($"[KnobProfile] Added KnobController to {target.name}");
        }
        else
        {
            Debug.Log($"[KnobProfile] Found existing KnobController on {target.name}");
        }
        
        Debug.Log($"[KnobProfile] Calling Configure() on KnobController for {target.name}");
        knobController.Configure(this);
        Debug.Log($"[KnobProfile] Successfully configured KnobController for {target.name}");
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
    
    /// <summary>
    /// Apply AutoHand-specific components for knob interactions
    /// </summary>
    protected override void ApplyAutoHandComponents(GameObject target)
    {
        Debug.Log($"[KnobProfile] Applying AutoHand components to {target.name}");
        
        // Remove any existing XRI components to avoid conflicts
        var existingXRI = target.GetComponent<XRGrabInteractable>();
        if (existingXRI != null)
        {
            Debug.Log($"[KnobProfile] Removing existing XRGrabInteractable from {target.name}");
            #if UNITY_EDITOR
            if (Application.isPlaying)
                Object.Destroy(existingXRI);
            else
                Object.DestroyImmediate(existingXRI);
            #else
            Object.Destroy(existingXRI);
            #endif
        }
        
        // Remove existing KnobController as it's XRI-specific
        var existingKnobController = target.GetComponent<KnobController>();
        if (existingKnobController != null)
        {
            Debug.Log($"[KnobProfile] Removing existing KnobController from {target.name}");
            #if UNITY_EDITOR
            if (Application.isPlaying)
                Object.Destroy(existingKnobController);
            else
                Object.DestroyImmediate(existingKnobController);
            #else
            Object.Destroy(existingKnobController);
            #endif
        }
        
        // Check if AutoHand is available
        if (!IsAutoHandAvailable())
        {
            Debug.LogError("[KnobProfile] AutoHand components not found in project. Please ensure AutoHand asset is imported.");
            return;
        }
        
        // Add AutoHand Grabbable component using robust type detection
        try
        {
            var grabbableType = GetAutoHandType("Grabbable");
            if (grabbableType == null)
            {
                Debug.LogError("[KnobProfile] Autohand.Grabbable type not found. Please check AutoHand installation.");
                return;
            }
            
            var grabbable = target.GetComponent(grabbableType);
            if (grabbable == null)
            {
                grabbable = target.AddComponent(grabbableType);
                Debug.Log($"[KnobProfile] Added Grabbable component to {target.name}");
            }
            
            // Set AutoHand properties for knob interaction
            SetAutoHandGrabbableProperties(grabbable, grabbableType);
            
            // Add AutoHand PhysicsGadgetHingeAngleReader for angle tracking
            var angleReaderType = GetAutoHandType("PhysicsGadgetHingeAngleReader");
            if (angleReaderType != null)
            {
                var angleReader = target.GetComponent(angleReaderType);
                if (angleReader == null)
                {
                    angleReader = target.AddComponent(angleReaderType);
                    Debug.Log($"[KnobProfile] Added PhysicsGadgetHingeAngleReader to {target.name}");
                }
                
                // Configure angle reader properties
                SetAutoHandAngleReaderProperties(angleReader, angleReaderType);
            }
            else
            {
                Debug.LogWarning("[KnobProfile] PhysicsGadgetHingeAngleReader type not found in AutoHand");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[KnobProfile] Failed to add AutoHand components: {e.Message}");
        }
    }
    
    /// <summary>
    /// Apply common components needed by both systems
    /// </summary>
    protected override void ApplyCommonComponents(GameObject target)
    {
        // Ensure Rigidbody exists
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
            rb.useGravity = true;
            Debug.Log($"[KnobProfile] Added Rigidbody to {target.name}");
        }
        
        // Configure rigidbody based on system type
        if (handSystem == HandSystemType.AutoHand)
        {
            rb.isKinematic = false; // AutoHand needs non-kinematic for physics
        }
        
        // Add and configure HingeJoint (both systems use this for rotation)
        HingeJoint joint = target.GetComponent<HingeJoint>();
        if (joint == null)
        {
            joint = target.AddComponent<HingeJoint>();
            Debug.Log($"[KnobProfile] Added HingeJoint to {target.name}");
        }
        
        ConfigureHingeJoint(joint);
        
        // Handle collider
        GameObject colliderTarget = target;
        if (colliderTarget.GetComponent<Collider>() == null && colliderType != ColliderType.None)
        {
            AddCollider(colliderTarget, colliderType);
        }
    }
    
    /// <summary>
    /// Set AutoHand Grabbable properties for knob interactions
    /// </summary>
    private void SetAutoHandGrabbableProperties(Component grabbable, System.Type grabbableType)
    {
        try
        {
            // Set properties appropriate for knobs
            SetFieldValue(grabbable, grabbableType, "instantGrab", false);
            SetFieldValue(grabbable, grabbableType, "useGentleGrab", true); // Good for knobs
            SetFieldValue(grabbable, grabbableType, "maintainGrabOffset", true); // Good for jointed objects
            SetFieldValue(grabbable, grabbableType, "parentOnGrab", false); // Don't parent jointed objects
            SetFieldValue(grabbable, grabbableType, "throwPower", 0f); // Knobs shouldn't be throwable
            SetFieldValue(grabbable, grabbableType, "jointBreakForce", 10000f); // High break force for knobs
            SetFieldValue(grabbable, grabbableType, "grabPriorityWeight", 1.0f);
            
            Debug.Log($"[KnobProfile] Successfully configured AutoHand Grabbable properties for {grabbable.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[KnobProfile] Could not set all AutoHand Grabbable properties: {e.Message}");
        }
    }
    
    /// <summary>
    /// Set AutoHand PhysicsGadgetHingeAngleReader properties
    /// </summary>
    private void SetAutoHandAngleReaderProperties(Component angleReader, System.Type angleReaderType)
    {
        try
        {
            // Set properties for angle reading
            SetFieldValue(angleReader, angleReaderType, "invertValue", false);
            SetFieldValue(angleReader, angleReaderType, "playRange", 0.05f);
            
            Debug.Log($"[KnobProfile] Successfully configured AutoHand angle reader properties for {angleReader.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[KnobProfile] Could not set all AutoHand angle reader properties: {e.Message}");
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
            Debug.LogWarning($"[KnobProfile] Field '{fieldName}' not found in {type.Name}");
        }
    }
    
    /// <summary>
    /// Validate GameObject for XRI system
    /// </summary>
    protected override bool ValidateForXRI(GameObject target)
    {
        return target != null && target.CompareTag("knob");
    }
    
    /// <summary>
    /// Validate GameObject for AutoHand system
    /// </summary>
    protected override bool ValidateForAutoHand(GameObject target)
    {
        if (target == null || !target.CompareTag("knob"))
            return false;
            
        // Additional AutoHand-specific validation
        if (!IsAutoHandAvailable())
        {
            Debug.LogWarning("[KnobProfile] AutoHand not available in project");
            return false;
        }
        
        return true;
    }
}
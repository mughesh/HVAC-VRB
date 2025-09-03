// SnapProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Profile for snap/socket interactions
/// </summary>
[CreateAssetMenu(fileName = "SnapProfile", menuName = "VR Training/Snap Profile")]
public class SnapProfile : InteractionProfile
{
    [Header("Socket Settings")]
    public float socketRadius = 0.1f;
    public bool showInteractableHoverMeshes = true;
    public Material hoverMaterial;
    
    [Header("Snap Behavior")]
    public bool socketActive = true;
    public float recycleDelayTime = 1.0f;
    public bool ejectOnDisconnect = true;
    
    [Header("Validation")]
    public string[] acceptedTags = new string[] { "grab" };
    public bool requireSpecificObjects = false;
    public GameObject[] specificAcceptedObjects;
    
    /// <summary>
    /// Apply XRI-specific components for snap/socket interactions
    /// </summary>
    protected override void ApplyXRIComponents(GameObject target)
    {
        // Add XRSocketInteractor
        XRSocketInteractor socketInteractor = target.GetComponent<XRSocketInteractor>();
        if (socketInteractor == null)
        {
            socketInteractor = target.AddComponent<XRSocketInteractor>();
        }
        
        // Apply settings
        socketInteractor.socketActive = socketActive;
        socketInteractor.showInteractableHoverMeshes = showInteractableHoverMeshes;
        socketInteractor.recycleDelayTime = recycleDelayTime;
        
        // Add SphereCollider for detection
        SphereCollider sphereCollider = target.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = target.AddComponent<SphereCollider>();
        }
        sphereCollider.isTrigger = true;
        sphereCollider.radius = socketRadius;
        
        // Add Rigidbody (required for trigger detection)
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Note: SnapValidator will be added in Phase 2 when we implement the event system
        // For now, the XRSocketInteractor handles basic validation through its built-in mechanisms
    }
    
    /// <summary>
    /// Apply AutoHand-specific components for snap/place point interactions
    /// </summary>
    protected override void ApplyAutoHandComponents(GameObject target)
    {
        Debug.Log($"[SnapProfile] Applying AutoHand components to {target.name}");
        
        // Remove any existing XRI components to avoid conflicts
        var existingXRI = target.GetComponent<XRSocketInteractor>();
        if (existingXRI != null)
        {
            Debug.Log($"[SnapProfile] Removing existing XRSocketInteractor from {target.name}");
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
            Debug.LogError("[SnapProfile] AutoHand components not found in project. Please ensure AutoHand asset is imported.");
            return;
        }
        
        // Add AutoHand PlacePoint component using robust type detection
        try
        {
            var placePointType = GetAutoHandType("PlacePoint");
            if (placePointType == null)
            {
                Debug.LogError("[SnapProfile] Autohand.PlacePoint type not found. Please check AutoHand installation.");
                return;
            }
            
            var placePoint = target.GetComponent(placePointType);
            if (placePoint == null)
            {
                placePoint = target.AddComponent(placePointType);
                Debug.Log($"[SnapProfile] Added PlacePoint component to {target.name}");
            }
            
            // Set AutoHand PlacePoint properties
            SetAutoHandPlacePointProperties(placePoint, placePointType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SnapProfile] Failed to add AutoHand components: {e.Message}");
        }
    }
    
    /// <summary>
    /// Apply common components needed by both systems
    /// </summary>
    protected override void ApplyCommonComponents(GameObject target)
    {
        // Add SphereCollider for detection (both systems use this)
        SphereCollider sphereCollider = target.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = target.AddComponent<SphereCollider>();
        }
        sphereCollider.isTrigger = true;
        sphereCollider.radius = socketRadius;
        
        // Add Rigidbody (required for trigger detection)
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Socket points are typically kinematic
            rb.useGravity = false;
        }
        
        // Note: SnapValidator will be added in Phase 2 when we implement the event system
        // For now, we just ensure the basic components are present
    }
    
    /// <summary>
    /// Set AutoHand PlacePoint properties
    /// </summary>
    private void SetAutoHandPlacePointProperties(Component placePoint, System.Type placePointType)
    {
        try
        {
            // Set basic AutoHand PlacePoint properties
            SetFieldValue(placePoint, placePointType, "placeRadius", socketRadius);
            SetFieldValue(placePoint, placePointType, "forcePlace", false);
            SetFieldValue(placePoint, placePointType, "forceHandRelease", true);
            SetFieldValue(placePoint, placePointType, "parentOnPlace", true);
            SetFieldValue(placePoint, placePointType, "matchPosition", true);
            SetFieldValue(placePoint, placePointType, "matchRotation", true);
            SetFieldValue(placePoint, placePointType, "disableRigidbodyOnPlace", false);
            SetFieldValue(placePoint, placePointType, "makePlacedKinematic", true);
            
            // Set validation properties if we have accepted tags
            if (acceptedTags != null && acceptedTags.Length > 0)
            {
                // Try to set object names for validation
                var objectNamesField = placePointType.GetField("objectNames");
                if (objectNamesField != null)
                {
                    objectNamesField.SetValue(placePoint, acceptedTags);
                }
            }
            
            Debug.Log($"[SnapProfile] Successfully configured AutoHand PlacePoint properties for {placePoint.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SnapProfile] Could not set all AutoHand PlacePoint properties: {e.Message}");
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
            Debug.LogWarning($"[SnapProfile] Field '{fieldName}' not found in {type.Name}");
        }
    }
    
    /// <summary>
    /// Validate GameObject for XRI system
    /// </summary>
    protected override bool ValidateForXRI(GameObject target)
    {
        return target != null && target.CompareTag("snap");
    }
    
    /// <summary>
    /// Validate GameObject for AutoHand system
    /// </summary>
    protected override bool ValidateForAutoHand(GameObject target)
    {
        if (target == null || !target.CompareTag("snap"))
            return false;
            
        // Additional AutoHand-specific validation
        if (!IsAutoHandAvailable())
        {
            Debug.LogWarning("[SnapProfile] AutoHand not available in project");
            return false;
        }
        
        return true;
    }
}
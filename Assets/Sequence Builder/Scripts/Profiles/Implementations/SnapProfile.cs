// SnapProfile.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Profile for snap/socket interactions
/// </summary>
[CreateAssetMenu(fileName = "SnapProfile", menuName = "Sequence Builder/Snap Profile")]
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
    
    public override void ApplyToGameObject(GameObject target)
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
        
        // Add SnapValidator for custom validation
        SnapValidator validator = target.GetComponent<SnapValidator>();
        if (validator == null)
        {
            validator = target.AddComponent<SnapValidator>();
        }
        validator.Configure(this);
    }
    
    public override bool ValidateGameObject(GameObject target)
    {
        return target != null && target.CompareTag("snap");
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SimpleScale : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Vector3 originalScale;
    
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        originalScale = transform.localScale;
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener((args) => OnGrab());
            grabInteractable.selectExited.AddListener((args) => OnRelease());
        }
    }
    
    void OnGrab()
    {
        transform.localScale = originalScale * 0.5f;
    }
    
    void OnRelease()
    {
        transform.localScale = originalScale;
    }
}
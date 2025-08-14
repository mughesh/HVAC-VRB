using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketColorBehavior : MonoBehaviour
{
    public Color dockedColor = Color.green;
    public Color undockedColor = Color.red;
    public Color defaultColor = Color.white;
    
    private XRGrabInteractable grabInteractable;
    private XRSocketInteractor nearbySocket;
    private Renderer objectRenderer;
    private bool wasInSocket = false;
    
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        objectRenderer = GetComponent<Renderer>();
        
        // Find the socket in the scene
        nearbySocket = GameObject.Find("Snap")?.GetComponent<XRSocketInteractor>();
        
        if (nearbySocket != null)
        {
            // Subscribe to socket events
            nearbySocket.selectEntered.AddListener(OnSocketed);
            nearbySocket.selectExited.AddListener(OnUnsocketed);
        }
        
        // Set default color
        if (objectRenderer != null)
        {
            objectRenderer.material.color = defaultColor;
        }
    }
    
    void OnSocketed(SelectEnterEventArgs args)
    {
        // Check if this object is the one being socketed
        if (args.interactableObject?.transform == transform)
        {
            if (objectRenderer != null)
            {
                objectRenderer.material.color = dockedColor;
            }
            wasInSocket = true;
        }
    }
    
    void OnUnsocketed(SelectExitEventArgs args)
    {
        // Check if this object is the one being unsocketed
        if (args.interactableObject?.transform == transform)
        {
            if (objectRenderer != null)
            {
                objectRenderer.material.color = undockedColor;
            }
            wasInSocket = false;
        }
    }
    
    void OnDestroy()
    {
        if (nearbySocket != null)
        {
            nearbySocket.selectEntered.RemoveListener(OnSocketed);
            nearbySocket.selectExited.RemoveListener(OnUnsocketed);
        }
    }
}
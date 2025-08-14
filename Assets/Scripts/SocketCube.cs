using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketCube : MonoBehaviour
{
    private Color dockedColor = Color.green;
    private Color undockedColor = Color.red;
    private Color defaultColor = Color.white;
    
    private Renderer cubeRenderer;
    private bool wasInSocket = false;
    
    void Start()
    {
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.material.color = defaultColor;
        }
        
        // Find socket interactor in scene
        XRSocketInteractor socket = FindFirstObjectByType<XRSocketInteractor>();
        if (socket != null)
        {
            socket.selectEntered.AddListener(OnSocketEntered);
            socket.selectExited.AddListener(OnSocketExited);
        }
    }
    
    void OnSocketEntered(SelectEnterEventArgs args)
    {
        // Check if this cube was socketed
        if (args.interactableObject.transform.gameObject == gameObject)
        {
            wasInSocket = true;
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = dockedColor;
            }
        }
    }
    
    void OnSocketExited(SelectExitEventArgs args)
    {
        // Check if this cube was removed from socket
        if (args.interactableObject.transform.gameObject == gameObject)
        {
            wasInSocket = false;
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = undockedColor;
            }
        }
    }
}
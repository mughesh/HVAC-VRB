using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ColorChangeOnGrab : MonoBehaviour
{
    private Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
    private int currentColorIndex = 0;
    private XRGrabInteractable grabInteractable;
    private Renderer objectRenderer;
    
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        objectRenderer = GetComponent<Renderer>();
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener((args) => {
                currentColorIndex = (currentColorIndex + 1) % colors.Length;
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = colors[currentColorIndex];
                }
            });
        }
    }
}
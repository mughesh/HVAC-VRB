using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BouncyCube : MonoBehaviour
{
    public float normalBounciness = 0.3f;
    public float superBounciness = 1.0f;
    public Material normalMaterial;
    public Material bouncyMaterial;
    
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Collider col;
    private PhysicsMaterial physicsMat;
    private Renderer objectRenderer;
    
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        objectRenderer = GetComponent<Renderer>();
        
        // Create physics material
        physicsMat = new PhysicsMaterial("BouncyMat");
        physicsMat.bounciness = normalBounciness;
        physicsMat.bounceCombine = PhysicsMaterialCombine.Maximum;
        col.material = physicsMat;
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener((args) => OnGrab());
            grabInteractable.selectExited.AddListener((args) => OnRelease());
            grabInteractable.activated.AddListener((args) => OnActivate());
        }
    }
    
    void OnGrab()
    {
        // Make it super bouncy
        physicsMat.bounciness = superBounciness;
        
        // Change color to indicate bouncy mode
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.magenta;
        }
        
        // Reduce drag for better bouncing
        if (rb != null)
        {
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
        }
    }
    
    void OnRelease()
    {
        // Keep bouncy for a moment after release
        Invoke("ResetBounciness", 2f);
    }
    
    void OnActivate()
    {
        // When trigger is pressed, add upward force
        if (rb != null)
        {
            rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }
    
    void ResetBounciness()
    {
        physicsMat.bounciness = normalBounciness;
        
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.white;
        }
        
        if (rb != null)
        {
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Add particle effect or sound here if desired
        if (physicsMat.bounciness > 0.5f)
        {
            // Extra effect when super bouncy
            Debug.Log("BOING! Bounced with force: " + collision.relativeVelocity.magnitude);
        }
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;
using System.Collections.Generic;

public class ExplodingCube : MonoBehaviour
{
    [Header("Explosion Settings")]
    public int numberOfPieces = 8;
    public float explosionForce = 5f;
    public float explosionRadius = 2f;
    public float reassembleDelay = 3f;
    public float reassembleSpeed = 2f;
    
    private XRGrabInteractable grabInteractable;
    private bool isExploded = false;
    private List<GameObject> pieces = new List<GameObject>();
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private MeshRenderer mainRenderer;
    private Collider mainCollider;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        mainRenderer = GetComponent<MeshRenderer>();
        mainCollider = GetComponent<Collider>();
        
        // Store initial transform
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.activated.AddListener(OnActivate);
        }
        
        CreatePieces();
    }
    
    
    void CreatePieces()
    {
        Vector3 cubeSize = transform.localScale * 0.4f;
        float offset = 0.15f;
        
        for (int i = 0; i < numberOfPieces; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.transform.parent = transform;
            piece.transform.localScale = cubeSize;
            
            // Position pieces in a grid pattern
            float x = (i % 2 == 0) ? offset : -offset;
            float y = ((i / 2) % 2 == 0) ? offset : -offset;
            float z = (i / 4 == 0) ? offset : -offset;
            
            piece.transform.localPosition = new Vector3(x, y, z);
            piece.SetActive(false);
            
            // Add physics
            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            
            // Store original transform
            originalPositions.Add(piece.transform.localPosition);
            originalRotations.Add(piece.transform.localRotation);
            
            // Random color for each piece
            piece.GetComponent<Renderer>().material.color = Random.ColorHSV();
            
            pieces.Add(piece);
        }
    }
    
    void OnGrab(SelectEnterEventArgs args)
    {
        if (!isExploded)
        {
            Explode();
        }
    }
    
    void OnActivate(ActivateEventArgs args)
    {
        if (isExploded)
        {
            StartCoroutine(Reassemble());
        }
    }
    
    void Explode()
    {
        isExploded = true;
        mainRenderer.enabled = false;
        mainCollider.enabled = false;
        
        foreach (GameObject piece in pieces)
        {
            piece.SetActive(true);
            piece.transform.parent = null;
            
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionPos = transform.position - Vector3.up * 0.5f;
                rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
        }
        
        StartCoroutine(AutoReassemble());
    }
    
    IEnumerator AutoReassemble()
    {
        yield return new WaitForSeconds(reassembleDelay);
        yield return Reassemble();
    }
    
    IEnumerator Reassemble()
    {
        // Reset main object position
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // Parent pieces back
        foreach (GameObject piece in pieces)
        {
            piece.transform.parent = transform;
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        
        // Animate back to position
        float elapsed = 0;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * reassembleSpeed;
            float t = Mathf.SmoothStep(0, 1, elapsed);
            
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].transform.localPosition = Vector3.Lerp(pieces[i].transform.localPosition, originalPositions[i], t);
                pieces[i].transform.localRotation = Quaternion.Lerp(pieces[i].transform.localRotation, originalRotations[i], t);
            }
            
            yield return null;
        }
        
        // Hide pieces and show main cube
        foreach (GameObject piece in pieces)
        {
            piece.SetActive(false);
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }
        
        mainRenderer.enabled = true;
        mainCollider.enabled = true;
        isExploded = false;
    }
    
    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveAllListeners();
            grabInteractable.activated.RemoveAllListeners();
        }
    }
}
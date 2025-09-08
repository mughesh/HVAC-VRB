// SnapValidator.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Validates what can be snapped to a socket
/// </summary>
public class SnapValidator : MonoBehaviour
{
    private SnapProfile profile;
    private XRSocketInteractor socketInteractor;
    
    private void Awake()
    {
        socketInteractor = GetComponent<XRSocketInteractor>();
    }
    
    private void OnEnable()
    {
        if (socketInteractor != null)
        {
            Debug.Log($"[SnapValidator] OnEnable called on {gameObject.name}");
            socketInteractor.selectEntered.AddListener(OnObjectSnapped);
            socketInteractor.selectExited.AddListener(OnObjectRemoved);
        }
    }
    
    private void OnDisable()
    {
        if (socketInteractor != null)
        {
            Debug.Log($"[SnapValidator] OnDisable called on {gameObject.name}");
            socketInteractor.selectEntered.RemoveListener(OnObjectSnapped);
            socketInteractor.selectExited.RemoveListener(OnObjectRemoved);
        }
    }
    
    public void Configure(SnapProfile snapProfile)
    {
        profile = snapProfile;
        
        if (socketInteractor != null)
        {
            // Set up hover validation
            socketInteractor.hoverEntered.AddListener(OnHoverEntered);
        }
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Validate if this object can be snapped
        if (!IsValidForSocket(args.interactableObject.transform.gameObject))
        {
            // Could add visual feedback for invalid object
            Debug.Log($"Object {args.interactableObject.transform.name} is not valid for this socket");
        }
    }
    
    private bool IsValidForSocket(GameObject obj)
    {
        if (profile == null) return true;
        
        // Check if requires specific objects
        if (profile.requireSpecificObjects && profile.specificAcceptedObjects != null)
        {
            foreach (var acceptedObj in profile.specificAcceptedObjects)
            {
                if (obj == acceptedObj) return true;
            }
            return false;
        }
        
        // Check tags
        if (profile.acceptedTags != null && profile.acceptedTags.Length > 0)
        {
            foreach (var tag in profile.acceptedTags)
            {
                if (obj.CompareTag(tag)) return true;
            }
            return false;
        }
        
        return true;
    }
    
    private void OnObjectSnapped(SelectEnterEventArgs args)
    {
        GameObject snappedObject = args.interactableObject.transform.gameObject;
        
        if (!IsValidForSocket(snappedObject))
        {
            // Eject invalid object
            StartCoroutine(EjectInvalidObject());
        }
        else
        {
            Debug.Log($"[SnapValidator] Successfully snapped: {snappedObject.name}");
            
            // Notify ToolController if the snapped object is a tool
            ToolController toolController = snappedObject.GetComponent<ToolController>();
            if (toolController != null)
            {
                toolController.OnSocketSnapped(gameObject);
                Debug.Log($"[SnapValidator] Notified ToolController on {snappedObject.name} about socket snap");
            }
            
            // Fire event for sequence system
            var sequenceController = FindObjectOfType<SequenceController>();
            sequenceController?.OnObjectSnapped(gameObject, snappedObject);
        }
    }
    
    private void OnObjectRemoved(SelectExitEventArgs args)
    {
        GameObject removedObject = args.interactableObject.transform.gameObject;
        Debug.Log($"[SnapValidator] Object removed: {removedObject.name}");
        
        // Notify ToolController if the removed object is a tool
        ToolController toolController = removedObject.GetComponent<ToolController>();
        if (toolController != null)
        {
            toolController.OnSocketReleased(gameObject);
            Debug.Log($"[SnapValidator] Notified ToolController on {removedObject.name} about socket release");
        }
        
        // Fire event for sequence system
        var sequenceController = FindObjectOfType<SequenceController>();
        sequenceController?.OnObjectUnsnapped(gameObject, removedObject);
    }
    
    private IEnumerator EjectInvalidObject()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (socketInteractor != null && socketInteractor.hasSelection)
        {
            // Force eject
            var interactable = socketInteractor.GetOldestInteractableSelected();
            socketInteractor.interactionManager.SelectExit(socketInteractor, interactable);
        }
    }
}
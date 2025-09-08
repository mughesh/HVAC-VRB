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
    
    private void Start()
    {
        Debug.Log($"[SnapValidator] Start() called on {gameObject.name}");
        
        // Subscribe to socket events in Start() - this should always work
        if (socketInteractor != null)
        {
            Debug.Log($"[SnapValidator] SocketInteractor found in Start(), subscribing to events");
            socketInteractor.selectEntered.AddListener(OnObjectSnapped);
            socketInteractor.selectExited.AddListener(OnObjectRemoved);
            Debug.Log($"[SnapValidator] Event listeners added in Start(): selectEntered, selectExited");
        }
        else
        {
            Debug.LogError($"[SnapValidator] SocketInteractor is NULL in Start()! Cannot subscribe to events!");
        }
    }
    
    private void OnEnable()
    {
        Debug.Log($"[SnapValidator] OnEnable called on {gameObject.name}");
        
        // Try to subscribe here too in case Start hasn't run yet or component was disabled/enabled
        if (socketInteractor != null)
        {
            Debug.Log($"[SnapValidator] SocketInteractor found in OnEnable(), refreshing event subscriptions");
            // Remove first to prevent duplicates, then add
            socketInteractor.selectEntered.RemoveListener(OnObjectSnapped);
            socketInteractor.selectExited.RemoveListener(OnObjectRemoved);
            socketInteractor.selectEntered.AddListener(OnObjectSnapped);
            socketInteractor.selectExited.AddListener(OnObjectRemoved);
            Debug.Log($"[SnapValidator] Event listeners refreshed in OnEnable()");
        }
        else
        {
            Debug.LogWarning($"[SnapValidator] SocketInteractor is NULL in OnEnable() - will try again in Start()");
        }
    }
    
    private void OnDisable()
    {
        Debug.Log($"[SnapValidator] OnDisable called on {gameObject.name}");
        
        // Clean up all event subscriptions
        if (socketInteractor != null)
        {
            Debug.Log($"[SnapValidator] Removing all event listeners in OnDisable");
            socketInteractor.selectEntered.RemoveListener(OnObjectSnapped);
            socketInteractor.selectExited.RemoveListener(OnObjectRemoved);
            socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
        }
    }
    
    public void Configure(SnapProfile snapProfile)
    {
        Debug.Log($"[SnapValidator] Configure() called on {gameObject.name} with profile: {snapProfile?.profileName ?? "NULL"}");
        
        profile = snapProfile;
        
        if (socketInteractor != null)
        {
            Debug.Log($"[SnapValidator] SocketInteractor found, adding hover listener only");
            // Only add hover validation here (not critical for snap events)
            socketInteractor.hoverEntered.RemoveListener(OnHoverEntered); // Remove to prevent duplicates
            socketInteractor.hoverEntered.AddListener(OnHoverEntered);
        }
        else
        {
            Debug.LogError($"[SnapValidator] SocketInteractor is NULL in Configure()!");
        }
        
        Debug.Log($"[SnapValidator] Configure() completed for {gameObject.name}");
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
        Debug.Log($"[SnapValidator] IsValidForSocket called for {obj.name}");
        
        if (profile == null) 
        {
            Debug.LogWarning($"[SnapValidator] Profile is NULL - Configure() may not have been called! Trying to find SnapProfile automatically...");
            
            // Try to auto-find SnapProfile if Configure() wasn't called
            SnapProfile foundProfile = FindSnapProfileInResources();
            if (foundProfile != null)
            {
                Debug.Log($"[SnapValidator] Found SnapProfile in Resources: {foundProfile.profileName}");
                profile = foundProfile;
            }
            else
            {
                Debug.LogWarning($"[SnapValidator] No SnapProfile found, accepting all objects by default");
                return true;
            }
        }
        
        // Check if requires specific objects
        if (profile.requireSpecificObjects && profile.specificAcceptedObjects != null)
        {
            Debug.Log($"[SnapValidator] Checking specific objects (count: {profile.specificAcceptedObjects.Length})");
            foreach (var acceptedObj in profile.specificAcceptedObjects)
            {
                Debug.Log($"[SnapValidator] Comparing {obj.name} with specific object {acceptedObj?.name ?? "NULL"}");
                if (obj == acceptedObj) 
                {
                    Debug.Log($"[SnapValidator] MATCH found with specific object!");
                    return true;
                }
            }
            Debug.Log($"[SnapValidator] No match found in specific objects, REJECTING");
            return false;
        }
        
        // Check tags
        if (profile.acceptedTags != null && profile.acceptedTags.Length > 0)
        {
            Debug.Log($"[SnapValidator] Checking tags for {obj.name} (tag: '{obj.tag}')");
            Debug.Log($"[SnapValidator] Accepted tags: [{string.Join(", ", profile.acceptedTags)}]");
            
            foreach (var tag in profile.acceptedTags)
            {
                Debug.Log($"[SnapValidator] Comparing '{obj.tag}' with accepted tag '{tag}'");
                if (obj.CompareTag(tag)) 
                {
                    Debug.Log($"[SnapValidator] TAG MATCH found! Accepting object");
                    return true;
                }
            }
            Debug.Log($"[SnapValidator] No matching tags found, REJECTING");
            return false;
        }
        
        Debug.Log($"[SnapValidator] No restrictions defined, accepting by default");
        return true;
    }
    
    private void OnObjectSnapped(SelectEnterEventArgs args)
    {
        Debug.Log($"[SnapValidator] OnObjectSnapped FIRED on {gameObject.name}!");
        
        GameObject snappedObject = args.interactableObject.transform.gameObject;
        Debug.Log($"[SnapValidator] Snapped object: {snappedObject.name} with tag: {snappedObject.tag}");
        
        // Check profile state
        if (profile == null)
        {
            Debug.LogWarning($"[SnapValidator] Profile is NULL on {gameObject.name}");
        }
        else
        {
            Debug.Log($"[SnapValidator] Profile found: acceptedTags=[{string.Join(", ", profile.acceptedTags ?? new string[0])}], requireSpecificObjects={profile.requireSpecificObjects}");
        }
        
        bool isValid = IsValidForSocket(snappedObject);
        Debug.Log($"[SnapValidator] Validation result for {snappedObject.name}: {isValid}");
        
        // ALWAYS notify ToolController and sequence system first, regardless of validation
        Debug.Log($"[SnapValidator] Processing snap notification for {snappedObject.name}");
        
        // Notify ToolController if the snapped object is a tool
        ToolController toolController = snappedObject.GetComponent<ToolController>();
        if (toolController != null)
        {
            Debug.Log($"[SnapValidator] Found ToolController on {snappedObject.name}, calling OnSocketSnapped");
            toolController.OnSocketSnapped(gameObject);
            Debug.Log($"[SnapValidator] Notified ToolController on {snappedObject.name} about socket snap");
        }
        else
        {
            Debug.Log($"[SnapValidator] No ToolController found on {snappedObject.name}");
        }
        
        // Fire event for sequence system
        var sequenceController = FindObjectOfType<SequenceController>();
        sequenceController?.OnObjectSnapped(gameObject, snappedObject);
        
        // Handle validation failure AFTER notifications (delayed ejection)
        if (!isValid)
        {
            Debug.LogWarning($"[SnapValidator] Will eject {snappedObject.name} in next frame - validation failed");
            StartCoroutine(DelayedEjectInvalidObject());
        }
        else
        {
            Debug.Log($"[SnapValidator] Successfully snapped: {snappedObject.name} - validation passed");
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
    
    private IEnumerator DelayedEjectInvalidObject()
    {
        Debug.Log($"[SnapValidator] DelayedEjectInvalidObject started - waiting for next frame");
        yield return null; // Wait one frame to ensure all events complete
        
        if (socketInteractor != null && socketInteractor.hasSelection)
        {
            Debug.Log($"[SnapValidator] Ejecting invalid object after delay");
            // Force eject
            var interactable = socketInteractor.GetOldestInteractableSelected();
            socketInteractor.interactionManager.SelectExit(socketInteractor, interactable);
        }
        else
        {
            Debug.Log($"[SnapValidator] No object to eject (socket empty or null)");
        }
    }
    
    private SnapProfile FindSnapProfileInResources()
    {
        // Try to find SnapProfile in Resources folder
        SnapProfile[] profiles = Resources.LoadAll<SnapProfile>("");
        if (profiles.Length > 0)
        {
            Debug.Log($"[SnapValidator] Found {profiles.Length} SnapProfile(s) in Resources, using first one: {profiles[0].profileName}");
            return profiles[0];
        }
        
        Debug.LogWarning($"[SnapValidator] No SnapProfile found in Resources folder");
        return null;
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
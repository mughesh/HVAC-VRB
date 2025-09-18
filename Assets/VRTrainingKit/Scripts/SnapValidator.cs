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
        VRTrainingDebug.Log($"[SnapValidator] Start() called on {gameObject.name}");
        
        // Subscribe to socket events in Start() - this should always work
        if (socketInteractor != null)
        {
            VRTrainingDebug.LogEvent($"[SnapValidator] SocketInteractor found, subscribing to events on {gameObject.name}");
            socketInteractor.selectEntered.AddListener(OnObjectSnapped);
            socketInteractor.selectExited.AddListener(OnObjectRemoved);
        }
        else
        {
            VRTrainingDebug.LogError($"[SnapValidator] SocketInteractor is NULL in Start()! Cannot subscribe to events!");
        }
    }
    
    private void OnEnable()
    {
        VRTrainingDebug.Log($"[SnapValidator] OnEnable called on {gameObject.name}");
        
        // Try to subscribe here too in case Start hasn't run yet or component was disabled/enabled
        if (socketInteractor != null)
        {
            VRTrainingDebug.LogEvent($"[SnapValidator] Refreshing event subscriptions on {gameObject.name}");
            // Remove first to prevent duplicates, then add
            socketInteractor.selectEntered.RemoveListener(OnObjectSnapped);
            socketInteractor.selectExited.RemoveListener(OnObjectRemoved);
            socketInteractor.selectEntered.AddListener(OnObjectSnapped);
            socketInteractor.selectExited.AddListener(OnObjectRemoved);
        }
        else
        {
            VRTrainingDebug.LogWarning($"[SnapValidator] SocketInteractor is NULL in OnEnable() - will try again in Start()");
        }
    }
    
    private void OnDisable()
    {
        VRTrainingDebug.Log($"[SnapValidator] OnDisable called on {gameObject.name}");
        
        // Clean up all event subscriptions
        if (socketInteractor != null)
        {
            VRTrainingDebug.LogEvent($"[SnapValidator] Removing all event listeners from {gameObject.name}");
            socketInteractor.selectEntered.RemoveListener(OnObjectSnapped);
            socketInteractor.selectExited.RemoveListener(OnObjectRemoved);
            socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
        }
    }
    
    public void Configure(SnapProfile snapProfile)
    {
        VRTrainingDebug.Log($"[SnapValidator] Configure() called on {gameObject.name} with profile: {snapProfile?.profileName ?? "NULL"}");
        
        profile = snapProfile;
        
        if (socketInteractor != null)
        {
            // Only add hover validation here (not critical for snap events)
            socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
            socketInteractor.hoverEntered.AddListener(OnHoverEntered);
        }
        else
        {
            VRTrainingDebug.LogError($"[SnapValidator] SocketInteractor is NULL in Configure()!");
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
        VRTrainingDebug.LogValidation($"[SnapValidator] Validating {obj.name} for socket {gameObject.name}");
        
        if (profile == null) 
        {
            VRTrainingDebug.LogWarning($"[SnapValidator] Profile is NULL - trying to find SnapProfile automatically...");
            
            // Try to auto-find SnapProfile if Configure() wasn't called
            SnapProfile foundProfile = FindSnapProfileInResources();
            if (foundProfile != null)
            {
                VRTrainingDebug.Log($"[SnapValidator] Found SnapProfile in Resources: {foundProfile.profileName}");
                profile = foundProfile;
            }
            else
            {
                VRTrainingDebug.LogWarning($"[SnapValidator] No SnapProfile found, accepting all objects by default");
                return true;
            }
        }
        
        // Check if requires specific objects
        if (profile.requireSpecificObjects && profile.specificAcceptedObjects != null)
        {
            VRTrainingDebug.LogValidation($"[SnapValidator] Checking specific objects for {obj.name}");
            foreach (var acceptedObj in profile.specificAcceptedObjects)
            {
                if (obj == acceptedObj) 
                {
                    VRTrainingDebug.LogValidation($"[SnapValidator] MATCH found with specific object!");
                    return true;
                }
            }
            VRTrainingDebug.LogValidation($"[SnapValidator] No match found in specific objects, REJECTING");
            return false;
        }
        
        // Check tags
        if (profile.acceptedTags != null && profile.acceptedTags.Length > 0)
        {
            VRTrainingDebug.LogValidation($"[SnapValidator] Checking tags for {obj.name} ('{obj.tag}') against [{string.Join(", ", profile.acceptedTags)}]");
            
            foreach (var tag in profile.acceptedTags)
            {
                if (obj.CompareTag(tag)) 
                {
                    VRTrainingDebug.LogValidation($"[SnapValidator] TAG MATCH found! Accepting object");
                    return true;
                }
            }
            VRTrainingDebug.LogValidation($"[SnapValidator] No matching tags found, REJECTING");
            return false;
        }
        
        VRTrainingDebug.LogValidation($"[SnapValidator] No restrictions defined, accepting by default");
        return true;
    }
    
    private void OnObjectSnapped(SelectEnterEventArgs args)
    {
        GameObject snappedObject = args.interactableObject.transform.gameObject;
        
        VRTrainingDebug.LogEvent($"[SnapValidator] Object snapped: {snappedObject.name} on {gameObject.name}");
        
        bool isValid = IsValidForSocket(snappedObject);
        VRTrainingDebug.LogValidation($"[SnapValidator] Validation result for {snappedObject.name}: {isValid}");
        
        // ALWAYS notify ToolController and sequence system first, regardless of validation
        
        // Notify ToolController if the snapped object is a tool
        ToolController toolController = snappedObject.GetComponent<ToolController>();
        if (toolController != null)
        {
            VRTrainingDebug.LogEvent($"[SnapValidator] Notifying ToolController on {snappedObject.name}");
            toolController.OnSocketSnapped(gameObject);
        }
        
        // Notify ValveController if the snapped object is a valve
        ValveController valveController = snappedObject.GetComponent<ValveController>();
        if (valveController != null)
        {
            VRTrainingDebug.LogEvent($"[SnapValidator] Notifying ValveController on {snappedObject.name}");
            valveController.OnSocketSnapped(gameObject);
        }
        
        // Fire event for sequence system
        var sequenceController = FindObjectOfType<LegacySequenceController>();
        sequenceController?.OnObjectSnapped(gameObject, snappedObject);
        
        // Handle validation failure AFTER notifications (delayed ejection)
        if (!isValid)
        {
            VRTrainingDebug.LogWarning($"[SnapValidator] Will eject {snappedObject.name} - validation failed");
            StartCoroutine(DelayedEjectInvalidObject());
        }
    }
    
    private void OnObjectRemoved(SelectExitEventArgs args)
    {
        GameObject removedObject = args.interactableObject.transform.gameObject;
        VRTrainingDebug.LogEvent($"[SnapValidator] Object removed: {removedObject.name} from {gameObject.name}");
        
        // Notify ToolController if the removed object is a tool
        ToolController toolController = removedObject.GetComponent<ToolController>();
        if (toolController != null)
        {
            toolController.OnSocketReleased(gameObject);
        }
        
        // Notify ValveController if the removed object is a valve
        ValveController valveController = removedObject.GetComponent<ValveController>();
        if (valveController != null)
        {
            valveController.OnSocketReleased(gameObject);
        }
        
        // Fire event for sequence system
        var sequenceController = FindObjectOfType<LegacySequenceController>();
        sequenceController?.OnObjectUnsnapped(gameObject, removedObject);
    }
    
    private IEnumerator DelayedEjectInvalidObject()
    {
        VRTrainingDebug.LogValidation($"[SnapValidator] Ejecting invalid object after delay");
        yield return null; // Wait one frame to ensure all events complete
        
        if (socketInteractor != null && socketInteractor.hasSelection)
        {
            // Force eject
            var interactable = socketInteractor.GetOldestInteractableSelected();
            socketInteractor.interactionManager.SelectExit(socketInteractor, interactable);
        }
    }
    
    private SnapProfile FindSnapProfileInResources()
    {
        // Try to find SnapProfile in Resources folder
        SnapProfile[] profiles = Resources.LoadAll<SnapProfile>("");
        if (profiles.Length > 0)
        {
            VRTrainingDebug.Log($"[SnapValidator] Found {profiles.Length} SnapProfile(s) in Resources, using: {profiles[0].profileName}");
            return profiles[0];
        }
        
        VRTrainingDebug.LogWarning($"[SnapValidator] No SnapProfile found in Resources folder");
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
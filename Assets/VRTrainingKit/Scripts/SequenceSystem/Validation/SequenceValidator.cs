// SequenceValidator.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Validates sequence requirements before allowing interactions
/// </summary>
public class SequenceValidator : MonoBehaviour
{
    [Header("Sequence Requirements")]
    public string requiredStateGroup = "";
    public bool allowWithWarning = true;
    public string warningMessage = "This action should not be performed yet!";
    
    private XRBaseInteractable interactable;
    private LegacySequenceController sequenceController;
    private bool isLocked = false;
    
    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        sequenceController = FindObjectOfType<LegacySequenceController>();
    }
    
    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.selectEntered.AddListener(OnSelectEntered);
        }
    }
    
    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        CheckSequenceRequirements();
    }
    
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (isLocked && !allowWithWarning)
        {
            // Prevent interaction
            StartCoroutine(ForceDeselect(args));
        }
        else if (isLocked && allowWithWarning)
        {
            // Show warning but allow
            ShowWarning();
        }
    }
    
    private void CheckSequenceRequirements()
    {
        if (sequenceController != null && !string.IsNullOrEmpty(requiredStateGroup))
        {
            isLocked = !sequenceController.IsStateGroupActive(requiredStateGroup);
            UpdateVisualFeedback();
        }
    }
    
    private void UpdateVisualFeedback()
    {
        // Change material or outline color based on lock state
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // This is simplified - you'd want a more sophisticated material swapping system
            if (isLocked)
            {
                // Could tint red or add outline
                renderer.material.color = new Color(1f, 0.7f, 0.7f);
            }
            else
            {
                renderer.material.color = Color.white;
            }
        }
    }
    
    private void ShowWarning()
    {
        Debug.LogWarning($"[Sequence Warning] {warningMessage}");
        // In a real implementation, this would show UI feedback
    }
    
    private IEnumerator ForceDeselect(SelectEnterEventArgs args)
    {
        yield return null; // Wait one frame
        
        if (interactable != null && args.interactorObject != null)
        {
            interactable.interactionManager.SelectExit(
                args.interactorObject as IXRSelectInteractor, 
                interactable as IXRSelectInteractable
            );
        }
    }
}
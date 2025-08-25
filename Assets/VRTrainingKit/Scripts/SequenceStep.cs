using UnityEngine;
using System.Linq;

/// <summary>
/// Individual step in a training sequence
/// Simple data structure for step requirements and conditions
/// </summary>
[System.Serializable]
public class SequenceStep
{
    [Header("Step Information")]
    [Tooltip("Name of this step (e.g., 'Connect Male Hose')")]
    public string stepName = "New Step";
    
    [Tooltip("Instruction text displayed to the user")]
    [TextArea(2, 4)]
    public string instruction = "Complete this interaction to continue";
    
    [Header("Requirements")]
    [Tooltip("What type of interaction is required")]
    public RequirementType requirementType = RequirementType.MustBeSnapped;
    
    [Tooltip("Primary object that must be interacted with")]
    public GameObject requiredObject;
    
    [Tooltip("Secondary object (e.g., snap point for snapping interactions)")]
    public GameObject secondaryObject;
    
    [Tooltip("Target value for knob/rotation requirements (in degrees)")]
    public float targetValue = 0f;
    
    [Tooltip("Tolerance for value-based requirements (±degrees)")]
    public float tolerance = 5f;
    
    [Header("Step Status")]
    [Tooltip("Is this step currently completed?")]
    [ReadOnly]
    public bool isCompleted = false;
    
    public enum RequirementType
    {
        MustBeSnapped,      // Object must be snapped to a point
        MustBeGrabbed,      // Object must be grabbed/held
        MustBeTurned,       // Knob must be turned to specific angle
        MustBeNearby,       // Object must be within distance
        Custom              // For future extensibility
    }
    
    /// <summary>
    /// Check if this step's requirements are met
    /// </summary>
    public bool CheckCompletion()
    {
        if (requiredObject == null)
        {
            Debug.LogWarning($"Step '{stepName}' has no required object assigned");
            return false;
        }
        
        switch (requirementType)
        {
            case RequirementType.MustBeSnapped:
                return CheckSnapRequirement();
                
            case RequirementType.MustBeGrabbed:
                return CheckGrabRequirement();
                
            case RequirementType.MustBeTurned:
                return CheckKnobRequirement();
                
            case RequirementType.MustBeNearby:
                return CheckProximityRequirement();
                
            default:
                return false;
        }
    }
    
    private bool CheckSnapRequirement()
    {
        // Check if required object is snapped to secondary object (snap point)
        var socketInteractor = secondaryObject?.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (socketInteractor != null && socketInteractor.hasSelection)
        {
            var snappedObject = socketInteractor.interactablesSelected.FirstOrDefault()?.transform.gameObject;
            return snappedObject == requiredObject;
        }
        return false;
    }
    
    private bool CheckGrabRequirement()
    {
        // Check if required object is currently being grabbed
        var grabInteractable = requiredObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        return grabInteractable != null && grabInteractable.isSelected;
    }
    
    private bool CheckKnobRequirement()
    {
        // Check if knob is at target angle
        var knobController = requiredObject.GetComponent<KnobController>();
        if (knobController != null)
        {
            float currentAngle = knobController.CurrentAngle;
            return Mathf.Abs(currentAngle - targetValue) <= tolerance;
        }
        return false;
    }
    
    private bool CheckProximityRequirement()
    {
        // Check if objects are within specified distance
        if (secondaryObject != null)
        {
            float distance = Vector3.Distance(requiredObject.transform.position, secondaryObject.transform.position);
            return distance <= tolerance; // Using tolerance as distance threshold
        }
        return false;
    }
    
    /// <summary>
    /// Update the completion status of this step
    /// </summary>
    public void UpdateCompletion()
    {
        bool wasCompleted = isCompleted;
        isCompleted = CheckCompletion();
        
        // Log status changes for debugging
        if (wasCompleted != isCompleted)
        {
            if (isCompleted)
                Debug.Log($"[Sequence] Step completed: {stepName}");
            else
                Debug.Log($"[Sequence] Step incomplete: {stepName}");
        }
    }
    
    /// <summary>
    /// Reset this step to incomplete state
    /// </summary>
    public void ResetStep()
    {
        isCompleted = false;
    }
}


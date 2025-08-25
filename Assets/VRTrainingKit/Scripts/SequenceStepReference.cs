using UnityEngine;
using System.Linq;

/// <summary>
/// Alternative approach using string names instead of direct GameObject references
/// This works better with ScriptableObjects and is more reliable for serialization
/// </summary>
[System.Serializable]
public class SequenceStepReference
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
    
    [Tooltip("Name of the primary object (must match GameObject name in scene)")]
    public string requiredObjectName = "";
    
    [Tooltip("Name of the secondary object (e.g., snap point name)")]
    public string secondaryObjectName = "";
    
    [Tooltip("Target value for knob/rotation requirements (in degrees)")]
    public float targetValue = 0f;
    
    [Tooltip("Tolerance for value-based requirements (±degrees)")]
    public float tolerance = 5f;
    
    [Header("Step Status")]
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
    /// Get the actual GameObject from the scene by name
    /// </summary>
    public GameObject GetRequiredObject()
    {
        if (string.IsNullOrEmpty(requiredObjectName))
            return null;
            
        return GameObject.Find(requiredObjectName);
    }
    
    /// <summary>
    /// Get the secondary GameObject from the scene by name
    /// </summary>
    public GameObject GetSecondaryObject()
    {
        if (string.IsNullOrEmpty(secondaryObjectName))
            return null;
            
        return GameObject.Find(secondaryObjectName);
    }
    
    /// <summary>
    /// Check if this step's requirements are met
    /// </summary>
    public bool CheckCompletion()
    {
        GameObject requiredObj = GetRequiredObject();
        if (requiredObj == null)
        {
            Debug.LogWarning($"Step '{stepName}' - Required object '{requiredObjectName}' not found in scene");
            return false;
        }
        
        switch (requirementType)
        {
            case RequirementType.MustBeSnapped:
                return CheckSnapRequirement(requiredObj);
                
            case RequirementType.MustBeGrabbed:
                return CheckGrabRequirement(requiredObj);
                
            case RequirementType.MustBeTurned:
                return CheckKnobRequirement(requiredObj);
                
            case RequirementType.MustBeNearby:
                return CheckProximityRequirement(requiredObj);
                
            default:
                return false;
        }
    }
    
    private bool CheckSnapRequirement(GameObject requiredObj)
    {
        GameObject secondaryObj = GetSecondaryObject();
        if (secondaryObj == null)
        {
            Debug.LogWarning($"Step '{stepName}' - Secondary object '{secondaryObjectName}' not found for snap requirement");
            return false;
        }
        
        var socketInteractor = secondaryObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (socketInteractor != null && socketInteractor.hasSelection)
        {
            var snappedObject = socketInteractor.interactablesSelected.FirstOrDefault()?.transform.gameObject;
            return snappedObject == requiredObj;
        }
        return false;
    }
    
    private bool CheckGrabRequirement(GameObject requiredObj)
    {
        var grabInteractable = requiredObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        return grabInteractable != null && grabInteractable.isSelected;
    }
    
    private bool CheckKnobRequirement(GameObject requiredObj)
    {
        var knobController = requiredObj.GetComponent<KnobController>();
        if (knobController != null)
        {
            float currentAngle = knobController.CurrentAngle;
            return Mathf.Abs(currentAngle - targetValue) <= tolerance;
        }
        return false;
    }
    
    private bool CheckProximityRequirement(GameObject requiredObj)
    {
        GameObject secondaryObj = GetSecondaryObject();
        if (secondaryObj != null)
        {
            float distance = Vector3.Distance(requiredObj.transform.position, secondaryObj.transform.position);
            return distance <= tolerance;
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
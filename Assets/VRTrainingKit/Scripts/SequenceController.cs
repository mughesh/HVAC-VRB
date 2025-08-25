using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controls the sequence and state flow of training scenarios
/// </summary>
public class SequenceController : MonoBehaviour
{
    [System.Serializable]
    public class StateGroup
    {
        public string groupName = "New State";
        public bool isActive = false;
        public List<Condition> activationConditions = new List<Condition>();
        public List<string> allowedActions = new List<string>();
        public List<string> lockedActions = new List<string>();
        
        [System.Serializable]
        public class Condition
        {
            public enum ConditionType
            {
                ObjectSnapped,
                ObjectGrabbed,
                KnobTurned,
                AllObjectsSnapped,
                Custom
            }
            
            public ConditionType type;
            public GameObject targetObject;
            public GameObject secondaryObject; // For snap conditions
            public float targetValue; // For knob conditions
            public bool isMet = false;
        }
        
        public bool CheckConditions()
        {
            if (activationConditions.Count == 0) return true;
            
            foreach (var condition in activationConditions)
            {
                if (!condition.isMet) return false;
            }
            return true;
        }
    }
    
    [Header("State Configuration")]
    public List<StateGroup> stateGroups = new List<StateGroup>();
    public StateGroup currentStateGroup;
    
    [Header("Visual Feedback")]
    public bool showDebugUI = true;
    public Color lockedColor = Color.red;
    public Color warningColor = Color.yellow;
    public Color availableColor = Color.green;
    
    // Tracking
    private Dictionary<GameObject, GameObject> snappedConnections = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, float> knobValues = new Dictionary<GameObject, float>();
    
    // Events
    public event System.Action<StateGroup> OnStateGroupChanged;
    public event System.Action<string> OnActionBlocked;
    public event System.Action<string> OnActionWarning;
    
    private void Start()
    {
        InitializeStateGroups();
    }
    
    private void InitializeStateGroups()
    {
        // Set initial state
        if (stateGroups.Count > 0)
        {
            currentStateGroup = stateGroups[0];
            currentStateGroup.isActive = true;
        }
        
        // Subscribe to knob events
        var knobControllers = FindObjectsOfType<KnobController>();
        foreach (var knob in knobControllers)
        {
            knob.OnAngleChanged += (angle) => OnKnobTurned(knob.gameObject, angle);
        }
    }
    
    public bool IsStateGroupActive(string groupName)
    {
        var group = stateGroups.FirstOrDefault(g => g.groupName == groupName);
        return group != null && group.isActive;
    }
    
    public void OnObjectSnapped(GameObject snapPoint, GameObject snappedObject)
    {
        Debug.Log($"[Sequence] Object snapped: {snappedObject.name} -> {snapPoint.name}");
        
        // Track connection
        snappedConnections[snapPoint] = snappedObject;
        
        // Update conditions
        UpdateSnapConditions(snapPoint, snappedObject, true);
        
        // Check for state transitions
        CheckStateTransitions();
    }
    
    public void OnObjectUnsnapped(GameObject snapPoint, GameObject unsnappedObject)
    {
        Debug.Log($"[Sequence] Object unsnapped: {unsnappedObject.name} from {snapPoint.name}");
        
        // Remove connection
        if (snappedConnections.ContainsKey(snapPoint))
        {
            snappedConnections.Remove(snapPoint);
        }
        
        // Update conditions
        UpdateSnapConditions(snapPoint, unsnappedObject, false);
        
        // Check for state transitions
        CheckStateTransitions();
    }
    
    public void OnKnobTurned(GameObject knob, float angle)
    {
        knobValues[knob] = angle;
        
        // Update conditions
        UpdateKnobConditions(knob, angle);
        
        // Check if this action is allowed
        if (!IsActionAllowed(knob.name))
        {
            OnActionWarning?.Invoke($"Warning: {knob.name} should not be operated yet!");
        }
        
        // Check for state transitions
        CheckStateTransitions();
    }
    
    private void UpdateSnapConditions(GameObject snapPoint, GameObject snappedObject, bool isSnapped)
    {
        foreach (var group in stateGroups)
        {
            foreach (var condition in group.activationConditions)
            {
                if (condition.type == StateGroup.Condition.ConditionType.ObjectSnapped)
                {
                    if (condition.targetObject == snapPoint && 
                        (condition.secondaryObject == null || condition.secondaryObject == snappedObject))
                    {
                        condition.isMet = isSnapped;
                    }
                }
                else if (condition.type == StateGroup.Condition.ConditionType.AllObjectsSnapped)
                {
                    // Check if all snap points have objects
                    var allSnapPoints = GameObject.FindGameObjectsWithTag("snap");
                    condition.isMet = allSnapPoints.All(sp => snappedConnections.ContainsKey(sp));
                }
            }
        }
    }
    
    private void UpdateKnobConditions(GameObject knob, float angle)
    {
        foreach (var group in stateGroups)
        {
            foreach (var condition in group.activationConditions)
            {
                if (condition.type == StateGroup.Condition.ConditionType.KnobTurned && 
                    condition.targetObject == knob)
                {
                    // Check if knob reached target value
                    condition.isMet = Mathf.Abs(angle - condition.targetValue) < 5f; // 5 degree tolerance
                }
            }
        }
    }
    
    private void CheckStateTransitions()
    {
        StateGroup newActiveGroup = null;
        
        // Check each state group's conditions
        foreach (var group in stateGroups)
        {
            if (!group.isActive && group.CheckConditions())
            {
                newActiveGroup = group;
                break;
            }
        }
        
        // Transition to new state if found
        if (newActiveGroup != null)
        {
            TransitionToStateGroup(newActiveGroup);
        }
    }
    
    private void TransitionToStateGroup(StateGroup newGroup)
    {
        Debug.Log($"[Sequence] Transitioning from {currentStateGroup?.groupName} to {newGroup.groupName}");
        
        // Deactivate current
        if (currentStateGroup != null)
        {
            currentStateGroup.isActive = false;
        }
        
        // Activate new
        currentStateGroup = newGroup;
        currentStateGroup.isActive = true;
        
        // Fire event
        OnStateGroupChanged?.Invoke(currentStateGroup);
        
        // Update visual feedback on all objects
        UpdateAllObjectsFeedback();
    }
    
    private bool IsActionAllowed(string actionName)
    {
        if (currentStateGroup == null) return true;
        
        // Check if action is explicitly locked
        if (currentStateGroup.lockedActions.Contains(actionName))
        {
            return false;
        }
        
        // Check if action is in allowed list (if list is used)
        if (currentStateGroup.allowedActions.Count > 0)
        {
            return currentStateGroup.allowedActions.Contains(actionName);
        }
        
        return true;
    }
    
    private void UpdateAllObjectsFeedback()
    {
        // Update all sequence validators
        var validators = FindObjectsOfType<SequenceValidator>();
        foreach (var validator in validators)
        {
            // Trigger re-evaluation
            validator.SendMessage("CheckSequenceRequirements", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Sequence Controller Debug");
        
        if (currentStateGroup != null)
        {
            GUILayout.Label($"Current State: {currentStateGroup.groupName}");
            GUILayout.Label($"Active: {currentStateGroup.isActive}");
            
            GUILayout.Label("Conditions:");
            foreach (var condition in currentStateGroup.activationConditions)
            {
                string status = condition.isMet ? "✓" : "✗";
                GUILayout.Label($"  {status} {condition.type}");
            }
        }
        
        GUILayout.Label($"Connections: {snappedConnections.Count}");
        foreach (var connection in snappedConnections)
        {
            GUILayout.Label($"  {connection.Key.name} <- {connection.Value.name}");
        }
        
        GUILayout.EndArea();
    }
}
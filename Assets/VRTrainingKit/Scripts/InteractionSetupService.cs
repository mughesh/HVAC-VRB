// InteractionSetupService.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// NO NAMESPACE - Fixes ScriptableObject issues
/// <summary>
/// Service for scanning scenes and applying interaction components
/// </summary>
public class InteractionSetupService
    {
        public class SceneAnalysis
        {
            public List<GameObject> grabObjects = new List<GameObject>();
            public List<GameObject> knobObjects = new List<GameObject>();
            public List<GameObject> snapObjects = new List<GameObject>();
            public List<GameObject> untaggedObjects = new List<GameObject>();
            
            public int TotalInteractables => grabObjects.Count + knobObjects.Count + snapObjects.Count;
        }
        
        /// <summary>
        /// Scans the scene for tagged objects
        /// </summary>
        public static SceneAnalysis ScanScene()
        {
            SceneAnalysis analysis = new SceneAnalysis();
            
            // Find all GameObjects in scene
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            
            foreach (var obj in allObjects)
            {
                // Skip if object is a prefab in project (not in scene)
                if (obj.scene.name == null) continue;
                
                if (obj.CompareTag("grab"))
                {
                    analysis.grabObjects.Add(obj);
                }
                else if (obj.CompareTag("knob"))
                {
                    analysis.knobObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found knob object: {obj.name} (Tag: {obj.tag})");
                }
                else if (obj.CompareTag("snap"))
                {
                    analysis.snapObjects.Add(obj);
                }
            }
            
            Debug.Log($"Scene Analysis Complete: {analysis.TotalInteractables} interactables found");
            Debug.Log($"  - Grab Objects: {analysis.grabObjects.Count}");
            Debug.Log($"  - Knob Objects: {analysis.knobObjects.Count}");
            Debug.Log($"  - Snap Points: {analysis.snapObjects.Count}");
            
            return analysis;
        }
        
        /// <summary>
        /// Applies components to all objects of a specific type
        /// </summary>
        public static void ApplyComponentsToObjects(List<GameObject> objects, InteractionProfile profile)
        {
            Debug.Log($"[InteractionSetupService] ApplyComponentsToObjects called with {objects.Count} objects and profile: {profile?.profileName ?? "NULL"}");
            
            if (profile == null)
            {
                Debug.LogError("No profile provided for component application");
                return;
            }
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var obj in objects)
            {
                Debug.Log($"[InteractionSetupService] Processing object: {obj.name} (Tag: {obj.tag})");
                
                if (profile.ValidateGameObject(obj))
                {
                    Debug.Log($"[InteractionSetupService] Object {obj.name} passed validation, applying profile...");
                    try
                    {
                        profile.ApplyToGameObject(obj);
                        successCount++;
                        Debug.Log($"[InteractionSetupService] Successfully configured: {obj.name}");
                    }
                    catch (System.Exception e)
                    {
                        failCount++;
                        Debug.LogError($"[InteractionSetupService] Failed to configure {obj.name}: {e.Message}");
                    }
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"[InteractionSetupService] Object {obj.name} failed validation for profile {profile.profileName}");
                }
            }
            
            Debug.Log($"Component Application Complete: {successCount} successful, {failCount} failed");
        }
        
        /// <summary>
        /// Quick setup with default profiles
        /// </summary>
        public static void QuickSetup()
        {
            var analysis = ScanScene();
            
            // Try to load default profiles from Resources
            GrabProfile defaultGrab = Resources.Load<GrabProfile>("DefaultGrabProfile");
            KnobProfile defaultKnob = Resources.Load<KnobProfile>("DefaultKnobProfile");
            SnapProfile defaultSnap = Resources.Load<SnapProfile>("DefaultSnapProfile");
            
            if (defaultGrab != null)
                ApplyComponentsToObjects(analysis.grabObjects, defaultGrab);
            else
                Debug.LogWarning("No default grab profile found in Resources folder");
            
            if (defaultKnob != null)
                ApplyComponentsToObjects(analysis.knobObjects, defaultKnob);
            else
                Debug.LogWarning("No default knob profile found in Resources folder");
            
            if (defaultSnap != null)
                ApplyComponentsToObjects(analysis.snapObjects, defaultSnap);
            else
                Debug.LogWarning("No default snap profile found in Resources folder");
        }
        
        /// <summary>
        /// Validates the current setup
        /// </summary>
        public static List<string> ValidateSetup()
        {
            List<string> issues = new List<string>();
            var analysis = ScanScene();
            
            // Check grab objects
            foreach (var obj in analysis.grabObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
                {
                    issues.Add($"Grab object '{obj.name}' missing XRGrabInteractable");
                }
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"Grab object '{obj.name}' missing Rigidbody");
                }
                if (obj.GetComponent<Collider>() == null)
                {
                    issues.Add($"Grab object '{obj.name}' missing Collider");
                }
            }
            
            // Check knob objects
            foreach (var obj in analysis.knobObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
                {
                    issues.Add($"Knob object '{obj.name}' missing XRGrabInteractable");
                }
                if (obj.GetComponent<KnobController>() == null)
                {
                    issues.Add($"Knob object '{obj.name}' missing KnobController");
                }
            }
            
            // Check snap points
            foreach (var obj in analysis.snapObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>() == null)
                {
                    issues.Add($"Snap point '{obj.name}' missing XRSocketInteractor");
                }
                if (obj.GetComponent<SphereCollider>() == null)
                {
                    issues.Add($"Snap point '{obj.name}' missing SphereCollider");
                }
            }
            
            if (issues.Count == 0)
            {
                Debug.Log("Setup validation passed - no issues found!");
            }
            else
            {
                Debug.LogWarning($"Setup validation found {issues.Count} issues");
            }
            
            return issues;
        }
        
        /// <summary>
        /// Removes all interaction components from tagged objects
        /// </summary>
        public static void CleanupComponents()
        {
            var analysis = ScanScene();
            int cleanedCount = 0;
            
            // Clean grab objects
            foreach (var obj in analysis.grabObjects)
            {
                if (RemoveInteractionComponents(obj))
                    cleanedCount++;
            }
            
            // Clean knob objects
            foreach (var obj in analysis.knobObjects)
            {
                if (RemoveInteractionComponents(obj))
                    cleanedCount++;
            }
            
            // Clean snap objects
            foreach (var obj in analysis.snapObjects)
            {
                if (RemoveInteractionComponents(obj))
                    cleanedCount++;
            }
            
            Debug.Log($"Cleanup complete: {cleanedCount} objects cleaned");
        }
        
        private static bool RemoveInteractionComponents(GameObject obj)
        {
            bool removed = false;
            
            // Remove XR components
            var grabInteractable = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null)
            {
                Object.DestroyImmediate(grabInteractable);
                removed = true;
            }
            
            var socketInteractor = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
            if (socketInteractor != null)
            {
                Object.DestroyImmediate(socketInteractor);
                removed = true;
            }
            
            // Remove helper components
            var knobController = obj.GetComponent<KnobController>();
            if (knobController != null)
            {
                Object.DestroyImmediate(knobController);
                removed = true;
            }
            
            var snapValidator = obj.GetComponent<SnapValidator>();
            if (snapValidator != null)
            {
                Object.DestroyImmediate(snapValidator);
                removed = true;
            }
            
            var sequenceValidator = obj.GetComponent<SequenceValidator>();
            if (sequenceValidator != null)
            {
                Object.DestroyImmediate(sequenceValidator);
                removed = true;
            }
            
            return removed;
        }
    }

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
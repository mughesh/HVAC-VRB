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
            if (profile == null)
            {
                Debug.LogError("No profile provided for component application");
                return;
            }
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var obj in objects)
            {
                if (profile.ValidateGameObject(obj))
                {
                    try
                    {
                        profile.ApplyToGameObject(obj);
                        successCount++;
                        Debug.Log($"Successfully configured: {obj.name}");
                    }
                    catch (System.Exception e)
                    {
                        failCount++;
                        Debug.LogError($"Failed to configure {obj.name}: {e.Message}");
                    }
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"Object {obj.name} failed validation for profile {profile.profileName}");
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


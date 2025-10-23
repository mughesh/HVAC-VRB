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
            public List<GameObject> toolObjects = new List<GameObject>();
            public List<GameObject> valveObjects = new List<GameObject>();
            public List<GameObject> turnObjects = new List<GameObject>();
            public List<GameObject> untaggedObjects = new List<GameObject>();

            public int TotalInteractables => grabObjects.Count + knobObjects.Count + snapObjects.Count + toolObjects.Count + valveObjects.Count + turnObjects.Count;
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
                else if (obj.CompareTag("tool"))
                {
                    analysis.toolObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found tool object: {obj.name} (Tag: {obj.tag})");
                }
                else if (obj.CompareTag("valve"))
                {
                    analysis.valveObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found valve object: {obj.name} (Tag: {obj.tag})");
                }
                else if (obj.CompareTag("turn"))
                {
                    analysis.turnObjects.Add(obj);
                    Debug.Log($"[InteractionSetupService] Found turn object: {obj.name} (Tag: {obj.tag})");
                }
            }
            
            Debug.Log($"Scene Analysis Complete: {analysis.TotalInteractables} interactables found");
            Debug.Log($"  - Grab Objects: {analysis.grabObjects.Count}");
            Debug.Log($"  - Knob Objects: {analysis.knobObjects.Count}");
            Debug.Log($"  - Snap Points: {analysis.snapObjects.Count}");
            Debug.Log($"  - Tool Objects: {analysis.toolObjects.Count}");
            Debug.Log($"  - Valve Objects: {analysis.valveObjects.Count}");
            Debug.Log($"  - Turn Objects: {analysis.turnObjects.Count}");
            
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
            ToolProfile defaultTool = Resources.Load<ToolProfile>("DefaultToolProfile");
            
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
            
            if (defaultTool != null)
                ApplyComponentsToObjects(analysis.toolObjects, defaultTool);
            else
                Debug.LogWarning("No default tool profile found in Resources folder");
        }
        
        /// <summary>
        /// Validates the current setup based on detected VR framework
        /// </summary>
        public static List<string> ValidateSetup()
        {
            List<string> issues = new List<string>();
            var analysis = ScanScene();

            // Detect current framework and validate accordingly
            var currentFramework = VRFrameworkDetector.DetectCurrentFramework();

            Debug.Log($"[InteractionSetupService] Validating setup for framework: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");

            switch (currentFramework)
            {
                case VRFramework.XRI:
                    ValidateXRIObjects(analysis, issues);
                    break;

                case VRFramework.AutoHands:
                    ValidateAutoHandsObjects(analysis, issues);
                    break;

                case VRFramework.None:
                    issues.Add("No VR framework detected in scene. Please add XR Origin or AutoHandPlayer rig.");
                    break;

                default:
                    issues.Add($"Unknown framework detected: {currentFramework}");
                    break;
            }

            // Log results
            if (issues.Count == 0)
            {
                Debug.Log($"[InteractionSetupService] Setup validation passed - no issues found for {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}!");
            }
            else
            {
                Debug.LogWarning($"[InteractionSetupService] Setup validation found {issues.Count} issues for {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");
            }

            return issues;
        }

        /// <summary>
        /// Validates XRI framework objects and components
        /// </summary>
        private static void ValidateXRIObjects(SceneAnalysis analysis, List<string> issues)
        {
            Debug.Log($"[InteractionSetupService] Validating {analysis.TotalInteractables} objects for XRI framework");

            // Check grab objects for XRI components
            foreach (var obj in analysis.grabObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
                {
                    issues.Add($"XRI: Grab object '{obj.name}' missing XRGrabInteractable");
                }
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"XRI: Grab object '{obj.name}' missing Rigidbody");
                }
                if (obj.GetComponent<Collider>() == null)
                {
                    issues.Add($"XRI: Grab object '{obj.name}' missing Collider");
                }
            }

            // Check knob objects for XRI components
            foreach (var obj in analysis.knobObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
                {
                    issues.Add($"XRI: Knob object '{obj.name}' missing XRGrabInteractable");
                }
                if (obj.GetComponent<KnobController>() == null)
                {
                    issues.Add($"XRI: Knob object '{obj.name}' missing KnobController");
                }
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"XRI: Knob object '{obj.name}' missing Rigidbody");
                }
            }

            // Check tool objects for XRI components
            foreach (var obj in analysis.toolObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
                {
                    issues.Add($"XRI: Tool object '{obj.name}' missing XRGrabInteractable");
                }
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"XRI: Tool object '{obj.name}' missing Rigidbody");
                }
                if (obj.GetComponent<Collider>() == null)
                {
                    issues.Add($"XRI: Tool object '{obj.name}' missing Collider");
                }
            }

            // Check valve objects for XRI components
            foreach (var obj in analysis.valveObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
                {
                    issues.Add($"XRI: Valve object '{obj.name}' missing XRGrabInteractable");
                }
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"XRI: Valve object '{obj.name}' missing Rigidbody");
                }
            }

            // Check snap points for XRI components
            foreach (var obj in analysis.snapObjects)
            {
                if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>() == null)
                {
                    issues.Add($"XRI: Snap point '{obj.name}' missing XRSocketInteractor");
                }
                if (obj.GetComponent<SphereCollider>() == null)
                {
                    issues.Add($"XRI: Snap point '{obj.name}' missing SphereCollider");
                }
            }

            Debug.Log($"[InteractionSetupService] XRI validation complete: {issues.Count} issues found");
        }

        /// <summary>
        /// Validates AutoHands framework objects and components
        /// Uses reflection-based detection for AutoHands components
        /// </summary>
        private static void ValidateAutoHandsObjects(SceneAnalysis analysis, List<string> issues)
        {
            Debug.Log($"[InteractionSetupService] Validating {analysis.TotalInteractables} objects for AutoHands framework");

            // Validate grab objects for Grabbable component
            foreach (var obj in analysis.grabObjects)
            {
                if (!HasAutoHandsComponent(obj, "Grabbable"))
                {
                    issues.Add($"AutoHands: Grab object '{obj.name}' missing Grabbable component");
                }
                else
                {
                    // Additional validation for grab objects
                    ValidateAutoHandsGrabbableSetup(obj, issues);
                }
            }

            // Validate snap objects for PlacePoint component
            foreach (var obj in analysis.snapObjects)
            {
                if (!HasAutoHandsComponent(obj, "PlacePoint"))
                {
                    issues.Add($"AutoHands: Snap point '{obj.name}' missing PlacePoint component");
                }
                else
                {
                    // Additional validation for snap objects
                    ValidateAutoHandsPlacePointSetup(obj, issues);
                }
            }

            // Validate knob objects (knobs use Grabbable + physics constraints)
            foreach (var obj in analysis.knobObjects)
            {
                if (!HasAutoHandsComponent(obj, "Grabbable"))
                {
                    issues.Add($"AutoHands: Knob object '{obj.name}' missing Grabbable component");
                }

                // Check for physics components (Rigidbody required for knobs)
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"AutoHands: Knob object '{obj.name}' missing Rigidbody for physics interaction");
                }
            }

            // Validate tool objects (tools use Grabbable)
            foreach (var obj in analysis.toolObjects)
            {
                if (!HasAutoHandsComponent(obj, "Grabbable"))
                {
                    issues.Add($"AutoHands: Tool object '{obj.name}' missing Grabbable component");
                }
            }

            // Validate valve objects (valves typically use both Grabbable and may have PlacePoint)
            foreach (var obj in analysis.valveObjects)
            {
                if (!HasAutoHandsComponent(obj, "Grabbable"))
                {
                    issues.Add($"AutoHands: Valve object '{obj.name}' missing Grabbable component");
                }
            }

            // Validate turn objects (turn tools use Grabbable + physics)
            foreach (var obj in analysis.turnObjects)
            {
                if (!HasAutoHandsComponent(obj, "Grabbable"))
                {
                    issues.Add($"AutoHands: Turn object '{obj.name}' missing Grabbable component");
                }

                // Check for physics components (Rigidbody required for turn objects)
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    issues.Add($"AutoHands: Turn object '{obj.name}' missing Rigidbody for physics interaction");
                }
            }

            Debug.Log($"[InteractionSetupService] AutoHands validation complete: {issues.Count} issues found");
        }

        /// <summary>
        /// Checks if a GameObject has a specific AutoHands component using reflection
        /// </summary>
        private static bool HasAutoHandsComponent(GameObject obj, string componentName)
        {
            if (obj == null) return false;

            // Get all MonoBehaviour components
            var components = obj.GetComponents<MonoBehaviour>();

            foreach (var component in components)
            {
                if (component != null && component.GetType().Name == componentName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Validates AutoHands Grabbable component setup
        /// </summary>
        private static void ValidateAutoHandsGrabbableSetup(GameObject obj, List<string> issues)
        {
            // Check for Rigidbody (required for AutoHands physics)
            if (obj.GetComponent<Rigidbody>() == null)
            {
                issues.Add($"AutoHands: Grabbable object '{obj.name}' missing Rigidbody component");
            }

            // Check for Collider (required for interaction)
            if (obj.GetComponent<Collider>() == null)
            {
                issues.Add($"AutoHands: Grabbable object '{obj.name}' missing Collider component");
            }
        }

        /// <summary>
        /// Validates AutoHands PlacePoint component setup
        /// </summary>
        private static void ValidateAutoHandsPlacePointSetup(GameObject obj, List<string> issues)
        {
            // Check for Collider (required for place detection)
            var collider = obj.GetComponent<Collider>();
            if (collider == null)
            {
                issues.Add($"AutoHands: PlacePoint '{obj.name}' missing Collider component");
            }
            else if (!collider.isTrigger)
            {
                issues.Add($"AutoHands: PlacePoint '{obj.name}' Collider should be set as Trigger");
            }
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
            
            // Clean tool objects
            foreach (var obj in analysis.toolObjects)
            {
                if (RemoveInteractionComponents(obj))
                    cleanedCount++;
            }

            // Clean turn objects
            foreach (var obj in analysis.turnObjects)
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


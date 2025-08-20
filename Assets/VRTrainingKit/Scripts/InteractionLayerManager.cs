// InteractionLayerManager.cs
// Place this in the Editor folder

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Helper class for managing XR Interaction Layers in the editor
/// </summary>
public static class InteractionLayerManager
{
    private static readonly string[] DEFAULT_LAYER_NAMES = new string[]
    {
        "Default",
        "Teleport",
        "Grab",
        "UI",
        // Add more default names as needed
    };
    
    /// <summary>
    /// Draw a multi-select interaction layer mask field
    /// </summary>
    public static LayerMask DrawLayerMaskField(string label, LayerMask mask, params GUILayoutOption[] options)
    {
        // Get current layer names
        List<string> layerNames = GetAllLayerNames();
        
        // Create the mask field
        int newMask = EditorGUILayout.MaskField(label, mask.value, layerNames.ToArray(), options);
        
        return newMask;
    }
    
    /// <summary>
    /// Draw a simplified dropdown for single layer selection with multi-layer support
    /// </summary>
    public static LayerMask DrawLayerDropdown(LayerMask currentMask, params GUILayoutOption[] options)
    {
        List<string> displayNames = new List<string>();
        List<int> maskValues = new List<int>();
        
        // Add special options
        displayNames.Add("Nothing");
        maskValues.Add(0);
        
        displayNames.Add("Everything");
        maskValues.Add(-1);
        
        displayNames.Add("─────────────");  // Separator
        maskValues.Add(currentMask.value);  // Keep current if separator selected
        
        // Add individual layers
        List<string> layerNames = GetAllLayerNames();
        for (int i = 0; i < layerNames.Count && i < 32; i++)
        {
            displayNames.Add($"{i}: {layerNames[i]}");
            maskValues.Add(1 << i);
        }
        
        displayNames.Add("─────────────");  // Separator
        maskValues.Add(currentMask.value);
        
        displayNames.Add("Multiple...");
        maskValues.Add(-2); // Special value for opening multi-select
        
        // Find current selection
        int currentIndex = 0;
        if (currentMask.value == 0)
            currentIndex = 0;
        else if (currentMask.value == -1)
            currentIndex = 1;
        else
        {
            // Check if it's a single layer
            for (int i = 3; i < maskValues.Count - 2; i++)
            {
                if (currentMask.value == maskValues[i])
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // If multiple layers selected, show current value in label
            if (currentIndex == 0 && currentMask.value != 0)
            {
                displayNames[0] = $"Multiple ({CountSetBits(currentMask.value)} layers)";
            }
        }
        
        // Draw dropdown
        int newIndex = EditorGUILayout.Popup(currentIndex, displayNames.ToArray(), options);
        
        // Handle selection
        if (newIndex == displayNames.Count - 1) // "Multiple..." selected
        {
            // Open a separate window or show mask field
            return DrawLayerMaskField("", currentMask);
        }
        else if (newIndex == 2 || newIndex == displayNames.Count - 2) // Separator
        {
            return currentMask; // Don't change
        }
        else if (newIndex >= 0 && newIndex < maskValues.Count)
        {
            return maskValues[newIndex];
        }
        
        return currentMask;
    }
    
    /// <summary>
    /// Get all configured interaction layer names
    /// </summary>
    public static List<string> GetAllLayerNames()
    {
        List<string> names = new List<string>();
        
        // Try to get names from XRI settings
        // For now, use a combination of defaults and layer indices
        for (int i = 0; i < 32; i++)
        {
            if (i < DEFAULT_LAYER_NAMES.Length)
            {
                names.Add(DEFAULT_LAYER_NAMES[i]);
            }
            else
            {
                names.Add($"Layer {i}");
            }
        }
        
        return names;
    }
    
    /// <summary>
    /// Set interaction layer for a GameObject
    /// </summary>
    public static void SetInteractionLayer(GameObject obj, LayerMask layerMask)
    {
        // Try XRGrabInteractable
        var grabInteractable = obj.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            Undo.RecordObject(grabInteractable, "Set Interaction Layer");
            grabInteractable.interactionLayers = layerMask.value;
            EditorUtility.SetDirty(grabInteractable);
            return;
        }
        
        // Try XRSocketInteractor
        var socketInteractor = obj.GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
        {
            Undo.RecordObject(socketInteractor, "Set Interaction Layer");
            socketInteractor.interactionLayers = layerMask.value;
            EditorUtility.SetDirty(socketInteractor);
            return;
        }
        
        // Try XRSimpleInteractable
        var simpleInteractable = obj.GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null)
        {
            Undo.RecordObject(simpleInteractable, "Set Interaction Layer");
            simpleInteractable.interactionLayers = layerMask.value;
            EditorUtility.SetDirty(simpleInteractable);
        }
    }
    
    /// <summary>
    /// Get interaction layer from a GameObject
    /// </summary>
    public static LayerMask GetInteractionLayer(GameObject obj)
    {
        // Try XRGrabInteractable
        var grabInteractable = obj.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
            return grabInteractable.interactionLayers.value;
        
        // Try XRSocketInteractor
        var socketInteractor = obj.GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
            return socketInteractor.interactionLayers.value;
        
        // Try XRSimpleInteractable
        var simpleInteractable = obj.GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null)
            return simpleInteractable.interactionLayers.value;
        
        return 0; // Nothing
    }
    
    /// <summary>
    /// Apply same layer to multiple objects
    /// </summary>
    public static void ApplyLayerToMultiple(List<GameObject> objects, LayerMask layerMask)
    {
        foreach (var obj in objects)
        {
            SetInteractionLayer(obj, layerMask);
        }
    }
    
    /// <summary>
    /// Create a pairing between a grab object and snap point
    /// </summary>
    public static void CreateExclusivePairing(GameObject grabObject, GameObject snapPoint, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= 32)
        {
            Debug.LogError("Layer index must be between 0 and 31");
            return;
        }
        
        LayerMask exclusiveLayer = 1 << layerIndex;
        
        SetInteractionLayer(grabObject, exclusiveLayer);
        SetInteractionLayer(snapPoint, exclusiveLayer);
        
        Debug.Log($"Created exclusive pairing on layer {layerIndex} between {grabObject.name} and {snapPoint.name}");
    }
    
    /// <summary>
    /// Find next available layer index
    /// </summary>
    public static int FindNextAvailableLayer(int startIndex = 2)
    {
        // Check all objects in scene to see which layers are in use
        HashSet<int> usedLayers = new HashSet<int>();
        
        // Find all interactables
        var allGrabInteractables = GameObject.FindObjectsOfType<XRGrabInteractable>();
        foreach (var interactable in allGrabInteractables)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((interactable.interactionLayers.value & (1 << i)) != 0)
                {
                    usedLayers.Add(i);
                }
            }
        }
        
        // Find all socket interactors
        var allSocketInteractors = GameObject.FindObjectsOfType<XRSocketInteractor>();
        foreach (var socket in allSocketInteractors)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((socket.interactionLayers.value & (1 << i)) != 0)
                {
                    usedLayers.Add(i);
                }
            }
        }
        
        // Find first unused layer
        for (int i = startIndex; i < 32; i++)
        {
            if (!usedLayers.Contains(i))
            {
                return i;
            }
        }
        
        return -1; // No available layers
    }
    
    /// <summary>
    /// Generate a report of layer usage
    /// </summary>
    public static Dictionary<int, List<GameObject>> GetLayerUsageReport()
    {
        Dictionary<int, List<GameObject>> usage = new Dictionary<int, List<GameObject>>();
        
        // Initialize dictionary
        for (int i = 0; i < 32; i++)
        {
            usage[i] = new List<GameObject>();
        }
        
        // Check all grab interactables
        var allGrabInteractables = GameObject.FindObjectsOfType<XRGrabInteractable>();
        foreach (var interactable in allGrabInteractables)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((interactable.interactionLayers.value & (1 << i)) != 0)
                {
                    usage[i].Add(interactable.gameObject);
                }
            }
        }
        
        // Check all socket interactors
        var allSocketInteractors = GameObject.FindObjectsOfType<XRSocketInteractor>();
        foreach (var socket in allSocketInteractors)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((socket.interactionLayers.value & (1 << i)) != 0)
                {
                    usage[i].Add(socket.gameObject);
                }
            }
        }
        
        return usage;
    }
    
    private static int CountSetBits(int n)
    {
        int count = 0;
        while (n != 0)
        {
            count++;
            n &= (n - 1);
        }
        return count;
    }
}

/// <summary>
/// Window for managing interaction layer pairings
/// </summary>
public class InteractionLayerPairingWindow : EditorWindow
{
    private Vector2 scrollPos;
    private Dictionary<GameObject, GameObject> pairings = new Dictionary<GameObject, GameObject>();
    private List<GameObject> grabObjects = new List<GameObject>();
    private List<GameObject> snapPoints = new List<GameObject>();
    
    [MenuItem("VR Training/Interaction Layer Pairing")]
    public static void ShowWindow()
    {
        var window = GetWindow<InteractionLayerPairingWindow>("Layer Pairing");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnEnable()
    {
        RefreshObjectLists();
    }
    
    private void RefreshObjectLists()
    {
        grabObjects.Clear();
        snapPoints.Clear();
        
        // Find all grab objects
        var allGrabs = GameObject.FindGameObjectsWithTag("grab");
        grabObjects.AddRange(allGrabs);
        
        var allKnobs = GameObject.FindGameObjectsWithTag("knob");
        grabObjects.AddRange(allKnobs);
        
        // Find all snap points
        var allSnaps = GameObject.FindGameObjectsWithTag("snap");
        snapPoints.AddRange(allSnaps);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quick Pairing Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Lists"))
        {
            RefreshObjectLists();
        }
        
        EditorGUILayout.Space();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        // Show pairing interface
        EditorGUILayout.LabelField($"Grab Objects: {grabObjects.Count}");
        EditorGUILayout.LabelField($"Snap Points: {snapPoints.Count}");
        
        EditorGUILayout.Space();
        
        // Pairing section
        EditorGUILayout.LabelField("Create Pairings:", EditorStyles.boldLabel);
        
        foreach (var snapPoint in snapPoints)
        {
            if (snapPoint == null) continue;
            
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(snapPoint.name, GUILayout.Width(150));
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            
            // Dropdown for grab objects
            if (!pairings.ContainsKey(snapPoint))
                pairings[snapPoint] = null;
            
            List<string> options = new List<string> { "None" };
            options.AddRange(grabObjects.Where(g => g != null).Select(g => g.name));
            
            int currentIndex = pairings[snapPoint] == null ? 0 : 
                grabObjects.IndexOf(pairings[snapPoint]) + 1;
            
            int newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray());
            
            if (newIndex == 0)
                pairings[snapPoint] = null;
            else if (newIndex > 0 && newIndex <= grabObjects.Count)
                pairings[snapPoint] = grabObjects[newIndex - 1];
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Apply Pairings", GUILayout.Height(30)))
        {
            ApplyPairings();
        }
        
        // Show layer usage
        if (GUILayout.Button("Show Layer Usage"))
        {
            ShowLayerUsage();
        }
    }
    
    private void ApplyPairings()
    {
        int layerIndex = 10; // Start from layer 10 for custom pairings
        
        foreach (var pair in pairings)
        {
            if (pair.Key != null && pair.Value != null)
            {
                InteractionLayerManager.CreateExclusivePairing(pair.Value, pair.Key, layerIndex);
                layerIndex++;
                
                if (layerIndex >= 32)
                {
                    EditorUtility.DisplayDialog("Layer Limit", 
                        "Reached maximum number of interaction layers (32)", "OK");
                    break;
                }
            }
        }
        
        EditorUtility.DisplayDialog("Complete", 
            $"Applied {pairings.Count(p => p.Value != null)} pairings", "OK");
    }
    
    private void ShowLayerUsage()
    {
        var usage = InteractionLayerManager.GetLayerUsageReport();
        
        Debug.Log("=== Interaction Layer Usage ===");
        for (int i = 0; i < 32; i++)
        {
            if (usage[i].Count > 0)
            {
                Debug.Log($"Layer {i}: {string.Join(", ", usage[i].Select(g => g.name))}");
            }
        }
    }
}
#endif
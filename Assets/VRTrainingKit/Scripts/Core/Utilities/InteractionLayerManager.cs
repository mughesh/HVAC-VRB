// InteractionLayerManager.cs
// Place this in the Editor folder

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Helper class for managing XR Interaction Layers in the editor
/// </summary>
public static class InteractionLayerManager
{
    private static ScriptableObject s_CachedSettings;
    private static SerializedObject s_SerializedSettings;
    
    /// <summary>
    /// Get the InteractionLayerSettings asset from project
    /// </summary>
    private static ScriptableObject GetInteractionLayerSettings()
    {
        if (s_CachedSettings == null)
        {
            // Try to find the InteractionLayerSettings asset in the project
            string[] guids = AssetDatabase.FindAssets("t:InteractionLayerSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_CachedSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
        }
        return s_CachedSettings;
    }
    
    /// <summary>
    /// Get actual layer names from XRI settings
    /// </summary>
    public static List<string> GetConfiguredLayerNames()
    {
        List<string> layerNames = new List<string>();
        var settings = GetInteractionLayerSettings();
        
        if (settings != null)
        {
            // Get the serialized object to read the actual layer names
            var so = new SerializedObject(settings);
            var layerNamesProperty = so.FindProperty("m_LayerNames");
            
            if (layerNamesProperty != null && layerNamesProperty.isArray)
            {
                for (int i = 0; i < layerNamesProperty.arraySize && i < 32; i++)
                {
                    string layerName = layerNamesProperty.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        layerNames.Add(layerName);
                    }
                }
            }
        }
        
        // Fallback if we can't get the settings
        if (layerNames.Count == 0)
        {
            for (int i = 0; i < 32; i++)
            {
                string name = InteractionLayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    layerNames.Add(name);
                }
            }
        }
        
        return layerNames;
    }
    
    /// <summary>
    /// Draw a layer mask field that mimics Unity's XRI layer dropdown
    /// </summary>
    public static LayerMask DrawLayerMaskDropdown(LayerMask currentMask, params GUILayoutOption[] options)
    {
        // Get configured layer names  
        var layerNames = GetConfiguredLayerNames();
        
        // Create array of only configured layer names (non-empty)
        List<string> displayNames = new List<string>();
        List<int> layerIndices = new List<int>();
        
        for (int i = 0; i < 32; i++)
        {
            if (i < layerNames.Count && !string.IsNullOrEmpty(layerNames[i]))
            {
                displayNames.Add(layerNames[i]);
                layerIndices.Add(i);
            }
        }
        
        // Convert current mask to display mask
        int displayMask = 0;
        for (int i = 0; i < layerIndices.Count; i++)
        {
            if ((currentMask.value & (1 << layerIndices[i])) != 0)
            {
                displayMask |= (1 << i);
            }
        }
        
        // Draw the mask field
        int newDisplayMask = EditorGUILayout.MaskField(displayMask, displayNames.ToArray(), options);
        
        // Convert display mask back to layer mask
        int newMask = 0;
        for (int i = 0; i < layerIndices.Count; i++)
        {
            if ((newDisplayMask & (1 << i)) != 0)
            {
                newMask |= (1 << layerIndices[i]);
            }
        }
        
        return newMask;
    }
    
    /// <summary>
    /// Get display text for layer mask (showing selected layers)
    /// </summary>
    private static string GetLayerMaskDisplayText(LayerMask mask, List<string> layerNames)
    {
        if (mask.value == 0)
            return "Nothing";
        if (mask.value == -1)
            return "Everything";
        
        List<string> selectedLayers = new List<string>();
        for (int i = 0; i < layerNames.Count && i < 32; i++)
        {
            if (!string.IsNullOrEmpty(layerNames[i]) && (mask.value & (1 << i)) != 0)
            {
                selectedLayers.Add($"{i}: {layerNames[i]}");
            }
        }
        
        if (selectedLayers.Count == 0)
            return "Nothing";
        if (selectedLayers.Count == 1)
            return selectedLayers[0];
        if (selectedLayers.Count <= 3)
            return string.Join(", ", selectedLayers);
        
        return $"Mixed ({selectedLayers.Count} layers)";
    }
    
    /// <summary>
    /// Open the Interaction Layer Settings in the Inspector
    /// </summary>
    public static void OpenInteractionLayerSettings()
    {
        var settings = GetInteractionLayerSettings();
        if (settings != null)
        {
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
        else
        {
            // Try to open project settings
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management/XR Interaction Toolkit");
        }
    }
    
    /// <summary>
    /// Add a new layer name to the settings
    /// </summary>
    public static bool AddInteractionLayer(string layerName)
    {
        var settings = GetInteractionLayerSettings();
        if (settings == null) return false;
        
        var so = new SerializedObject(settings);
        var layerNamesProperty = so.FindProperty("m_LayerNames");
        
        if (layerNamesProperty != null && layerNamesProperty.isArray)
        {
            // Find first empty slot
            for (int i = 0; i < layerNamesProperty.arraySize && i < 32; i++)
            {
                string existingName = layerNamesProperty.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrEmpty(existingName))
                {
                    layerNamesProperty.GetArrayElementAtIndex(i).stringValue = layerName;
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    
                    // Clear cache
                    s_CachedSettings = null;
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the name of a specific layer index
    /// </summary>
    public static string GetLayerNameAt(int index)
    {
        if (index < 0 || index >= 32) return "";
        
        var layerNames = GetConfiguredLayerNames();
        if (index < layerNames.Count)
            return layerNames[index];
        
        return "";
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
    /// Find next available layer index
    /// </summary>
    public static int FindNextAvailableLayer(int startIndex = 2)
    {
        var layerNames = GetConfiguredLayerNames();
        
        for (int i = startIndex; i < 32; i++)
        {
            if (i >= layerNames.Count || string.IsNullOrEmpty(layerNames[i]))
            {
                return i;
            }
        }
        
        return -1; // No available layers
    }
    
    /// <summary>
    /// Create a quick layer dialog for adding new layers
    /// </summary>
    public class AddLayerDialog : EditorWindow
    {
        private string newLayerName = "";
        private int selectedIndex = -1;
        
        public static void ShowDialog()
        {
            var window = GetWindow<AddLayerDialog>("Add Interaction Layer");
            window.minSize = new Vector2(300, 150);
            window.maxSize = new Vector2(300, 150);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Add New Interaction Layer", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Find next available index
            selectedIndex = FindNextAvailableLayer();
            
            if (selectedIndex == -1)
            {
                EditorGUILayout.HelpBox("All 32 interaction layers are in use!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Layer Index: {selectedIndex}");
                newLayerName = EditorGUILayout.TextField("Layer Name:", newLayerName);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                
                GUI.enabled = !string.IsNullOrEmpty(newLayerName);
                if (GUILayout.Button("Add Layer"))
                {
                    if (AddInteractionLayer(newLayerName))
                    {
                        Debug.Log($"Added interaction layer '{newLayerName}' at index {selectedIndex}");
                        Close();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to add layer", "OK");
                    }
                }
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
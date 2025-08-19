using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class GameObjectHierarchyExporter : EditorWindow
{
    private GameObject targetObject;
    private string exportPath = "";
    private bool includeInactiveObjects = true;
    private bool useMarkdown = true;
    
    [MenuItem("Tools/GameObject Hierarchy Exporter")]
    public static void ShowWindow()
    {
        GetWindow<GameObjectHierarchyExporter>("Hierarchy Exporter");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("GameObject Hierarchy Exporter", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // Object selection
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        
        // Options
        includeInactiveObjects = EditorGUILayout.Toggle("Include Inactive Objects", includeInactiveObjects);
        useMarkdown = EditorGUILayout.Toggle("Use Markdown Format", useMarkdown);
        
        EditorGUILayout.Space();
        
        // Export path
        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Hierarchy File",
                Application.dataPath,
                targetObject != null ? targetObject.name + "_hierarchy" : "hierarchy",
                useMarkdown ? "md" : "txt"
            );
            if (!string.IsNullOrEmpty(path))
            {
                exportPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Export buttons
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = targetObject != null;
        
        if (GUILayout.Button("Export Hierarchy"))
        {
            ExportHierarchy();
        }
        
        if (GUILayout.Button("Export & Open"))
        {
            if (ExportHierarchy())
            {
                System.Diagnostics.Process.Start(exportPath);
            }
        }
        
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        // Quick export for selected object
        EditorGUILayout.Space();
        if (GUILayout.Button("Export Selected GameObject"))
        {
            if (Selection.activeGameObject != null)
            {
                targetObject = Selection.activeGameObject;
                string defaultPath = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                    targetObject.name + "_hierarchy" + (useMarkdown ? ".md" : ".txt")
                );
                exportPath = defaultPath;
                ExportHierarchy();
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject in the hierarchy.", "OK");
            }
        }
    }
    
    private bool ExportHierarchy()
    {
        if (targetObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject to export.", "OK");
            return false;
        }
        
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = EditorUtility.SaveFilePanel(
                "Save Hierarchy File",
                Application.dataPath,
                targetObject.name + "_hierarchy",
                useMarkdown ? "md" : "txt"
            );
            
            if (string.IsNullOrEmpty(exportPath))
                return false;
        }
        
        StringBuilder sb = new StringBuilder();
        
        if (useMarkdown)
        {
            sb.AppendLine($"# {targetObject.name} Hierarchy");
            sb.AppendLine($"\nGenerated on: {System.DateTime.Now}");
            sb.AppendLine($"Unity Version: {Application.unityVersion}");
            sb.AppendLine("\n---\n");
        }
        else
        {
            sb.AppendLine($"{targetObject.name} Hierarchy");
            sb.AppendLine($"Generated on: {System.DateTime.Now}");
            sb.AppendLine($"Unity Version: {Application.unityVersion}");
            sb.AppendLine("\n" + new string('-', 50) + "\n");
        }
        
        // Process hierarchy
        ProcessGameObject(targetObject, sb, 0);
        
        // Write to file
        try
        {
            File.WriteAllText(exportPath, sb.ToString());
            Debug.Log($"Hierarchy exported successfully to: {exportPath}");
            EditorUtility.DisplayDialog("Success", $"Hierarchy exported to:\n{exportPath}", "OK");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to export hierarchy: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to export hierarchy:\n{e.Message}", "OK");
            return false;
        }
    }
    
    private void ProcessGameObject(GameObject obj, StringBuilder sb, int depth)
    {
        if (!includeInactiveObjects && !obj.activeInHierarchy)
            return;
        
        string indent = new string(' ', depth * 2);
        string status = obj.activeInHierarchy ? "" : " [INACTIVE]";
        
        if (useMarkdown)
        {
            // GameObject name with proper markdown formatting
            if (depth == 0)
            {
                sb.AppendLine($"## {obj.name}{status}");
            }
            else
            {
                sb.AppendLine($"{indent}- **{obj.name}**{status}");
            }
            
            // Components
            Component[] components = obj.GetComponents<Component>();
            if (components.Length > 0)
            {
                sb.AppendLine($"{indent}  *Components:*");
                foreach (Component comp in components)
                {
                    if (comp != null)
                    {
                        string componentName = comp.GetType().Name;
                        sb.AppendLine($"{indent}  - `{componentName}`");
                    }
                }
            }
        }
        else
        {
            // Plain text format
            sb.AppendLine($"{indent}{obj.name}{status}");
            
            Component[] components = obj.GetComponents<Component>();
            if (components.Length > 0)
            {
                sb.AppendLine($"{indent}  Components:");
                foreach (Component comp in components)
                {
                    if (comp != null)
                    {
                        sb.AppendLine($"{indent}    - {comp.GetType().Name}");
                    }
                }
            }
        }
        
        sb.AppendLine();
        
        // Process children
        foreach (Transform child in obj.transform)
        {
            ProcessGameObject(child.gameObject, sb, depth + 1);
        }
    }
    
    // Context menu for quick access
    [MenuItem("GameObject/Export Hierarchy to File", false, 49)]
    private static void ExportSelectedHierarchy()
    {
        if (Selection.activeGameObject != null)
        {
            GameObjectHierarchyExporter window = GetWindow<GameObjectHierarchyExporter>("Hierarchy Exporter");
            window.targetObject = Selection.activeGameObject;
            window.Show();
        }
        else
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject in the hierarchy.", "OK");
        }
    }
    
    [MenuItem("GameObject/Export Hierarchy to File", true)]
    private static bool ValidateExportSelectedHierarchy()
    {
        return Selection.activeGameObject != null;
    }
}
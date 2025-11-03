#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Utility to fix existing GameObjectReference instances by populating hierarchy paths
/// This should be run once after upgrading to the hierarchical path system
/// </summary>
public class FixGameObjectReferences : EditorWindow
{
    [MenuItem("VR Training/Utilities/Fix GameObject References")]
    public static void ShowWindow()
    {
        GetWindow<FixGameObjectReferences>("Fix References");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "This utility will scan all TrainingSequenceAsset files and refresh their GameObject references.\n\n" +
            "This will:\n" +
            "1. Populate hierarchy paths for all references\n" +
            "2. Fix broken references where possible\n" +
            "3. Save the updated assets\n\n" +
            "IMPORTANT: Save your scene before running this!",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Fix All Training Sequence Assets", GUILayout.Height(30)))
        {
            FixAllSequenceAssets();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Fix Selected Asset Only", GUILayout.Height(30)))
        {
            FixSelectedAsset();
        }
    }

    private static void FixAllSequenceAssets()
    {
        if (!EditorUtility.DisplayDialog("Fix All Assets",
            "This will modify all TrainingSequenceAsset files in the project. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        // Find all TrainingSequenceAsset files
        string[] guids = AssetDatabase.FindAssets("t:TrainingSequenceAsset");
        int fixedCount = 0;
        int totalReferences = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TrainingSequenceAsset asset = AssetDatabase.LoadAssetAtPath<TrainingSequenceAsset>(path);

            if (asset != null)
            {
                Debug.Log($"Processing: {asset.name}");
                int refs = FixSequenceAsset(asset);
                totalReferences += refs;
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete",
            $"Fixed {fixedCount} assets with {totalReferences} total references.\n\nCheck Console for details.",
            "OK");
    }

    private static void FixSelectedAsset()
    {
        var selected = Selection.activeObject as TrainingSequenceAsset;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a TrainingSequenceAsset first.", "OK");
            return;
        }

        int refs = FixSequenceAsset(selected);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Complete",
            $"Fixed {refs} references in {selected.name}",
            "OK");
    }

    private static int FixSequenceAsset(TrainingSequenceAsset asset)
    {
        int refCount = 0;

        foreach (var module in asset.Program.modules)
        {
            foreach (var taskGroup in module.taskGroups)
            {
                foreach (var step in taskGroup.steps)
                {
                    // Refresh all references
                    if (step.targetObject != null)
                    {
                        step.targetObject.RefreshReference();
                        refCount++;

                        var obj = step.targetObject.GameObject;
                        if (obj != null)
                        {
                            Debug.Log($"  ✓ Fixed targetObject: {obj.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"  ⚠ Could not resolve targetObject: {step.targetObject.GameObjectName}");
                        }
                    }

                    if (step.destination != null)
                    {
                        step.destination.RefreshReference();
                        refCount++;

                        var obj = step.destination.GameObject;
                        if (obj != null)
                        {
                            Debug.Log($"  ✓ Fixed destination: {obj.name}");
                        }
                    }

                    if (step.targetSocket != null)
                    {
                        step.targetSocket.RefreshReference();
                        refCount++;

                        var obj = step.targetSocket.GameObject;
                        if (obj != null)
                        {
                            Debug.Log($"  ✓ Fixed targetSocket: {obj.name}");
                        }
                    }

                    if (step.targetArrow != null)
                    {
                        step.targetArrow.RefreshReference();
                        refCount++;

                        var obj = step.targetArrow.GameObject;
                        if (obj != null)
                        {
                            Debug.Log($"  ✓ Fixed targetArrow: {obj.name}");
                        }
                        else if (!string.IsNullOrEmpty(step.targetArrow.GameObjectName))
                        {
                            Debug.LogWarning($"  ⚠ Could not resolve targetArrow: {step.targetArrow.GameObjectName}");
                        }
                    }

                    if (step.destinationArrow != null)
                    {
                        step.destinationArrow.RefreshReference();
                        refCount++;

                        var obj = step.destinationArrow.GameObject;
                        if (obj != null)
                        {
                            Debug.Log($"  ✓ Fixed destinationArrow: {obj.name}");
                        }
                        else if (!string.IsNullOrEmpty(step.destinationArrow.GameObjectName))
                        {
                            Debug.LogWarning($"  ⚠ Could not resolve destinationArrow: {step.destinationArrow.GameObjectName}");
                        }
                    }
                }
            }
        }

        EditorUtility.SetDirty(asset);
        return refCount;
    }
}
#endif

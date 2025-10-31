#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for SequenceArrowRegistry with better UX for arrow assignment
/// </summary>
[CustomEditor(typeof(SequenceArrowRegistry))]
public class SequenceArrowRegistryEditor : Editor
{
    private SerializedProperty sequenceAssetProp;
    private SerializedProperty arrowMappingsProp;
    private SerializedProperty arrowTagProp;

    private Vector2 scrollPos;
    private bool showHelp = true;

    private void OnEnable()
    {
        sequenceAssetProp = serializedObject.FindProperty("sequenceAsset");
        arrowMappingsProp = serializedObject.FindProperty("arrowMappings");
        arrowTagProp = serializedObject.FindProperty("arrowTag");
    }

    public override void OnInspectorGUI()
    {
        var registry = (SequenceArrowRegistry)target;
        serializedObject.Update();

        // Help box
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "This registry stores arrow references for your sequence.\n\n" +
                "IMPORTANT: Arrow references are stored HERE (in the scene), not in the ScriptableObject.\n\n" +
                "Steps:\n" +
                "1. Assign your Training Sequence Asset\n" +
                "2. Click 'Generate Mappings from Sequence'\n" +
                "3. Assign arrow GameObjects for each step\n" +
                "4. Save the scene (Ctrl+S)\n\n" +
                "The arrows will now persist in builds!",
                MessageType.Info);

            if (GUILayout.Button("Hide Help"))
            {
                showHelp = false;
            }
        }
        else
        {
            if (GUILayout.Button("Show Help"))
            {
                showHelp = true;
            }
        }

        EditorGUILayout.Space();

        // Sequence Asset field
        EditorGUILayout.PropertyField(sequenceAssetProp, new GUIContent("Training Sequence Asset"));

        if (registry.sequenceAsset == null)
        {
            EditorGUILayout.HelpBox("Assign a Training Sequence Asset to get started!", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space();

        // Utility buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Mappings from Sequence", GUILayout.Height(25)))
        {
            Undo.RecordObject(registry, "Generate Arrow Mappings");
            registry.GenerateMappingsFromSequence();
            EditorUtility.SetDirty(registry);
            serializedObject.Update();
        }

        if (GUILayout.Button("Cleanup Stale Mappings", GUILayout.Height(25)))
        {
            Undo.RecordObject(registry, "Cleanup Arrow Mappings");
            registry.CleanupStaleMapping();
            EditorUtility.SetDirty(registry);
            serializedObject.Update();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear All Mappings"))
        {
            if (EditorUtility.DisplayDialog("Clear All Mappings",
                "This will remove all arrow assignments. Continue?", "Yes", "Cancel"))
            {
                Undo.RecordObject(registry, "Clear Arrow Mappings");
                registry.ClearAllMappings();
                EditorUtility.SetDirty(registry);
                serializedObject.Update();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Arrow tag field
        EditorGUILayout.PropertyField(arrowTagProp, new GUIContent("Arrow Tag (for filtering)"));

        EditorGUILayout.Space();

        // Statistics
        int totalMappings = registry.arrowMappings.Count;
        int withTargetArrow = registry.arrowMappings.Count(m => m.targetArrow != null);
        int withDestArrow = registry.arrowMappings.Count(m => m.destinationArrow != null);

        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Steps: {totalMappings}");
        EditorGUILayout.LabelField($"Steps with Target Arrow: {withTargetArrow}/{totalMappings}");
        EditorGUILayout.LabelField($"Steps with Destination Arrow: {withDestArrow}/{totalMappings}");

        EditorGUILayout.Space();

        // Mappings list
        EditorGUILayout.LabelField("Arrow Mappings", EditorStyles.boldLabel);

        if (registry.arrowMappings.Count == 0)
        {
            EditorGUILayout.HelpBox("No mappings yet. Click 'Generate Mappings from Sequence' to create them.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < arrowMappingsProp.arraySize; i++)
        {
            var mappingProp = arrowMappingsProp.GetArrayElementAtIndex(i);
            var stepPathProp = mappingProp.FindPropertyRelative("stepPath");
            var targetArrowProp = mappingProp.FindPropertyRelative("targetArrow");
            var destArrowProp = mappingProp.FindPropertyRelative("destinationArrow");
            var hideTargetProp = mappingProp.FindPropertyRelative("hideTargetArrowAfterGrab");
            var showDestProp = mappingProp.FindPropertyRelative("showDestinationAfterGrab");

            // Box for each mapping
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Step path header with colored background
            bool hasTargetArrow = targetArrowProp.objectReferenceValue != null;
            bool hasDestArrow = destArrowProp.objectReferenceValue != null;

            Color headerColor = Color.white;
            if (!hasTargetArrow && !hasDestArrow)
            {
                headerColor = new Color(1f, 0.8f, 0.8f); // Light red
            }
            else if (hasTargetArrow && hasDestArrow)
            {
                headerColor = new Color(0.8f, 1f, 0.8f); // Light green
            }
            else
            {
                headerColor = new Color(1f, 1f, 0.8f); // Light yellow
            }

            GUI.backgroundColor = headerColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(stepPathProp.stringValue, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            // Arrow fields
            EditorGUILayout.PropertyField(targetArrowProp, new GUIContent("Target Arrow"));
            EditorGUILayout.PropertyField(destArrowProp, new GUIContent("Destination Arrow"));

            // Options
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(hideTargetProp, new GUIContent("Hide Target After Grab"), GUILayout.Width(200));
            EditorGUILayout.PropertyField(showDestProp, new GUIContent("Show Dest After Grab"), GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            // Delete button
            if (GUILayout.Button("Remove Mapping", GUILayout.Height(20)))
            {
                Undo.RecordObject(registry, "Remove Arrow Mapping");
                registry.arrowMappings.RemoveAt(i);
                EditorUtility.SetDirty(registry);
                serializedObject.Update();
                break; // Exit loop after modification
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        serializedObject.ApplyModifiedProperties();

        // Warning about saving
        if (GUI.changed)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Remember to save the scene (Ctrl+S) to persist these arrow assignments!", MessageType.Warning);
        }
    }
}
#endif

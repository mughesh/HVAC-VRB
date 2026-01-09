#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for SequenceRegistry with better UX for arrow assignment
/// </summary>
[CustomEditor(typeof(SequenceRegistry))]
public class SequenceRegistryEditor : Editor
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
        var registry = (SequenceRegistry)target;
        serializedObject.Update();

        // Help box
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "AUTOMATED WORKFLOW:\n\n" +
                "1. Assign your Training Sequence Asset below\n" +
                "2. Edit references in Sequence Builder UI (Window > Sequence Builder > Setup Assistant > Sequence tab)\n" +
                "3. Click 'Sync from Sequence Asset' button to transfer ALL references here automatically\n" +
                "4. Save scene (Ctrl+S)\n\n" +
                "Benefits:\n" +
                "✓ Edit in ONE place (Sequence Builder)\n" +
                "✓ Automatic transfer to registry\n" +
                "✓ References persist in builds\n" +
                "✓ No double-entry!",
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

        // AUTO-SYNC CHECKBOX (Prominent placement)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Auto-Sync Settings", EditorStyles.boldLabel);

        SerializedProperty autoSyncOnSaveProp = serializedObject.FindProperty("autoSyncOnSave");
        EditorGUILayout.PropertyField(autoSyncOnSaveProp,
            new GUIContent("Auto-Sync on Scene Save",
                "Automatically sync registry from sequence asset whenever you save the scene (Ctrl+S or auto-save)"));

        if (autoSyncOnSaveProp.boolValue)
        {
            EditorGUILayout.HelpBox(
                "✓ Auto-sync ENABLED\n\n" +
                "Registry will automatically rebuild from sequence asset every time the scene is saved.\n" +
                "This prevents data loss and handles renamed/deleted steps automatically.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "⚠ Auto-sync DISABLED\n\n" +
                "You must manually click 'Sync from Sequence Asset' button or use the 'Sync Registry' button in the Sequence tab.\n" +
                "Remember to sync before saving the scene!",
                MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // BIG SYNC BUTTON
        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); // Green
        if (GUILayout.Button("🔄 SYNC FROM SEQUENCE ASSET", GUILayout.Height(40)))
        {
            Undo.RecordObject(registry, "Sync Arrow Mappings");
            int synced = registry.SyncFromSequenceAsset();
            EditorUtility.SetDirty(registry);
            serializedObject.Update();

            if (synced > 0)
            {
                EditorUtility.DisplayDialog("Sync Complete",
                    $"Successfully synced {synced} references from sequence asset!\n\n" +
                    "Remember to save the scene (Ctrl+S) to persist these references.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Sync Warning",
                    "No references were synced. This usually means:\n\n" +
                    "1. References aren't set in the Sequence Builder yet, OR\n" +
                    "2. Unity has already cleared them (editor was restarted)\n\n" +
                    "Solution: Open the Sequence Builder, assign references, then Sync immediately.",
                    "OK");
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // Sync status display
        EditorGUILayout.LabelField("Sync Status", EditorStyles.boldLabel);
        string status = registry.GetSyncStatus();
        EditorGUILayout.HelpBox(status, MessageType.Info);

        EditorGUILayout.Space();

        // Utility buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Empty Mappings", GUILayout.Height(25)))
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
            var targetObjProp = mappingProp.FindPropertyRelative("targetObject");
            var destObjProp = mappingProp.FindPropertyRelative("destinationObject");
            var targetArrowProp = mappingProp.FindPropertyRelative("targetArrow");
            var destArrowProp = mappingProp.FindPropertyRelative("destinationArrow");
            var hideTargetProp = mappingProp.FindPropertyRelative("hideTargetArrowAfterGrab");
            var showDestProp = mappingProp.FindPropertyRelative("showDestinationAfterGrab");

            // Box for each mapping
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Step path header with colored background
            bool hasTargetObj = targetObjProp.objectReferenceValue != null;
            bool hasTargetArrow = targetArrowProp.objectReferenceValue != null;
            bool hasDestArrow = destArrowProp.objectReferenceValue != null;

            Color headerColor = Color.white;
            int refCount = 0;
            if (hasTargetObj) refCount++;
            if (hasTargetArrow) refCount++;
            if (hasDestArrow) refCount++;

            if (refCount == 0)
            {
                headerColor = new Color(1f, 0.8f, 0.8f); // Light red - nothing set
            }
            else if (refCount >= 2)
            {
                headerColor = new Color(0.8f, 1f, 0.8f); // Light green - mostly complete
            }
            else
            {
                headerColor = new Color(1f, 1f, 0.8f); // Light yellow - partial
            }

            GUI.backgroundColor = headerColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(stepPathProp.stringValue, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            // Step objects
            EditorGUILayout.PropertyField(targetObjProp, new GUIContent("Target Object"));
            EditorGUILayout.PropertyField(destObjProp, new GUIContent("Destination Object"));

            EditorGUILayout.Space(3);

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

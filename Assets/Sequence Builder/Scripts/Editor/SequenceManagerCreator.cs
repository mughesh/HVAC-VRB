// SequenceManagerCreator.cs
// Creates Sequence Manager GameObject with all step handlers
// Solves the prefab GUID issue when distributing DLLs
using UnityEngine;
using UnityEditor;
using VRTrainingKit.Utilities;

/// <summary>
/// Creates a Sequence Manager GameObject with all necessary step handlers
/// This avoids prefab GUID issues when distributing as DLLs
/// </summary>
public class SequenceManagerCreator : EditorWindow
{
    /// <summary>
    /// Creates a complete Sequence Manager setup in the scene
    /// Can be called from menu or from Setup Assistant
    /// </summary>
    /// <returns>The created Sequence Manager root GameObject</returns>
    public static GameObject CreateSequenceManagerInScene()
    {
        // Check if one already exists
        var existing = FindObjectOfType<ModularTrainingSequenceController>();
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Sequence Manager Already Exists",
                "A Sequence Manager already exists in the scene at: " + existing.gameObject.name + "\n\n" +
                "Do you want to replace it?",
                "Replace",
                "Cancel"
            );

            if (!replace)
                return existing.gameObject;

            DestroyImmediate(existing.transform.root.gameObject);
        }

        // Create main GameObject (root parent)
        GameObject sequenceManager = new GameObject("Sequence Manager");

        // Create child objects
        CreateStepHandlers(sequenceManager);  // Creates "Sequence Manager" child with controller + handlers
        CreateUtilities(sequenceManager);      // Creates runtime, Scene Registry, VRTrainingDebug, HandColliderRegistry

        // Select the created object
        Selection.activeGameObject = sequenceManager;
        EditorGUIUtility.PingObject(sequenceManager);

        Debug.Log("✅ Sequence Manager created successfully!");

        return sequenceManager;
    }

    [MenuItem("Sequence Builder/Create Sequence Manager")]
    public static void CreateSequenceManager()
    {
        var created = CreateSequenceManagerInScene();

        if (created != null)
        {
            EditorUtility.DisplayDialog(
                "Sequence Manager Created!",
                "Sequence Manager has been created in your scene.\n\n" +
                "Next steps:\n" +
                "1. Assign a Training Sequence Asset to the controller\n" +
                "2. Configure step handlers if needed\n" +
                "3. Test in Play mode",
                "OK"
            );
        }
    }

    private static void CreateStepHandlers(GameObject parent)
    {
        // Add main controller and all handlers to the child "Sequence Manager" GameObject
        GameObject sequenceChild = new GameObject("Sequence Manager");
        sequenceChild.transform.SetParent(parent.transform);

        // Add main controller
        sequenceChild.AddComponent<ModularTrainingSequenceController>();

        // Add XRI step handlers
        sequenceChild.AddComponent<GrabStepHandler>();
        sequenceChild.AddComponent<KnobStepHandler>();
        sequenceChild.AddComponent<SnapStepHandler>();
        sequenceChild.AddComponent<ScrewStepHandler>();

        // Add AutoHands step handlers
        sequenceChild.AddComponent<AutoHandsGrabStepHandler>();
        sequenceChild.AddComponent<AutoHandsSnapStepHandler>();
        sequenceChild.AddComponent<AutoHandsKnobStepHandler>();
        sequenceChild.AddComponent<AutoHandsScrewStepHandler>();
        sequenceChild.AddComponent<AutoHandsWaitForScriptConditionHandler>();
        sequenceChild.AddComponent<AutoHandsTeleportStepHandler>();

        // Add utilities
        sequenceChild.AddComponent<SnapObjectsAtStart>();

        Debug.Log("✅ Added main controller and all step handlers");
    }

    private static void CreateUtilities(GameObject parent)
    {
        // Create runtime settings
        GameObject runtime = new GameObject("runtime");
        runtime.transform.SetParent(parent.transform);
        runtime.AddComponent<RuntimeMonitorSettings>();

        // Create Scene Registry
        GameObject sceneRegistry = new GameObject("Scene Registry");
        sceneRegistry.transform.SetParent(parent.transform);
        sceneRegistry.AddComponent<SequenceRegistry>();

        // Create VRTrainingDebug
        GameObject debugObj = new GameObject("VRTrainingDebug");
        debugObj.transform.SetParent(parent.transform);
        debugObj.AddComponent<VRTrainingDebug>();

        // Create HandColliderRegistry
        GameObject handRegistry = new GameObject("HandColliderRegistry");
        handRegistry.transform.SetParent(parent.transform);
        handRegistry.AddComponent<DummyCondition>();
        handRegistry.AddComponent<VRHandColliderRegistry>();

        Debug.Log("✅ Added utility components");
    }

    [MenuItem("Sequence Builder/Create Sequence Manager", true)]
    private static bool ValidateCreateSequenceManager()
    {
        // Menu item is always available
        return true;
    }
}

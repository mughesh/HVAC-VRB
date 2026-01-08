// #if UNITY_EDITOR
// using UnityEngine;
// using UnityEditor;

// /// <summary>
// /// Test script to verify that the improved GameObjectReference system works correctly
// /// This helps validate that object references survive name changes and Unity restarts
// /// </summary>
// public static class TestSequenceReferences
// {
//     [MenuItem("VR Training/Tests/Test Object Reference Reliability")]
//     public static void TestObjectReferences()
//     {
//         Debug.Log("[TEST] Starting GameObject reference reliability test...");

//         // Find or create test objects
//         GameObject testObject = GameObject.Find("TestObject") ?? new GameObject("TestObject");
//         GameObject testDestination = GameObject.Find("TestDestination") ?? new GameObject("TestDestination");

//         // Create test references
//         var targetRef = new GameObjectReference(testObject);
//         var destRef = new GameObjectReference(testDestination);

//         Debug.Log($"[TEST] Created references - Target: {targetRef}, Destination: {destRef}");
//         Debug.Log($"[TEST] Initial validity - Target: {targetRef.IsValid}, Destination: {destRef.IsValid}");

//         // Test 1: Basic reference functionality
//         if (targetRef.GameObject == testObject && destRef.GameObject == testDestination)
//         {
//             Debug.Log("✅ [TEST] Basic reference retrieval works correctly");
//         }
//         else
//         {
//             Debug.LogError("❌ [TEST] Basic reference retrieval failed!");
//             return;
//         }

//         // Test 2: Rename objects and check if references survive
//         string originalName1 = testObject.name;
//         string originalName2 = testDestination.name;

//         testObject.name = "RenamedTestObject";
//         testDestination.name = "RenamedTestDestination";

//         Debug.Log($"[TEST] Renamed objects - Target: {originalName1} → {testObject.name}, Destination: {originalName2} → {testDestination.name}");

//         // Force references to refresh
//         targetRef.RefreshReference();
//         destRef.RefreshReference();

//         if (targetRef.IsValid && destRef.IsValid)
//         {
//             Debug.Log("✅ [TEST] References survived object renaming!");
//             Debug.Log($"[TEST] After rename - Target: {targetRef}, Destination: {destRef}");
//         }
//         else
//         {
//             Debug.LogError("❌ [TEST] References lost after object renaming!");
//             Debug.LogError($"[TEST] Target valid: {targetRef.IsValid}, Destination valid: {destRef.IsValid}");
//         }

//         // Test 3: Test instance ID persistence
//         int targetInstanceID = testObject.GetInstanceID();
//         int destInstanceID = testDestination.GetInstanceID();

//         Debug.Log($"[TEST] Instance IDs - Target: {targetInstanceID}, Destination: {destInstanceID}");

//         // Test 4: Create a training step to test in context
//         var testStep = new InteractionStep("Test Step", InteractionStep.StepType.GrabAndSnap)
//         {
//             targetObject = targetRef,
//             destination = destRef,
//             hint = "This is a test step"
//         };

//         if (testStep.IsValid())
//         {
//             Debug.Log("✅ [TEST] Training step validation works with renamed objects");
//         }
//         else
//         {
//             Debug.LogError("❌ [TEST] Training step validation failed!");
//             Debug.LogError($"[TEST] Validation message: {testStep.GetValidationMessage()}");
//         }

//         // Cleanup
//         if (EditorUtility.DisplayDialog("Test Complete",
//             "Object reference reliability test completed. Check the console for results.\n\nDelete test objects?",
//             "Yes", "No"))
//         {
//             Object.DestroyImmediate(testObject);
//             Object.DestroyImmediate(testDestination);
//         }

//         Debug.Log("[TEST] Object reference reliability test completed. Check results above.");
//     }

//     [MenuItem("VR Training/Tests/Test Training Asset Persistence")]
//     public static void TestAssetPersistence()
//     {
//         Debug.Log("[TEST] Starting training asset persistence test...");

//         // Create test objects
//         GameObject valve = new GameObject("TestValve");
//         GameObject socket = new GameObject("TestSocket");

//         // Create a test training asset
//         var asset = TrainingSequenceAssetManager.CreateEmptyAsset("Test_Persistence_Asset");

//         // Add a test step with object references
//         var testModule = new TrainingModule("Test Module", "Testing persistence");
//         var testGroup = new TaskGroup("Test Group", "Testing group");
//         var testStep = new InteractionStep("Test Valve Installation", InteractionStep.StepType.InstallValve)
//         {
//             targetObject = new GameObjectReference(valve),
//             targetSocket = new GameObjectReference(socket),
//             hint = "Test step for persistence"
//         };

//         testGroup.steps.Add(testStep);
//         testModule.taskGroups.Add(testGroup);
//         asset.Program.modules.Add(testModule);

//         // Save the asset
//         TrainingSequenceAssetManager.SaveAssetToSequencesFolder(asset, "TestPersistenceAsset.asset");

//         Debug.Log("✅ [TEST] Created test training asset with object references");
//         Debug.Log($"[TEST] Step validation before rename: {testStep.IsValid()} - {testStep.GetValidationMessage()}");

//         // Rename the objects
//         valve.name = "RenamedTestValve";
//         socket.name = "RenamedTestSocket";

//         Debug.Log("[TEST] Renamed test objects");
//         Debug.Log($"[TEST] Step validation after rename: {testStep.IsValid()} - {testStep.GetValidationMessage()}");

//         // Mark asset as dirty to test auto-save
//         EditorUtility.SetDirty(asset);

//         if (EditorUtility.DisplayDialog("Test In Progress",
//             "Persistence test created. Now:\n\n1. Check the test asset in VR Training/Setup Assistant > Sequence tab\n2. Try renaming the test objects in the scene\n3. Restart Unity and check if references persist\n\nDelete test objects now?",
//             "Yes", "No"))
//         {
//             Object.DestroyImmediate(valve);
//             Object.DestroyImmediate(socket);
//         }

//         Debug.Log("[TEST] Training asset persistence test setup completed. Check the Sequence tab in VR Training Setup Assistant.");
//     }
// }
// #endif
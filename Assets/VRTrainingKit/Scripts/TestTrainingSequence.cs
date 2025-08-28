using UnityEngine;

  public class TestTrainingSequence : MonoBehaviour
  {
      public TrainingProgram testProgram;
      public TrainingSequenceAsset testAsset;

      void Start()
      {
          // Test 1: Direct factory method (like Phase 1)
          testProgram = TrainingSequenceFactory.CreateHVACLeakTestingProgram();
          Debug.Log($"Direct factory stats: Modules: {testProgram.modules.Count}");

          // Test 2: Create HVAC template asset
          testAsset = TrainingSequenceAssetManager.CreateHVACTemplateAsset();
          var stats = testAsset.GetStats();
          Debug.Log($"Asset template stats: {stats}");

          // Test 3: Validation
          var validation = testAsset.ValidateProgram();
          Debug.Log($"Validation - Errors: {validation.errors.Count}, Warnings: {validation.warnings.Count}");
          foreach(var error in validation.errors)
          {
              Debug.LogWarning($"Validation Error: {error}");
          }
      }
  }
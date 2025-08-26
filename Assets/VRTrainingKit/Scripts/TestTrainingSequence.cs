using UnityEngine;

  public class TestTrainingSequence : MonoBehaviour
  {
      public TrainingProgram testProgram;

  void Start()
  {
      testProgram = TrainingSequenceFactory.CreateHVACLeakTestingProgram();

      // Test validation on the first step
      var firstStep = testProgram.modules[0].taskGroups[0].steps[0];
      Debug.Log($"Step '{firstStep.stepName}' is valid: {firstStep.IsValid()}");
      Debug.Log($"Validation message: {firstStep.GetValidationMessage()}");
  }
  }
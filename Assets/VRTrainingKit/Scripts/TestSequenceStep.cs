using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test script to verify SequenceStep serialization works properly
/// Temporary script for debugging the GameObject field issue
/// </summary>
public class TestSequenceStep : MonoBehaviour
{
    [Header("Test SequenceStep Serialization")]
    public SequenceStep testStep = new SequenceStep();
    
    [Header("Test GameObject Fields Directly")]
    public GameObject testRequiredObject;
    public GameObject testSecondaryObject;
    
    [Header("Test List of Steps")]
    public List<SequenceStep> testSteps = new List<SequenceStep>();
    
    [Header("Test TrainingChapter")]
    public TrainingChapter testChapter;
    
    private void Start()
    {
        Debug.Log("[Test] TestSequenceStep component loaded successfully");
        
        if (testStep != null)
        {
            Debug.Log($"[Test] Test step name: {testStep.stepName}");
            Debug.Log($"[Test] Required object: {(testStep.requiredObject != null ? testStep.requiredObject.name : "NULL")}");
        }
    }
    
    [ContextMenu("Test Step Validation")]
    public void TestStepValidation()
    {
        if (testStep != null)
        {
            Debug.Log($"[Test] Step '{testStep.stepName}' completion check: {testStep.CheckCompletion()}");
        }
    }
    
    [ContextMenu("Create Sample Step")]
    public void CreateSampleStep()
    {
        testStep = new SequenceStep();
        testStep.stepName = "Test Snap Interaction";
        testStep.instruction = "Connect the test object to the snap point";
        testStep.requirementType = SequenceStep.RequirementType.MustBeSnapped;
        testStep.requiredObject = testRequiredObject;
        testStep.secondaryObject = testSecondaryObject;
        
        Debug.Log("[Test] Sample step created");
    }
}
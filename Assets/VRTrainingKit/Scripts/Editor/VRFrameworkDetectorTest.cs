// VRFrameworkDetectorTest.cs
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Simple test script for VRFrameworkDetector functionality
/// Menu: VR Training > Test Framework Detection
/// </summary>
public static class VRFrameworkDetectorTest
{
    [MenuItem("VR Training/Test Framework Detection")]
    public static void TestFrameworkDetection()
    {
        Debug.Log("=== VR Framework Detection Test ===");

        // Test basic detection
        var detectedFramework = VRFrameworkDetector.DetectCurrentFramework();
        var displayName = VRFrameworkDetector.GetFrameworkDisplayName(detectedFramework);

        Debug.Log($"Detected Framework: {detectedFramework} ({displayName})");

        // Test detailed info
        var frameworkInfo = VRFrameworkDetector.GetFrameworkInfo();
        Debug.Log($"Framework Info:\n{frameworkInfo}");

        // Test validation
        var isValid = VRFrameworkDetector.ValidateFrameworkSetup();
        Debug.Log($"Framework Setup Valid: {isValid}");

        // Test specific framework checks
        Debug.Log($"XRI Available: {VRFrameworkDetector.IsFrameworkAvailable(VRFramework.XRI)}");
        Debug.Log($"AutoHands Available: {VRFrameworkDetector.IsFrameworkAvailable(VRFramework.AutoHands)}");

        // Test VRFrameworkManager
        Debug.Log("=== VRFrameworkManager Test ===");
        var manager = VRFrameworkManager.Instance;
        var activeFramework = manager.GetActiveFramework();
        var hasMismatch = manager.HasFrameworkMismatch();
        var validation = manager.ValidateCurrentSetup();

        Debug.Log($"Active Framework (Manager): {VRFrameworkDetector.GetFrameworkDisplayName(activeFramework)}");
        Debug.Log($"Framework Mismatch: {hasMismatch}");
        Debug.Log($"Manager Validation Errors: {validation.errors.Count}");
        Debug.Log($"Manager Validation Warnings: {validation.warnings.Count}");

        // Test framework-aware validation
        Debug.Log("=== Framework-Aware Validation Test ===");
        var validationIssues = InteractionSetupService.ValidateSetup();
        Debug.Log($"Setup Validation Issues Found: {validationIssues.Count}");

        foreach (var issue in validationIssues)
        {
            Debug.Log($"  - {issue}");
        }

        Debug.Log("=== Framework Test Complete ===");

        // Show results in dialog for easy viewing
        var validationSummary = validationIssues.Count > 0
            ? $"\nSetup Validation Issues ({validationIssues.Count}):\n{string.Join("\n", validationIssues.Take(3))}"
            : "\nSetup Validation: All checks passed!";

        var message = $"Framework Test Results:\n\n" +
                     $"Detected: {displayName}\n" +
                     $"Active (Manager): {VRFrameworkDetector.GetFrameworkDisplayName(activeFramework)}\n" +
                     $"Valid Setup: {isValid}\n" +
                     $"Has Mismatch: {hasMismatch}\n" +
                     validationSummary + "\n\n" +
                     $"Framework Details:\n{frameworkInfo}";

        EditorUtility.DisplayDialog("Framework Test Results", message, "OK");
    }

    [MenuItem("VR Training/Test Framework Detection", true)]
    public static bool ValidateTestFrameworkDetection()
    {
        // Only enable menu item when in play mode or with scene loaded
        return true;
    }
}
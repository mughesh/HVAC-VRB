// VRStepHandlerTest.cs
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Test script for framework-aware step handlers
/// Menu: VR Training > Test Step Handlers
/// </summary>
public static class VRStepHandlerTest
{
    [MenuItem("VR Training/Test Step Handlers")]
    public static void TestStepHandlers()
    {
        Debug.Log("=== VR Step Handler Framework Test ===");

        // Detect current framework
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        var frameworkName = VRFrameworkDetector.GetFrameworkDisplayName(currentFramework);
        Debug.Log($"Current Framework: {frameworkName}");

        // Find all step handlers in scene
        var allHandlers = Object.FindObjectsOfType<MonoBehaviour>().OfType<IStepHandler>().ToList();
        Debug.Log($"Found {allHandlers.Count} step handlers in scene");

        // Test framework compatibility
        var compatibleHandlers = 0;
        var incompatibleHandlers = 0;

        foreach (var handler in allHandlers)
        {
            var handlerName = handler.GetType().Name;
            var supportsCurrentFramework = handler.SupportsFramework(currentFramework);
            var supportedFrameworks = GetSupportedFrameworksString(handler);

            Debug.Log($"Handler: {handlerName}");
            Debug.Log($"  Supports Current Framework ({frameworkName}): {supportsCurrentFramework}");
            Debug.Log($"  Supported Frameworks: {supportedFrameworks}");

            if (supportsCurrentFramework)
                compatibleHandlers++;
            else
                incompatibleHandlers++;
        }

        // Test step type handling
        Debug.Log("=== Step Type Compatibility Test ===");
        foreach (var stepType in System.Enum.GetValues(typeof(InteractionStep.StepType)).Cast<InteractionStep.StepType>())
        {
            var handlersForStep = allHandlers.Where(h => h.CanHandle(stepType) && h.SupportsFramework(currentFramework)).ToList();
            Debug.Log($"Step Type '{stepType}': {handlersForStep.Count} compatible handlers");

            foreach (var handler in handlersForStep)
            {
                Debug.Log($"  - {handler.GetType().Name}");
            }
        }

        Debug.Log("=== Step Handler Test Complete ===");

        // Show results in dialog
        var message = $"Step Handler Test Results:\n\n" +
                     $"Framework: {frameworkName}\n" +
                     $"Total Handlers: {allHandlers.Count}\n" +
                     $"Compatible: {compatibleHandlers}\n" +
                     $"Incompatible: {incompatibleHandlers}\n\n" +
                     $"Check Console for detailed results.";

        EditorUtility.DisplayDialog("Step Handler Test", message, "OK");
    }

    /// <summary>
    /// Get supported frameworks as string for a handler
    /// </summary>
    private static string GetSupportedFrameworksString(IStepHandler handler)
    {
        var supported = new System.Collections.Generic.List<string>();

        if (handler.SupportsFramework(VRFramework.XRI))
            supported.Add("XRI");
        if (handler.SupportsFramework(VRFramework.AutoHands))
            supported.Add("AutoHands");
        if (handler.SupportsFramework(VRFramework.None))
            supported.Add("None");

        return supported.Count > 0 ? string.Join(", ", supported) : "Unknown";
    }

    [MenuItem("VR Training/Test Step Handlers", true)]
    public static bool ValidateTestStepHandlers()
    {
        return true; // Always available
    }
}
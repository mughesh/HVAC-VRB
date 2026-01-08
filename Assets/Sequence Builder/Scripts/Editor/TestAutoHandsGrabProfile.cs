// TestAutoHandsGrabProfile.cs
// Simple test script to validate AutoHandsGrabProfile implementation
using UnityEngine;

/// <summary>
/// Test script to validate AutoHandsGrabProfile configuration
/// Phase 2.1.1: Basic Grabbable component testing
/// </summary>
public class TestAutoHandsGrabProfile : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("AutoHands Grab Profile to test")]
    public AutoHandsGrabProfile testProfile;

    [Tooltip("Target object to apply profile to (must have 'grab' tag)")]
    public GameObject testTarget;

    [Header("Test Controls")]
    [Space]
    public bool enableDetailedLogging = true;

    [Space]
    [Header("Test Actions")]
    [Space]
    [Tooltip("Apply the profile to the test target")]
    public bool applyProfile = false;

    [Tooltip("Validate the configuration")]
    public bool validateConfiguration = false;

    [Tooltip("Check component properties")]
    public bool checkProperties = false;

    [Tooltip("Clean up test components")]
    public bool cleanupComponents = false;

    private void Update()
    {
        // Handle test actions via inspector toggles
        if (applyProfile)
        {
            applyProfile = false;
            TestApplyProfile();
        }

        if (validateConfiguration)
        {
            validateConfiguration = false;
            TestValidateConfiguration();
        }

        if (checkProperties)
        {
            checkProperties = false;
            TestCheckProperties();
        }

        if (cleanupComponents)
        {
            cleanupComponents = false;
            TestCleanupComponents();
        }
    }

    /// <summary>
    /// Test applying the AutoHandsGrabProfile to the target object
    /// </summary>
    private void TestApplyProfile()
    {
        Log("🧪 Testing AutoHandsGrabProfile.ApplyToGameObject()");

        if (testProfile == null)
        {
            LogError("❌ Test Profile is null! Please assign an AutoHandsGrabProfile.");
            return;
        }

        if (testTarget == null)
        {
            LogError("❌ Test Target is null! Please assign a GameObject with 'grab' tag.");
            return;
        }

        if (!testTarget.CompareTag("grab"))
        {
            LogError($"❌ Test Target '{testTarget.name}' must have 'grab' tag!");
            return;
        }

        Log($"📋 Profile Settings: grabType={testProfile.grabType}, handType={testProfile.handType}");
        Log($"🎯 Applying profile to: {testTarget.name}");

        try
        {
            testProfile.ApplyToGameObject(testTarget);
            Log("✅ Profile application completed!");
        }
        catch (System.Exception ex)
        {
            LogError($"❌ Profile application failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test validating the configuration
    /// </summary>
    private void TestValidateConfiguration()
    {
        Log("🔍 Testing configuration validation");

        if (testProfile == null || testTarget == null)
        {
            LogError("❌ Profile or Target is null!");
            return;
        }

        bool isValid = testProfile.ValidateGameObject(testTarget);
        Log($"📊 Validation Result: {(isValid ? "✅ VALID" : "❌ INVALID")}");

        // Check for required components
        var rigidbody = testTarget.GetComponent<Rigidbody>();
        var collider = testTarget.GetComponent<Collider>();
        var grabbable = testTarget.GetComponent<Autohand.Grabbable>();

        Log($"🔧 Components Check:");
        Log($"   - Rigidbody: {(rigidbody != null ? "✅ Found" : "❌ Missing")}");
        Log($"   - Collider: {(collider != null ? "✅ Found" : "❌ Missing")}");
        Log($"   - Grabbable: {(grabbable != null ? "✅ Found" : "❌ Missing")}");
    }

    /// <summary>
    /// Test checking Grabbable component properties
    /// </summary>
    private void TestCheckProperties()
    {
        Log("🔎 Testing Grabbable component properties");

        if (testTarget == null)
        {
            LogError("❌ Test Target is null!");
            return;
        }

        var grabbable = testTarget.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            LogError("❌ No Grabbable component found! Apply profile first.");
            return;
        }

        var grabbableType = grabbable.GetType();
        Log($"📝 Grabbable Component Type: {grabbableType.Name}");

        // Check key properties that we configured
        CheckProperty(grabbable, grabbableType, "grabType");
        CheckProperty(grabbable, grabbableType, "handType");
        CheckProperty(grabbable, grabbableType, "singleHandOnly");
        CheckProperty(grabbable, grabbableType, "ignoreWeight");
        CheckProperty(grabbable, grabbableType, "parentOnGrab");
        CheckProperty(grabbable, grabbableType, "jointBreakForce");
        CheckProperty(grabbable, grabbableType, "grabPriorityWeight");

        Log("✅ Property check completed!");
    }

    /// <summary>
    /// Helper method to check and log a specific property value
    /// </summary>
    private void CheckProperty(Component component, System.Type componentType, string propertyName)
    {
        try
        {
            // Try property first
            var property = componentType.GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(component);
                Log($"   - {propertyName}: {value} (property)");
                return;
            }

            // Try field if property not found
            var field = componentType.GetField(propertyName);
            if (field != null)
            {
                var value = field.GetValue(component);
                Log($"   - {propertyName}: {value} (field)");
                return;
            }

            Log($"   - {propertyName}: ⚠️ Not found");
        }
        catch (System.Exception ex)
        {
            Log($"   - {propertyName}: ❌ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up test components from target object
    /// </summary>
    private void TestCleanupComponents()
    {
        Log("🧹 Cleaning up test components");

        if (testTarget == null)
        {
            LogError("❌ Test Target is null!");
            return;
        }

        // Remove Grabbable component
        var grabbable = testTarget.GetComponent<Autohand.Grabbable>();
        if (grabbable != null)
        {
            DestroyImmediate(grabbable);
            Log("🗑️ Removed Grabbable component");
        }

        // Optionally remove Rigidbody and Collider (be careful here)
        Log("⚠️ Rigidbody and Collider left intact (remove manually if needed)");
        Log("✅ Cleanup completed!");
    }

    /// <summary>
    /// Logging helper methods
    /// </summary>
    private void Log(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AutoHandsGrabTest] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[AutoHandsGrabTest] {message}");
    }

    /// <summary>
    /// Load default AutoHandsGrabProfile from Resources for quick testing
    /// </summary>
    [ContextMenu("Load Default AutoHands Grab Profile")]
    private void LoadDefaultProfile()
    {
        testProfile = Resources.Load<AutoHandsGrabProfile>("Auto hands - Profiles/AutoHandsGrabProfile");
        if (testProfile != null)
        {
            Log("✅ Loaded default AutoHandsGrabProfile from Resources");
        }
        else
        {
            LogError("❌ Could not load default AutoHandsGrabProfile from Resources");
        }
    }
}
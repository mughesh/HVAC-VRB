// TestAutoHandsSequenceIntegration.cs
// Test script to validate AutoHands grab events work with sequence system
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Test script to validate AutoHands grab step handler integration with sequence builder
/// This helps verify that AutoHands grab events trigger step completion properly
/// </summary>
public class TestAutoHandsSequenceIntegration : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Target object to grab (must have AutoHands Grabbable component)")]
    public GameObject testGrabbableObject;

    [Header("Test Controls")]
    [Space]
    public bool enableDetailedLogging = true;

    [Space]
    [Header("Test Actions")]
    [Space]
    [Tooltip("Check if AutoHandsGrabStepHandler is properly registered")]
    public bool checkHandlerRegistration = false;

    [Tooltip("Check if test object has required components")]
    public bool validateTestObject = false;

    [Tooltip("Test grab event subscription manually")]
    public bool testGrabEvents = false;

    [Tooltip("Check sequence controller setup")]
    public bool checkSequenceController = false;

    private void Update()
    {
        // Handle test actions via inspector toggles
        if (checkHandlerRegistration)
        {
            checkHandlerRegistration = false;
            CheckHandlerRegistration();
        }

        if (validateTestObject)
        {
            validateTestObject = false;
            ValidateTestObject();
        }

        if (testGrabEvents)
        {
            testGrabEvents = false;
            TestGrabEvents();
        }

        if (checkSequenceController)
        {
            checkSequenceController = false;
            CheckSequenceController();
        }
    }

    /// <summary>
    /// Check if AutoHandsGrabStepHandler is properly registered in the scene
    /// </summary>
    private void CheckHandlerRegistration()
    {
        Log("🔍 Checking AutoHandsGrabStepHandler registration...");

        var autoHandsGrabHandler = FindObjectOfType<AutoHandsGrabStepHandler>();
        if (autoHandsGrabHandler != null)
        {
            Log($"✅ Found AutoHandsGrabStepHandler: {autoHandsGrabHandler.name}");

            // Check if it's initialized
            var controller = FindObjectOfType<ModularTrainingSequenceController>();
            if (controller != null)
            {
                Log($"✅ Found ModularTrainingSequenceController: {controller.name}");
            }
            else
            {
                LogWarning("⚠️ No ModularTrainingSequenceController found in scene");
            }
        }
        else
        {
            LogError("❌ AutoHandsGrabStepHandler not found! Make sure to add it to a GameObject in your scene.");
        }

        // Also check for any XRI handlers (for comparison)
        var xriGrabHandler = FindObjectOfType<GrabStepHandler>();
        if (xriGrabHandler != null)
        {
            Log($"ℹ️ Also found XRI GrabStepHandler: {xriGrabHandler.name}");
        }
    }

    /// <summary>
    /// Validate that the test object has the required AutoHands components
    /// </summary>
    private void ValidateTestObject()
    {
        Log("🔍 Validating test object components...");

        if (testGrabbableObject == null)
        {
            LogError("❌ Test grabbable object is not assigned!");
            return;
        }

        // Check for AutoHands Grabbable component
        var grabbable = testGrabbableObject.GetComponent<Autohand.Grabbable>();
        if (grabbable != null)
        {
            Log($"✅ Found Grabbable component on {testGrabbableObject.name}");
            Log($"   - Grab Type: {grabbable.grabType}");
            Log($"   - Hand Type: {grabbable.handType}");
            Log($"   - Single Hand Only: {grabbable.singleHandOnly}");
        }
        else
        {
            LogError($"❌ No Grabbable component found on {testGrabbableObject.name}!");
            LogError("   Use your AutoHandsGrabProfile to configure this object first.");
        }

        // Check for required Unity components
        var rigidbody = testGrabbableObject.GetComponent<Rigidbody>();
        var collider = testGrabbableObject.GetComponent<Collider>();

        Log($"🔧 Component Status:");
        Log($"   - Rigidbody: {(rigidbody != null ? "✅ Found" : "❌ Missing")}");
        Log($"   - Collider: {(collider != null ? "✅ Found" : "❌ Missing")}");
        Log($"   - Tag: {testGrabbableObject.tag} {(testGrabbableObject.CompareTag("grab") ? "✅" : "⚠️ Should be 'grab'")}");
    }

    /// <summary>
    /// Test subscribing to AutoHands grab events manually
    /// </summary>
    private void TestGrabEvents()
    {
        Log("🔍 Testing AutoHands grab event subscription...");

        if (testGrabbableObject == null)
        {
            LogError("❌ Test grabbable object is not assigned!");
            return;
        }

        var grabbable = testGrabbableObject.GetComponent<Autohand.Grabbable>();
        if (grabbable == null)
        {
            LogError("❌ No Grabbable component found!");
            return;
        }

        // Subscribe to grab events temporarily for testing
        grabbable.OnGrabEvent += TestOnGrabbed;
        grabbable.OnReleaseEvent += TestOnReleased;

        Log("✅ Subscribed to grab/release events for testing");
        Log("🎯 Try grabbing the object now - you should see event logs");

        // Auto-unsubscribe after 10 seconds
        Invoke(nameof(UnsubscribeTestEvents), 10f);
    }

    /// <summary>
    /// Test event handler for grab
    /// </summary>
    private void TestOnGrabbed(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        Log($"🎯 TEST EVENT: Object grabbed!");
        Log($"   - Object: {grabbable.name}");
        Log($"   - Hand: {hand.name}");
    }

    /// <summary>
    /// Test event handler for release
    /// </summary>
    private void TestOnReleased(Autohand.Hand hand, Autohand.Grabbable grabbable)
    {
        Log($"🎯 TEST EVENT: Object released!");
        Log($"   - Object: {grabbable.name}");
        Log($"   - Hand: {hand.name}");
    }

    /// <summary>
    /// Unsubscribe from test events
    /// </summary>
    private void UnsubscribeTestEvents()
    {
        if (testGrabbableObject != null)
        {
            var grabbable = testGrabbableObject.GetComponent<Autohand.Grabbable>();
            if (grabbable != null)
            {
                grabbable.OnGrabEvent -= TestOnGrabbed;
                grabbable.OnReleaseEvent -= TestOnReleased;
                Log("🔄 Unsubscribed from test events");
            }
        }
    }

    /// <summary>
    /// Check sequence controller configuration
    /// </summary>
    private void CheckSequenceController()
    {
        Log("🔍 Checking sequence controller configuration...");

        var controller = FindObjectOfType<ModularTrainingSequenceController>();
        if (controller == null)
        {
            LogError("❌ No ModularTrainingSequenceController found in scene!");
            return;
        }

        Log($"✅ Found sequence controller: {controller.name}");

        // Check current framework
        var currentFramework = VRFrameworkDetector.DetectCurrentFramework();
        Log($"🔧 Detected Framework: {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}");

        if (currentFramework == VRFramework.AutoHands)
        {
            Log("✅ AutoHands framework detected - AutoHandsGrabStepHandler should be active");
        }
        else
        {
            LogWarning($"⚠️ Framework is {VRFrameworkDetector.GetFrameworkDisplayName(currentFramework)}, not AutoHands");
        }

        // Check if controller has any training sequences loaded
        if (controller.currentProgram != null)
        {
            Log($"📋 Current program: {controller.currentProgram.programName}");
            Log($"📊 Program has {controller.currentProgram.modules.Count} modules");
        }
        else
        {
            Log("ℹ️ No training program currently loaded");
        }
    }

    /// <summary>
    /// Logging helper methods
    /// </summary>
    private void Log(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AutoHandsSequenceTest] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[AutoHandsSequenceTest] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[AutoHandsSequenceTest] {message}");
    }

    /// <summary>
    /// Auto-load test object if not assigned
    /// </summary>
    [ContextMenu("Find Test Grabbable Object")]
    private void FindTestGrabbableObject()
    {
        if (testGrabbableObject == null)
        {
            var grabbable = FindObjectOfType<Autohand.Grabbable>();
            if (grabbable != null)
            {
                testGrabbableObject = grabbable.gameObject;
                Log($"✅ Auto-assigned test object: {testGrabbableObject.name}");
            }
            else
            {
                LogError("❌ No Grabbable objects found in scene");
            }
        }
    }
}
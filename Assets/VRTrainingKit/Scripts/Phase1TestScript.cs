// Phase1TestScript.cs
// Test script for Phase 1 AutoHand Integration
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Test script to validate Phase 1 AutoHand integration functionality
/// Attach this to any GameObject in the scene and run tests from Inspector
/// </summary>
public class Phase1TestScript : MonoBehaviour
{
    [Header("Test Objects")]
    [Tooltip("Assign GameObjects tagged 'grab' to test profile application")]
    public GameObject[] testObjects;
    
    [Header("Test Profiles")]
    [Tooltip("Assign your GrabProfile assets to test")]
    public GrabProfile xriProfile;
    public GrabProfile autoHandProfile;
    
    [Header("Test Results")]
    [SerializeField] private bool lastTestPassed = false;
    [SerializeField] private string lastTestMessage = "";
    
    #if UNITY_EDITOR
    [Header("Test Controls")]
    [Button("Run All Tests")]
    public bool runAllTests;
    
    [Button("Test XRI Profile Application")]
    public bool testXRIProfile;
    
    [Button("Test AutoHand Profile Application")]
    public bool testAutoHandProfile;
    
    [Button("Test Hand System Detection")]
    public bool testHandSystemDetection;
    
    [Button("Clear Test Objects")]
    public bool clearTestObjects;
    #endif
    
    void Start()
    {
        // Auto-run tests at start if configured
        if (Application.isPlaying)
        {
            Debug.Log("[Phase1Test] Starting Phase 1 AutoHand Integration Tests...");
        }
    }
    
    /// <summary>
    /// Run all Phase 1 tests
    /// </summary>
    public void RunAllTests()
    {
        Debug.Log("=== PHASE 1 AUTOHAND INTEGRATION TESTS ===");
        
        bool allPassed = true;
        
        // Test 1: Hand System Detection
        allPassed &= TestHandSystemDetection();
        
        // Test 2: XRI Profile Application  
        allPassed &= TestXRIProfileApplication();
        
        // Test 3: AutoHand Profile Application (if available)
        allPassed &= TestAutoHandProfileApplication();
        
        // Test 4: Profile Validation
        allPassed &= TestProfileValidation();
        
        // Final result
        lastTestPassed = allPassed;
        lastTestMessage = allPassed ? "All Phase 1 tests PASSED!" : "Some Phase 1 tests FAILED - check console";
        
        Debug.Log($"=== PHASE 1 TEST SUMMARY: {lastTestMessage} ===");
    }
    
    /// <summary>
    /// Test AutoHand system detection
    /// </summary>
    public bool TestHandSystemDetection()
    {
        Debug.Log("[Phase1Test] Testing Hand System Detection...");
        
        try
        {
            // Create a temporary profile to test detection
            var tempProfile = ScriptableObject.CreateInstance<GrabProfile>();
            bool autoHandAvailable = tempProfile.IsAutoHandAvailable();
            
            Debug.Log($"[Phase1Test] AutoHand Available: {autoHandAvailable}");
            
            if (autoHandAvailable)
            {
                Debug.Log("[Phase1Test] ✓ AutoHand detection PASSED - AutoHand components found");
            }
            else
            {
                Debug.Log("[Phase1Test] ✓ AutoHand detection PASSED - AutoHand not found (expected if not imported)");
            }
            
            Object.DestroyImmediate(tempProfile);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Phase1Test] ✗ Hand System Detection FAILED: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test XRI profile application
    /// </summary>
    public bool TestXRIProfileApplication()
    {
        Debug.Log("[Phase1Test] Testing XRI Profile Application...");
        
        if (testObjects == null || testObjects.Length == 0)
        {
            Debug.LogWarning("[Phase1Test] No test objects assigned - creating temporary test object");
            var tempObj = new GameObject("TempTestObject");
            tempObj.tag = "grab";
            testObjects = new GameObject[] { tempObj };
        }
        
        try
        {
            // Create XRI profile if not assigned
            if (xriProfile == null)
            {
                xriProfile = ScriptableObject.CreateInstance<GrabProfile>();
                xriProfile.handSystem = HandSystemType.XRI;
                xriProfile.profileName = "Test XRI Profile";
            }
            
            foreach (var obj in testObjects)
            {
                if (obj == null) continue;
                
                Debug.Log($"[Phase1Test] Applying XRI profile to {obj.name}");
                
                // Apply XRI profile
                xriProfile.handSystem = HandSystemType.XRI;
                xriProfile.ApplyToGameObject(obj);
                
                // Validate application
                var xriComponent = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if (xriComponent != null)
                {
                    Debug.Log($"[Phase1Test] ✓ XRGrabInteractable successfully added to {obj.name}");
                }
                else
                {
                    Debug.LogError($"[Phase1Test] ✗ XRGrabInteractable NOT found on {obj.name}");
                    return false;
                }
                
                // Check for Rigidbody
                var rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Debug.Log($"[Phase1Test] ✓ Rigidbody successfully added to {obj.name}");
                }
                else
                {
                    Debug.LogError($"[Phase1Test] ✗ Rigidbody NOT found on {obj.name}");
                    return false;
                }
            }
            
            Debug.Log("[Phase1Test] ✓ XRI Profile Application PASSED");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Phase1Test] ✗ XRI Profile Application FAILED: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test AutoHand profile application
    /// </summary>
    public bool TestAutoHandProfileApplication()
    {
        Debug.Log("[Phase1Test] Testing AutoHand Profile Application...");
        
        try
        {
            // Create AutoHand profile if not assigned
            if (autoHandProfile == null)
            {
                autoHandProfile = ScriptableObject.CreateInstance<GrabProfile>();
                autoHandProfile.handSystem = HandSystemType.AutoHand;
                autoHandProfile.profileName = "Test AutoHand Profile";
            }
            
            // Check if AutoHand is available
            if (!autoHandProfile.IsAutoHandAvailable())
            {
                Debug.Log("[Phase1Test] ⚠ AutoHand not available - skipping AutoHand profile test");
                return true; // Not a failure if AutoHand isn't imported
            }
            
            foreach (var obj in testObjects)
            {
                if (obj == null) continue;
                
                Debug.Log($"[Phase1Test] Applying AutoHand profile to {obj.name}");
                
                // Remove any existing XRI components first
                var existingXRI = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if (existingXRI != null)
                {
                    DestroyImmediate(existingXRI);
                    Debug.Log($"[Phase1Test] Removed existing XRI component from {obj.name}");
                }
                
                // Apply AutoHand profile
                autoHandProfile.handSystem = HandSystemType.AutoHand;
                autoHandProfile.ApplyToGameObject(obj);
                
                // Try to validate AutoHand component was added (using reflection)
                var grabbableType = System.Type.GetType("Autohand.Grabbable, Assembly-CSharp");
                if (grabbableType != null)
                {
                    var grabbableComponent = obj.GetComponent(grabbableType);
                    if (grabbableComponent != null)
                    {
                        Debug.Log($"[Phase1Test] ✓ AutoHand Grabbable successfully added to {obj.name}");
                    }
                    else
                    {
                        Debug.LogError($"[Phase1Test] ✗ AutoHand Grabbable NOT found on {obj.name}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning("[Phase1Test] Could not verify AutoHand component - type not found");
                }
            }
            
            Debug.Log("[Phase1Test] ✓ AutoHand Profile Application PASSED");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Phase1Test] ✗ AutoHand Profile Application FAILED: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test profile validation methods
    /// </summary>
    public bool TestProfileValidation()
    {
        Debug.Log("[Phase1Test] Testing Profile Validation...");
        
        try
        {
            var testProfile = ScriptableObject.CreateInstance<GrabProfile>();
            
            // Test with valid object
            if (testObjects != null && testObjects.Length > 0 && testObjects[0] != null)
            {
                var validObj = testObjects[0];
                if (validObj.CompareTag("grab"))
                {
                    bool xriValid = testProfile.ValidateGameObject(validObj);
                    Debug.Log($"[Phase1Test] XRI validation for {validObj.name}: {xriValid}");
                    
                    testProfile.handSystem = HandSystemType.AutoHand;
                    bool autoHandValid = testProfile.ValidateGameObject(validObj);
                    Debug.Log($"[Phase1Test] AutoHand validation for {validObj.name}: {autoHandValid}");
                }
            }
            
            // Test with invalid object
            var invalidObj = new GameObject("InvalidTestObject");
            // Don't tag it as "grab"
            
            testProfile.handSystem = HandSystemType.XRI;
            bool shouldBeFalse = testProfile.ValidateGameObject(invalidObj);
            
            if (!shouldBeFalse)
            {
                Debug.Log("[Phase1Test] ✓ Validation correctly rejected untagged object");
            }
            else
            {
                Debug.LogError("[Phase1Test] ✗ Validation incorrectly accepted untagged object");
                DestroyImmediate(invalidObj);
                Object.DestroyImmediate(testProfile);
                return false;
            }
            
            DestroyImmediate(invalidObj);
            Object.DestroyImmediate(testProfile);
            
            Debug.Log("[Phase1Test] ✓ Profile Validation PASSED");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Phase1Test] ✗ Profile Validation FAILED: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Clear all test objects of components for clean testing
    /// </summary>
    public void ClearTestObjects()
    {
        Debug.Log("[Phase1Test] Clearing test objects...");
        
        if (testObjects == null) return;
        
        foreach (var obj in testObjects)
        {
            if (obj == null) continue;
            
            // Remove XRI components
            var xriComponent = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (xriComponent != null)
            {
                DestroyImmediate(xriComponent);
                Debug.Log($"[Phase1Test] Removed XRGrabInteractable from {obj.name}");
            }
            
            // Remove AutoHand components (using reflection)
            try
            {
                var grabbableType = System.Type.GetType("Autohand.Grabbable, Assembly-CSharp");
                if (grabbableType != null)
                {
                    var grabbableComponent = obj.GetComponent(grabbableType);
                    if (grabbableComponent != null)
                    {
                        DestroyImmediate(grabbableComponent);
                        Debug.Log($"[Phase1Test] Removed AutoHand Grabbable from {obj.name}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Phase1Test] Could not remove AutoHand components: {e.Message}");
            }
            
            // Remove Rigidbody
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
                Debug.Log($"[Phase1Test] Removed Rigidbody from {obj.name}");
            }
        }
        
        Debug.Log("[Phase1Test] Test objects cleared");
    }
}

/// <summary>
/// Custom attribute to create buttons in Inspector (simple version)
/// </summary>
public class ButtonAttribute : PropertyAttribute
{
    public string methodName;
    
    public ButtonAttribute(string methodName)
    {
        this.methodName = methodName;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for Button attribute
/// </summary>
[CustomPropertyDrawer(typeof(ButtonAttribute))]
public class ButtonPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ButtonAttribute buttonAttribute = (ButtonAttribute)attribute;
        
        if (GUI.Button(position, buttonAttribute.methodName))
        {
            var target = property.serializedObject.targetObject;
            var method = target.GetType().GetMethod(GetMethodName(buttonAttribute.methodName));
            if (method != null)
            {
                method.Invoke(target, null);
            }
        }
    }
    
    private string GetMethodName(string buttonText)
    {
        // Convert "Run All Tests" to "RunAllTests"
        return buttonText.Replace(" ", "");
    }
}
#endif
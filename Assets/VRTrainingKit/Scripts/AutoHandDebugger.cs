// AutoHandDebugger.cs
// Debug script to help identify AutoHand detection issues
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Debug utility to help diagnose AutoHand detection issues
/// Attach to any GameObject and run the debug functions
/// </summary>
public class AutoHandDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool runDebugOnStart = true;
    
    #if UNITY_EDITOR
    [Header("Manual Debug Controls")]
    [AutoHandButton("Debug AutoHand Detection")]
    public bool debugDetection;
    
    [AutoHandButton("List All Assemblies")]  
    public bool listAssemblies;
    
    [AutoHandButton("Search AutoHand Types")]
    public bool searchTypes;
    #endif
    
    void Start()
    {
        if (runDebugOnStart)
        {
            DebugAutoHandDetection();
        }
    }
    
    /// <summary>
    /// Run comprehensive AutoHand detection debug
    /// </summary>
    public void DebugAutoHandDetection()
    {
        Debug.Log("=== AUTOHAND DETECTION DEBUG ===");
        
        // Test the profile detection method
        var testProfile = ScriptableObject.CreateInstance<GrabProfile>();
        bool detected = testProfile.IsAutoHandAvailable();
        
        Debug.Log($"Profile.IsAutoHandAvailable() returned: {detected}");
        
        // List all assemblies
        ListAllAssemblies();
        
        // Search for AutoHand types
        SearchAutoHandTypes();
        
        // Clean up
        DestroyImmediate(testProfile);
        
        Debug.Log("=== END AUTOHAND DEBUG ===");
    }
    
    /// <summary>
    /// List all loaded assemblies
    /// </summary>
    public void ListAllAssemblies()
    {
        Debug.Log("--- LOADED ASSEMBLIES ---");
        
        try
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Debug.Log($"Total assemblies loaded: {assemblies.Length}");
            
            foreach (var assembly in assemblies)
            {
                var name = assembly.GetName().Name;
                Debug.Log($"Assembly: {name}");
                
                // Check if this assembly might contain AutoHand
                if (name.ToLower().Contains("auto") || name.ToLower().Contains("hand"))
                {
                    Debug.Log($"  -> Potential AutoHand assembly: {name}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error listing assemblies: {e.Message}");
        }
    }
    
    /// <summary>
    /// Search for AutoHand-related types in all assemblies
    /// </summary>
    public void SearchAutoHandTypes()
    {
        Debug.Log("--- SEARCHING FOR AUTOHAND TYPES ---");
        
        try
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            bool foundAny = false;
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // Look for AutoHand namespace
                        if (type.Namespace != null && type.Namespace.ToLower().Contains("autohand"))
                        {
                            Debug.Log($"Found AutoHand type: {type.FullName} in assembly: {assembly.GetName().Name}");
                            foundAny = true;
                        }
                        
                        // Look for specific AutoHand classes
                        if (type.Name == "Hand" || type.Name == "Grabbable" || type.Name == "PlacePoint")
                        {
                            Debug.Log($"Found potential AutoHand class: {type.FullName} in assembly: {assembly.GetName().Name}");
                            if (type.Namespace == "Autohand")
                            {
                                Debug.Log($"  -> CONFIRMED AutoHand class: {type.Name}");
                                foundAny = true;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not examine types in assembly {assembly.GetName().Name}: {e.Message}");
                }
            }
            
            if (!foundAny)
            {
                Debug.LogWarning("No AutoHand types found in any assembly!");
                
                // Let's also check for files in the project
                #if UNITY_EDITOR
                CheckAutoHandAssets();
                #endif
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error searching for AutoHand types: {e.Message}");
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Check for AutoHand assets in the project
    /// </summary>
    private void CheckAutoHandAssets()
    {
        Debug.Log("--- CHECKING FOR AUTOHAND ASSETS ---");
        
        // Look for .cs files containing "Hand" or "Grabbable" in AutoHand folder
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new string[] { "Assets/AutoHand" });
        
        Debug.Log($"Found {guids.Length} scripts in AutoHand folder");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            
            if (script != null)
            {
                var scriptClass = script.GetClass();
                if (scriptClass != null)
                {
                    Debug.Log($"Script: {path} -> Class: {scriptClass.FullName}");
                    
                    if (scriptClass.Name == "Hand" || scriptClass.Name == "Grabbable" || scriptClass.Name == "PlacePoint")
                    {
                        Debug.Log($"  -> FOUND KEY AUTOHAND CLASS: {scriptClass.FullName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Script {path} has no associated class (might not be compiled)");
                }
            }
        }
    }
    #endif
}

/// <summary>
/// Simple Button attribute for inspector buttons
/// </summary>
public class AutoHandButtonAttribute : PropertyAttribute
{
    public string methodName;
    
    public AutoHandButtonAttribute(string methodName)
    {
        this.methodName = methodName;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Property drawer for button attribute
/// </summary>
[CustomPropertyDrawer(typeof(AutoHandButtonAttribute))]
public class AutoHandButtonDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        AutoHandButtonAttribute buttonAttribute = (AutoHandButtonAttribute)attribute;
        
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
        return buttonText.Replace(" ", "").Replace("-", "");
    }
}
#endif
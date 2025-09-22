// GameObjectReferenceDrawer.cs
// Custom property drawer for GameObjectReference to show proper GameObject fields in Inspector
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer that makes GameObjectReference appear as a normal GameObject field
/// This solves the "Type mismatch" issue by providing proper Unity Inspector integration
/// </summary>
[CustomPropertyDrawer(typeof(GameObjectReference))]
public class GameObjectReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get the serialized fields from GameObjectReference
        var gameObjectProp = property.FindPropertyRelative("_gameObject");
        var gameObjectNameProp = property.FindPropertyRelative("_gameObjectName");
        var scenePathProp = property.FindPropertyRelative("_scenePath");
        var isValidProp = property.FindPropertyRelative("_isValid");
        
        // Create the GameObject field
        EditorGUI.BeginChangeCheck();
        
        // Get current GameObject reference - try direct reference first
        GameObject currentGameObject = gameObjectProp.objectReferenceValue as GameObject;
        
        // If direct reference is null but we have a name, try to find it and use for display
        if (currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue))
        {
            // Try to find the GameObject by name for display purposes
            currentGameObject = GameObject.Find(gameObjectNameProp.stringValue);
            
            // If found, update the direct reference (this helps with runtime scenarios)
            if (currentGameObject != null && Application.isPlaying)
            {
                gameObjectProp.objectReferenceValue = currentGameObject;
                isValidProp.boolValue = true;
            }
        }
        
        // Calculate space for warning icon if needed
        bool showWarning = currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue);
        float fieldWidth = showWarning ? position.width - 25 : position.width;
        Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
        
        // Draw the GameObject field with proper type filtering
        GameObject newGameObject = (GameObject)EditorGUI.ObjectField(
            fieldRect, 
            label, 
            currentGameObject, 
            typeof(GameObject), 
            true // allowSceneObjects = true
        );
        
        // Update the reference if changed
        if (EditorGUI.EndChangeCheck())
        {
            // Update all the internal fields
            gameObjectProp.objectReferenceValue = newGameObject;
            gameObjectNameProp.stringValue = newGameObject != null ? newGameObject.name : "";
            scenePathProp.stringValue = newGameObject != null && newGameObject.scene.IsValid() ? newGameObject.scene.path : "";
            isValidProp.boolValue = newGameObject != null;
            
            // Force immediate serialization update
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            
            // Force repaint of Inspector
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            
            Debug.Log($"GameObjectReference updated: {(newGameObject != null ? newGameObject.name : "None")}");
        }
        
        // Show validation status if we have a name but no valid direct reference
        if (showWarning)
        {
            // Create a small rect for the warning icon
            Rect warningRect = new Rect(position.x + position.width - 20, position.y, 20, position.height);
            string tooltipText = currentGameObject != null ? 
                $"Reference found by name: {gameObjectNameProp.stringValue}\n(Scene references don't persist in assets)" :
                $"Missing GameObject: {gameObjectNameProp.stringValue}";
            EditorGUI.LabelField(warningRect, new GUIContent("⚠", tooltipText));
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif
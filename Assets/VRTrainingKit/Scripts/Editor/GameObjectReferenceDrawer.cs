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
        
        // Get current GameObject reference
        GameObject currentGameObject = gameObjectProp.objectReferenceValue as GameObject;
        
        // Draw the GameObject field with proper type filtering
        GameObject newGameObject = (GameObject)EditorGUI.ObjectField(
            position, 
            label, 
            currentGameObject, 
            typeof(GameObject), 
            true // allowSceneObjects = true (this is key!)
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
        
        // Show validation status if we have a name but no valid object
        if (currentGameObject == null && !string.IsNullOrEmpty(gameObjectNameProp.stringValue))
        {
            // Create a small rect for the warning icon
            Rect warningRect = new Rect(position.x + position.width - 20, position.y, 20, position.height);
            EditorGUI.LabelField(warningRect, new GUIContent("⚠", $"Missing: {gameObjectNameProp.stringValue}"));
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif
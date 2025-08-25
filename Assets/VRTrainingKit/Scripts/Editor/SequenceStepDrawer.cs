#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer for SequenceStep to properly display in Inspector
/// </summary>
[CustomPropertyDrawer(typeof(SequenceStep))]
public class SequenceStepDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get properties
        var stepNameProp = property.FindPropertyRelative("stepName");
        var instructionProp = property.FindPropertyRelative("instruction");
        var requirementTypeProp = property.FindPropertyRelative("requirementType");
        var requiredObjectProp = property.FindPropertyRelative("requiredObject");
        var secondaryObjectProp = property.FindPropertyRelative("secondaryObject");
        var targetValueProp = property.FindPropertyRelative("targetValue");
        var toleranceProp = property.FindPropertyRelative("tolerance");
        var isCompletedProp = property.FindPropertyRelative("isCompleted");
        
        // Calculate rects
        var yPos = position.y;
        var lineHeight = EditorGUIUtility.singleLineHeight + 2;
        var fieldWidth = position.width;
        
        // Foldout header
        var headerRect = new Rect(position.x, yPos, fieldWidth, lineHeight);
        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, 
            stepNameProp.stringValue ?? "New Step", true);
        yPos += lineHeight;
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            // Step Information
            EditorGUI.LabelField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                "Step Information", EditorStyles.boldLabel);
            yPos += lineHeight;
            
            EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                stepNameProp, new GUIContent("Step Name"));
            yPos += lineHeight;
            
            // Instruction (multiline)
            var instructionHeight = EditorGUI.GetPropertyHeight(instructionProp);
            EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, instructionHeight), 
                instructionProp, new GUIContent("Instruction"));
            yPos += instructionHeight + 5;
            
            // Requirements
            EditorGUI.LabelField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                "Requirements", EditorStyles.boldLabel);
            yPos += lineHeight;
            
            EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                requirementTypeProp, new GUIContent("Requirement Type"));
            yPos += lineHeight;
            
            // Required Object - This should work now
            EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                requiredObjectProp, new GUIContent("Required Object"));
            yPos += lineHeight;
            
            // Show secondary object field based on requirement type
            var reqType = (SequenceStep.RequirementType)requirementTypeProp.enumValueIndex;
            if (reqType == SequenceStep.RequirementType.MustBeSnapped || 
                reqType == SequenceStep.RequirementType.MustBeNearby)
            {
                EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                    secondaryObjectProp, new GUIContent("Secondary Object"));
                yPos += lineHeight;
            }
            
            // Show target value for knob requirements
            if (reqType == SequenceStep.RequirementType.MustBeTurned)
            {
                EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                    targetValueProp, new GUIContent("Target Angle"));
                yPos += lineHeight;
                
                EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                    toleranceProp, new GUIContent("Tolerance (±)"));
                yPos += lineHeight;
            }
            
            // Show tolerance for proximity requirements
            if (reqType == SequenceStep.RequirementType.MustBeNearby)
            {
                EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                    toleranceProp, new GUIContent("Max Distance"));
                yPos += lineHeight;
            }
            
            // Step Status
            EditorGUI.LabelField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                "Step Status", EditorStyles.boldLabel);
            yPos += lineHeight;
            
            // Show completion status (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(new Rect(position.x, yPos, fieldWidth, lineHeight), 
                isCompletedProp, new GUIContent("Is Completed"));
            EditorGUI.EndDisabledGroup();
            yPos += lineHeight;
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight + 2;
        
        var lineHeight = EditorGUIUtility.singleLineHeight + 2;
        var height = lineHeight; // Header
        
        // Step Information section
        height += lineHeight; // Section header
        height += lineHeight; // Step name
        
        // Instruction (multiline)
        var instructionProp = property.FindPropertyRelative("instruction");
        height += EditorGUI.GetPropertyHeight(instructionProp) + 5;
        
        // Requirements section
        height += lineHeight; // Section header
        height += lineHeight; // Requirement type
        height += lineHeight; // Required object
        
        // Additional fields based on requirement type
        var requirementTypeProp = property.FindPropertyRelative("requirementType");
        var reqType = (SequenceStep.RequirementType)requirementTypeProp.enumValueIndex;
        
        if (reqType == SequenceStep.RequirementType.MustBeSnapped || 
            reqType == SequenceStep.RequirementType.MustBeNearby)
        {
            height += lineHeight; // Secondary object
        }
        
        if (reqType == SequenceStep.RequirementType.MustBeTurned)
        {
            height += lineHeight * 2; // Target value + tolerance
        }
        
        if (reqType == SequenceStep.RequirementType.MustBeNearby)
        {
            height += lineHeight; // Distance tolerance
        }
        
        // Step Status section
        height += lineHeight; // Section header
        height += lineHeight; // Is completed
        
        return height;
    }
}

/// <summary>
/// Custom property drawer for ReadOnly attribute
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(position, property, label);
        EditorGUI.EndDisabledGroup();
    }
}
#endif
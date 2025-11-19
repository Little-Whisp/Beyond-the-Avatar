using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TagSelectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif
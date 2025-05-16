using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    [CustomPropertyDrawer(typeof(EntityComponentDefinition), true)]
    public class EntityComponentDefinitionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, true);
        }
    }
}
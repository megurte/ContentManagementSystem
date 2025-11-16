using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    [CustomPropertyDrawer(typeof(CMSEntityPfb), true)]
    public class CMSEntityPfbDrawer : PropertyDrawer
    {
        private static List<CMSEntityPfb> _cachedPrefabs;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            var labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var fieldRect = new Rect(position.x + labelWidth, position.y,
                position.width - labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            var evt = Event.current;

            if (evt.type == EventType.MouseDown && fieldRect.Contains(evt.mousePosition))
            {
                EnsureCache();
                var current = property.objectReferenceValue as CMSEntityPfb;

                CMSSelectorDropdown.Show(fieldRect, _cachedPrefabs, current, selected =>
                {
                    property.objectReferenceValue = selected;
                    property.serializedObject.ApplyModifiedProperties();
                });

                evt.Use();
            }

            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.ObjectField(
                fieldRect,
                GUIContent.none,
                property.objectReferenceValue,
                typeof(CMSEntityPfb),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = newObj;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static void EnsureCache()
        {
            if (_cachedPrefabs != null) return;

            var guids = AssetDatabase.FindAssets("t:GameObject", new[] {"Assets/Resources/CMS"});
            _cachedPrefabs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(go => go != null)
                .Select(go => go.GetComponent<CMSEntityPfb>())
                .Where(p => p != null)
                .OrderBy(p => p.name)
                .ToList();
        }
    }
}
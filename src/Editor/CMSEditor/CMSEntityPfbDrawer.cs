using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Runtime;
using src.Editor.CMSEditor;
using src.Editor.CMSEditor.Utils;
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
            var fullFieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);
            
            EditorGUI.LabelField(labelRect, label);

            var current = property.objectReferenceValue as CMSEntityPfb;
            var hasValue = current != null;
            var btnSize = position.height;
            var btnRect = hasValue
                ? new Rect(fullFieldRect.xMax - btnSize, fullFieldRect.y, btnSize, btnSize)
                : Rect.zero;
            var objectRect = hasValue
                ? new Rect(fullFieldRect.x, fullFieldRect.y, fullFieldRect.width - btnSize - 2f, fullFieldRect.height)
                : fullFieldRect;
            
            var evt = Event.current;

            if (hasValue)
            {
                EditorCustomTools.DrawOpenPrefabButton(btnRect, current);

                if (evt.type == EventType.MouseDown && btnRect.Contains(evt.mousePosition))
                {
                    CMSEntityInspectorWindow.ShowWindow(
                        current,
                        btnRect,
                        explorer: null,
                        selectedId: -1);

                    evt.Use();
                    EditorGUI.EndProperty();
                    EditorGUI.indentLevel = indent;
                    return;
                }
            }

            if (evt.type == EventType.MouseDown && objectRect.Contains(evt.mousePosition))
            {
                EnsureCache();

                var filterCondition = fieldInfo?.GetCustomAttribute<FilterTagsAttribute>(true);
                var hasFilter = filterCondition?.TagTypes == null || filterCondition.TagTypes.Length == 0;
                var filteredList = hasFilter ? _cachedPrefabs : CMSHelpers.FilterByTags(_cachedPrefabs, filterCondition.TagTypes);
                
                CMSSelectorDropdown.Show(objectRect, filteredList, current, selected =>
                {
                    property.objectReferenceValue = selected;
                    property.serializedObject.ApplyModifiedProperties();
                });

                evt.Use();
            }

            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.ObjectField(
                objectRect,
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
            => EditorGUIUtility.singleLineHeight;

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
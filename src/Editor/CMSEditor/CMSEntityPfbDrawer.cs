using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    [CustomPropertyDrawer(typeof(CMSEntityPfb), true)]
    public class CMSEntityPfbDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var objRef = property.objectReferenceValue as CMSEntityPfb;
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - 22f, position.height);
            var buttonRect = new Rect(position.x + position.width - 20f, position.y, 20f, position.height);

            EditorGUI.LabelField(labelRect, label);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(fieldRect, GUIContent.none, objRef, typeof(CMSEntityPfb), false);
            EditorGUI.EndDisabledGroup();

            if (GUI.Button(buttonRect, "\ud83d\uddc2\ufe0f"))
            {
                var prefabs = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Resources/CMS" });
                var allPrefabs = prefabs
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                    .Where(go => go != null)
                    .Select(go => go.GetComponent<CMSEntityPfb>())
                    .Where(p => p != null)
                    .ToList();

                CMSSelectorPopup.Show(allPrefabs, objRef, selected =>
                {
                    property.objectReferenceValue = selected;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            EditorGUI.EndProperty();
        }
    }
}
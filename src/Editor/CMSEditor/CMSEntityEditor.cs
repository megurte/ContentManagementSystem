using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    [CustomEditor(typeof(CMSEntity))]
    public class CMSEntityEditor : UnityEditor.Editor
    {
        private SerializedProperty _idProperty;
        private SerializedProperty _componentsProperty;

        private void OnEnable()
        {
            _idProperty = serializedObject.FindProperty("id");
            _componentsProperty = serializedObject.FindProperty("components");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Card State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_idProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < _componentsProperty.arraySize; i++)
                {
                    var element = _componentsProperty.GetArrayElementAtIndex(i);
                    var typeName = element.managedReferenceFullTypename?.Split(' ').Last() ?? "Unknown";

                    EditorGUILayout.BeginHorizontal();

                    element.isExpanded = EditorGUILayout.Foldout(
                        element.isExpanded,
                        $"[{i}] {typeName}",
                        true,
                        EditorStyles.foldout
                    );

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        _componentsProperty.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (element.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(element, GUIContent.none, true);
                        EditorGUI.indentLevel--;
                    }
                }

                if (GUILayout.Button("+ Add Component"))
                {
                    ShowAddComponentMenu();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowAddComponentMenu()
        {
            GenericMenu menu = new GenericMenu();

            Type baseType = typeof(EntityComponentDefinition);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => baseType.IsAssignableFrom(p) && p != baseType && !p.IsAbstract);

            foreach (Type type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    var newComponent = Activator.CreateInstance(type) as EntityComponentDefinition;

                    _componentsProperty.serializedObject.Update();
                    _componentsProperty.arraySize++;
                    _componentsProperty.GetArrayElementAtIndex(_componentsProperty.arraySize - 1).managedReferenceValue =
                        newComponent;
                    _componentsProperty.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}
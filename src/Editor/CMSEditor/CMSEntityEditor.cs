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
        private SerializedProperty idProperty;
        private SerializedProperty componentsProperty;

        private void OnEnable()
        {
            idProperty = serializedObject.FindProperty("id");
            componentsProperty = serializedObject.FindProperty("components");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Card State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(idProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < componentsProperty.arraySize; i++)
                {
                    var element = componentsProperty.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($"[{i}] {element.managedReferenceFullTypename.Split(' ')[1]}",
                        EditorStyles.boldLabel);

                    // Кнопка удаления компонента
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        componentsProperty.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    EditorGUI.indentLevel--;
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

                    componentsProperty.serializedObject.Update();
                    componentsProperty.arraySize++;
                    componentsProperty.GetArrayElementAtIndex(componentsProperty.arraySize - 1).managedReferenceValue =
                        newComponent;
                    componentsProperty.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}
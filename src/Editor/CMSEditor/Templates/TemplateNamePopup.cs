using System;
using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor.Templates
{
    public class TemplateNamePopup : EditorWindow
    {
        private string _templateName = "NewTemplate";
        private Action<string> _onConfirm;

        public static void Show(Action<string> onConfirm)
        {
            var window = CreateInstance<TemplateNamePopup>();
            window.titleContent = new GUIContent("Save Template");
            window._onConfirm = onConfirm;
            window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 300, 80);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Label("Template Name:", EditorStyles.boldLabel);
            _templateName = EditorGUILayout.TextField(_templateName);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                _onConfirm?.Invoke(_templateName);
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}
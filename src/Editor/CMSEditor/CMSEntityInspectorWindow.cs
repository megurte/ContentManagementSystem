using Editor.CMSEditor;
using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor
{
    public class CMSEntityInspectorWindow : EditorWindow
    {
        private Object _target;
        private CMSEntityExplorer _explorer;
        private int _selectedId;
        private Vector2 _scrollPosition;
        
        public static void ShowWindow(Object target, Rect anchorRect, CMSEntityExplorer explorer, int selectedId)
        {
            var window = CreateInstance<CMSEntityInspectorWindow>();
            window._target = target;
            window._explorer = explorer;
            window._selectedId = selectedId;
            window.titleContent = new GUIContent(target.name);
            window.position = new Rect(anchorRect.xMin - 400 - 10, anchorRect.yMin, 600, anchorRect.height);
            window.ShowUtility();
            window.Focus();
        }

        private void OnGUI()
        {
            if (_target == null)
            {
                EditorGUILayout.HelpBox("No target to inspect.", MessageType.Warning);
                return;
            }

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
    
            EditorGUI.indentLevel = 0;
            var editor = UnityEditor.Editor.CreateEditor(_target);
            editor.OnInspectorGUI();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void OnDestroy()
        {
            if (_explorer != null && _selectedId != -1)
            {
                _explorer.FocusTreeViewAndReselect(_selectedId);
            }
        }
    }
}
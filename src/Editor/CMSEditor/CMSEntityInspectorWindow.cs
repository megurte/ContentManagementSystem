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
        private UnityEditor.Editor _cachedEditor;

        public static void ShowWindow(Object target, Rect anchorRect, CMSEntityExplorer explorer, int selectedId)
        {
            var window = CreateInstance<CMSEntityInspectorWindow>();
            window._target = target;
            window._explorer = explorer;
            window._selectedId = selectedId;
            window.titleContent = new GUIContent(target.name);
            window.position = CenteredRect();
            window.ShowUtility();
            window.Focus();
        }

        private static Rect CenteredRect()
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var size = new Vector2(620f, Mathf.Min(760f, main.height * 0.85f));
            return new Rect(main.center.x - size.x * 0.5f, main.center.y - size.y * 0.5f, size.x, size.y);
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
            if (_cachedEditor == null || _cachedEditor.target != _target)
            {
                UnityEditor.Editor.CreateCachedEditor(_target, null, ref _cachedEditor);
            }
            _cachedEditor.OnInspectorGUI();

            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }

            if (_target != null)
            {
                AssetDatabase.SaveAssetIfDirty(_target);
            }

            if (_explorer != null && _selectedId != -1)
            {
                _explorer.FocusTreeViewAndReselect(_selectedId);
            }
        }
    }
}
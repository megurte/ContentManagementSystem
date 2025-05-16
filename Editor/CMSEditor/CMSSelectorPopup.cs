using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    public class CMSSelectorPopup : EditorWindow
    {
        private static CMSSelectorPopup _currentWindow;
        
        private List<CMSEntityPfb> _prefabs;
        private string _searchQuery = "";
        private Action<CMSEntityPfb> _onSelected;
        private Vector2 _scrollPos;
        private CMSEntityPfb _currentSelected;

        public static void Show(List<CMSEntityPfb> prefabs, CMSEntityPfb currentSelection, Action<CMSEntityPfb> onSelected)
        {
            if (_currentWindow != null)
                _currentWindow.Close();
            
            var window = CreateInstance<CMSSelectorPopup>();
            window._prefabs = prefabs;
            window._currentSelected = currentSelection;
            window._onSelected = onSelected;
            Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.position = new Rect(mousePos.x - 100, mousePos.y, 400, 400);
            window.titleContent = new GUIContent("Select CMS Prefab");
            window.ShowPopup();
            
            _currentWindow = window;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
    
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("SearchField");
            _searchQuery = EditorGUILayout.TextField(_searchQuery, GUI.skin.FindStyle("ToolbarSearchTextField"));
            if (GUILayout.Button("X", GUILayout.Width(20)))
                Close();
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            var filtered = string.IsNullOrEmpty(_searchQuery)
                ? _prefabs
                : _prefabs.Where(p => p.name.ToLower().Contains(_searchQuery.ToLower())).ToList();

            foreach (var prefab in filtered)
                DrawSelectableRow(prefab);

            EditorGUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawSelectableRow(CMSEntityPfb prefab)
        {
            var rect = GUILayoutUtility.GetRect(new GUIContent(prefab.name), EditorStyles.label, GUILayout.ExpandWidth(true));

            bool isHovered = rect.Contains(Event.current.mousePosition);

            if (_currentSelected == prefab)
            {
                EditorGUI.DrawRect(rect, new Color(0.25f, 0.5f, 1f, 0.5f));
            }
            else if (isHovered)
            {
                EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));
            }

            var labelRect = new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height);
            EditorGUI.LabelField(labelRect, prefab.name);

            // Handle mouse click
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _onSelected?.Invoke(prefab);
                Close();
                Event.current.Use();
            }
        }
        
        private void OnDestroy()
        {
            if (_currentWindow == this)
            {
                _currentWindow = null;
            }
        }
    }
}
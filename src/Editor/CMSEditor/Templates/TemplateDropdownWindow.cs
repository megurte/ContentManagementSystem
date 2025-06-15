using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.CMSEditor;
using src.Editor.CMSEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor.Templates
{
    public class TemplateDropdownWindow : EditorWindow
    {
        private string _templatesFolder => CMSEntityExplorer.TemplatesFolder;
        private Action<string> _onTemplateSelected;

        private List<string> _templateNames;
        private Vector2 _scroll;
        private static Rect _alignTo;
        
        private class TemplateEntry
        {
            public string name;
            public Texture2D icon;
        }
        
        private List<TemplateEntry> _entries;
        private string _hoveredItem;

        public static void Show(Rect alignTo, Action<string> onTemplateSelected)
        {
            var window = CreateInstance<TemplateDropdownWindow>();
            window._onTemplateSelected = onTemplateSelected;
            window.LoadTemplates();
            ShowWithSize(alignTo, window);
        }

        private static void ShowWithSize(Rect alignTo, TemplateDropdownWindow window)
        {
            _alignTo = alignTo;
            var height = CalculateWindowSize(window);
            window.ShowAsDropDown(alignTo, new Vector2(250, height));
        }

        private static float CalculateWindowSize(TemplateDropdownWindow window)
        {
            const float rowHeight = 25f;
            const int maxVisibleRows = 10;
            var count = window._entries?.Count ?? 0;
            var visibleRows = Mathf.Min(count, maxVisibleRows);
            var height = visibleRows * rowHeight;
            return height;
        }

        private void LoadTemplates()
        {
            if (!Directory.Exists(_templatesFolder))
                Directory.CreateDirectory(_templatesFolder);

            _entries = new List<TemplateEntry>();

            foreach (var path in Directory.GetFiles(_templatesFolder, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                _entries.Add(new TemplateEntry { name = fileName});
            }
        }
        
        private void OnGUI()
        {
            this.DrawWindowBorder();

            if (_entries == null) return;
            
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var entry in _entries.ToList())
            {
                var rect = EditorGUILayout.BeginHorizontal();
                HandleHover(rect, entry);

                DrawIconPrefab();
                DrawTemplateName(entry);
                var rowRect = GUILayoutUtility.GetLastRect();
                var deletePressed = DrawDeleteButton(entry);
                
                EditorGUILayout.EndHorizontal();

                this.DrawLineBetween();
                
                HandleMouseClick(deletePressed, rowRect, entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void HandleMouseClick(bool deletePressed, Rect rowRect, TemplateEntry entry)
        {
            if (!deletePressed &&
                Event.current.type == EventType.MouseDown &&
                rowRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                _onTemplateSelected?.Invoke(entry.name);
                GUIUtility.ExitGUI();
            }
        }

        private void HandleHover(Rect rect, TemplateEntry entry)
        {
            var isHover = rect.Contains(Event.current.mousePosition);
            if (isHover) _hoveredItem = entry.name;

            if (isHover)
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.4f, 0.6f, 0.3f));
        }

        private static void DrawIconPrefab()
        {
            var prefabIcon = EditorGUIUtility.IconContent("Prefab Icon").image;
            GUILayout.Label(prefabIcon, GUILayout.Width(20), GUILayout.Height(20));
        }

        private static void DrawTemplateName(TemplateEntry entry)
        {
            GUILayout.Space(4);
            GUILayout.Label(entry.name, GlobalStyles.TemplateStyle, GUILayout.ExpandWidth(true));
        }

        private bool DrawDeleteButton(TemplateEntry entry)
        {
            if (GUILayout.Button("×", GlobalStyles.ClearButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Delete Template", $"Delete template '{entry.name}'?", "Yes", "Cancel"))
                {
                    var path = Path.Combine(_templatesFolder, $"{entry.name}.json");
                    File.Delete(path);
                    AssetDatabase.Refresh();
                    _entries.Remove(entry);
                    Repaint();
                    return true;
                }
            }

            return false;
        }
    }
}
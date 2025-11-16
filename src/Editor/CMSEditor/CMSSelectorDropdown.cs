using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    public class CMSSelectorDropdown : PopupWindowContent
    {
        private readonly List<CMSEntityPfb> _allPrefabs;
        private readonly Action<CMSEntityPfb> _onSelected;
        private CMSEntityPfb _current;
        private string _search = "";
        private Vector2 _scroll;
        private int _keyboardIndex = -1;

        private const float RowHeight = 20f;
        private const float IconSize = 18f;
        private const float Padding = 4f;
        private const float ToolbarHeight = 18f;

        public static void Show(Rect activatorRect, List<CMSEntityPfb> prefabs, CMSEntityPfb current,
            Action<CMSEntityPfb> onSelected)
        {
            var content = new CMSSelectorDropdown(prefabs, current, onSelected);
            PopupWindow.Show(activatorRect, content);
        }

        private CMSSelectorDropdown(List<CMSEntityPfb> prefabs, CMSEntityPfb current, Action<CMSEntityPfb> onSelected)
        {
            _allPrefabs = prefabs;
            _current = current;
            _onSelected = onSelected;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 300);
        }

        public override void OnGUI(Rect rect)
        {
            var e = Event.current;
            var filtered = GetFiltered(_search);
            var totalRows = filtered.Count + 1;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    e.Use();
                    return;
                }

                if (e.keyCode is KeyCode.UpArrow or KeyCode.DownArrow or KeyCode.Return or KeyCode.KeypadEnter)
                {
                    HandleKeyboard(e, filtered, totalRows);
                }
            }

            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.18f, 0.18f, 0.18f));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("CMS_SearchField");
            _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSearchTextField"));
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                editorWindow.Close();
            }

            GUILayout.EndHorizontal();

            filtered = GetFiltered(_search);
            totalRows = filtered.Count + 1;

            _scroll = GUILayout.BeginScrollView(_scroll);

            int rowIndex = 0;

            // None item
            DrawRowNone(rowIndex);
            rowIndex++;

            // All CMSEntityPfb
            for (int i = 0; i < filtered.Count; i++, rowIndex++)
            {
                var prefab = filtered[i];
                if (prefab == null) continue;
                DrawRowPrefab(prefab, rowIndex);
            }

            GUILayout.EndScrollView();
        }

        private List<CMSEntityPfb> GetFiltered(string search)
        {
            if (string.IsNullOrEmpty(search))
                return _allPrefabs;

            return _allPrefabs
                .Where(p => p != null &&
                            p.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void HandleKeyboard(Event e, List<CMSEntityPfb> filtered, int totalRows)
        {
            if (filtered == null) return;
            if (totalRows <= 0) return;

            switch (e.keyCode)
            {
                case KeyCode.DownArrow:
                    if (_keyboardIndex < 0)
                    {
                        _keyboardIndex = filtered.Count > 0 ? 1 : 0;
                    }
                    else
                    {
                        _keyboardIndex = Mathf.Min(_keyboardIndex + 1, totalRows - 1);
                    }

                    UpdateCurrentByKeyboard(filtered);
                    ScrollToIndex(_keyboardIndex, totalRows);
                    e.Use();
                    break;

                case KeyCode.UpArrow:
                    if (_keyboardIndex < 0)
                    {
                        _keyboardIndex = 0;
                    }
                    else
                    {
                        _keyboardIndex = Mathf.Max(_keyboardIndex - 1, 0);
                    }

                    UpdateCurrentByKeyboard(filtered);
                    ScrollToIndex(_keyboardIndex, totalRows);
                    e.Use();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_keyboardIndex < 0)
                    {
                        e.Use();
                        return;
                    }

                    CMSEntityPfb selected;
                    if (_keyboardIndex == 0)
                        selected = null; // None
                    else
                        selected = filtered[_keyboardIndex - 1];

                    _current = selected;
                    _onSelected?.Invoke(selected);
                    editorWindow.Close();
                    e.Use();
                    break;
            }
        }

        private void UpdateCurrentByKeyboard(List<CMSEntityPfb> filtered)
        {
            if (_keyboardIndex == 0)
            {
                _current = null; // None
            }
            else
            {
                int prefabIndex = _keyboardIndex - 1;
                if (prefabIndex >= 0 && prefabIndex < filtered.Count)
                    _current = filtered[prefabIndex];
            }
        }

        private void DrawRowNone(int rowIndex)
        {
            var rowRect = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
            var icon = EditorGUIUtility.IconContent("d_GameObject Icon").image;
            var isHovered = rowRect.Contains(Event.current.mousePosition);
            var isSelectedByKeyboard = _keyboardIndex == rowIndex;
            var isSelectedByValue = _keyboardIndex < 0 && _current == null;
            ColorizeRow(isSelectedByKeyboard, isSelectedByValue, rowRect, isHovered);

            var iconRect = new Rect(
                rowRect.x + Padding,
                rowRect.y + (RowHeight - IconSize) * 0.5f,
                IconSize,
                IconSize
            );

            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            var labelRect = new Rect(iconRect.xMax + Padding, rowRect.y, rowRect.width - iconRect.width - Padding * 2,
                RowHeight);
            EditorGUI.LabelField(labelRect, "None");
            HandleClick(rowIndex, rowRect, null);
        }

        private void ScrollToIndex(int index, int count)
        {
            var viewHeight = GetWindowSize().y - ToolbarHeight;
            var rowTop = index * RowHeight;
            var rowBottom = rowTop + RowHeight;

            if (rowTop < _scroll.y)
            {
                _scroll.y = rowTop;
            }
            else if (rowBottom > _scroll.y + viewHeight)
            {
                _scroll.y = rowBottom - viewHeight;
            }
        }

        private void DrawRowPrefab(CMSEntityPfb prefab, int rowIndex)
        {
            var rowRect = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
            var isHovered = rowRect.Contains(Event.current.mousePosition);
            var isSelectedByKeyboard = _keyboardIndex == rowIndex;
            var isSelectedByValue = _keyboardIndex < 0 && prefab == _current;

            ColorizeRow(isSelectedByKeyboard, isSelectedByValue, rowRect, isHovered);

            var iconRect = new Rect(
                rowRect.x + Padding,
                rowRect.y + (RowHeight - IconSize) * 0.5f,
                IconSize,
                IconSize);
            var icon = GetPrefabIcon(prefab);
            if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            var labelRect = new Rect(iconRect.xMax + Padding, rowRect.y, rowRect.width - iconRect.width - Padding * 2,
                RowHeight);

            EditorGUI.LabelField(labelRect, prefab.name);
            HandleClick(rowIndex, rowRect, prefab);
        }

        private void HandleClick(int rowIndex, Rect rowRect, CMSEntityPfb prefab)
        {
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                _current = prefab;
                _keyboardIndex = rowIndex;
                _onSelected?.Invoke(prefab);
                editorWindow.Close();
                Event.current.Use();
            }
        }

        private static void ColorizeRow(bool isSelectedByKeyboard, bool isSelectedByValue, Rect rowRect, bool isHovered)
        {
            if (isSelectedByKeyboard || isSelectedByValue)
                EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.5f, 1f, 0.35f));
            else if (isHovered)
                EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.05f));
        }

        private static Texture GetPrefabIcon(CMSEntityPfb prefab)
        {
            var go = prefab.gameObject;
            return AssetPreview.GetMiniThumbnail(go) ?? EditorGUIUtility.ObjectContent(go, typeof(GameObject)).image;
        }
    }
}
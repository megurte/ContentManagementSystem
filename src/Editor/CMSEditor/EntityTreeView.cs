using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.CMSEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace src.Editor.CMSEditor
{
    public class EntityTreeViewItem : TreeViewItem
    {
        public GameObject prefab;
        public CMSEntityPfb entity;
        public Sprite sprite;
        public string path;
        public List<string> componentTypeNames;
    }
    
    public class EntityTreeViewFolder : TreeViewItem
    {
        public string path;
    }
    
    public class EntityTreeView : TreeView
    {
        public bool IsRenaming => _renameId > 0;
        
        private ViewModeExplorer _viewMode;
        private List<SearchResult> _searchResults = new();
        private const float RowHeight = 32; // Increased height to accommodate sprite
        private int _renameId  = -1;

        public Action focusSearchFieldRequest;

        public EntityTreeView(TreeViewState state) : base(state)
        {
            rowHeight = RowHeight;
            Reload();
        }

        public void SetSearchResults(List<SearchResult> results, ViewModeExplorer mode)
        {
            _searchResults = results;
            _viewMode = mode;
            Reload();
        }

        public EntityTreeViewItem GetEntityItemById(int id)
        {
            return FindItem(id, rootItem) as EntityTreeViewItem;
        }
        
        public TreeViewItem GetItemById(int id)
        {
            return FindItem(id, rootItem);
        }
        
        public CMSEntityPfb GetSelectedEntity()
        {
            var id = GetSelection().FirstOrDefault();
            return GetEntityItemById(id)?.entity;
        }

        protected override void KeyEvent()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                HandleOpenEntityKey();

                HandleRenameEntityKey();

                HandleDeleteKey();

                HandleDuplicateKey();

                HandleRestoreKey();

                // Arrow Up to move to search bar
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    var selected = GetSelection();
                    if (selected.Count == 1 && selected[0] == GetRows().FirstOrDefault()?.id)
                    {
                        focusSearchFieldRequest?.Invoke();
                        Event.current.Use();
                    }
                }
                else
                {
                    base.KeyEvent();
                }
            }
        }

        private void HandleRenameEntityKey()
        {
            if (Event.current.keyCode == KeyCode.F2)
            {
                BeginRenameSelectedItem();
                Event.current.Use();
            }
        }

        private void HandleOpenEntityKey()
        {
            // Enter to open current entity
            if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
            {
                var selected = GetSelection();
                if (selected.Count == 1)
                {
                    if (FindItem(selected[0], rootItem) is EntityTreeViewItem item)
                    {
                        OpenEntity(item);
                        Event.current.Use();
                    }
                }
            }
        }

        private void HandleDeleteKey()
        {
            if (IsRenaming) return;

            if (Event.current.keyCode == KeyCode.Delete)
            {
                GetExplorerWindow().DeleteSelectedEntities();
                Event.current.Use();
            }
        }

        private void HandleDuplicateKey()
        {
            if (IsRenaming) return;

            if (Event.current.keyCode == KeyCode.D && (Event.current.control || Event.current.command))
            {
                GetExplorerWindow().DuplicateSelectedEntity();
                Event.current.Use();
            }
        }

        private void HandleRestoreKey()
        {
            if (IsRenaming) return;

            if (Event.current.keyCode == KeyCode.Z && (Event.current.control || Event.current.command))
            {
                var explorerWindow = GetExplorerWindow();
                if (!explorerWindow.HasDeletedEntitiesToRestore) return;

                explorerWindow.RestoreLastDeleted();
                Event.current.Use();
            }
        }

        private static void OpenEntity(EntityTreeViewItem item)
        {
            var explorerWindow = GetExplorerWindow();
            var windowRect = explorerWindow.position;

            CMSEntityInspectorWindow.ShowWindow(item.entity, windowRect, explorerWindow, item.id);
        }

        private static CMSEntityExplorer GetExplorerWindow()
        {
            return EditorWindow.GetWindow<CMSEntityExplorer>();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};

            if (_viewMode == ViewModeExplorer.SearchView)
            {
                var id = 1;
                root.children = _searchResults
                    .Select(result => new EntityTreeViewItem
                    {
                        id = id++,
                        depth = 0,
                        displayName = result.displayName,
                        prefab = result.prefab,
                        entity = result.entity,
                        sprite = result.sprite,
                        componentTypeNames = result.componentTypeNames
                    })
                    .Cast<TreeViewItem>()
                    .ToList();

                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            var pathToItem = new Dictionary<string, TreeViewItem>();
            pathToItem[""] = root;
            var idCounter = 1;

            var commonFolderPrefix = ComputeCommonFolderPrefix(_searchResults);
            var prefixToStrip = string.IsNullOrEmpty(commonFolderPrefix) ? "" : commonFolderPrefix + "/";

            foreach (var result in _searchResults)
            {
                var assetPath = AssetDatabase.GetAssetPath(result.prefab);
                var relativePath = assetPath.Replace(prefixToStrip, "").Replace(".prefab", "");
                var parts = relativePath.Split('/');

                var currentPath = "";
                var parent = root;

                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                    if (!pathToItem.TryGetValue(currentPath, out var item))
                    {
                        var isLeaf = i == parts.Length - 1;

                        item = isLeaf
                            ? new EntityTreeViewItem
                            {
                                id = idCounter++,
                                depth = i,
                                displayName = result.displayName,
                                prefab = result.prefab,
                                entity = result.entity,
                                sprite = result.sprite,
                                path = assetPath,
                                componentTypeNames = result.componentTypeNames
                            }
                            : new EntityTreeViewFolder
                            {
                                id = idCounter++,
                                depth = i,
                                displayName = part,
                                path = $"{Path.GetDirectoryName(assetPath)}"
                            };

                        pathToItem[currentPath] = item;

                        parent.children ??= new List<TreeViewItem>();
                        parent.children.Add(item);
                    }

                    parent = item;
                }
            }

            root.children ??= new List<TreeViewItem>();

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private static string ComputeCommonFolderPrefix(List<SearchResult> results)
        {
            var commonSegments = (string[]) null;

            foreach (var result in results)
            {
                var assetPath = AssetDatabase.GetAssetPath(result.prefab);
                var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "";
                var segments = directory.Split('/');

                commonSegments = commonSegments == null ? segments : IntersectSegments(commonSegments, segments);

                if (commonSegments.Length == 0)
                    return "";
            }

            return commonSegments != null ? string.Join("/", commonSegments) : "";
        }

        private static string[] IntersectSegments(string[] a, string[] b)
        {
            var matchLength = 0;
            var maxLength = Math.Min(a.Length, b.Length);

            while (matchLength < maxLength && a[matchLength] == b[matchLength])
                matchLength++;

            return a.Take(matchLength).ToArray();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var indent = GetContentIndent(args.item);
            var rowRect = args.rowRect;
            var iconPadding = 4f;
            var iconSize = rowHeight - 4f;
            var iconOffset = indent;
            
            HandleCancelRenameKey();
            
            if (args.item is EntityTreeViewItem entityItem)
            {
                var iconRect = new Rect(rowRect.x + iconOffset, rowRect.y + 2f, iconSize, iconSize);

                if (entityItem.sprite != null)
                {
                    DrawSpriteIcon(iconRect, entityItem.sprite);
                }
                else
                {
                    DrawFallbackIcon(iconRect, entityItem.displayName);
                }

                var labelRect = new Rect(iconRect.xMax + iconPadding, rowRect.y, rowRect.width, rowHeight);
                DrawEntityLabels(labelRect, args.label, entityItem.componentTypeNames);
            }
            else
            {
                var folderIcon = EditorGUIUtility.IconContent("Folder Icon").image;
                var iconRect = new Rect(rowRect.x + indent, rowRect.y + (rowHeight - iconSize) / 2, iconSize, iconSize);
                GUI.DrawTexture(iconRect, folderIcon, ScaleMode.ScaleToFit);

                var labelRect = new Rect(iconRect.xMax + iconPadding, rowRect.y, rowRect.width, rowHeight);
                EditorGUI.LabelField(labelRect, args.label);
            }
        }

        private static void DrawSpriteIcon(Rect iconRect, Sprite sprite)
        {
            var aspect = sprite.textureRect.width / sprite.textureRect.height;
            var drawRect = iconRect;

            if (aspect > 1f)
            {
                drawRect.height = iconRect.width / aspect;
                drawRect.y += (iconRect.height - drawRect.height) * 0.5f;
            }
            else if (aspect < 1f)
            {
                drawRect.width = iconRect.height * aspect;
                drawRect.x += (iconRect.width - drawRect.width) * 0.5f;
            }

            GUI.DrawTextureWithTexCoords(
                drawRect,
                sprite.texture,
                new Rect(
                    sprite.textureRect.x / sprite.texture.width,
                    sprite.textureRect.y / sprite.texture.height,
                    sprite.textureRect.width / sprite.texture.width,
                    sprite.textureRect.height / sprite.texture.height
                )
            );
        }

        private static GUIStyle _fallbackIconStyle;
        private static GUIStyle _rowNameStyle;
        private static GUIStyle _rowSubtitleStyle;

        private static void DrawFallbackIcon(Rect iconRect, string displayName)
        {
            _fallbackIconStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = {textColor = Color.white}
            };

            var hash = string.IsNullOrEmpty(displayName) ? 0 : displayName.GetHashCode();
            var hue = Mathf.Abs(hash % 360) / 360f;
            var color = Color.HSVToRGB(hue, 0.35f, 0.55f);

            EditorGUI.DrawRect(iconRect, color);

            var letter = string.IsNullOrEmpty(displayName) ? "?" : displayName.Substring(0, 1).ToUpper();
            GUI.Label(iconRect, letter, _fallbackIconStyle);
        }

        private static void DrawEntityLabels(Rect labelRect, string label, List<string> componentTypeNames)
        {
            _rowNameStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            var nameRect = new Rect(labelRect.x, labelRect.y + 2f, labelRect.width, 16f);
            EditorGUI.LabelField(nameRect, label, _rowNameStyle);

            if (componentTypeNames == null || componentTypeNames.Count == 0)
                return;

            if (_rowSubtitleStyle == null)
            {
                var subtitleColor = EditorStyles.label.normal.textColor;
                subtitleColor.a = 0.5f;
                _rowSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 9,
                    normal = {textColor = subtitleColor}
                };
            }

            var subtitleRect = new Rect(labelRect.x, nameRect.yMax, labelRect.width, 14f);
            EditorGUI.LabelField(subtitleRect, string.Join(", ", componentTypeNames), _rowSubtitleStyle);
        }

        protected override void SingleClickedItem(int id)
        {
            var clickedItem = FindItem(id, rootItem);

            if (clickedItem is EntityTreeViewItem entityItem)
            {
                EditorGUIUtility.PingObject(entityItem.prefab);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var clickedItem = FindItem(id, rootItem);

            if (clickedItem is EntityTreeViewItem entityItem)
            {
                Selection.activeObject = entityItem.prefab;
                EditorUtility.OpenPropertyEditor(entityItem.entity);
            }
        }
        
        private void HandleCancelRenameKey()
        {
            var evt = Event.current;

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                _renameId = -1;
                evt.Use();
            }
        }
                
        private bool IsRenamingItem(int itemId)
        {
            return _renameId == itemId;
        }
        
        protected override bool CanRename(TreeViewItem item)
        {
            return item is EntityTreeViewItem;
        }
        
        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
                return;

            var item = FindItem(args.itemID, rootItem) as EntityTreeViewItem;
            if (item == null) return;

            var oldPath = AssetDatabase.GetAssetPath(item.prefab);
            var newName = args.newName;
            var newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(oldPath), newName + ".prefab");

            var result = AssetDatabase.RenameAsset(oldPath, newName);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError($"Rename failed: {result}");
                return;
            }

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
            var entity = go.GetComponent<CMSEntityPfb>();
            go.name = newName;
            CMSEntityIdSetter.UpdateEntityId(entity, newPath);
            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();

            item.displayName = newName;
            _renameId = -1;
            Reload(); 
        }
        
        private void BeginRenameSelectedItem()
        {
            var selected = GetSelection();
            if (selected.Count != 1)
                return;

            var item = FindItem(selected[0], rootItem);
            if (item != null && CanRename(item))
            {
                _renameId = item.id;
                GUI.FocusControl("RenameField");
                BeginRename(item);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            if (FindItem(id, rootItem) is not EntityTreeViewItem item)
                return;

            BuildContextMenu(item).ShowAsContext();
        }

        private GenericMenu BuildContextMenu(EntityTreeViewItem item)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Open"), false, () => OpenEntity(item));
            menu.AddItem(new GUIContent("Rename"), false, () => ContextRename(item));
            menu.AddItem(new GUIContent("Duplicate"), false, () => ContextDuplicate(item));
            menu.AddItem(new GUIContent("Delete"), false, () => ContextDelete(item));
            menu.AddItem(new GUIContent("Show in Project"), false, () => ContextShowInProject(item));
            menu.AddItem(new GUIContent("Copy Id"), false, () => ContextCopyId(item));

            return menu;
        }

        private void ContextRename(EntityTreeViewItem item)
        {
            SetSelection(new[] { item.id });
            BeginRenameSelectedItem();
        }

        private void ContextDuplicate(EntityTreeViewItem item)
        {
            SetSelection(new[] { item.id });
            GetExplorerWindow().DuplicateSelectedEntity();
        }

        private void ContextDelete(EntityTreeViewItem item)
        {
            if (!GetSelection().Contains(item.id))
                SetSelection(new[] { item.id });

            GetExplorerWindow().DeleteSelectedEntities();
        }

        private static void ContextShowInProject(EntityTreeViewItem item)
        {
            EditorGUIUtility.PingObject(item.prefab);
            Selection.activeObject = item.prefab;
        }

        private static void ContextCopyId(EntityTreeViewItem item)
        {
            GUIUtility.systemCopyBuffer = item.entity.GetId();
        }
    }
}
using System;
using System.Collections.Generic;
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
                        var explorerWindow = EditorWindow.GetWindow<CMSEntityExplorer>();
                        var windowRect = explorerWindow.position;

                        CMSEntityInspectorWindow.ShowWindow(item.entity, windowRect, explorerWindow, item.id);
                        Event.current.Use();
                    }
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};

            if (_viewMode == ViewModeExplorer.SearchView)
            {
                // Full list
                var id = 1;
                root.children = _searchResults
                    .Select(result => new EntityTreeViewItem
                    {
                        id = id++,
                        depth = 0,
                        displayName = result.displayName,
                        prefab = result.prefab,
                        entity = result.entity,
                        sprite = result.sprite
                    })
                    .Cast<TreeViewItem>()
                    .ToList();

                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            var pathToItem = new Dictionary<string, TreeViewItem>();
            pathToItem[""] = root;
            var idCounter = 1;

            foreach (var result in _searchResults)
            {
                var assetPath = AssetDatabase.GetAssetPath(result.prefab);
                var relativePath = assetPath.Replace("Assets/Resources/CMS/Prefabs/", "").Replace(".prefab", "");
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
                                sprite = result.sprite
                            }
                            : new TreeViewItem
                            {
                                id = idCounter++,
                                depth = i,
                                displayName = part
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
                var sprite = entityItem.sprite;
                var iconRect = new Rect(rowRect.x + iconOffset, rowRect.y + 2f, iconSize, iconSize);

                if (sprite != null)
                {
                    GUI.DrawTextureWithTexCoords(
                        iconRect,
                        sprite.texture,
                        new Rect(
                            sprite.textureRect.x / sprite.texture.width,
                            sprite.textureRect.y / sprite.texture.height,
                            sprite.textureRect.width / sprite.texture.width,
                            sprite.textureRect.height / sprite.texture.height
                        )
                    );
                }

                var labelRect = new Rect(iconRect.xMax + iconPadding, rowRect.y, rowRect.width, rowHeight);
                EditorGUI.LabelField(labelRect, args.label, new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft
                });
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
    }
}
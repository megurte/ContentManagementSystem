using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.CMSEditor
{
    public enum ViewModeExplorer
    {
        DefaultView = 0,
        SearchView = 1
    }
    
    public class CMSEntityExplorer : EditorWindow
    {
        private const string SEARCH_PATH = "Assets/Resources";
        private const string SEARCH_CONTROL_NAME = "CMSSearchField";
        private bool _focusFirstItemNextFrame;

        private string _searchQuery = "";
        private TreeViewState _treeViewState;
        private EntityTreeView _treeView;
        private Vector2 _scrollPosition;

        private GUIStyle _clearButtonStyle;

        [MenuItem("CMS/Explore/CMS Entity Explorer #&c")]
        public static void ShowWindow()
        {
            var window = GetWindow<CMSEntityExplorer>();
            window.titleContent = new GUIContent("CMS Entity Explorer");
            window.Show();
        }

        private void OnEnable()
        {
            CMS.Init();

            if (_treeViewState == null)
                _treeViewState = new TreeViewState();

            _treeView = new EntityTreeView(_treeViewState);
            _treeView.FocusSearchFieldRequest = FocusSearchBar;
            PerformSearch();
            
            _focusFirstItemNextFrame = true;
        }

        public void FocusSearchBar()
        {
            GUI.FocusControl(SEARCH_CONTROL_NAME);
        }
        
        private void FocusFirstItem()
        {
            var firstItem = _treeView.GetRows().FirstOrDefault();
            if (firstItem != null)
            {
                FocusItem(firstItem.id);
                Event.current.Use();
            }
        }

        private void OnGUI()
        {
            var key = Event.current;
            if (HandleExitOnKey(key)) return;
                        
            HandleSelectFirstItemAfterSearch();
            
            if (_treeView == null)
            {
                OnEnable();
                return;
            }

            if (_clearButtonStyle == null)
            {
                _clearButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    fixedWidth = 16,
                    fixedHeight = 16
                };
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("CMS Entity Explorer", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUI.SetNextControlName(SEARCH_CONTROL_NAME);
            var newSearch = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
            
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                if (GUILayout.Button("×", _clearButtonStyle, GUILayout.Width(16)))
                {
                    newSearch = "";
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (newSearch != _searchQuery)
            {
                _searchQuery = newSearch;
                PerformSearch();
            }

            EditorGUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            if (_treeView != null)
            {
                _treeView.OnGUI(rect);
            }
            
            if (_focusFirstItemNextFrame && _treeView.GetRows().Count > 0)
            {
                _focusFirstItemNextFrame = false;

                FocusFirstItem();
            }
        }

        private void HandleSelectFirstItemAfterSearch()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow)
            {
                if (GUI.GetNameOfFocusedControl() == SEARCH_CONTROL_NAME)
                {
                    FocusFirstItem();
                }
            }
        }

        private bool HandleExitOnKey(Event key)
        {
            if (key.type == EventType.KeyDown && key.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
                return true;
            }

            return false;
        }

        public void FocusTreeViewAndReselect(int id)
        {
            if (_treeView == null)
                return;

            Focus();
            FocusItem(id);
        }

        private void FocusItem(int id)
        {
            _treeView.SetSelection(new[] { id });
            _treeView.FrameItem(id);
            _treeView.SetFocus();
        }

        private void PerformSearch()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] {SEARCH_PATH});
            var results = new List<SearchResult>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    var cmsEntity = prefab.GetComponent<CMSEntityPfb>();

                    if (cmsEntity != null)
                    {
                        if (string.IsNullOrEmpty(_searchQuery) ||
                            (cmsEntity.name != null && cmsEntity.name.ToLower().Contains(_searchQuery.ToLower())))
                        {
                            results.Add(new SearchResult
                            {
                                prefab = prefab,
                                entity = cmsEntity,
                                displayName = $"{prefab.name} ({cmsEntity.GetType().Name})",
                                sprite = cmsEntity.GetSprite()
                            });
                        }
                    }
                }
            }

            var view = !string.IsNullOrEmpty(_searchQuery) ? ViewModeExplorer.SearchView : ViewModeExplorer.DefaultView;
            _treeView.SetSearchResults(results, view);
        }

        public void OnDestroy()
        {
            CMSMenuItems.CMSReload();
        }
    }

    public class EntityTreeView : TreeView
    {
        private ViewModeExplorer _viewMode;
        private List<SearchResult> _searchResults = new();
        private const float ROW_HEIGHT = 32f; // Increased height to accommodate sprite
        
        public Action FocusSearchFieldRequest;
        
        public EntityTreeView(TreeViewState state) : base(state)
        {
            rowHeight = ROW_HEIGHT;
            Reload();
        }
        
        public void SetSearchResults(List<SearchResult> results, ViewModeExplorer mode)
        {
            _searchResults = results;
            _viewMode = mode;
            Reload();
        }
        
        protected override void KeyEvent()
        {
            if (Event.current.type == EventType.KeyDown)
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
                
                // Arrow Up to move to search bar
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    var selected = GetSelection();
                    if (selected.Count == 1 && selected[0] == GetRows().FirstOrDefault()?.id)
                    {
                        FocusSearchFieldRequest?.Invoke();
                        Event.current.Use();
                    }
                }
                else
                {
                    base.KeyEvent();
                }
            }
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
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
                string assetPath = AssetDatabase.GetAssetPath(result.prefab);
                string relativePath = assetPath.Replace("Assets/Resources/CMS/Prefabs/", "").Replace(".prefab", "");
                string[] parts = relativePath.Split('/');

                string currentPath = "";
                TreeViewItem parent = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                    if (!pathToItem.TryGetValue(currentPath, out var item))
                    {
                        bool isLeaf = i == parts.Length - 1;

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

                        if (parent.children == null)
                            parent.children = new List<TreeViewItem>();

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
                EditorGUI.LabelField(labelRect, args.label);
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
    }
    
    public class CMSEntityInspectorWindow : EditorWindow
    {
        private UnityEngine.Object _target;
        private CMSEntityExplorer _explorer;
        private int _selectedId;
        private Vector2 _scrollPosition;
        
        public static void ShowWindow(UnityEngine.Object target, Rect anchorRect, CMSEntityExplorer explorer, int selectedId)
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

    public class EntityTreeViewItem : TreeViewItem
    {
        public GameObject prefab;
        public CMSEntityPfb entity;
        public Sprite sprite;
    }

    public class SearchResult
    {
        public GameObject prefab;
        public CMSEntityPfb entity;
        public string displayName;
        public Sprite sprite;
    }
    
    [CustomEditor(typeof(CMSEntityPfb))]
    public class CMSEntityPfbEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var entity = (CMSEntityPfb)target;
            var entitySprite = entity.GetSprite();

            if (entitySprite != null)
            {
                GUILayout.Label("Entity Icon", EditorStyles.boldLabel);

                var pixelsPerUnit = entitySprite.pixelsPerUnit;
                var width = entitySprite.rect.width / pixelsPerUnit;
                var height = entitySprite.rect.height / pixelsPerUnit;
                var aspectRatio = width / height;
                var previewHeight = 124f;
                var previewWidth = previewHeight * aspectRatio;

                var spriteRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));
                var uv = new Rect(
                    entitySprite.textureRect.x / entitySprite.texture.width,
                    entitySprite.textureRect.y / entitySprite.texture.height,
                    entitySprite.textureRect.width / entitySprite.texture.width,
                    entitySprite.textureRect.height / entitySprite.texture.height
                );

                GUI.DrawTextureWithTexCoords(spriteRect, entitySprite.texture, uv);
            }

            DrawDefaultInspector();
        }
    }
}
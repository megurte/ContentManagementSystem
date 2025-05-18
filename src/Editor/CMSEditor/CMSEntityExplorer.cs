using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.CMSEditor
{
    public class CMSEntityExplorer : EditorWindow
    {
        private const string SEARCH_PATH = "Assets/Resources";

        private string searchQuery = "";
        private TreeViewState treeViewState;
        private EntityTreeView treeView;
        private Vector2 scrollPosition;

        private GUIStyle clearButtonStyle;

        [MenuItem("CMS/Explore/CMS Entity Explorer")]
        public static void ShowWindow()
        {
            var window = GetWindow<CMSEntityExplorer>();
            window.titleContent = new GUIContent("CMS Entity Explorer");
            window.Show();
        }

        private void OnEnable()
        {
            CMS.Init();

            if (treeViewState == null)
                treeViewState = new TreeViewState();

            treeView = new EntityTreeView(treeViewState);
            PerformSearch();
        }

        private void OnGUI()
        {
            if (treeView == null)
            {
                OnEnable();
                return;
            }

            if (clearButtonStyle == null)
            {
                clearButtonStyle = new GUIStyle(EditorStyles.miniButton)
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

            string newSearch = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                if (GUILayout.Button("×", clearButtonStyle, GUILayout.Width(16)))
                {
                    newSearch = "";
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (newSearch != searchQuery)
            {
                searchQuery = newSearch;
                PerformSearch();
            }

            EditorGUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            if (treeView != null)
            {
                treeView.OnGUI(rect);
            }
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
                        if (string.IsNullOrEmpty(searchQuery) ||
                            (cmsEntity.name != null && cmsEntity.name.ToLower().Contains(searchQuery.ToLower())))
                        {
                            results.Add(new SearchResult
                            {
                                Prefab = prefab,
                                Entity = cmsEntity,
                                DisplayName = $"{prefab.name} ({cmsEntity.GetType().Name})",
                                Sprite = cmsEntity.GetSprite()
                            });
                        }
                    }
                }
            }

            treeView.SetSearchResults(results);
        }
    }

    public class EntityTreeView : TreeView
    {
        private List<SearchResult> searchResults = new List<SearchResult>();
        private const float ROW_HEIGHT = 32f; // Increased height to accommodate sprite

        public EntityTreeView(TreeViewState state) : base(state)
        {
            rowHeight = ROW_HEIGHT;
            Reload();
        }

        public void SetSearchResults(List<SearchResult> results)
        {
            searchResults = results;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var pathToItem = new Dictionary<string, TreeViewItem>();
            pathToItem[""] = root;

            int idCounter = 1;

            foreach (var result in searchResults)
            {
                string assetPath = AssetDatabase.GetAssetPath(result.Prefab);
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
                                displayName = result.DisplayName,
                                prefab = result.Prefab,
                                entity = result.Entity,
                                sprite = result.Sprite
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
            
            if (root.children == null)
                root.children = new List<TreeViewItem>();
            
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var indent = GetContentIndent(args.item);
            var rowRect = args.rowRect;
            float iconPadding = 4f;
            float iconSize = rowHeight - 4f;
            float iconOffset = indent;

            if (args.item is EntityTreeViewItem entityItem)
            {
                var sprite = entityItem.sprite;
                var iconRect = new Rect(rowRect.x + iconOffset, rowRect.y + 2f, iconSize, iconSize);

                if (sprite != null)
                {
                    // 🖼 Отрисовка спрайта
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

                // 📝 Текст — всегда смещается, даже если спрайта нет
                var labelRect = new Rect(iconRect.xMax + iconPadding, rowRect.y, rowRect.width, rowHeight);
                EditorGUI.LabelField(labelRect, args.label);
            }
            else
            {
                // 📁 Папка
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

    public class EntityTreeViewItem : TreeViewItem
    {
        public GameObject prefab;
        public CMSEntityPfb entity;
        public Sprite sprite;
    }

    public class SearchResult
    {
        public GameObject Prefab;
        public CMSEntityPfb Entity;
        public string DisplayName;
        public Sprite Sprite;
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
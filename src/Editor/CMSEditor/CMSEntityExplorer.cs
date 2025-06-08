using System.Collections.Generic;
using System.Linq;
using src.Editor.CMSEditor;
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
        private const string SearchPath = "Assets/Resources";
        private const string SearchControlName = "CMSSearchField";
        private bool _focusFirstItemNextFrame;

        private string _searchQuery = "";
        private TreeViewState _treeViewState;
        private EntityTreeView _treeView;
        private Vector2 _scrollPosition;
        private GUIStyle _clearButtonStyle;
        private ViewModeExplorer _viewMode;

        [MenuItem("CMS/CMS Entity Explorer #&c")]
        public static void ShowWindow()
        {
            var window = GetWindow<CMSEntityExplorer>();
            window.titleContent = new GUIContent("CMS Entity Explorer");
            window.Show();
        }

        private void OnEnable()
        {
            CMS.Init();
            
            _viewMode = ViewModeExplorer.DefaultView;
            
            if (_treeViewState == null)
                _treeViewState = new TreeViewState();

            _treeView = new EntityTreeView(_treeViewState);
            _treeView.focusSearchFieldRequest = FocusSearchBar;
            PerformSearch();
            
            _focusFirstItemNextFrame = true;
            
            EditorApplication.projectChanged += OnProjectChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }
        
        private void OnProjectChanged()
        {
            if (_viewMode == ViewModeExplorer.DefaultView)
            {
                PerformSearch();
                Repaint();
            }
        }

        private void FocusSearchBar()
        {
            GUI.FocusControl(SearchControlName);
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
            //GUILayout.Label("CMS Entity Explorer", EditorStyles.boldLabel);
            DrawToolButtons();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUI.SetNextControlName(SearchControlName);
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

        private void DrawToolButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                AddNewEntityFromSelection();
            }

            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                DeleteSelectedEntity();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddNewEntityFromSelection()
        {
            if (_treeView == null || _treeView.GetSelection().Count == 0)
            {
                Debug.LogWarning("No selection in tree view.");
                return;
            }

            var selectedId = _treeView.GetSelection()[0];
            var item = _treeView.GetEntityItemById(selectedId);
            if (item == null)
                return;

            var prefabPath = AssetDatabase.GetAssetPath(item.prefab);
            var folderPath = System.IO.Path.GetDirectoryName(prefabPath);

            AddNewEntity(folderPath);
        }
        
        private void AddNewEntity(string folderPath)
        {
            var path = folderPath;
            var baseName = "NewEntity";
            var counter = 1;

            while (AssetDatabase.LoadAssetAtPath<GameObject>($"{path}/{baseName}{counter}.prefab") != null)
            {
                counter++;
            }

            var finalName = $"{baseName}{counter}";
            var assetPath = $"{path}/{finalName}.prefab";

            var go = new GameObject(finalName);
            var entity = go.AddComponent<CMSEntityPfb>();
            entity.name = finalName;
            CMSEntityIdSetter.UpdateEntityId(entity, assetPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            EditorUtility.SetDirty(prefab);
            DestroyImmediate(go);

            AssetDatabase.Refresh();
            PerformSearch();
        }

        private void DeleteSelectedEntity()
        {
            if (_treeView == null || _treeView.GetSelection().Count == 0)
                return;

            var selectedId = _treeView.GetSelection()[0];
            var item = _treeView.GetEntityItemById(selectedId);

            if (item == null)
                return;

            var assetPath = AssetDatabase.GetAssetPath(item.prefab);

            if (!EditorUtility.DisplayDialog("Delete Entity",
                    $"Are you sure you want to delete '{item.prefab.name}'?", "Yes", "Cancel"))
                return;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();
            PerformSearch();
        }

        private void HandleSelectFirstItemAfterSearch()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow)
            {
                if (GUI.GetNameOfFocusedControl() == SearchControlName)
                {
                    FocusFirstItem();
                }
            }
        }

        private bool HandleExitOnKey(Event key)
        {
            if (key.type == EventType.KeyDown 
                && key.keyCode == KeyCode.Escape 
                && !_treeView.IsRenaming)
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
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] {SearchPath});
            var results = new List<SearchResult>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    var cmsEntity = prefab.GetComponent<CMSEntityPfb>();

                    if (cmsEntity != null)
                    {
                        if (string.IsNullOrEmpty(_searchQuery) ||
                            (cmsEntity.name.ToLower().Contains(_searchQuery.ToLower())))
                        {
                            results.Add(new SearchResult
                            {
                                prefab = prefab,
                                entity = cmsEntity,
                                displayName = $"{prefab.name}",
                                sprite = cmsEntity.GetSprite()
                            });
                        }
                    }
                }
            }

            _viewMode = !string.IsNullOrEmpty(_searchQuery) ? ViewModeExplorer.SearchView : ViewModeExplorer.DefaultView;
            _treeView.SetSearchResults(results, _viewMode);
        }

        public void OnDestroy()
        {
            CMSMenuItems.CMSReload();
        }
    }

    public class SearchResult
    {
        public GameObject prefab;
        public CMSEntityPfb entity;
        public string displayName;
        public Sprite sprite;
    }
}
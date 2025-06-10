using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using src.Editor.CMSEditor;
using src.Editor.CMSEditor.Templates;
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
        private const string TemplatesFolder = "Assets/Resources/CMS/Templates";
        private const string SearchPath = "Assets/Resources";
        private const string SearchControlName = "CMSSearchField";
        private bool _focusFirstItemNextFrame;

        private string _searchQuery = "";
        private TreeViewState _treeViewState;
        private EntityTreeView _treeView;
        private Vector2 _scrollPosition;
        private GUIStyle _clearButtonStyle;
        private ViewModeExplorer _viewMode;
        private GenericMenu _templateMenu;

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
            
            _treeViewState ??= new TreeViewState();

            _treeView = new EntityTreeView(_treeViewState)
            {
                focusSearchFieldRequest = FocusSearchBar
            };
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
            
            if (GUILayout.Button("Use Template", EditorStyles.toolbarDropDown, GUILayout.Width(100)))
            {
                BuildTemplateMenu();
            }

            if (GUILayout.Button("Save Template", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                var selectedItem = _treeView.GetSelectedEntity();
                if (selectedItem != null)
                {
                    SaveTemplate(selectedItem);
                }
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
        
        private void BuildTemplateMenu()
        {
            if (!Directory.Exists(TemplatesFolder))
                Directory.CreateDirectory(TemplatesFolder);

            var jsonFiles = Directory.GetFiles(TemplatesFolder, "*.json");

            _templateMenu = new GenericMenu();

            foreach (var file in jsonFiles)
            {
                var tempName = Path.GetFileNameWithoutExtension(file);
                _templateMenu.AddItem(new GUIContent(tempName), false, () =>
                {
                    ApplyTemplateFromPath(file);
                });
            }

            _templateMenu.ShowAsContext();
        }
        
        private void ApplyTemplateFromPath(string path)
        {
            var json = File.ReadAllText(path);
            var template = JsonUtility.FromJson<EntityTemplate>(json);
            if (template == null) return;

            var folder = "Assets/Resources/CMS";
            var baseName = template.templateName;
            var finalName = baseName;
            var counter = 1;

            while (AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{finalName}.prefab") != null)
            {
                finalName = $"{baseName}{counter}";
                counter++;
            }

            var go = new GameObject(finalName);
            var entity = go.AddComponent<CMSEntityPfb>();
            entity.name = finalName;
            entity.Components = new List<EntityComponentDefinition>();

            foreach (var ser in template.components)
            {
                var type = Type.GetType(ser.type);
                if (type == null)
                {
                    Debug.LogWarning($"Unknown component type: {ser.type}");
                    continue;
                }

                var instance = (EntityComponentDefinition)JsonUtility.FromJson(ser.jsonData, type);
                entity.Components.Add(instance);
            }

            var prefabPath = $"{folder}/{finalName}.prefab";
            CMSEntityIdSetter.UpdateEntityId(entity, prefabPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            EditorUtility.SetDirty(prefab);
            DestroyImmediate(go);

            AssetDatabase.Refresh();
            PerformSearch();
        }
        
        private void SaveTemplate(CMSEntityPfb entity)
        {
            if (!Directory.Exists(TemplatesFolder))
                Directory.CreateDirectory(TemplatesFolder);

            TemplateNamePopup.Show(templateName =>
            {
                var path = Path.Combine(TemplatesFolder, $"{templateName}.json");

                var template = new EntityTemplate
                {
                    templateName = templateName,
                    components = new List<SerializableComponent>()
                };
                
                foreach (var component in entity.Components)
                {
                    var type = component.GetType();
                    var json = JsonUtility.ToJson(component);

                    template.components.Add(new SerializableComponent
                    {
                        type = type.AssemblyQualifiedName,
                        jsonData = json
                    });
                }

                var jsonResult = JsonUtility.ToJson(template, true);
                File.WriteAllText(path, jsonResult);
                AssetDatabase.Refresh();
            });
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
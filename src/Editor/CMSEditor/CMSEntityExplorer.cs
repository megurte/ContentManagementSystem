using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using src.Editor.CMSEditor;
using src.Editor.CMSEditor.Templates;
using src.Editor.CMSEditor.Utils;
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
        public const string TemplatesFolder = "Assets/Resources/CMS/Templates";
        private const string SearchPath = "Assets/Resources";
        private const string SearchControlName = "CMSSearchField";
        private bool _focusFirstItemNextFrame;

        private string _searchQuery = "";
        private TreeViewState _treeViewState;
        private EntityTreeView _treeView;
        private Vector2 _scrollPosition;
        private ViewModeExplorer _viewMode;
        private List<DeletedAssetCache> _lastDeletedCache = new();

        public bool HasDeletedEntitiesToRestore => _lastDeletedCache.Count > 0;

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
            PerformSearch();
            Repaint();
        }

        private void OnFocus()
        {
            if (_treeView == null)
                return;

            PerformSearch();
            Repaint();
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
                if (GUILayout.Button("×", GlobalStyles.ClearButtonStyle, GUILayout.Width(16)))
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
                DeleteSelectedEntities();
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

        public void DuplicateSelectedEntity()
        {
            if (_treeView == null || _treeView.GetSelection().Count == 0)
                return;

            var selectedId = _treeView.GetSelection()[0];
            var item = _treeView.GetEntityItemById(selectedId);
            if (item == null)
                return;

            var srcPath = AssetDatabase.GetAssetPath(item.prefab);
            var dstPath = BuildDuplicatePath(srcPath);

            if (!AssetDatabase.CopyAsset(srcPath, dstPath))
            {
                Debug.LogError($"Failed to duplicate '{srcPath}' to '{dstPath}'.");
                return;
            }

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);
            var entity = go.GetComponent<CMSEntityPfb>();
            CMSEntityIdSetter.UpdateEntityId(entity, dstPath);
            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            PerformSearch();
            SelectEntityByPath(dstPath);
        }

        private static string BuildDuplicatePath(string srcPath)
        {
            var folder = Path.GetDirectoryName(srcPath);
            var baseName = $"{Path.GetFileNameWithoutExtension(srcPath)} Copy";
            var finalName = baseName;
            var counter = 1;

            while (AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{finalName}.prefab") != null)
            {
                finalName = $"{baseName} {counter}";
                counter++;
            }

            return $"{folder}/{finalName}.prefab";
        }

        private void SelectEntityByPath(string path)
        {
            var match = _treeView.GetRows()
                .OfType<EntityTreeViewItem>()
                .FirstOrDefault(row => AssetDatabase.GetAssetPath(row.prefab) == path);

            if (match != null)
                FocusTreeViewAndReselect(match.id);
        }

        public void DeleteSelectedEntities()
        {
            if (_treeView == null || _treeView.GetSelection().Count == 0)
                return;

            var items = GetSelectedEntityItems();
            if (items.Count == 0)
                return;

            if (!ConfirmDelete(items))
                return;

            CacheAndDeleteEntities(items);

            AssetDatabase.Refresh();
            PerformSearch();
        }

        private List<EntityTreeViewItem> GetSelectedEntityItems()
        {
            return _treeView.GetSelection()
                .Select(id => _treeView.GetEntityItemById(id))
                .Where(item => item != null)
                .ToList();
        }

        private static bool ConfirmDelete(List<EntityTreeViewItem> items)
        {
            var message = items.Count == 1
                ? $"Are you sure you want to delete '{items[0].prefab.name}'?"
                : $"Are you sure you want to delete {items.Count} entities?\n\n{string.Join("\n", items.Select(i => i.prefab.name))}";

            return EditorUtility.DisplayDialog("Delete Entity", message, "Yes", "Cancel");
        }

        private void CacheAndDeleteEntities(List<EntityTreeViewItem> items)
        {
            _lastDeletedCache = new List<DeletedAssetCache>();

            foreach (var item in items)
            {
                var path = AssetDatabase.GetAssetPath(item.prefab);
                var metaPath = path + ".meta";

                _lastDeletedCache.Add(new DeletedAssetCache
                {
                    path = path,
                    prefabText = File.ReadAllText(path),
                    metaText = File.ReadAllText(metaPath)
                });

                AssetDatabase.DeleteAsset(path);
            }
        }

        public void RestoreLastDeleted()
        {
            if (_lastDeletedCache.Count == 0)
                return;

            foreach (var cached in _lastDeletedCache)
            {
                RestoreDeletedAsset(cached);
            }

            _lastDeletedCache.Clear();

            AssetDatabase.Refresh();
            PerformSearch();
        }

        private static void RestoreDeletedAsset(DeletedAssetCache cached)
        {
            var directory = Path.GetDirectoryName(cached.path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(cached.path, cached.prefabText);
            File.WriteAllText(cached.path + ".meta", cached.metaText);

            AssetDatabase.ImportAsset(cached.path);
        }

        private class DeletedAssetCache
        {
            public string path;
            public string prefabText;
            public string metaText;
        }


        private void BuildTemplateMenu()
        {
            var guiRect = GUILayoutUtility.GetLastRect();
            var globalPos = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.x, guiRect.yMax));
            var rect = new Rect(globalPos.x + 90, globalPos.y + 20, guiRect.width, 0);
            TemplateDropdownWindow.Show(rect, templateName =>
            {
                var path = Path.Combine(TemplatesFolder, $"{templateName}.json");
                ApplyTemplateFromPath(path);
            });
        }
        
        private void ApplyTemplateFromPath(string path)
        {
            var json = File.ReadAllText(path);
            var template = JsonUtility.FromJson<EntityTemplate>(json);
            if (template == null) return;

            var folder = GetTargetFolderOrDefault();
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
        
        private string GetTargetFolderOrDefault()
        {
            var id = _treeView.GetSelection().FirstOrDefault();
            var item = _treeView.GetItemById(id);
            if (item != null)
            {
                if (item is EntityTreeViewItem entity)
                {
                    if (entity.prefab != null) 
                        return Path.GetDirectoryName(AssetDatabase.GetAssetPath(entity.prefab));
                }

                if (item is EntityTreeViewFolder folder)
                {
                    if (!string.IsNullOrEmpty(folder.path) && AssetDatabase.IsValidFolder(folder.path)) 
                        return folder.path;
                }
            }

            return "Assets/Resources/CMS";
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
                        var componentTypeNames = GetComponentTypeNames(cmsEntity);

                        if (MatchesSearchQuery(cmsEntity, componentTypeNames))
                        {
                            results.Add(new SearchResult
                            {
                                prefab = prefab,
                                entity = cmsEntity,
                                displayName = $"{prefab.name}",
                                sprite = cmsEntity.GetSprite(),
                                componentTypeNames = componentTypeNames
                            });
                        }
                    }
                }
            }

            _viewMode = !string.IsNullOrEmpty(_searchQuery) ? ViewModeExplorer.SearchView : ViewModeExplorer.DefaultView;
            _treeView.SetSearchResults(results, _viewMode);
        }

        private static List<string> GetComponentTypeNames(CMSEntityPfb cmsEntity)
        {
            var names = new List<string>();
            if (cmsEntity.Components == null)
                return names;

            foreach (var component in cmsEntity.Components)
            {
                if (component == null)
                    continue;

                names.Add(component.GetType().Name);
            }

            return names;
        }

        private bool MatchesSearchQuery(CMSEntityPfb cmsEntity, List<string> componentTypeNames)
        {
            if (string.IsNullOrEmpty(_searchQuery))
                return true;

            var query = _searchQuery.ToLower();

            if (cmsEntity.name.ToLower().Contains(query))
                return true;

            if (cmsEntity.GetId()?.ToLower().Contains(query) == true)
                return true;

            return componentTypeNames.Any(typeName => typeName.ToLower().Contains(query));
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
        public List<string> componentTypeNames;
    }
}
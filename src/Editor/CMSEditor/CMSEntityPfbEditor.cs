using System;
using System.Linq;
using MackySoft.SerializeReferenceExtensions.Editor;
using src.Editor.CMSEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor
{
    [CustomEditor(typeof(CMSEntityPfb))]
    public class CMSEntityPfbEditor : UnityEditor.Editor
    {
        private const float IconSize = 64f;

        private SerializedProperty _componentsProperty;
        private GUIStyle _boldFoldoutStyle;
        private string _componentFilter = "";

        private GUIStyle BoldFoldoutStyle => _boldFoldoutStyle ??=
            new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 12 };

        private void OnEnable()
        {
            _componentsProperty = serializedObject.FindProperty("Components");
            ExpandAllComponents();
        }

        private void ExpandAllComponents()
        {
            for (var i = 0; i < _componentsProperty.arraySize; i++)
                _componentsProperty.GetArrayElementAtIndex(i).isExpanded = true;
        }

        public override void OnInspectorGUI()
        {
            var entity = (CMSEntityPfb)target;

            serializedObject.Update();

            DrawHeader(entity);
            EditorGUILayout.Space();

            DrawComponentFilterField();

            if (DrawComponentsList())
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawAddComponentButton();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(CMSEntityPfb entity)
        {
            EditorGUILayout.BeginHorizontal();
            DrawHeaderIcon(entity);
            DrawHeaderInfo(entity);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawHeaderIcon(CMSEntityPfb entity)
        {
            var sprite = entity.GetSprite();
            if (sprite == null)
                return;

            var iconRect = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
            DrawAspectFitIcon(iconRect, sprite);
        }

        private static void DrawAspectFitIcon(Rect iconRect, Sprite sprite)
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

        private static GUIStyle _headerNameStyle;
        private static GUIStyle _idStyle;

        private static void DrawHeaderInfo(CMSEntityPfb entity)
        {
            _headerNameStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };

            EditorGUILayout.BeginVertical();
            GUILayout.Label(entity.name, _headerNameStyle);
            DrawIdRow(entity);
            EditorGUILayout.EndVertical();
        }

        private static void DrawIdRow(CMSEntityPfb entity)
        {
            _idStyle ??= new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(entity.GetId(), _idStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (GUILayout.Button("Copy", GUILayout.Width(50f)))
                GUIUtility.systemCopyBuffer = entity.GetId();

            if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                EditorGUIUtility.PingObject(entity.gameObject);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawComponentFilterField()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _componentFilter = EditorGUILayout.TextField(_componentFilter, EditorStyles.toolbarSearchField);

            if (!string.IsNullOrEmpty(_componentFilter) && GUILayout.Button("×", GlobalStyles.ClearButtonStyle, GUILayout.Width(16)))
            {
                _componentFilter = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawComponentsList()
        {
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);

            var query = _componentFilter.Trim().ToLowerInvariant();
            var visibleCount = 0;

            for (var i = 0; i < _componentsProperty.arraySize; i++)
            {
                var element = _componentsProperty.GetArrayElementAtIndex(i);
                if (!MatchesFilter(element, query))
                    continue;

                visibleCount++;
                if (DrawComponentCard(i))
                    return true;
            }

            if (visibleCount == 0 && !string.IsNullOrEmpty(query))
                EditorGUILayout.HelpBox("No components match", MessageType.Info);

            return false;
        }

        private static bool MatchesFilter(SerializedProperty element, string query)
        {
            if (string.IsNullOrEmpty(query))
                return true;

            if (!string.IsNullOrEmpty(element.managedReferenceFullTypename) &&
                GetShortTypeName(element.managedReferenceFullTypename).ToLowerInvariant().Contains(query))
                return true;

            return MatchesDescendants(element, query);
        }

        private static bool MatchesDescendants(SerializedProperty element, string query)
        {
            var endProperty = element.GetEndProperty();
            var child = element.Copy();

            while (child.NextVisible(true) && !SerializedProperty.EqualContents(child, endProperty))
            {
                if (PropertyMatchesQuery(child, query))
                    return true;
            }

            return false;
        }

        private static bool PropertyMatchesQuery(SerializedProperty property, string query)
        {
            if (property.displayName.ToLowerInvariant().Contains(query))
                return true;

            if (property.propertyType == SerializedPropertyType.String &&
                property.stringValue.ToLowerInvariant().Contains(query))
                return true;

            if (property.propertyType == SerializedPropertyType.ManagedReference &&
                !string.IsNullOrEmpty(property.managedReferenceFullTypename) &&
                GetShortTypeName(property.managedReferenceFullTypename).ToLowerInvariant().Contains(query))
                return true;

            return false;
        }

        private bool DrawComponentCard(int index)
        {
            var element = _componentsProperty.GetArrayElementAtIndex(index);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(4f);
            var mutated = DrawComponentCardHeader(index, element);
            if (!mutated && element.isExpanded)
            {
                GUILayout.Space(3f);
                DrawComponentCardBody(element);
            }
            GUILayout.Space(4f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(2f);

            return mutated;
        }

        private bool DrawComponentCardHeader(int index, SerializedProperty element)
        {
            var isNull = string.IsNullOrEmpty(element.managedReferenceFullTypename);
            var wasExpanded = element.isExpanded;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20f));
            GUILayout.Space(12f);

            if (isNull)
            {
                EditorGUILayout.LabelField("(null)", EditorStyles.boldLabel);
            }
            else
            {
                var expandedNow = EditorGUILayout.Foldout(wasExpanded, GetShortTypeName(element.managedReferenceFullTypename), true, BoldFoldoutStyle);
                if (expandedNow != wasExpanded)
                    element.isExpanded = expandedNow;
            }

            GUILayout.FlexibleSpace();

            if (!isNull && GUILayout.Button("Edit", GUILayout.Width(40f), GUILayout.Height(18f)))
                OpenComponentScript(element.managedReferenceFullTypename);

            var mutated = !isNull && DrawReorderButtons(index);

            if (GUILayout.Button("×", GUILayout.Width(20f), GUILayout.Height(18f)))
            {
                _componentsProperty.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                mutated = true;
            }

            EditorGUILayout.EndHorizontal();

            var foldoutToggled = !isNull && element.isExpanded != wasExpanded;
            HandleHeaderClick(element, index, isNull, mutated || foldoutToggled);

            return mutated;
        }

        private void HandleHeaderClick(SerializedProperty element, int index, bool isNull, bool suppressToggle)
        {
            var headerRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type != EventType.MouseDown || !headerRect.Contains(Event.current.mousePosition))
                return;

            if (Event.current.button == 1)
            {
                ShowComponentContextMenu(index);
                Event.current.Use();
                return;
            }

            if (isNull || suppressToggle || Event.current.button != 0)
                return;

            element.isExpanded = !element.isExpanded;
            Event.current.Use();
        }

        private void ShowComponentContextMenu(int index)
        {
            var menu = new GenericMenu();
            var element = _componentsProperty.GetArrayElementAtIndex(index);
            var hasInstance = element.managedReferenceValue != null;

            if (hasInstance)
            {
                menu.AddItem(new GUIContent($"Copy \"{GetShortTypeName(element.managedReferenceFullTypename)}\""), false, () => CopyComponentAt(index));
                menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateComponentAt(index));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
                menu.AddDisabledItem(new GUIContent("Duplicate"));
            }

            if (ManagedReferenceClipboard.CanPasteAs(typeof(EntityComponentDefinition)))
                menu.AddItem(new GUIContent($"Paste \"{ManagedReferenceClipboard.PeekTypeName()}\" Below"), false, () => InsertComponentAt(index + 1, ManagedReferenceClipboard.CreateCopy()));
            else
                menu.AddDisabledItem(new GUIContent("Paste"));

            menu.ShowAsContext();
        }

        private void CopyComponentAt(int index)
        {
            ManagedReferenceClipboard.Copy(_componentsProperty.GetArrayElementAtIndex(index).managedReferenceValue);
        }

        private void DuplicateComponentAt(int index)
        {
            InsertComponentAt(index + 1, ManagedReferenceClipboard.DeepClone(_componentsProperty.GetArrayElementAtIndex(index).managedReferenceValue));
        }

        private void InsertComponentAt(int index, object instance)
        {
            if (instance is not EntityComponentDefinition component)
                return;

            serializedObject.Update();
            _componentsProperty.arraySize++;
            _componentsProperty.MoveArrayElement(_componentsProperty.arraySize - 1, index);
            var element = _componentsProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = component;
            element.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private bool DrawReorderButtons(int index)
        {
            var mutated = false;

            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("▲", GUILayout.Width(22f), GUILayout.Height(18f)))
                    mutated = SwapComponents(index, index - 1);
            }

            using (new EditorGUI.DisabledScope(index == _componentsProperty.arraySize - 1))
            {
                if (GUILayout.Button("▼", GUILayout.Width(22f), GUILayout.Height(18f)))
                    mutated = SwapComponents(index, index + 1);
            }

            return mutated;
        }

        private bool SwapComponents(int a, int b)
        {
            var pa = _componentsProperty.GetArrayElementAtIndex(a);
            var pb = _componentsProperty.GetArrayElementAtIndex(b);

            (pa.managedReferenceValue, pb.managedReferenceValue) = (pb.managedReferenceValue, pa.managedReferenceValue);
            (pa.isExpanded, pb.isExpanded) = (pb.isExpanded, pa.isExpanded);

            serializedObject.ApplyModifiedProperties();
            return true;
        }

        private static void DrawComponentCardBody(SerializedProperty element)
        {
            var endProperty = element.GetEndProperty();
            var child = element.Copy();
            var enterChildren = true;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            EditorGUILayout.BeginVertical();

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, endProperty))
            {
                EditorGUILayout.PropertyField(child, true);
                enterChildren = false;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
            EditorGUILayout.EndHorizontal();
        }

        private static void OpenComponentScript(string managedReferenceFullTypename)
        {
            var typeName = GetShortTypeName(managedReferenceFullTypename);

            foreach (var guid in AssetDatabase.FindAssets($"{typeName} t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != typeName)
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    AssetDatabase.OpenAsset(script);
                    return;
                }
            }

            Debug.LogWarning($"CMS: script '{typeName}.cs' not found.");
        }

        private static string GetShortTypeName(string managedReferenceFullTypename)
        {
            var lastSpace = managedReferenceFullTypename.LastIndexOf(' ');
            var typeName = lastSpace >= 0 ? managedReferenceFullTypename[(lastSpace + 1)..] : managedReferenceFullTypename;
            var lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
        }

        private void DrawAddComponentButton()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Add Component"))
                ShowAddComponentMenu();

            if (ManagedReferenceClipboard.CanPasteAs(typeof(EntityComponentDefinition)) &&
                GUILayout.Button($"Paste \"{ManagedReferenceClipboard.PeekTypeName()}\"", GUILayout.ExpandWidth(false)))
                InsertComponentAt(_componentsProperty.arraySize, ManagedReferenceClipboard.CreateCopy());

            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddComponentMenu()
        {
            var menu = new GenericMenu();

            var baseType = typeof(EntityComponentDefinition);
            var types = TypeCache.GetTypesDerivedFrom<EntityComponentDefinition>()
                .Where(t => t != baseType && !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name);

            foreach (var type in types)
            {
                var capturedType = type;
                menu.AddItem(new GUIContent(BuildAddComponentMenuPath(type)), false, () => AddComponent(capturedType));
            }

            menu.ShowAsContext();
        }

        private static string BuildAddComponentMenuPath(Type type)
        {
            var group = string.IsNullOrEmpty(type.Namespace) ? null : type.Namespace.Split('.').Last();
            return group == null ? type.Name : $"{group}/{type.Name}";
        }

        private void AddComponent(Type type)
        {
            var newComponent = Activator.CreateInstance(type) as EntityComponentDefinition;

            serializedObject.Update();
            _componentsProperty.arraySize++;
            var newElement = _componentsProperty.GetArrayElementAtIndex(_componentsProperty.arraySize - 1);
            newElement.managedReferenceValue = newComponent;
            newElement.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

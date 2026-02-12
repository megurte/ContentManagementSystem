using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor.Utils
{
    public static class EditorCustomTools
    {
        public static void DrawWindowBorder(this EditorWindow editorWindow)
        {
            var borderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            var thickness = 1f;
            var rect = new Rect(0, 0, editorWindow.position.width, editorWindow.position.height);

            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), borderColor); // top
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), borderColor); // bottom
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), borderColor); // left
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), borderColor); // right
        }

        public static void DrawLineBetween(this EditorWindow editorWindow)
        {
            var lineRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, new Color(0.25f, 0.25f, 0.25f, 1f));
        }
        
        public static void DrawOpenPrefabButton(Rect rect, CMSEntityPfb prefab)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var icon = GetPrefabIcon(prefab);
            if (icon == null) return;

            var pad = 2f;
            var iconRect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2, rect.height - pad * 2);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        public static Texture GetPrefabIcon(CMSEntityPfb prefab)
        {
            var go = prefab != null ? prefab.gameObject : null;
            if (go == null) return null;
            return AssetPreview.GetMiniThumbnail(go) ?? EditorGUIUtility.ObjectContent(go, typeof(GameObject)).image;
        }
    }
}
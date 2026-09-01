using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor.Utils
{
    public static class GlobalStyles
    {
        private static GUIStyle _hoverStyle;
        private static GUIStyle _templateStyle;
        private static GUIStyle _clearButtonStyle;

        public static GUIStyle HoverStyle => _hoverStyle ??= new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.grayTexture }
        };

        public static GUIStyle TemplateStyle => _templateStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            padding = new RectOffset(0, 0, 2, 0)
        };

        public static GUIStyle ClearButtonStyle => _clearButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 0),
            fixedWidth = 16,
            fixedHeight = 16
        };
    }
}

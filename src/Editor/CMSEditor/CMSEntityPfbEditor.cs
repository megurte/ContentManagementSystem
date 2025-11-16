using UnityEditor;
using UnityEngine;

namespace src.Editor.CMSEditor
{
    [CustomEditor(typeof(CMSEntityPfb))]
    public class CMSEntityPfbEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var entity = (CMSEntityPfb)target;
            var entitySprite = entity.GetSprite();

            GUILayout.Label("Entity Icon", EditorStyles.boldLabel);

            var previewHeight = 124f;
            var aspectRatio = 1f;

            if (entitySprite != null)
            {
                var ppu = entitySprite.pixelsPerUnit;
                var width = entitySprite.rect.width / ppu;
                var height = entitySprite.rect.height / ppu;
                if (height > 0.0001f)
                    aspectRatio = width / height;
            }

            var previewWidth = previewHeight * aspectRatio;

            var spriteRect = GUILayoutUtility.GetRect(
                previewWidth,
                previewHeight,
                GUILayout.ExpandWidth(false)
            );

            if (entitySprite != null)
            {
                var tex = entitySprite.texture;
                var uv = new Rect(
                    entitySprite.textureRect.x / tex.width,
                    entitySprite.textureRect.y / tex.height,
                    entitySprite.textureRect.width / tex.width,
                    entitySprite.textureRect.height / tex.height
                );

                GUI.DrawTextureWithTexCoords(spriteRect, tex, uv);
            }

            DrawDefaultInspector();
        }
    }
}
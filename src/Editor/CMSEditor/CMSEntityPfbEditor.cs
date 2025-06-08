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
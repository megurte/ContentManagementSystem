using System.Collections.Generic;
using Runtime;
using UnityEditor;
using UnityEngine;

public class CMSEntityPfb : MonoBehaviour
{
    [SerializeField]//[HideInInspector]
    private string idCMS;

    public string GetId() => idCMS;
    
    [SerializeReference, SubclassSelector]
    public List<EntityComponentDefinition> Components;
    
    public CMSEntity AsEntity()
    {
        return CMS.Get<CMSEntity>(GetId());
    }

    public T As<T>() where T : EntityComponentDefinition, new()
    {
        return AsEntity().Get<T>();
    }
    
    public virtual Sprite GetSprite()
    {
        if (Components == null) return null;
        
        foreach (var component in Components)
        {
            // Use this to fetch sprite data from your different view variations
            switch (component)
            {
                case TagSprite tagSprite when tagSprite.sprite != null:
                    return tagSprite.sprite;
                case TagMesh tagMesh:
                    if (tagMesh.TryGetMesh(out var mesh, out var go))
                        return CreateSpritePreviewFromMesh(go);
                    break;
            }
        }
        
        return null;
    }
    
    private static Sprite CreateSpritePreviewFromMesh(GameObject ownerGO)
    {
#if UNITY_EDITOR
        if (ownerGO == null)
            return null;

        var preview = AssetPreview.GetAssetPreview(ownerGO);

        if (preview == null)
        {
            if (!AssetPreview.IsLoadingAssetPreview(ownerGO.GetInstanceID()))
                preview = AssetPreview.GetMiniThumbnail(ownerGO);
        }

        if (preview == null)
            return null;

        return Sprite.Create(preview, new Rect(0, 0, preview.width, preview.height), new Vector2(0.5f, 0.5f));
#else
    return null;
#endif
    }
}
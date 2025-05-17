using System.Collections.Generic;
using TagsCommon;
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
    
    public Sprite GetSprite()
    {
        if (Components == null) return null;
        
        foreach (var component in Components)
        {
            switch (component)
            {
                case TagSprite tagSprite when tagSprite.sprite != null:
                    return tagSprite.sprite;
            }
        }

        return null;
    }
}
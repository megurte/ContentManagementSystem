using System.Collections.Generic;
using Runtime;
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
            }
        }
        
        return null;
    }
}
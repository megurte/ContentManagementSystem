using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public class TagSprite : EntityComponentDefinition
    {
        public Sprite sprite;
    }

    [Serializable]
    public class TagMesh: EntityComponentDefinition
    {
        public GameObject model;

        public bool TryGetMesh(out Mesh mesh, out GameObject owner)
        {
            mesh = null;
            owner = null;

            if (model == null)
                return false;

            var mr = model.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mesh = mf.sharedMesh;
                    owner = mr.gameObject;
                    return true;
                }
            }

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                mesh = smr.sharedMesh;
                owner = smr.gameObject;
                return true;
            }

            return false;
        }
    }
    
    [Serializable]
    public class TagCMSEntity : EntityComponentDefinition
    {
        public CMSEntityPfb pfb;
    }
    
    [Serializable]
    public class TagListCMSEntity : EntityComponentDefinition
    {
        public List<CMSEntityPfb> pfbs;
    }
}
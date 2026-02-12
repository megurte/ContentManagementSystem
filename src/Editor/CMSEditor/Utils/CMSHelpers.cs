using System;
using System.Collections.Generic;
using System.Linq;

namespace src.Editor.CMSEditor.Utils
{
    public class CMSHelpers
    {
        public static void ReloadCMS()
        {
            CMS.Unload();
            CMS.Init();
        }
        
        public static List<CMSEntityPfb> FilterByTags(List<CMSEntityPfb> prefabs, Type[] tagTypes)
        {
            if (prefabs == null || prefabs.Count == 0) return new List<CMSEntityPfb>();
            if (tagTypes == null || tagTypes.Length == 0) return prefabs;

            var allowed = new HashSet<Type>(tagTypes.Where(t => t != null));

            return prefabs
                .Where(p => p != null)
                .Where(p =>
                {
                    var comps = p.Components;
                    if (comps == null) return false;

                    return comps.Any(c =>
                    {
                        if (c == null) return false;
                        var t = c.GetType();
                        return allowed.Contains(t) && IsValidTagType(t);
                    });
                })
                .ToList();
        }
        
        private static bool IsValidTagType(Type t)
        {
            return !t.IsAbstract && !t.IsGenericTypeDefinition;
        }
    }
}
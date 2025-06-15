using src.Editor.CMSEditor.Utils;
using UnityEditor;

namespace Editor.CMSEditor
{
    public static class CMSMenuItems
    {
        [MenuItem("CMS/Reload")]
        public static void CMSReload()
        {
            CMSHelpers.ReloadCMS();
        }
    }
}
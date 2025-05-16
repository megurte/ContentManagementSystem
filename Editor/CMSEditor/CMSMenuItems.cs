using UnityEditor;

namespace Editor.CMSEditor
{
    public static class CMSMenuItems
    {
        [MenuItem("CMS/Reload")]
        public static void CMSReload()
        {
            CMS.Unload();
            CMS.Init();
        }
    }
}
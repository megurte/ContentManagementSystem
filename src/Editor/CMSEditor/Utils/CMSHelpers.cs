namespace src.Editor.CMSEditor.Utils
{
    public class CMSHelpers
    {
        public static void ReloadCMS()
        {
            CMS.Unload();
            CMS.Init();
        }
    }
}
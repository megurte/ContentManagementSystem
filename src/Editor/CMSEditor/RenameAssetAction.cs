using System.IO;
using UnityEditor;

namespace Editor.CMSEditor
{
    public class RenameAssetAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(instanceId), Path.GetFileNameWithoutExtension(pathName));
            AssetDatabase.Refresh();
        }
    }
}
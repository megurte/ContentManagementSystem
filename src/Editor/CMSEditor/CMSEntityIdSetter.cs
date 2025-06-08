using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    public static class CMSEntityIdSetter
    {
        [MenuItem("CMS/Auto-Fill IDs")]
        public static void AutoFillCMSIds()
        {
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] {"Assets/Resources/CMS"});
            var updatedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                var entity = prefab.GetComponent<CMSEntityPfb>();
                if (entity == null) continue;

                var so = new SerializedObject(entity);
                var idProp = so.FindProperty("idCMS");

                if (idProp == null)
                {
                    // It should be found or else CMSEntity is broken or property name is incorrect
                    Debug.LogError($"No 'id' field found on {prefab.name}, skipping.");
                    continue;
                }

                if (!path.StartsWith("Assets/Resources/") || !path.EndsWith(".prefab"))
                {
                    Debug.LogWarning($"Prefab not in Resources or not a .prefab: {path}");
                    continue;
                }

                var id = FormatEntityId(path);

                if (idProp.stringValue == id) continue;
            
                idProp.stringValue = id;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(prefab);
                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CMS] Updated {updatedCount} prefab ID(s).");
        }

        public static string FormatEntityId(string path)
        {
            var relativePath = path.Substring("Assets/Resources/".Length);
            return relativePath.Substring(0, relativePath.Length - ".prefab".Length);
        }

        public static void UpdateEntityId(CMSEntityPfb entity, string path)
        {
            var so = new SerializedObject(entity);
            var idProp = so.FindProperty("idCMS");
            idProp.stringValue = FormatEntityId(path);
            so.ApplyModifiedProperties();
        }
    }
}

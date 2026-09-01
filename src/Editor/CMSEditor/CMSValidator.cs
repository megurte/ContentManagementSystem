using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CMSEditor
{
    public static class CMSValidator
    {
        [MenuItem("CMS/Validate")]
        public static void Validate()
        {
            var entities = CollectEntities();
            var issues = new List<string>();

            var hasErrors = ValidateIds(entities, issues);
            hasErrors |= ValidateDuplicateIds(entities, issues);
            hasErrors |= ValidateNullComponents(entities, issues);
            ValidateSprites(entities, issues);

            Report(entities.Count, issues, hasErrors);
        }

        private static List<(string path, CMSEntityPfb entity)> CollectEntities()
        {
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] {"Assets/Resources/CMS"});
            var result = new List<(string path, CMSEntityPfb entity)>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                var entity = go.GetComponent<CMSEntityPfb>();
                if (entity == null) continue;

                result.Add((path, entity));
            }

            return result;
        }

        private static bool ValidateIds(List<(string path, CMSEntityPfb entity)> entities, List<string> issues)
        {
            var hasErrors = false;

            foreach (var (path, entity) in entities)
            {
                if (!path.StartsWith("Assets/Resources/") || !path.EndsWith(".prefab")) continue;

                var expectedId = CMSEntityIdSetter.FormatEntityId(path);
                var actualId = entity.GetId();

                if (actualId == expectedId) continue;

                issues.Add($"[Id mismatch] {path}: expected '{expectedId}', got '{actualId}'");
                hasErrors = true;
            }

            return hasErrors;
        }

        private static bool ValidateDuplicateIds(List<(string path, CMSEntityPfb entity)> entities, List<string> issues)
        {
            var hasErrors = false;

            var groups = entities
                .Where(e => !string.IsNullOrEmpty(e.entity.GetId()))
                .GroupBy(e => e.entity.GetId());

            foreach (var group in groups)
            {
                if (group.Count() <= 1) continue;

                var paths = string.Join(", ", group.Select(e => e.path));
                issues.Add($"[Duplicate id] '{group.Key}': {paths}");
                hasErrors = true;
            }

            return hasErrors;
        }

        private static bool ValidateNullComponents(List<(string path, CMSEntityPfb entity)> entities, List<string> issues)
        {
            var hasErrors = false;

            foreach (var (path, entity) in entities)
            {
                if (entity.Components == null) continue;

                for (var i = 0; i < entity.Components.Count; i++)
                {
                    if (entity.Components[i] != null) continue;

                    issues.Add($"[Null component] {path}: index {i}");
                    hasErrors = true;
                }
            }

            return hasErrors;
        }

        private static void ValidateSprites(List<(string path, CMSEntityPfb entity)> entities, List<string> issues)
        {
            foreach (var (path, entity) in entities)
            {
                if (entity.GetSprite() != null) continue;

                issues.Add($"[No TagSprite] {path}");
            }
        }

        private static void Report(int entityCount, List<string> issues, bool hasErrors)
        {
            if (issues.Count == 0)
            {
                Debug.Log($"[CMS Validate] OK, {entityCount} entities, no issues");
                return;
            }

            var message = $"[CMS Validate] {issues.Count} issues:\n" + string.Join("\n", issues);

            if (hasErrors)
                Debug.LogError(message);
            else
                Debug.LogWarning(message);
        }
    }
}

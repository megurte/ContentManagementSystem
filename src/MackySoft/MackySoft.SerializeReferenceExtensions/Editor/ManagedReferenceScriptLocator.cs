using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MackySoft.SerializeReferenceExtensions.Editor
{

	public static class ManagedReferenceScriptLocator {

		static readonly Dictionary<string,MonoScript> m_ScriptCache = new Dictionary<string,MonoScript>();

		public static bool TryGetScript (string managedReferenceFullTypename,out MonoScript script) {
			script = null;
			if (string.IsNullOrEmpty(managedReferenceFullTypename)) {
				return false;
			}

			if (m_ScriptCache.TryGetValue(managedReferenceFullTypename,out script)) {
				return script != null;
			}

			Type type = ManagedReferenceUtility.GetType(managedReferenceFullTypename);
			script = (type != null) ? FindScript(type) : null;
			m_ScriptCache[managedReferenceFullTypename] = script;
			return script != null;
		}

		public static void Open (MonoScript script) {
			if (script != null) {
				AssetDatabase.OpenAsset(script);
			}
		}

		static MonoScript FindScript (Type type) {
			string typeName = type.Name;
			int genericIndex = typeName.IndexOf('`');
			if (genericIndex >= 0) {
				typeName = typeName.Substring(0,genericIndex);
			}

			MonoScript fallback = null;
			foreach (string guid in AssetDatabase.FindAssets($"{typeName} t:MonoScript")) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				MonoScript candidate = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (candidate == null) {
					continue;
				}
				if (candidate.GetClass() == type) {
					return candidate;
				}
				if ((fallback == null) && (Path.GetFileNameWithoutExtension(path) == typeName)) {
					fallback = candidate;
				}
			}
			return fallback;
		}

	}
}

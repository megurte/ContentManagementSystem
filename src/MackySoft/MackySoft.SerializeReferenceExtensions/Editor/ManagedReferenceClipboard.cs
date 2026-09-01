#if UNITY_2021_3_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;

namespace MackySoft.SerializeReferenceExtensions.Editor
{
	public sealed class ManagedReferenceClipboardHost : ScriptableObject
	{
		[SerializeReference] public object value;
	}

	public static class ManagedReferenceClipboard
	{
		const string kJsonKey = "SerializeReferenceExtensions.Clipboard.Json";
		const string kTypeKey = "SerializeReferenceExtensions.Clipboard.Type";

		static string s_cachedTypeName;
		static Type s_cachedType;

		public static bool HasContent => PeekType() != null;

		public static string PeekTypeName()
		{
			var type = PeekType();
			return type != null ? type.Name : string.Empty;
		}

		public static Type PeekType()
		{
			string typeName = SessionState.GetString(kTypeKey, string.Empty);
			if (string.IsNullOrEmpty(typeName))
				return null;

			if (typeName != s_cachedTypeName)
			{
				s_cachedTypeName = typeName;
				s_cachedType = Type.GetType(typeName);
			}

			return s_cachedType;
		}

		public static bool CanPasteAs(Type baseType)
		{
			var type = PeekType();
			return type != null && baseType != null && baseType.IsAssignableFrom(type);
		}

		public static void Copy(object instance)
		{
			if (instance == null)
				return;

			SessionState.SetString(kJsonKey, ToJson(instance));
			SessionState.SetString(kTypeKey, instance.GetType().AssemblyQualifiedName);
		}

		public static object CreateCopy()
		{
			string json = SessionState.GetString(kJsonKey, string.Empty);
			return string.IsNullOrEmpty(json) ? null : FromJson(json);
		}

		public static object DeepClone(object instance)
		{
			return instance == null ? null : FromJson(ToJson(instance));
		}

		// ScriptableObject host: EditorJsonUtility on a UnityEngine.Object root is the only serializer
		// that round-trips nested [SerializeReference] graphs; plain JsonUtility silently drops them.
		static string ToJson(object instance)
		{
			var host = ScriptableObject.CreateInstance<ManagedReferenceClipboardHost>();
			try
			{
				host.value = instance;
				return EditorJsonUtility.ToJson(host);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(host);
			}
		}

		static object FromJson(string json)
		{
			var host = ScriptableObject.CreateInstance<ManagedReferenceClipboardHost>();
			try
			{
				EditorJsonUtility.FromJsonOverwrite(json, host);
				return host.value;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(host);
			}
		}
	}
}
#endif

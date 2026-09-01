using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MackySoft.SerializeReferenceExtensions.Editor
{

	public static class ManagedReferenceUtility {

		public static object SetManagedReference (this SerializedProperty property,Type type) {
			object result = null;

#if UNITY_2021_3_OR_NEWER
			// NOTE: managedReferenceValue getter is available only in Unity 2021.3 or later.
			if ((type != null) && (property.managedReferenceValue != null))
			{
				// Restore an previous values from json.
				string json = JsonUtility.ToJson(property.managedReferenceValue);
				result = JsonUtility.FromJson(json, type);
			}
#endif

			if (result == null)
			{
				result = (type != null) ? Activator.CreateInstance(type) : null;
			}
			
			property.managedReferenceValue = result;
			return result;

		}

		public static Type GetType (string typeName) {
			if (string.IsNullOrEmpty(typeName))
			{
				return null;
			}

			int splitIndex = typeName.IndexOf(' ');
			var assembly = Assembly.Load(typeName.Substring(0,splitIndex));
			return assembly.GetType(typeName.Substring(splitIndex + 1));
		}

		const string kArrayElementMarker = ".Array.data[";

		public static bool IsArrayElement (string propertyPath) {
			return propertyPath.EndsWith("]", StringComparison.Ordinal) && propertyPath.Contains(kArrayElementMarker);
		}

		public static int GetArrayElementIndex (string propertyPath) {
			int open = propertyPath.LastIndexOf('[');
			return int.Parse(propertyPath.Substring(open + 1, propertyPath.Length - open - 2));
		}

		public static SerializedProperty GetParentArrayProperty (SerializedProperty element) {
			string path = element.propertyPath;
			int marker = path.LastIndexOf(kArrayElementMarker, StringComparison.Ordinal);
			return marker < 0 ? null : element.serializedObject.FindProperty(path.Substring(0, marker));
		}

	}
}
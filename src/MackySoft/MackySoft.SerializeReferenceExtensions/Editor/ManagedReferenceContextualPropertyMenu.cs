// NOTE: managedReferenceValue getter is available only in Unity 2021.3 or later.
#if UNITY_2021_3_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;

namespace MackySoft.SerializeReferenceExtensions.Editor
{
	public static class ManagedReferenceContextualPropertyMenu
	{

		static readonly GUIContent kCopyContent = new GUIContent("Copy");
		static readonly GUIContent kPasteContent = new GUIContent("Paste");
		static readonly GUIContent kDuplicateContent = new GUIContent("Duplicate");
		static readonly GUIContent kDeleteContent = new GUIContent("Delete");
		static readonly GUIContent kNewInstanceContent = new GUIContent("New Instance");
		static readonly GUIContent kResetAndNewInstanceContent = new GUIContent("Reset and New Instance");

		[InitializeOnLoadMethod]
		static void Initialize ()
		{
			EditorApplication.contextualPropertyMenu += OnContextualPropertyMenu;
		}

		static void OnContextualPropertyMenu (GenericMenu menu, SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				return;
			}

			// NOTE: When the callback function is called, the SerializedProperty is rewritten to the property that was being moused over at the time,
			// so a new SerializedProperty instance must be created.
			SerializedProperty clonedProperty = property.Copy();

			bool hasInstance = clonedProperty.managedReferenceValue != null;
			Type fieldType = ManagedReferenceUtility.GetType(clonedProperty.managedReferenceFieldTypename);
			bool canPaste = fieldType != null && ManagedReferenceClipboard.CanPasteAs(fieldType);
			bool isArrayElement = ManagedReferenceUtility.IsArrayElement(clonedProperty.propertyPath);
			string clipboardTypeName = ManagedReferenceClipboard.PeekTypeName();

			if (hasInstance)
			{
				menu.AddItem(new GUIContent($"Copy \"{clonedProperty.managedReferenceValue.GetType().Name}\""), false, Copy, clonedProperty);
			}
			else
			{
				menu.AddDisabledItem(kCopyContent);
			}

			if (canPaste)
			{
				menu.AddItem(new GUIContent($"Paste \"{clipboardTypeName}\""), false, PasteReplace, clonedProperty);
			}
			else
			{
				menu.AddDisabledItem(kPasteContent);
			}

			if (isArrayElement)
			{
				if (canPaste)
				{
					menu.AddItem(new GUIContent($"Paste \"{clipboardTypeName}\" Below"), false, PasteInsertBelow, clonedProperty);
				}

				if (hasInstance)
				{
					menu.AddItem(kDuplicateContent, false, Duplicate, clonedProperty);
				}
				else
				{
					menu.AddDisabledItem(kDuplicateContent);
				}

				menu.AddItem(kDeleteContent, false, Delete, clonedProperty);
			}

			menu.AddSeparator("");

			if (hasInstance)
			{
				menu.AddItem(kNewInstanceContent, false, NewInstance, clonedProperty);
				menu.AddItem(kResetAndNewInstanceContent, false, ResetAndNewInstance, clonedProperty);
			}
			else
			{
				menu.AddDisabledItem(kNewInstanceContent);
				menu.AddDisabledItem(kResetAndNewInstanceContent);
			}
		}

		static void Copy (object customData)
		{
			SerializedProperty property = (SerializedProperty)customData;
			ManagedReferenceClipboard.Copy(property.managedReferenceValue);
		}

		static void PasteReplace (object customData)
		{
			SerializedProperty property = (SerializedProperty)customData;
			object instance = ManagedReferenceClipboard.CreateCopy();
			if (instance == null)
			{
				return;
			}

			property.serializedObject.Update();
			property.managedReferenceValue = instance;
			property.isExpanded = true;
			property.serializedObject.ApplyModifiedProperties();
		}

		static void PasteInsertBelow (object customData)
		{
			InsertBelow((SerializedProperty)customData, ManagedReferenceClipboard.CreateCopy());
		}

		static void Duplicate (object customData)
		{
			SerializedProperty property = (SerializedProperty)customData;
			InsertBelow(property, ManagedReferenceClipboard.DeepClone(property.managedReferenceValue));
		}

		static void InsertBelow (SerializedProperty element, object instance)
		{
			if (instance == null)
			{
				return;
			}

			SerializedProperty arrayProperty = ManagedReferenceUtility.GetParentArrayProperty(element);
			if (arrayProperty == null)
			{
				return;
			}

			int index = ManagedReferenceUtility.GetArrayElementIndex(element.propertyPath);

			element.serializedObject.Update();
			arrayProperty.arraySize++;
			arrayProperty.MoveArrayElement(arrayProperty.arraySize - 1, index + 1);
			SerializedProperty newElement = arrayProperty.GetArrayElementAtIndex(index + 1);
			newElement.managedReferenceValue = instance;
			newElement.isExpanded = true;
			element.serializedObject.ApplyModifiedProperties();
		}

		static void Delete (object customData)
		{
			SerializedProperty property = (SerializedProperty)customData;
			SerializedProperty arrayProperty = ManagedReferenceUtility.GetParentArrayProperty(property);
			if (arrayProperty == null)
			{
				return;
			}

			property.serializedObject.Update();
			arrayProperty.DeleteArrayElementAtIndex(ManagedReferenceUtility.GetArrayElementIndex(property.propertyPath));
			property.serializedObject.ApplyModifiedProperties();
		}

		static void NewInstance (object customData)
		{
			SerializedProperty property = (SerializedProperty)customData;

			property.serializedObject.Update();
			property.managedReferenceValue = ManagedReferenceClipboard.DeepClone(property.managedReferenceValue);
			property.serializedObject.ApplyModifiedProperties();

			Debug.Log($"Create new instance of \"{property.propertyPath}\".");
		}

		static void ResetAndNewInstance (object customData)
		{
			SerializedProperty property = (SerializedProperty)customData;

			property.serializedObject.Update();
			property.managedReferenceValue = Activator.CreateInstance(property.managedReferenceValue.GetType());
			property.serializedObject.ApplyModifiedProperties();

			Debug.Log($"Reset property and created new instance of \"{property.propertyPath}\".");
		}

	}
}
#endif

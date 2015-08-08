using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Finder), true)]
public class FinderDrawer : PropertyDrawer
{
	int GetRowCount (SerializedProperty property)
	{
		var propMode = property.FindPropertyRelative ("findMode");
		if (propMode.hasMultipleDifferentValues)
			return 5;
		
		var mode = (Finder.FindModes)propMode.intValue;
		if (mode == Finder.FindModes.ByName ||
		    mode == Finder.FindModes.ByScope)
			return 5;

		if (mode == Finder.FindModes.ByReferenceComponents) {
			var com = property.FindPropertyRelative("referenceComponents");
			return 3 + (com.isExpanded ? com.arraySize + 2 : 1);
		}

		if (mode == Finder.FindModes.ByTag)
			return 4;

		if (mode == Finder.FindModes.ByReferenceGameObjects) {
			var com = property.FindPropertyRelative("referenceObjects");
			return 4 + (com.isExpanded ? com.arraySize + 2 : 1);
		}

		return 3;
	}
	
	public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
	{
		var rowCount = GetRowCount (property);
		return EditorGUIUtility.singleLineHeight * rowCount + 
			EditorGUIUtility.standardVerticalSpacing * (rowCount - 1);
	}
	
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty (position, label, property);
		
		position.height = EditorGUIUtility.singleLineHeight;
		position.y += 3;
		
		// First line: bind mode selector.
		var propMode = property.FindPropertyRelative ("findMode");
		Popup<Finder.FindModes> (position, label, propMode);
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		                    
		// Indent the line.
		position.width -= 16;
		position.x += 16;
		EditorGUIUtility.labelWidth -= 16;
		                    
		var mode = (Finder.FindModes) propMode.intValue;

		// Auto Bind
		if (propMode.hasMultipleDifferentValues || mode == Finder.FindModes.ByScope)
		{
			// Master Script
			EditorGUI.PropertyField(position, property.FindPropertyRelative("from"));
			if (property.FindPropertyRelative("from").objectReferenceValue == null)
				property.FindPropertyRelative("from").objectReferenceValue = property.serializedObject.targetObject;
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			// Scope Selector
			var propScope = property.FindPropertyRelative("scope");
			Popup<Finder.Scopes> (position, new GUIContent("Scope"), propScope);
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
		
		// Component Reference box.
		if (propMode.hasMultipleDifferentValues || mode == Finder.FindModes.ByReferenceComponents)
		{
			var components = property.FindPropertyRelative("referenceComponents");
			EditorGUI.PropertyField(position, components, true);
			var d = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			position.y += d * (components.isExpanded ? components.arraySize + 2 : 1);
		}

		// GameObject Reference box.
		if (propMode.hasMultipleDifferentValues || mode == Finder.FindModes.ByReferenceGameObjects)
		{
			var components2 = property.FindPropertyRelative("referenceObjects");
			EditorGUI.PropertyField(position, components2, true);
			var d = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			position.y += d * (components2.isExpanded ? components2.arraySize + 2 : 1);

			// Scope Selector
			var propScope = property.FindPropertyRelative("scope");
			Popup<Finder.Scopes> (position, new GUIContent("Scope"), propScope);
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
		
		// Name box.
		if (propMode.hasMultipleDifferentValues || mode == Finder.FindModes.ByName)
		{
			EditorGUI.PropertyField (position, property.FindPropertyRelative ("name"));
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			// Scope Selector
			var propScope = property.FindPropertyRelative("scope");
			Popup<Finder.Scopes> (position, new GUIContent("Scope"), propScope);
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		// Tag box.
		if (propMode.hasMultipleDifferentValues || mode == Finder.FindModes.ByTag)
		{
			var a = EditorGUI.TagField (position, new GUIContent("Tag"), property.FindPropertyRelative ("tag").stringValue);
			property.FindPropertyRelative ("tag").stringValue = a;
			// EditorGUI.PropertyField (position, property.FindPropertyRelative ("tag"));
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		// Cache mode
		EditorGUI.PropertyField(position, property.FindPropertyRelative("isCache"), new GUIContent("Cache"));
		position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		// Return null when not found
		EditorGUI.PropertyField(position, property.FindPropertyRelative("exceptionWhenNotFound"));

		EditorGUI.EndProperty();
	}

	private void Popup<T> (Rect position, GUIContent label, SerializedProperty property) where T : IConvertible {
		if (!typeof(T).IsEnum)
			throw new InvalidCastException ("T must be Enum");

		var enums = EnumToArray<T> ();
		EditorGUI.IntPopup (
			position,
			property,
			enums.Select (b => new GUIContent (b.ToString ())).ToArray (),
			enums.Select (b => b.ToInt32(null)).ToArray ()
		);
	}

	private T[] EnumToArray<T>() where T : IConvertible {
		var enums = System.Enum.GetValues (typeof(T));
		var list = new List<T> ();
		foreach (T e in enums)
			list.Add (e);
		return list.ToArray ();
	}
}

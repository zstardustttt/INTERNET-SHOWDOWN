#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyTextEffects.Editor.EditorDocumentation
{
    [CustomPropertyDrawer(typeof(ToggleDocAttribute))]
    public class ToggleDocAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Calculate the button rect
            Rect buttonRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Draw the button
            if (GUI.Button(buttonRect, "Toggle Documentation"))
            {
                EditorDocumentation.show = !EditorDocumentation.show;
            }

            // Adjust the property position to be below the button
            Rect propertyRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, position.height);

            // Draw the property field
            EditorGUI.PropertyField(propertyRect, property, label);
        }
    }
}
#endif
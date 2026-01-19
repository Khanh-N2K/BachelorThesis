#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace N2K
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var info = (InfoBoxAttribute)attribute;

            // Draw the help box
            var helpBoxRect = new Rect(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight * 2f);

            EditorGUI.HelpBox(helpBoxRect, info.message, ConvertType(info.type));

            // Draw the property field BELOW the box
            var fieldRect = new Rect(
                position.x,
                position.y + helpBoxRect.height + 2,
                position.width,
                EditorGUI.GetPropertyHeight(property, label, true)
            );

            EditorGUI.PropertyField(fieldRect, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Height of the help box + property field
            float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2f;
            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            return helpBoxHeight + propertyHeight + 4f;
        }

        private static UnityEditor.MessageType ConvertType(MessageType type)
        {
            switch (type)
            {
                default:
                case MessageType.None: return UnityEditor.MessageType.None;
                case MessageType.Info: return UnityEditor.MessageType.Info;
                case MessageType.Warning: return UnityEditor.MessageType.Warning;
                case MessageType.Error: return UnityEditor.MessageType.Error;
            }
        }
    }
}
#endif

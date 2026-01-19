#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace N2K
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SerializeReferenceDrawer : PropertyDrawer
    {
        private const float LINE = 18f;
        private const float PAD = 2f;
        private const float ARROW_WIDTH = 14f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // ===== HEADER LAYOUT =====
            Rect header = new Rect(position.x, position.y, position.width, LINE);

            Rect arrowRect = new Rect(header.x, header.y, ARROW_WIDTH, LINE);
            Rect labelRect = new Rect(
                arrowRect.xMax,
                header.y,
                EditorGUIUtility.labelWidth - ARROW_WIDTH,
                LINE);

            Rect dropdownRect = new Rect(
                labelRect.xMax,
                header.y,
                header.width - labelRect.width - ARROW_WIDTH,
                LINE);

            // ===== DRAW FOLDOUT ARROW (visual only) =====
            EditorGUI.Foldout(arrowRect, property.isExpanded, GUIContent.none, true);
            EditorGUI.LabelField(labelRect, label);

            // ===== CLICK ZONE: arrow + label =====
            Rect toggleZone = new Rect(
                arrowRect.x,
                arrowRect.y,
                labelRect.xMax - arrowRect.x,
                LINE);

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                toggleZone.Contains(Event.current.mousePosition))
            {
                property.isExpanded = !property.isExpanded;
                Event.current.Use();
            }

            // ===== TYPE DROPDOWN =====
            DrawDropdown(dropdownRect, property);

            // ===== CHILD PROPERTIES =====
            if (property.isExpanded && property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                DrawChildren(position, property);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = LINE;

            if (property.isExpanded && property.managedReferenceValue != null)
            {
                SerializedProperty it = property.Copy();
                bool enterChildren = true;

                while (it.NextVisible(enterChildren))
                {
                    if (it.depth <= property.depth)
                        break;

                    height += EditorGUI.GetPropertyHeight(it, true) + PAD;
                    enterChildren = false;
                }
            }

            return height;
        }

        // =========================
        // ===== DROPDOWN ==========
        // =========================
        private void DrawDropdown(Rect rect, SerializedProperty property)
        {
            Type baseType = GetBaseType(property);
            if (baseType == null)
                return;

            Type[] types = SubclassTypeCache.GetDerivedTypes(baseType);

            string currentLabel = property.managedReferenceValue == null
                ? "None"
                : ObjectNames.NicifyVariableName(
                    property.managedReferenceValue.GetType().Name);

            if (EditorGUI.DropdownButton(rect, new GUIContent(currentLabel), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("None"),
                    property.managedReferenceValue == null,
                    () =>
                    {
                        property.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    });

                foreach (var type in types)
                {
                    bool selected = property.managedReferenceValue?.GetType() == type;

                    menu.AddItem(
                        new GUIContent(ObjectNames.NicifyVariableName(type.Name)),
                        selected,
                        () =>
                        {
                            property.managedReferenceValue = Activator.CreateInstance(type);
                            property.isExpanded = true;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                }

                menu.ShowAsContext();
            }
        }

        // =========================
        // ===== CHILD DRAW ========
        // =========================
        private void DrawChildren(Rect position, SerializedProperty property)
        {
            SerializedProperty it = property.Copy();
            bool enterChildren = true;
            float y = position.y + LINE + PAD;

            while (it.NextVisible(enterChildren))
            {
                if (it.depth <= property.depth)
                    break;

                float h = EditorGUI.GetPropertyHeight(it, true);
                EditorGUI.PropertyField(
                    new Rect(position.x, y, position.width, h),
                    it,
                    true);

                y += h + PAD;
                enterChildren = false;
            }
        }

        // =========================
        // ===== BASE TYPE =========
        // =========================
        private static Type GetBaseType(SerializedProperty property)
        {
            string typename = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(typename))
                return null;

            // format: "AssemblyName Full.Type.Name"
            string[] split = typename.Split(' ');
            return Type.GetType($"{split[1]}, {split[0]}");
        }
    }
}
#endif
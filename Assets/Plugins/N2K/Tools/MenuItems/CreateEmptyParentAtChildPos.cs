#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace N2K
{
    public static class CreateEmptyParentAtChildPos
    {
        [MenuItem("GameObject/Create Empty Parent (At Child Pos)", false, 0)]
        static void CreateParentAtChildPosition()
        {
            if (Selection.transforms.Length == 0)
                return;
            Transform[] selection = Selection.transforms;

            // Calculate average world position
            Vector3 center = Vector3.zero;
            foreach (var t in selection)
                center += t.position;
            center /= selection.Length;

            // Create parent
            GameObject parent = new GameObject("Empty Parent");
            Undo.RegisterCreatedObjectUndo(parent, "Create Empty Parent");
            parent.transform.position = center;
            parent.transform.rotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;

            // Preserve hierarchy level
            parent.transform.SetParent(selection[0].parent, true);

            // Re-parent selected objects
            foreach (var t in selection)
            {
                Undo.SetTransformParent(t, parent.transform, "Reparent To Empty");
            }
            Selection.activeGameObject = parent;
        }

        [MenuItem("GameObject/Create Empty Parent (At Child Pos)", true)]
        static bool ValidateCreateParent()
        {
            return Selection.transforms.Length > 0;
        }
    }
}
#endif
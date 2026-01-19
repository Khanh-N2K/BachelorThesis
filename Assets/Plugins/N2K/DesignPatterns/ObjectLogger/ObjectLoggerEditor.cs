#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace N2K
{
    [CustomEditor(typeof(ObjectLogger))]
    public class ObjectLoggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ObjectLogger objLogger = target as ObjectLogger;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);

            GUI.backgroundColor = Color.red;
            GUILayout.Space(10);
            if (GUILayout.Button("Clear all logs"))
                objLogger.ClearAllLogs();

            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);
            if (GUILayout.Button("Show all logs"))
                objLogger.ShowAllLogs();
        }
    }
}
#endif
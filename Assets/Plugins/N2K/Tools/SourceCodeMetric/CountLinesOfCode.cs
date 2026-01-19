#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace N2K
{
    public class CountLinesOfCode : EditorWindow
    {
        [SerializeField]
        private List<string> foldersToIgnore = new List<string>();

        [MenuItem("Tools/Count Lines of Code")]
        public static void ShowWindow()
        {
            GetWindow<CountLinesOfCode>("Count Lines of Code");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Folders to Ignore (relative to Assets)", EditorStyles.boldLabel);
            SerializedObject so = new SerializedObject(this);
            SerializedProperty prop = so.FindProperty("foldersToIgnore");
            EditorGUILayout.PropertyField(prop, true);
            so.ApplyModifiedProperties();

            GUILayout.Space(10);

            if (GUILayout.Button("Count Lines of Code"))
            {
                CountLines();
            }
        }

        private void CountLines()
        {
            string assetsPath = Application.dataPath.Replace("\\", "/");
            string[] scripts = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);

            List<string> ignorePaths = new List<string>();
            foreach (var folder in foldersToIgnore)
            {
                string path = Path.Combine(assetsPath, folder).Replace("\\", "/");
                if (Directory.Exists(path))
                    ignorePaths.Add(path);
            }

            int codeLines = 0;
            int commentLines = 0;
            int blankLines = 0;

            foreach (string script in scripts)
            {
                string normalizedPath = script.Replace("\\", "/");

                bool ignored = false;
                foreach (string ignore in ignorePaths)
                {
                    if (normalizedPath.StartsWith(ignore))
                    {
                        ignored = true;
                        break;
                    }
                }

                if (ignored) continue;

                bool inBlockComment = false;
                string[] lines = File.ReadAllLines(script);

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrEmpty(line))
                    {
                        blankLines++;
                        continue;
                    }

                    if (inBlockComment)
                    {
                        commentLines++;
                        if (line.Contains("*/"))
                            inBlockComment = false;
                        continue;
                    }

                    if (line.StartsWith("//"))
                    {
                        commentLines++;
                    }
                    else if (line.StartsWith("/*"))
                    {
                        commentLines++;
                        if (!line.Contains("*/"))
                            inBlockComment = true;
                    }
                    else
                    {
                        codeLines++;
                    }
                }
            }

            Debug.Log(
                $"📊 Lines of Code Report\n" +
                $"Code Lines: {codeLines}\n" +
                $"Comment Lines: {commentLines}\n" +
                $"Blank Lines: {blankLines}\n" +
                $"Total Lines: {codeLines + commentLines + blankLines}"
            );
        }
    }
}
#endif
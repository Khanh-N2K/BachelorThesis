#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace N2K
{

    public static class CountClassesTool
    {
        /// <summary>
        /// Count number of C# classes in Assets folder
        /// </summary>
        /// <param name="foldersToIgnore">Relative to Assets (e.g. "Editor", "Plugins")</param>
        public static int CountClasses(List<string> foldersToIgnore = null)
        {
            string assetsPath = Application.dataPath.Replace("\\", "/");
            string[] scripts = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);

            List<string> ignorePaths = new List<string>();
            if (foldersToIgnore != null)
            {
                foreach (string folder in foldersToIgnore)
                {
                    string path = Path.Combine(assetsPath, folder).Replace("\\", "/");
                    if (Directory.Exists(path))
                        ignorePaths.Add(path);
                }
            }

            int classCount = 0;

            Regex classRegex = new Regex(@"\bclass\s+\w+", RegexOptions.Compiled);

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
                        continue;

                    if (inBlockComment)
                    {
                        if (line.Contains("*/"))
                            inBlockComment = false;
                        continue;
                    }

                    if (line.StartsWith("//"))
                        continue;

                    if (line.StartsWith("/*"))
                    {
                        if (!line.Contains("*/"))
                            inBlockComment = true;
                        continue;
                    }

                    if (classRegex.IsMatch(line))
                    {
                        classCount++;
                    }
                }
            }

            return classCount;
        }

        // Quick menu for testing
        [MenuItem("Tools/Count Classes")]
        private static void CountClassesMenu()
        {
            int count = CountClasses(new List<string>
        {
            "Editor",
            "Plugins",
            "ThirdParty"
        });

            Debug.Log($"📦 Total Classes: {count}");
        }
    }

}
#endif
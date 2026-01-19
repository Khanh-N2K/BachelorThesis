#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace N2K
{
    public static class GameObjPrefabSaver
    {
        public static void SaveWithInstancedMaterials(GameObject go, string baseFolder)
        {
            string objectFolder = $"{baseFolder}/{go.name}";
            string materialsFolder = $"{objectFolder}/_Materials";
            string meshesFolder = $"{objectFolder}/_Meshes";

            // === Ensure Folder Structure ===
            if (!AssetDatabase.IsValidFolder(objectFolder))
                AssetDatabase.CreateFolder(baseFolder, go.name);

            if (!AssetDatabase.IsValidFolder(materialsFolder))
                AssetDatabase.CreateFolder(objectFolder, "_Materials");

            if (!AssetDatabase.IsValidFolder(meshesFolder))
                AssetDatabase.CreateFolder(objectFolder, "_Meshes");

            // === Clone so we don't touch scene object ===
            GameObject copy = Object.Instantiate(go);
            copy.name = go.name;

            SaveInstancedMeshes(copy, meshesFolder);
            SaveInstancedMaterials(copy, materialsFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // === Save prefab ===
            string prefabPath = $"{objectFolder}/{go.name}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
            Object.DestroyImmediate(copy);

            Debug.Log($"[GameObjPrefabSaver] Saved prefab + materials + meshes → {objectFolder}");
        }

        // ================== MESH HANDLING ==================
        static void SaveInstancedMeshes(GameObject root, string meshesFolder)
        {
            int meshIndex = 0;

            // MeshFilter (static meshes)
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;

                bool isInstanced = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh));

                if (!isInstanced) continue;

                string safeName = mf.name.Replace("/", "_");
                string meshPath = $"{meshesFolder}/{safeName}_Mesh{meshIndex}.asset";
                meshIndex++;

                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
                    AssetDatabase.DeleteAsset(meshPath);

                Mesh newMesh = Object.Instantiate(mesh);
                newMesh.name = mesh.name;

                AssetDatabase.CreateAsset(newMesh, meshPath);
                mf.sharedMesh = newMesh;

                Debug.Log($"Saved Mesh → {meshPath}");
            }

            // SkinnedMeshRenderer (skinned meshes)
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = smr.sharedMesh;
                if (mesh == null) continue;

                bool isInstanced = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh));

                if (!isInstanced) continue;

                string safeName = smr.name.Replace("/", "_");
                string meshPath = $"{meshesFolder}/{safeName}_SkinnedMesh{meshIndex}.asset";
                meshIndex++;

                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
                    AssetDatabase.DeleteAsset(meshPath);

                Mesh newMesh = Object.Instantiate(mesh);
                newMesh.name = mesh.name;

                AssetDatabase.CreateAsset(newMesh, meshPath);
                smr.sharedMesh = newMesh;

                Debug.Log($"Saved SkinnedMesh → {meshPath}");
            }
        }

        // ================== MATERIAL HANDLING ==================
        static void SaveInstancedMaterials(GameObject root, string materialsFolder)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            int globalMatIndex = 0;

            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null) continue;

                    bool isInstanced =
                        string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)) ||
                        AssetDatabase.IsSubAsset(mat);

                    if (!isInstanced) continue;

                    string safeName = r.name.Replace("/", "_");
                    string matPath = $"{materialsFolder}/{safeName}_Slot{i}_Mat{globalMatIndex}.mat";
                    globalMatIndex++;

                    if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                        AssetDatabase.DeleteAsset(matPath);

                    Material newMat = Object.Instantiate(mat);

                    // Ensure real shader asset
                    string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                    Shader realShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                    newMat.shader = realShader != null ? realShader : Shader.Find(mat.shader.name);

                    AssetDatabase.CreateAsset(newMat, matPath);
                    mats[i] = newMat;

                    Debug.Log($"Saved Material → {matPath}");
                }

                r.sharedMaterials = mats;
            }
        }

        // ================== MENU ==================
        [MenuItem("Tools/N2K/Save Selected GameObject As Prefab")]
        static void SaveSelected()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("No GameObject selected!");
                return;
            }
            string baseFolder = "Assets/Prefabs/Prefabs Saved By GameObjPrefabSaver";
            EnsureFolderExists(baseFolder);
            SaveWithInstancedMaterials(Selection.activeGameObject, baseFolder);

            static void EnsureFolderExists(string fullPath)
            {
                if (AssetDatabase.IsValidFolder(fullPath))
                    return;

                string[] parts = fullPath.Split('/');
                string current = parts[0]; // "Assets"

                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        [MenuItem("Tools/N2K/Save Selected GameObject As Prefab", true)]
        static bool ValidateSaveSelected()
        {
            if (Selection.activeGameObject == null)
                return false;

            // Must be a scene object, not an asset
            if (EditorUtility.IsPersistent(Selection.activeGameObject))
                return false;

            return true;
        }
    }
}
#endif

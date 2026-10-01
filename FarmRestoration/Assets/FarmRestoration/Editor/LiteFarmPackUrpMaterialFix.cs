using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Creates project-owned URP material copies for the imported built-in-render-pipeline pack.</summary>
    public static class LiteFarmPackUrpMaterialFix
    {
        [MenuItem("Tools/Farm Restoration/Fix All FarmDemo Materials for URP")]
        public static void FixAllFarmDemoMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("URP shader unavailable", "Universal Render Pipeline/Lit was not found. Confirm the project is using URP.", "OK");
                return;
            }

            Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
            EnsureFolder("Assets/FarmRestoration/Materials/ConvertedFarmDemo");
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || IsCompatible(source)) continue;
                    if (!converted.TryGetValue(source, out Material replacement))
                    {
                        replacement = GetOrCreateConvertedMaterial(source, urpLit, "Assets/FarmRestoration/Materials/ConvertedFarmDemo");
                        converted.Add(source, replacement);
                    }
                    materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("FarmDemo URP sweep replaced " + converted.Count + " incompatible material(s).");
        }

        [MenuItem("Tools/Farm Restoration/Fix Lite Farm Pack Materials for URP")]
        public static void FixMaterials()
        {
            Transform root = GameObject.Find("LiteFarmPackArt")?.transform;
            if (root == null)
            {
                EditorUtility.DisplayDialog("Nothing to convert", "Run Upgrade Farm Zone With Lite Farm Pack first.", "OK");
                return;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("URP shader unavailable", "Universal Render Pipeline/Lit was not found. Confirm the project is using URP.", "OK");
                return;
            }

            int convertedCount = ConvertMaterialsUnder(root, urpLit, "ConvertedLiteFarm");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Converted " + convertedCount + " Lite Farm Pack material(s) to URP/Lit. The magenta fallback should be gone.", root.gameObject);
        }

        public static int ConvertMaterialsUnder(Transform root, Shader urpLit, string targetFolderName)
        {
            string convertedFolder = "Assets/FarmRestoration/Materials/" + targetFolderName;
            EnsureFolder(convertedFolder);
            Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    if (!converted.TryGetValue(materials[i], out Material replacement))
                    {
                        replacement = GetOrCreateConvertedMaterial(materials[i], urpLit, convertedFolder);
                        converted.Add(materials[i], replacement);
                    }
                    materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
            }

            return converted.Count;
        }

        private static Material GetOrCreateConvertedMaterial(Material source, Shader urpLit, string convertedFolder)
        {
            string safeName = source.name.Replace("/", "_").Replace("\\", "_");
            string path = convertedFolder + "/URP_" + safeName + ".mat";
            Material converted = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (converted == null)
            {
                converted = new Material(urpLit);
                AssetDatabase.CreateAsset(converted, path);
            }

            converted.shader = urpLit;
            Texture mainTexture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
            Color tint = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            converted.SetTexture("_BaseMap", mainTexture);
            converted.SetColor("_BaseColor", tint);
            converted.SetFloat("_Smoothness", 0.12f);
            return converted;
        }

        private static bool IsCompatible(Material material)
        {
            string shaderName = material.shader == null ? string.Empty : material.shader.name;
            return shaderName.StartsWith("Universal Render Pipeline/") || shaderName.StartsWith("TextMeshPro/");
        }

        private static void EnsureFolder(string convertedFolder)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Materials")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Materials");
            if (!AssetDatabase.IsValidFolder(convertedFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", convertedFolder.Substring(convertedFolder.LastIndexOf('/') + 1));
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Creates a dense, smooth stylized nature layer for FarmDemo without HDRP-only assets.</summary>
    public static class HighDetailNatureSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string MaterialFolder = "Assets/FarmRestoration/Materials/HighDetail";
        private const string SkyTexturePath = "Assets/FarmRestoration/Textures/Generated/HighDetailFarmSky.png";

        [MenuItem("Tools/Farm Restoration/Build High Detail Nature Layer")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                EditorUtility.DisplayDialog("URP unavailable", "Universal Render Pipeline/Lit was not found.", "OK");
                return;
            }

            Transform root = GetOrCreateRoot(scene, "HighDetailNature").transform;
            ClearChildren(root);
            ConfigureLighting(lit);
            CreateTreeBoundary(root, lit);
            CreateOrchard(root, lit);
            CreateRocksAndShrubs(root, lit);
            CreateFlowerBorders(root, lit);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("High-detail nature layer created: smooth multi-canopy trees, shrubs, rocks, flowers, fog and panoramic sky.", root.gameObject);
        }

        private static void ConfigureLighting(Shader lit)
        {
            Texture skyTexture = AssetDatabase.LoadAssetAtPath<Texture>(SkyTexturePath);
            Shader skyboxShader = Shader.Find("Skybox/Panoramic");
            if (skyTexture != null && skyboxShader != null)
            {
                Material skybox = GetMaterial("FarmSkybox", skyboxShader, Color.white);
                skybox.SetTexture("_MainTex", skyTexture);
                skybox.SetFloat("_Exposure", 1.05f);
                RenderSettings.skybox = skybox;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.54f, 0.63f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.64f, 0.78f, 0.83f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.91f, 0.74f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.72f;
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            }
        }

        private static void CreateTreeBoundary(Transform root, Shader lit)
        {
            int index = 0;
            for (int x = -23; x <= 23; x += 5)
            {
                CreateDetailedTree(root, "NorthTree_" + index++, new Vector3(x, 0f, 23f), 1.25f + (index % 3) * 0.13f, lit);
                CreateDetailedTree(root, "SouthTree_" + index++, new Vector3(x, 0f, -23f), 1.08f + (index % 3) * 0.10f, lit);
            }
            for (int z = -16; z <= 18; z += 6)
            {
                CreateDetailedTree(root, "WestTree_" + index++, new Vector3(-23f, 0f, z), 1.15f, lit);
                CreateDetailedTree(root, "EastTree_" + index++, new Vector3(23f, 0f, z), 1.05f, lit);
            }
        }

        private static void CreateOrchard(Transform root, Shader lit)
        {
            int index = 0;
            for (int x = -18; x <= -10; x += 4)
            for (int z = 13; z <= 17; z += 4)
                CreateDetailedTree(root, "OrchardTree_" + index++, new Vector3(x, 0f, z), 0.82f, lit, true);
        }

        private static void CreateDetailedTree(Transform root, string name, Vector3 position, float scale, Shader lit, bool blossom = false)
        {
            Transform tree = new GameObject(name).transform;
            tree.SetParent(root, false);
            tree.position = position;
            Material trunk = GetMaterial("Nature_Trunk", lit, new Color(0.18f, 0.075f, 0.025f));
            Material leafA = GetMaterial(blossom ? "Nature_BlossomA" : "Nature_LeafA", lit, blossom ? new Color(1f, 0.60f, 0.73f) : new Color(0.11f, 0.39f, 0.12f));
            Material leafB = GetMaterial(blossom ? "Nature_BlossomB" : "Nature_LeafB", lit, blossom ? new Color(1f, 0.78f, 0.86f) : new Color(0.22f, 0.58f, 0.17f));
            CreateSmoothPart(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2.0f * scale, 0f), new Vector3(0.38f, 2f, 0.38f) * scale, trunk);
            CreateSmoothPart(tree, "BranchA", PrimitiveType.Cylinder, new Vector3(0.42f, 3.3f, 0.05f) * scale, new Vector3(0.14f, 1.3f, 0.14f) * scale, trunk, new Vector3(0f, 0f, -35f));
            CreateSmoothPart(tree, "BranchB", PrimitiveType.Cylinder, new Vector3(-0.38f, 3.0f, 0.12f) * scale, new Vector3(0.13f, 1.1f, 0.13f) * scale, trunk, new Vector3(0f, 0f, 37f));
            Vector3[] canopy = { new Vector3(0f,4.55f,0f), new Vector3(0.75f,4.2f,0.2f), new Vector3(-0.7f,4.15f,0.3f), new Vector3(0.25f,5.15f,0.35f), new Vector3(-0.25f,4.65f,-0.65f), new Vector3(0.7f,4.55f,-0.55f) };
            for (int i = 0; i < canopy.Length; i++)
                CreateSmoothPart(tree, "Canopy_" + i, PrimitiveType.Sphere, canopy[i] * scale, new Vector3(1.55f, 1.35f, 1.45f) * scale * (i % 2 == 0 ? 1f : 0.88f), i % 2 == 0 ? leafA : leafB);
        }

        private static void CreateRocksAndShrubs(Transform root, Shader lit)
        {
            Material rock = GetMaterial("Nature_Rock", lit, new Color(0.16f, 0.19f, 0.16f));
            Material shrubA = GetMaterial("Nature_ShrubA", lit, new Color(0.20f, 0.50f, 0.16f));
            Material shrubB = GetMaterial("Nature_ShrubB", lit, new Color(0.34f, 0.67f, 0.22f));
            for (int i = 0; i < 24; i++)
            {
                float x = -20f + (i % 8) * 5.6f;
                float z = i % 2 == 0 ? 20.5f : -20.5f;
                CreateSmoothPart(root, "Rock_" + i, PrimitiveType.Sphere, new Vector3(x, 0.42f, z + ((i % 3) - 1) * 0.8f), new Vector3(0.95f, 0.62f, 0.78f), rock, new Vector3(i * 7f, i * 21f, 0f));
                CreateSmoothPart(root, "Shrub_" + i, PrimitiveType.Sphere, new Vector3(x + 1.1f, 0.50f, z - 1.0f), new Vector3(1.1f, 0.85f, 1f), i % 2 == 0 ? shrubA : shrubB);
            }
        }

        private static void CreateFlowerBorders(Transform root, Shader lit)
        {
            Material pink = GetMaterial("Nature_FlowerPink", lit, new Color(1f, 0.40f, 0.63f));
            Material yellow = GetMaterial("Nature_FlowerYellow", lit, new Color(1f, 0.78f, 0.08f));
            for (int i = 0; i < 30; i++)
            {
                float x = -20f + (i % 10) * 4.4f;
                float z = i < 15 ? 11.5f : -12f;
                CreateSmoothPart(root, "Flower_" + i, PrimitiveType.Sphere, new Vector3(x, 0.24f, z + (i % 3) * 0.35f), Vector3.one * 0.22f, i % 2 == 0 ? pink : yellow);
            }
        }

        private static void CreateSmoothPart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Vector3 euler = default)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = localScale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static Material GetMaterial(string name, Shader shader, Color color)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Materials")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "HighDetail");
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
            return material;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}

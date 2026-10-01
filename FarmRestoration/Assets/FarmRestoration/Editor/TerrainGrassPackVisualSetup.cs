using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Uses the imported Terrain & Grass Pack on the existing playable floating-island layout.</summary>
    public static class TerrainGrassPackVisualSetup
    {
        private const string Pack = "Assets/FlipGameDev/Terrain&GrassPack";
        private const string TerrainTextures = Pack + "/Art/Textures/Terrain_Textures/";
        private const string Prefabs = Pack + "/Prefabs/";
        private const string FarmDemo = "Assets/FarmRestoration/Scenes/FarmDemo.unity";

        [MenuItem("Tools/Farm Restoration/Apply Terrain & Grass Pack Visuals")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemo)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }

            Texture2D grass = Texture("Grass_Leaves_1_AlbedoTransparency.png");
            Texture2D flowerGrass = Texture("Grass_Flower_AlbedoTransparency.png");
            Texture2D mud = Texture("Mud_Pebbles_AlbedoTransparency.png");
            Texture2D wetMud = Texture("Mud_Leaves_2_AlbedoTransparency.png");
            Texture2D rockwall = Texture("Rockwall_AlbedoTransparency.png");
            if (grass == null || mud == null || rockwall == null)
            {
                EditorUtility.DisplayDialog("Terrain pack is incomplete", "The terrain textures are still importing. Wait until Unity finishes, then run this tool again.", "OK");
                return;
            }

            ApplyIslandTextures(grass, flowerGrass, mud, rockwall);
            ApplyGardenTextures(mud, wetMud, grass);
            CreateNatureScatter(scene, grass);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Terrain & Grass Pack visuals applied: textured island, soil beds, grass and rock scatter.");
        }

        private static void ApplyIslandTextures(Texture2D grass, Texture2D flowerGrass, Texture2D mud, Texture2D rockwall)
        {
            SetMaterialTexture("Assets/FarmRestoration/Materials/HighDetail/IslandGrass.mat", grass, new Vector2(7f, 7f), Color.white);
            SetMaterialTexture("Assets/FarmRestoration/Materials/HighDetail/IslandLedgeGrass.mat", flowerGrass != null ? flowerGrass : grass, new Vector2(4f, 2f), Color.white);
            SetMaterialTexture("Assets/FarmRestoration/Materials/HighDetail/IslandTopsoil.mat", mud, new Vector2(8f, 8f), new Color(0.72f, 0.58f, 0.43f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/HighDetail/IslandOchreEarth.mat", mud, new Vector2(6f, 5f), new Color(0.58f, 0.36f, 0.18f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/HighDetail/IslandWarmRock.mat", rockwall, new Vector2(5f, 4f), new Color(0.76f, 0.63f, 0.52f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/HighDetail/IslandDeepRock.mat", rockwall, new Vector2(4f, 4f), new Color(0.36f, 0.35f, 0.34f));
        }

        private static void ApplyGardenTextures(Texture2D mud, Texture2D wetMud, Texture2D grass)
        {
            SetMaterialTexture("Assets/FarmRestoration/Materials/Garden/GardenGrass.mat", grass, new Vector2(2f, 2f), Color.white);
            SetMaterialTexture("Assets/FarmRestoration/Materials/Garden/GardenTilledSoil.mat", mud, new Vector2(1.5f, 1.5f), new Color(0.63f, 0.39f, 0.18f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/Garden/GardenSeededSoil.mat", mud, new Vector2(1.5f, 1.5f), new Color(0.46f, 0.27f, 0.12f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/Garden/GardenWateredSoil.mat", wetMud != null ? wetMud : mud, new Vector2(1.5f, 1.5f), new Color(0.22f, 0.30f, 0.22f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/Garden/GardenGrowingSoil.mat", mud, new Vector2(1.5f, 1.5f), new Color(0.34f, 0.24f, 0.12f));
            SetMaterialTexture("Assets/FarmRestoration/Materials/Garden/GardenReadySoil.mat", mud, new Vector2(1.5f, 1.5f), new Color(0.43f, 0.25f, 0.11f));
        }

        private static void CreateNatureScatter(Scene scene, Texture2D grass)
        {
            GameObject old = GameObject.Find("TerrainGrassPackDetail");
            if (old != null) Object.DestroyImmediate(old);
            GameObject root = new GameObject("TerrainGrassPackDetail");
            SceneManager.MoveGameObjectToScene(root, scene);

            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "MeshBillboard/Grass_6_Billboard_Mesh.prefab");
            GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "MeshBillboard/Bush_Mesh.prefab");
            GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Rocks/Rock_4.prefab");
            Random.InitState(7319);

            // Sparse, deterministic scatter outside the play lanes and garden centres.
            for (int i = 0; i < 100; i++)
            {
                float angle = i * Mathf.PI * 2f / 100f + Random.Range(-0.035f, 0.035f);
                float radius = Random.Range(20f, 24.3f);
                Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0.03f, Mathf.Sin(angle) * radius);
                if (grassPrefab != null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, scene);
                    instance.name = "EdgeGrass";
                    instance.transform.SetParent(root.transform, true);
                    instance.transform.position = point;
                    instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    instance.transform.localScale = Vector3.one * Random.Range(0.48f, 0.8f);
                }
            }
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f + 0.12f;
                Vector3 point = new Vector3(Mathf.Cos(angle) * 21.5f, 0.08f, Mathf.Sin(angle) * 21.5f);
                GameObject prefab = i % 3 == 0 ? bushPrefab : rockPrefab;
                if (prefab == null) continue;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = prefab == bushPrefab ? "WildBush" : "TerrainRock";
                instance.transform.SetParent(root.transform, true);
                instance.transform.position = point;
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                instance.transform.localScale = Vector3.one * Random.Range(0.6f, 1.15f);
            }
        }

        private static Texture2D Texture(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTextures + file);

        private static void SetMaterialTexture(string path, Texture2D texture, Vector2 tiling, Color tint)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || texture == null) return;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            else if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        }
    }
}

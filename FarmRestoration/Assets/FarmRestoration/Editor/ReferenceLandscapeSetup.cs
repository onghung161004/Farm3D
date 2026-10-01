using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Rebuilds the supplied valley illustration as a playable 3D landscape around FarmDemo.</summary>
    public static class ReferenceLandscapeSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string RootName = "IllustratedValleyMap";
        private const string MeshFolder = "Assets/FarmRestoration/Models/Generated";
        private const string MaterialFolder = "Assets/FarmRestoration/Materials/IllustratedValley";
        private const string RanchPrefabs = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/";
        private const string PackTextures = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/";
        private const string RockPrefabs = "Assets/FlipGameDev/Terrain&GrassPack/Prefabs/Rocks/";
        private const string MeadowPrefabs = "Assets/FlipGameDev/Terrain&GrassPack/Prefabs/MeshBillboard/";
        private const string ImportedWaterTexture = "Assets/FarmRestoration/Textures/ImportedWater/RiverWaterFromModel.png";
        private const string ImportedWaterHeight = "Assets/FarmRestoration/Textures/ImportedWater/WaterHeightFromModel.png";
        private const int Grid = 100;
        private const float HalfSize = 100f;

        private static readonly Vector2[] RiverNodes =
        {
            new Vector2(-72f, -108f), new Vector2(-62f, -73f), new Vector2(-47f, -45f),
            new Vector2(-38f, -16f), new Vector2(-43f, 12f), new Vector2(-41f, 39f),
            new Vector2(-13f, 65f), new Vector2(31f, 76f), new Vector2(64f, 105f)
        };
        private static readonly List<Vector3> RiverCurve = SampleCurve(RiverNodes, 8);

        [MenuItem("Tools/Farm Restoration/Build Illustrated 3D Valley Map")]
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
                Debug.LogError("URP Lit shader is unavailable; the valley map was not changed.");
                return;
            }

            Transform root = GetOrCreateRoot(scene).transform;
            root.gameObject.SetActive(true);
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.localScale = Vector3.one;
            ClearChildren(root);

            Material grass = GetMaterial("ValleyGrass", lit, new Color(0.72f, 0.85f, 0.67f), "Grass_Leaves_1_AlbedoTransparency.png", new Vector2(20f, 20f));
            Material road = GetMaterial("ValleyRoad", lit, new Color(0.73f, 0.56f, 0.36f), "Road_AlbedoTransparency.png", new Vector2(1f, 8f));
            Material water = GetWaterMaterial(lit);
            Material riverbank = GetMaterial("ValleyBank", lit, new Color(0.58f, 0.49f, 0.34f), "Sand_AlbedoTransparency.png", new Vector2(1f, 8f));
            Material rock = GetMaterial("ValleyRock", lit, new Color(0.52f, 0.57f, 0.56f), "Rockwall_AlbedoTransparency.png", new Vector2(4f, 4f));
            Material snow = GetMaterial("ValleySnow", lit, new Color(0.91f, 0.96f, 0.98f), null, Vector2.one);
            Material wood = GetMaterial("ValleyBridgeWood", lit, new Color(0.35f, 0.20f, 0.09f), null, Vector2.one);

            if (!BuildTerrain(root, grass))
            {
                Debug.LogError("Valley terrain failed its centre-height or collider check. Existing ground was left active.");
                return;
            }
            HideOldLandscape();
            BuildRiver(root, water, riverbank);
            BuildRoads(root, road);
            if (!ImportedLandmarksSetup.PlaceBridge(root)) BuildBridge(root, wood);
            BuildVillage(root, lit);
            BuildForests(root, lit);
            BuildRocks(root);
            BuildRiverbankStones(root);
            BuildMeadows(root);
            if (!ImportedLandmarksSetup.PlaceMountains(root)) BuildMountains(root, rock, snow);

            EditorUtility.SetDirty(root.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAtDirect(new Vector3(-5f, 0f, 20f), Quaternion.Euler(48f, 0f, 0f), 135f);
            Debug.Log("3D valley map built around the existing farm. Save FarmDemo with Ctrl+S.", root.gameObject);
        }

        private static void HideOldLandscape()
        {
            string[] oldNames = { "FarmGround", "FloatingFarmIsland", "HighDetailNature", "TerrainGrassPackDetail" };
            foreach (string name in oldNames)
            {
                GameObject old = GameObject.Find(name);
                if (old != null) old.SetActive(false);
            }
        }

        private static bool BuildTerrain(Transform root, Material grass)
        {
            GameObject ground = new GameObject("WalkableValleyTerrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            ground.transform.SetParent(root, false);
            Mesh mesh = GetMesh("IllustratedValleyTerrain");
            int width = Grid + 1;
            Vector3[] vertices = new Vector3[width * width];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[Grid * Grid * 6];
            for (int z = 0; z <= Grid; z++)
            for (int x = 0; x <= Grid; x++)
            {
                float px = Mathf.Lerp(-HalfSize, HalfSize, x / (float)Grid);
                float pz = Mathf.Lerp(-HalfSize, HalfSize, z / (float)Grid);
                int index = z * width + x;
                vertices[index] = new Vector3(px, HeightAt(px, pz), pz);
                uv[index] = new Vector2(x / (float)Grid, z / (float)Grid);
            }
            int t = 0;
            for (int z = 0; z < Grid; z++)
            for (int x = 0; x < Grid; x++)
            {
                int a = z * width + x, b = a + 1, c = a + width, d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            ground.GetComponent<MeshFilter>().sharedMesh = mesh;
            ground.GetComponent<MeshCollider>().sharedMesh = mesh;
            ground.GetComponent<MeshRenderer>().sharedMaterial = grass;
            float centreHeight = vertices[(Grid / 2) * width + Grid / 2].y;
            bool valid = Mathf.Abs(centreHeight) < 0.05f
                && ground.GetComponent<MeshCollider>().sharedMesh == mesh
                && mesh.triangles.Length == Grid * Grid * 6;
            if (!valid)
            {
                Debug.LogError("Valley terrain centre must be Y=0; generated Y=" + centreHeight, ground);
                ground.SetActive(false);
            }
            return valid;
        }

        private static float HeightAt(float x, float z)
        {
            float distanceFromFarm = new Vector2(x, z).magnitude;
            float outskirts = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(27f, 42f, distanceFromFarm));
            float hills = 1.8f * Mathf.Sin(x * 0.055f) * Mathf.Cos(z * 0.045f)
                + 2.1f * Mathf.Sin((x + z) * 0.026f)
                + Hill(x, z, -68f, 39f, 10f, 25f)
                + Hill(x, z, 64f, 53f, 8f, 30f)
                + Hill(x, z, -75f, -40f, 6f, 20f);
            float riverDistance = DistanceToRiver(new Vector2(x, z));
            float riverFalloff = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.2f, 9.5f, riverDistance));
            float channel = 2.8f * (1f - riverFalloff);
            float naturalHeight = hills * outskirts - channel;
            // The river is always below the water surface, including where hills cross its curve.
            // A wider flat bed also keeps the coarse 2 m terrain grid from covering the water edge.
            float bankBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 10f, riverDistance));
            return Mathf.Lerp(Mathf.Min(naturalHeight, -1.6f), naturalHeight, bankBlend);
        }

        internal static float GroundHeightAt(float x, float z) => HeightAt(x, z);

        private static float Hill(float x, float z, float cx, float cz, float height, float radius)
        {
            float dx = (x - cx) / radius, dz = (z - cz) / radius;
            return height * Mathf.Exp(-(dx * dx + dz * dz) * 1.5f);
        }

        private static float DistanceToRiver(Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < RiverCurve.Count - 1; i++)
            {
                Vector2 a = new Vector2(RiverCurve[i].x, RiverCurve[i].z);
                Vector2 b = new Vector2(RiverCurve[i + 1].x, RiverCurve[i + 1].z);
                Vector2 span = b - a;
                float factor = Mathf.Clamp01(Vector2.Dot(point - a, span) / span.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(point, a + span * factor));
            }
            return best;
        }

        private static void BuildRiver(Transform root, Material water, Material bank)
        {
            List<Vector3> river = SampleCurve(RiverNodes, 8);
            CreateWaterSurface(root, river, water);
            CreateRibbon(root, "RiverShore", river, 6.6f, -1.12f, bank, false);
        }

        [MenuItem("Tools/Farm Restoration/Replace River With Imported 3D Water")]
        public static void ReplaceRiverWater()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open FarmDemo, then run this command.", "OK");
                return;
            }
            GameObject rootObject = GameObject.Find(RootName);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (rootObject == null || lit == null)
            {
                EditorUtility.DisplayDialog("Valley map missing", "Build the Illustrated 3D Valley Map first.", "OK");
                return;
            }
            if (!RepairRiverTerrain(rootObject.transform)) return;
            Transform oldWater = rootObject.transform.Find("RiverWater");
            if (oldWater != null) Object.DestroyImmediate(oldWater.gameObject);
            CreateWaterSurface(rootObject.transform, SampleCurve(RiverNodes, 8), GetWaterMaterial(lit));
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Repaired the riverbed and rebuilt the optimized 3D water surface. Save FarmDemo with Ctrl+S.", rootObject);
        }

        private static bool RepairRiverTerrain(Transform root)
        {
            Transform ground = root.Find("WalkableValleyTerrain");
            MeshFilter filter = ground == null ? null : ground.GetComponent<MeshFilter>();
            MeshCollider collider = ground == null ? null : ground.GetComponent<MeshCollider>();
            Mesh mesh = filter == null ? null : filter.sharedMesh;
            if (mesh == null || collider == null)
            {
                Debug.LogError("River repair stopped: the generated terrain mesh or collider is missing.", root);
                return false;
            }
            Undo.RecordObject(mesh, "Repair river channel");
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].y = HeightAt(vertices[i].x, vertices[i].z);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
            EditorUtility.SetDirty(mesh);
            return true;
        }

        private static Material GetWaterMaterial(Shader lit)
        {
            ConfigureWaterTexture(ImportedWaterTexture, false, 512);
            Material material = GetMaterial("ValleyRiver", lit, new Color(0.66f, 0.85f, 0.95f), null, Vector2.one);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImportedWaterTexture);
            if (texture == null)
                Debug.LogWarning("Imported water texture is missing; the river will use its blue fallback colour.");
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.9f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.03f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureWaterTexture(string path, bool readable, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool changed = importer.isReadable != readable || importer.maxTextureSize != maxSize
                || importer.wrapMode != TextureWrapMode.Repeat;
            if (!changed) return;
            importer.isReadable = readable;
            importer.maxTextureSize = maxSize;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        private static void CreateWaterSurface(Transform root, List<Vector3> centerline, Material material)
        {
            const int Across = 8;
            const float HalfWidth = 4.7f;
            GameObject water = new GameObject("RiverWater", typeof(MeshFilter), typeof(MeshRenderer));
            water.transform.SetParent(root, false);
            Mesh mesh = GetMesh("IllustratedRiverWater");
            ConfigureWaterTexture(ImportedWaterHeight, true, 128);
            Texture2D sourceHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(ImportedWaterHeight);
            int columns = Across + 1;
            Vector3[] vertices = new Vector3[centerline.Count * columns];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(centerline.Count - 1) * Across * 6];
            float distance = 0f;
            for (int i = 0; i < centerline.Count; i++)
            {
                if (i > 0) distance += Vector3.Distance(centerline[i - 1], centerline[i]);
                Vector3 before = centerline[Mathf.Max(0, i - 1)];
                Vector3 after = centerline[Mathf.Min(centerline.Count - 1, i + 1)];
                Vector3 tangent = (after - before).normalized;
                Vector3 side = new Vector3(-tangent.z, 0f, tangent.x);
                for (int x = 0; x <= Across; x++)
                {
                    float across = x / (float)Across;
                    Vector3 point = centerline[i] + side * Mathf.Lerp(-HalfWidth, HalfWidth, across);
                    float shoreDip = 0.11f * Mathf.Pow(Mathf.Abs(across * 2f - 1f), 3f);
                    float ripple = 0.025f * Mathf.Sin(distance * 2.3f + across * 17f)
                        + 0.015f * Mathf.Sin(distance * 4.6f - across * 29f);
                    float modelRelief = sourceHeight == null ? 0f
                        : (sourceHeight.GetPixelBilinear(across * 1.7f, distance * 0.17f).r - 0.5f) * 0.1f;
                    point.y = -1.02f - shoreDip + ripple + modelRelief;
                    int index = i * columns + x;
                    vertices[index] = point;
                    uv[index] = new Vector2(across * 2f, distance * 0.22f);
                }
            }
            for (int i = 0; i < centerline.Count - 1; i++)
            for (int x = 0; x < Across; x++)
            {
                int t = (i * Across + x) * 6;
                int a = i * columns + x, b = a + 1, c = a + columns, d = c + 1;
                triangles[t] = a; triangles[t + 1] = b; triangles[t + 2] = c;
                triangles[t + 3] = b; triangles[t + 4] = d; triangles[t + 5] = c;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            water.GetComponent<MeshFilter>().sharedMesh = mesh;
            water.GetComponent<MeshRenderer>().sharedMaterial = material;
            // The water is visual only. The terrain mesh remains the sole walkable collider.
        }

        private static void BuildRoads(Transform root, Material road)
        {
            Vector2[] villageRoad =
            {
                new Vector2(-20f, 24f), new Vector2(-28f, 29f), new Vector2(-40f, 37f),
                new Vector2(-53f, 45f), new Vector2(-70f, 51f)
            };
            Vector2[] eastRoad =
            {
                new Vector2(24f, -18f), new Vector2(39f, -8f), new Vector2(51f, 13f),
                new Vector2(70f, 19f), new Vector2(90f, 31f)
            };
            CreateRibbon(root, "VillageDirtRoad", SampleCurve(villageRoad, 7), 2.5f, 0.07f, road, true);
            CreateRibbon(root, "EasternDirtRoad", SampleCurve(eastRoad, 7), 2.3f, 0.07f, road, true);
        }

        private static void BuildBridge(Transform root, Material wood)
        {
            // The bridge crosses the river west of the working farm, near the village road.
            Vector3 center = new Vector3(-41f, 0.15f, 37f);
            for (int i = -5; i <= 5; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "BridgePlank_" + i;
                plank.transform.SetParent(root, false);
                plank.transform.position = center + new Vector3(i * 0.95f, 0f, 0f);
                plank.transform.localScale = new Vector3(0.9f, 0.24f, 4.4f);
                plank.GetComponent<Renderer>().sharedMaterial = wood;
            }
        }

        private static void BuildVillage(Transform root, Shader lit)
        {
            Transform village = new GameObject("RiversideVillage").transform;
            village.SetParent(root, false);
            PlaceRanch(village, "Bld_FarmerHouse", "VillageHouse_A", -58f, 44f, 0.58f, 15f);
            PlaceRanch(village, "Bld_FarmerHouse", "VillageHouse_B", -66f, 51f, 0.48f, -18f);
            PlaceRanch(village, "Bld_StoreBuilding_01", "VillageShop", -51f, 52f, 0.55f, 45f);
            PlaceRanch(village, "Bld_FarmMill_01", "HillWindmill", -74f, 68f, 0.70f, -25f);
            PlaceRanch(village, "Bld_Barn_02", "VillageBarn", -75f, 42f, 0.54f, 10f);
            LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(village, lit, "ConvertedValleyVillage");
        }

        private static void PlaceRanch(Transform parent, string prefabName, string name, float x, float z, float scale, float rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RanchPrefabs + prefabName + ".prefab");
            if (prefab == null) return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = new Vector3(x, HeightAt(x, z), z);
            instance.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            instance.transform.localScale = Vector3.one * scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }

        private static void BuildForests(Transform root, Shader lit)
        {
            Transform forest = new GameObject("ValleyForests").transform;
            forest.SetParent(root, false);
            Random.InitState(20260930);
            for (int i = 0; i < 145; i++)
            {
                float x = Random.Range(-94f, 94f);
                float z = Random.Range(-90f, 94f);
                bool forestZone = x < -57f || x > 52f || z > 72f;
                if (!forestZone || DistanceToRiver(new Vector2(x, z)) < 10f || new Vector2(x, z).magnitude < 32f) continue;
                string prefabName = "Env_Tree_0" + (1 + (i % 7));
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RanchPrefabs + prefabName + ".prefab");
                if (prefab == null) continue;
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, forest);
                tree.name = "ForestTree_" + i;
                tree.transform.position = new Vector3(x, HeightAt(x, z), z);
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * Random.Range(0.7f, 1.1f);
                foreach (Collider collider in tree.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
            LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(forest, lit, "ConvertedValleyForest");
        }

        private static void BuildRocks(Transform root)
        {
            Transform rocks = new GameObject("ValleyBoulders").transform;
            rocks.SetParent(root, false);
            Random.InitState(20261001);
            for (int i = 0; i < 34; i++)
            {
                float x = Random.Range(-92f, 92f), z = Random.Range(-88f, 88f);
                if (new Vector2(x, z).magnitude < 35f || DistanceToRiver(new Vector2(x, z)) < 7f) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockPrefabs + "Rock_" + (1 + i % 9) + ".prefab");
                if (prefab == null) continue;
                GameObject stone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rocks);
                stone.name = "Boulder_" + i;
                stone.transform.position = new Vector3(x, HeightAt(x, z) - 0.2f, z);
                stone.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                stone.transform.localScale = Vector3.one * Random.Range(0.55f, 1.4f);
                foreach (Collider collider in stone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
        }

        private static void BuildRiverbankStones(Transform root)
        {
            Transform stones = new GameObject("RiverbankStones").transform;
            stones.SetParent(root, false);
            List<Vector3> river = SampleCurve(RiverNodes, 8);
            for (int i = 4; i < river.Count - 4; i += 3)
            {
                Vector3 tangent = (river[i + 1] - river[i - 1]).normalized;
                Vector3 side = new Vector3(-tangent.z, 0f, tangent.x);
                Vector3 point = river[i] + side * (i % 2 == 0 ? 7.2f : -7.2f);
                if (Mathf.Abs(point.x) > HalfSize || Mathf.Abs(point.z) > HalfSize) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/FlipGameDev/Terrain&GrassPack/Prefabs/Rocks_Small/Rock_Small_" + (1 + i % 5) + ".prefab");
                if (prefab == null) continue;
                GameObject stone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stones);
                stone.name = "BankStone_" + i;
                stone.transform.position = new Vector3(point.x, HeightAt(point.x, point.z) - 0.08f, point.z);
                stone.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                stone.transform.localScale = Vector3.one * (0.45f + (i % 3) * 0.16f);
                foreach (Collider collider in stone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
        }

        private static void BuildMeadows(Transform root)
        {
            Transform meadow = new GameObject("MeadowGrassAndFlowers").transform;
            meadow.SetParent(root, false);
            GameObject grass = AssetDatabase.LoadAssetAtPath<GameObject>(MeadowPrefabs + "Grass_6_Billboard_Mesh.prefab");
            GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(MeadowPrefabs + "Bush_Mesh.prefab");
            Random.InitState(20261002);
            for (int i = 0; i < 110; i++)
            {
                float x = Random.Range(-85f, 85f), z = Random.Range(-83f, 83f);
                float farmDistance = new Vector2(x, z).magnitude;
                if (farmDistance < 33f || DistanceToRiver(new Vector2(x, z)) < 9f) continue;
                GameObject prefab = i % 9 == 0 ? bush : grass;
                if (prefab == null) continue;
                GameObject detail = (GameObject)PrefabUtility.InstantiatePrefab(prefab, meadow);
                detail.name = i % 9 == 0 ? "WildBush_" + i : "MeadowGrass_" + i;
                detail.transform.position = new Vector3(x, HeightAt(x, z) + 0.04f, z);
                detail.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                detail.transform.localScale = Vector3.one * Random.Range(0.45f, 0.85f);
                foreach (Collider collider in detail.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
        }

        private static void BuildMountains(Transform root, Material rock, Material snow)
        {
            Transform mountainRoot = new GameObject("DistantMountainRange").transform;
            mountainRoot.SetParent(root, false);
            for (int i = 0; i < 10; i++)
            {
                float x = -125f + i * 27f;
                float radius = 18f + (i * 13 % 16);
                float height = 28f + (i * 17 % 28);
                GameObject mountain = new GameObject("Mountain_" + i, typeof(MeshFilter), typeof(MeshRenderer));
                mountain.transform.SetParent(mountainRoot, false);
                mountain.transform.position = new Vector3(x, -2f, 132f + (i % 3) * 8f);
                Mesh mesh = GetMesh("ValleyMountain_" + i);
                const int sides = 12;
                Vector3[] vertices = new Vector3[sides * 3];
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    float uneven = 0.82f + 0.17f * Mathf.Sin(side * 6.1f + i);
                    vertices[side] = new Vector3(Mathf.Cos(angle) * radius * uneven, 0f, Mathf.Sin(angle) * radius * uneven);
                    vertices[sides + side] = vertices[side] * 0.38f + Vector3.up * height * 0.72f;
                    vertices[sides * 2 + side] = new Vector3(0f, height, 0f);
                }
                List<int> lower = new List<int>(), upper = new List<int>();
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    lower.Add(side); lower.Add(sides + side); lower.Add(next);
                    lower.Add(next); lower.Add(sides + side); lower.Add(sides + next);
                    upper.Add(sides + side); upper.Add(sides * 2 + side); upper.Add(sides + next);
                }
                mesh.Clear(); mesh.vertices = vertices; mesh.subMeshCount = 2;
                mesh.SetTriangles(lower, 0); mesh.SetTriangles(upper, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                mountain.GetComponent<MeshFilter>().sharedMesh = mesh;
                mountain.GetComponent<MeshRenderer>().sharedMaterials = new[] { rock, snow };
            }
        }

        private static List<Vector3> SampleCurve(Vector2[] nodes, int stepsPerSegment)
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < nodes.Length - 1; i++)
            for (int step = 0; step < stepsPerSegment; step++)
            {
                float t = step / (float)stepsPerSegment;
                Vector2 p0 = nodes[Mathf.Max(0, i - 1)], p1 = nodes[i];
                Vector2 p2 = nodes[i + 1], p3 = nodes[Mathf.Min(nodes.Length - 1, i + 2)];
                Vector2 point = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
                result.Add(new Vector3(point.x, 0f, point.y));
            }
            Vector2 last = nodes[nodes.Length - 1];
            result.Add(new Vector3(last.x, 0f, last.y));
            return result;
        }

        private static void CreateRibbon(Transform root, string name, List<Vector3> centerline, float halfWidth, float offset, Material material, bool followTerrain)
        {
            GameObject ribbon = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            ribbon.transform.SetParent(root, false);
            Mesh mesh = GetMesh("Illustrated" + name);
            Vector3[] vertices = new Vector3[centerline.Count * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(centerline.Count - 1) * 6];
            for (int i = 0; i < centerline.Count; i++)
            {
                Vector3 before = centerline[Mathf.Max(0, i - 1)], after = centerline[Mathf.Min(centerline.Count - 1, i + 1)];
                Vector3 tangent = (after - before).normalized;
                Vector3 side = new Vector3(-tangent.z, 0f, tangent.x);
                for (int s = 0; s < 2; s++)
                {
                    Vector3 point = centerline[i] + side * (s == 0 ? -halfWidth : halfWidth);
                    point.y = (followTerrain ? HeightAt(point.x, point.z) : 0f) + offset;
                    vertices[i * 2 + s] = point;
                    uv[i * 2 + s] = new Vector2(s, i / 5f);
                }
                if (i == centerline.Count - 1) continue;
                int t = i * 6, a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                triangles[t] = a; triangles[t + 1] = b; triangles[t + 2] = c;
                triangles[t + 3] = b; triangles[t + 4] = d; triangles[t + 5] = c;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            ribbon.GetComponent<MeshFilter>().sharedMesh = mesh;
            ribbon.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material GetMaterial(string name, Shader shader, Color color, string textureName, Vector2 tiling)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Materials")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "IllustratedValley");
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            Texture2D texture = textureName == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(PackTextures + textureName);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh GetMesh(string name)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Models")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Models");
            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Models", "Generated");
            string path = MeshFolder + "/" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static GameObject GetOrCreateRoot(Scene scene)
        {
            foreach (GameObject objectInScene in scene.GetRootGameObjects())
                if (objectInScene.name == RootName) return objectInScene;
            GameObject created = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}

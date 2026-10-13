using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Applies the imported Polytope nature and village kit to FarmDemo's existing valley.</summary>
    public static class PolytopeNatureSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string Pack = "Assets/Polytope Studio/";
        private const string Nature = Pack + "Lowpoly_Environments/Prefabs/";
        private const string Textures = Pack + "Lowpoly_Environments/Sources/Textures/";
        private const string Village = Pack + "Lowpoly_Village/Prefabs/Modular/";
        private const string MaterialFolder = "Assets/FarmRestoration/Materials/PolytopeNature";
        private const string WalkwayPath = "Assets/FarmRestoration/Models/Generated/PolytopeBridgeWalkway.asset";
        private const string LayerName = "PolytopeNatureEnvironment";
        private const string FarmSurfaceName = "FarmSurfaceV5";
        private const string BridgeAlignmentMarker = "BridgeDeckAlignedV3";
        private const string ImportedRiverMaterial = "Assets/BluBlu Games/Low Poly Forest Mini Pack/Materials/Water.mat";

        [InitializeOnLoadMethod]
        private static void ApplyOnceAfterImport()
        {
            EditorApplication.delayCall += ApplyWhenReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ApplyWhenReady;
            };
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += ApplyWhenReady;
        }

        private static void ApplyWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath) return;
            GameObject valley = GameObject.Find("IllustratedValleyMap");
            if (valley == null || !HasPack()) return;
            Transform layer = valley.transform.Find(LayerName);
            if (layer == null)
            {
                if (ApplyToRoot(valley.transform)) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            else
            {
                bool changed = false;
                if (layer.Find(FarmSurfaceName) == null)
                {
                    RemoveChild(layer, "FarmSurfaceV2");
                    RemoveChild(layer, "FarmSurfaceV3");
                    ApplyFarmSurfaceMaterials(layer);
                    changed = true;
                }
                if (layer.Find(BridgeAlignmentMarker) == null)
                {
                    ReferenceLandscapeSetup.RefreshBridgeRoads(valley.transform, PathMaterial());
                    PlaceBridge(layer);
                    changed = true;
                }
                Renderer river = valley.transform.Find("RiverWater")?.GetComponent<Renderer>();
                Material importedWater = AssetDatabase.LoadAssetAtPath<Material>(ImportedRiverMaterial);
                if (river != null && importedWater != null && river.sharedMaterial != importedWater)
                {
                    river.sharedMaterial = importedWater;
                    changed = true;
                }
                if (ReferenceLandscapeSetup.RefreshRiverShore(valley.transform)) changed = true;
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    AssetDatabase.SaveAssets();
                }
            }
        }

        [MenuItem("Tools/Farm Restoration/Apply Polytope Nature Environment")]
        public static void ApplyInFarmDemo()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo", "Exit Play Mode and open FarmDemo.unity before applying the environment.", "OK");
                return;
            }
            GameObject valley = GameObject.Find("IllustratedValleyMap");
            if (valley == null || !HasPack())
            {
                EditorUtility.DisplayDialog("Environment unavailable", "Build the Illustrated Valley Map and finish importing the Polytope pack first.", "OK");
                return;
            }
            if (!ApplyToRoot(valley.transform)) return;
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = valley.transform.Find(LayerName).gameObject;
        }

        internal static bool HasPack() => Load("Trees/PT_Pine_Tree_03_green") != null
            && Load("Trees/PT_Fruit_Tree_01_green") != null
            && AssetDatabase.LoadAssetAtPath<GameObject>(Village + "Bridge/PT_Wooden_Bridge_02.prefab") != null
            && AssetDatabase.LoadAssetAtPath<GameObject>(Village + "Fence/PT_Modular_Fence_Wood_01.prefab") != null;

        internal static bool ApplyToRoot(Transform valley)
        {
            if (valley == null || !HasPack() || Shader.Find("Universal Render Pipeline/Lit") == null) return false;
            EnsureMaterialFolder();
            ApplySurface(valley, "WalkableValleyTerrain", "PT_GrassGround", "PT_Ground_Grass_Green_01.png", new Color(0.90f, 1f, 0.90f), new Vector2(12f, 12f));
            ApplySurface(valley, "VillageDirtRoad", "PT_DirtPath", "PT_Ground_Generic_03.png", new Color(0.86f, 0.73f, 0.53f), new Vector2(1f, 8f));
            ApplySurface(valley, "BridgeEastApproach", "PT_DirtPath", "PT_Ground_Generic_03.png", new Color(0.86f, 0.73f, 0.53f), new Vector2(1f, 8f));
            ApplySurface(valley, "EasternDirtRoad", "PT_DirtPath", "PT_Ground_Generic_03.png", new Color(0.86f, 0.73f, 0.53f), new Vector2(1f, 8f));
            ApplySurface(valley, "VillageSquareSoil", "PT_DirtSquare", "PT_Ground_Generic_03.png", new Color(0.84f, 0.70f, 0.49f), new Vector2(3f, 2f));
            ApplySurface(valley, "VillageRoadsideSoil", "PT_DirtSquare", "PT_Ground_Generic_03.png", new Color(0.84f, 0.70f, 0.49f), new Vector2(3f, 2f));
            ApplySurface(valley, "EasternRoadsideSoil", "PT_DirtSquare", "PT_Ground_Generic_03.png", new Color(0.84f, 0.70f, 0.49f), new Vector2(3f, 2f));
            ApplyWater(valley);

            string[] replaced = { "ValleyForests", "ValleyBoulders", "RiverbankStones", "MeadowGrassAndFlowers", "ImportedRiverBridge", "ImportedBridgeWalkway" };
            foreach (string name in replaced) RemoveChild(valley, name);
            for (int i = valley.childCount - 1; i >= 0; i--)
                if (valley.GetChild(i).name.StartsWith("BridgePlank_", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(valley.GetChild(i).gameObject);
            RemoveChild(valley, LayerName);
            Transform layer = new GameObject(LayerName).transform;
            layer.SetParent(valley, false);

            PlaceBridge(layer);
            PlaceNature(layer);
            PlaceFences(layer);
            ReplaceVillageFences(valley);
            ConvertPackMaterials(layer);
            ApplyFarmSurfaceMaterials(layer);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.73f, 0.88f, 0.91f);
            RenderSettings.fogDensity = 0.004f;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Polytope environment applied: green ground, dirt paths, river, wooden bridge, trees, flowers and fences.", layer.gameObject);
            return true;
        }

        private static void ApplySurface(Transform valley, string objectName, string materialName, string textureName, Color tint, Vector2 tiling)
        {
            Renderer renderer = valley.Find(objectName)?.GetComponent<Renderer>();
            if (renderer == null) return;
            Material material = GetMaterial(materialName, textureName, tint, tiling);
            if (material != null) renderer.sharedMaterial = material;
        }

        private static void ApplyWater(Transform valley)
        {
            Renderer renderer = valley.Find("RiverWater")?.GetComponent<Renderer>();
            if (renderer == null) return;
            Material importedWater = AssetDatabase.LoadAssetAtPath<Material>(ImportedRiverMaterial);
            if (importedWater != null)
            {
                renderer.sharedMaterial = importedWater;
                return;
            }
            Material water = GetMaterial("PT_ClearRiver", null, new Color(0.35f, 0.76f, 0.82f), new Vector2(2f, 8f));
            if (water == null) return;
            Texture2D riverTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/FarmRestoration/Textures/ImportedWater/RiverWaterFromModel.png");
            water.SetTexture("_BaseMap", riverTexture);
            water.SetFloat("_Smoothness", 0.82f);
            renderer.sharedMaterial = water;
            EditorUtility.SetDirty(water);
        }

        private static void PlaceBridge(Transform layer)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Village + "Bridge/PT_Wooden_Bridge_02.prefab");
            Transform oldBridge = layer.Find("PolytopeWoodenBridge");
            if (oldBridge != null) UnityEngine.Object.DestroyImmediate(oldBridge.gameObject);
            Transform oldWalkway = layer.Find("PolytopeBridgeWalkway");
            if (oldWalkway != null) UnityEngine.Object.DestroyImmediate(oldWalkway.gameObject);
            GameObject bridge = Place(prefab, layer, "PolytopeWoodenBridge", Vector3.zero, 1f, 0f);
            ConvertPackMaterials(bridge.transform);
            ReferenceLandscapeSetup.GetBridgeAlignment(out Vector3 center, out Vector3 direction);
            Bounds bounds = RendererBounds(bridge);
            Vector3 localAxis = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
            float length = Mathf.Max(bounds.size.x, bounds.size.z);
            bridge.transform.rotation = Quaternion.FromToRotation(localAxis, direction);
            if (length > 0.01f)
            {
                Vector3 scale = bridge.transform.localScale;
                if (localAxis == Vector3.right) scale.x *= 20.4f / length;
                else scale.z *= 20.4f / length;
                bridge.transform.localScale = scale;
            }
            bounds = RendererBounds(bridge);
            bridge.transform.position += new Vector3(center.x - bounds.center.x, -0.15f - bounds.min.y, center.z - bounds.center.z);
            MeshCollider deckCollider = bridge.transform.Find("PT_Wooden_Bridge_02_LOD0")?.GetComponent<MeshCollider>()
                ?? bridge.GetComponentInChildren<MeshCollider>(true);
            if (deckCollider != null)
            {
                deckCollider.enabled = true;
                Physics.SyncTransforms();
                bool nearHit = TryDeckHeight(deckCollider, center - direction * 8.8f, bounds.max.y, out float nearDeck);
                bool farHit = TryDeckHeight(deckCollider, center + direction * 8.8f, bounds.max.y, out float farDeck);
                if (nearHit && farHit)
                {
                    float nearBank = ReferenceLandscapeSetup.GroundHeightAt(center.x - direction.x * 9.8f, center.z - direction.z * 9.8f) + 0.13f;
                    float farBank = ReferenceLandscapeSetup.GroundHeightAt(center.x + direction.x * 9.8f, center.z + direction.z * 9.8f) + 0.13f;
                    float angle = Mathf.Atan2((farBank - nearBank) - (farDeck - nearDeck), 17.6f) * Mathf.Rad2Deg;
                    bridge.transform.rotation = Quaternion.AngleAxis(angle, Vector3.Cross(direction, Vector3.up)) * bridge.transform.rotation;
                    Physics.SyncTransforms();
                    TryDeckHeight(deckCollider, center - direction * 8.8f, RendererBounds(bridge).max.y, out nearDeck);
                    TryDeckHeight(deckCollider, center + direction * 8.8f, RendererBounds(bridge).max.y, out farDeck);
                    bridge.transform.position += Vector3.up * ((nearBank + farBank - nearDeck - farDeck) * 0.5f);
                    Physics.SyncTransforms();
                }
                else Debug.LogWarning("Bridge deck raycast missed an end; retaining the prefab's original elevation.", bridge);
            }
            CreateWalkway(layer, center, direction, deckCollider);
            if (deckCollider != null) deckCollider.enabled = false;
            Transform marker = layer.Find(BridgeAlignmentMarker);
            if (marker == null) new GameObject(BridgeAlignmentMarker).transform.SetParent(layer, false);
        }

        private static bool TryDeckHeight(Collider bridge, Vector3 point, float top, out float height)
        {
            Ray ray = new Ray(new Vector3(point.x, top + 5f, point.z), Vector3.down);
            bool hitDeck = bridge.Raycast(ray, out RaycastHit hit, 30f);
            height = hitDeck ? hit.point.y : point.y;
            return hitDeck;
        }

        private static void CreateWalkway(Transform layer, Vector3 center, Vector3 direction, Collider bridge)
        {
            GameObject walkway = new GameObject("PolytopeBridgeWalkway", typeof(MeshCollider), typeof(FootstepSurface));
            walkway.GetComponent<FootstepSurface>().Configure(FootstepSurface.Kind.Wood);
            walkway.transform.SetParent(layer, false);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WalkwayPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "PolytopeBridgeWalkway" };
                AssetDatabase.CreateAsset(mesh, WalkwayPath);
            }
            float[] rows = { -10.2f, -5.2f, 5.2f, 10.2f };
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3[] vertices = new Vector3[rows.Length * 2];
            int[] triangles = new int[(rows.Length - 1) * 6];
            float bridgeTop = bridge == null ? 0f : RendererBounds(bridge.gameObject).max.y;
            for (int i = 0; i < rows.Length; i++)
            for (int s = 0; s < 2; s++)
            {
                Vector3 point = center + direction * rows[i] + side * (s == 0 ? -1.65f : 1.65f);
                float bankHeight = ReferenceLandscapeSetup.GroundHeightAt(point.x, point.z) + 0.13f;
                float deckHeight = bankHeight;
                bool hasDeck = bridge != null && TryDeckHeight(bridge, center + direction * rows[i], bridgeTop, out deckHeight);
                point.y = i == 0 || i == rows.Length - 1 || bridge == null
                    ? bankHeight : hasDeck ? deckHeight + 0.04f : bankHeight;
                vertices[i * 2 + s] = point;
            }
            for (int i = 0; i < rows.Length - 1; i++)
            {
                int a = i * 2, t = i * 6;
                triangles[t] = a; triangles[t + 1] = a + 2; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = a + 2; triangles[t + 5] = a + 3;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            walkway.GetComponent<MeshCollider>().sharedMesh = mesh;
            EditorUtility.SetDirty(mesh);
        }

        private static void PlaceNature(Transform layer)
        {
            Transform trees = Child(layer, "Trees");
            Transform details = Child(layer, "MeadowAndRiverbank");
            GameObject pine = Load("Trees/PT_Pine_Tree_03_green");
            GameObject fruit = Load("Trees/PT_Fruit_Tree_01_green");
            GameObject apple = Load("Trees/PT_Fruit_Tree_01_apples");
            GameObject plum = Load("Trees/PT_Fruit_Tree_01_plums");
            GameObject grass = Load("Plants/PT_Grass_02");
            GameObject flower = Load("Flowers/PT_Poppy_02");
            GameObject shrub = Load("Shrubs/PT_Generic_Shrub_01_green");
            GameObject rock = Load("Rocks/PT_River_Rock_Pile_02");
            System.Random random = new System.Random(20261004);
            Scatter(pine, trees, random, 230, 1.2f, 2.1f, true, 14f);
            Scatter(fruit, trees, random, 42, 1.1f, 1.6f, false, 14f);
            Scatter(apple, trees, random, 18, 1.1f, 1.5f, false, 14f);
            Scatter(plum, trees, random, 18, 1.1f, 1.5f, false, 14f);
            Scatter(grass, details, random, 550, 0.6f, 1.1f, false, 7f);
            Scatter(flower, details, random, 150, 0.7f, 1.2f, false, 7f);
            Scatter(shrub, details, random, 85, 0.65f, 1.1f, false, 9f);
            Scatter(rock, details, random, 36, 0.7f, 1.2f, false, 5.8f);
        }

        private static void Scatter(GameObject prefab, Transform parent, System.Random random, int attempts, float minScale, float maxScale, bool forestOnly, float riverClearance)
        {
            if (prefab == null) return;
            for (int i = 0; i < attempts; i++)
            {
                float x = Mathf.Lerp(-94f, 94f, (float)random.NextDouble());
                float z = Mathf.Lerp(-94f, 94f, (float)random.NextDouble());
                Vector2 point = new Vector2(x, z);
                if (point.magnitude < 35f || Vector2.Distance(point, new Vector2(-63f, 51f)) < 24f
                    || ReferenceLandscapeSetup.GroundDistanceToRiver(point) < riverClearance
                    || FishingPondTerrain.IsNear(point, 5f)
                    || VillageHouseLayout.IsNearHome(point, forestOnly ? 8f : 5f)
                    || NearPath(point, 5.5f) || (forestOnly && Mathf.Abs(x) < 49f && z < 71f)) continue;
                float scale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
                Place(prefab, parent, prefab.name + "_" + i,
                    new Vector3(x, ReferenceLandscapeSetup.GroundHeightAt(x, z), z), scale,
                    (float)random.NextDouble() * 360f);
            }
        }

        private static bool NearPath(Vector2 point, float clearance)
        {
            Vector2[] road = { new Vector2(-20f, 24f), new Vector2(-28f, 29f), new Vector2(-41f, 39f), new Vector2(-53f, 45f), new Vector2(-70f, 51f) };
            for (int i = 0; i < road.Length - 1; i++)
            {
                Vector2 segment = road[i + 1] - road[i];
                float t = Mathf.Clamp01(Vector2.Dot(point - road[i], segment) / segment.sqrMagnitude);
                if (Vector2.Distance(point, road[i] + segment * t) < clearance) return true;
            }
            return false;
        }

        private static void PlaceFences(Transform layer)
        {
            GameObject fence = AssetDatabase.LoadAssetAtPath<GameObject>(Village + "Fence/PT_Modular_Fence_Wood_01.prefab");
            Transform root = Child(layer, "Fences");
            FenceLine(fence, root, new Vector2(-97f, 32f), new Vector2(-97f, 89f), 22);
            FenceLine(fence, root, new Vector2(-38f, 56f), new Vector2(-38f, 86f), 12);
            FenceLine(fence, root, new Vector2(-97f, 94f), new Vector2(-40f, 94f), 22);
        }

        internal static void RefreshVillageFences(Transform valley)
        {
            Transform layer = valley?.Find(LayerName);
            if (layer == null) return;
            RemoveChild(layer, "Fences");
            PlaceFences(layer);
            ConvertPackMaterials(layer.Find("Fences"));
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void ReplaceVillageFences(Transform valley)
        {
            Transform village = valley.Find("RiversideVillage/UniformWoodenHamlet")
                ?? valley.Find("RiversideVillage/VillageHousesPackVillage");
            if (village == null) return;
            for (int i = village.childCount - 1; i >= 0; i--)
                if (village.GetChild(i).name.StartsWith("VillageFence_", StringComparison.Ordinal))
                    village.GetChild(i).gameObject.SetActive(false);
        }

        private static void ApplyFarmSurfaceMaterials(Transform layer)
        {
            EnsureMaterialFolder();
            Child(layer, FarmSurfaceName);
            Material grass = GetMaterial("PT_FarmPlotGrass", "PT_Ground_Grass_Green_01.png",
                new Color(0.94f, 1f, 0.92f), Vector2.one);
            Material drySoil = GetMaterial("PT_FarmDrySoil", "PT_Ground_Generic_03.png",
                new Color(0.72f, 0.49f, 0.31f), Vector2.one);
            Material wetSoil = GetMaterial("PT_FarmWetSoil", "PT_Ground_Generic_03.png",
                new Color(0.37f, 0.32f, 0.28f), Vector2.one);
            Material softSoil = GetSoftGardenSoilMaterial();
            if (softSoil == null) RemoveChild(layer, FarmSurfaceName);
            Transform cropGardens = GameObject.Find("CropGardens")?.transform;
            if (cropGardens != null)
            {
                foreach (GardenSoilSurface oldSurface in cropGardens.GetComponentsInChildren<GardenSoilSurface>(true))
                    if (oldSurface.transform != cropGardens) UnityEngine.Object.DestroyImmediate(oldSurface);
                if (softSoil != null)
                {
                    GardenSoilSurface surface = cropGardens.GetComponent<GardenSoilSurface>();
                    if (surface == null) surface = cropGardens.gameObject.AddComponent<GardenSoilSurface>();
                    surface.Configure(softSoil);
                }
                foreach (Transform garden in cropGardens)
                {
                    FarmPlot[] plots = garden.GetComponentsInChildren<FarmPlot>(true);
                    if (plots.Length == 0) continue;
                    foreach (FarmPlot plot in plots)
                    {
                        SetPlotGround(plot.transform, "UntilledVisual", grass, softSoil != null);
                        SetPlotGround(plot.transform, "TilledVisual", drySoil, softSoil != null);
                        SetPlotGround(plot.transform, "SeededVisual", drySoil, softSoil != null);
                        SetPlotGround(plot.transform, "WateredVisual", wetSoil, softSoil != null);
                        SetPlotGround(plot.transform, "GrowingVisual", drySoil, softSoil != null);
                        SetPlotGround(plot.transform, "ReadyToHarvestVisual", drySoil, softSoil != null);
                    }
                }
            }
            string[] farmBeds = { "ActiveFieldCenter/PumpkinField", "ActiveFieldCenter/VegetableBeds",
                "OrchardNorth/WheatField", "EntranceSouth/DirtRoad" };
            Transform farmZone = GameObject.Find("FarmZone")?.transform;
            foreach (string path in farmBeds)
            {
                Renderer renderer = farmZone?.Find(path)?.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = drySoil;
            }
            Transform oldEntrance = GameObject.Find("LiteFarmPackArt")?.transform.Find("WarmDirtEntrance");
            if (oldEntrance != null) oldEntrance.gameObject.SetActive(false);

        }

        internal static void RefreshFarmSurfaceMaterials(Transform valley)
        {
            Transform layer = valley?.Find(LayerName);
            if (layer == null) return;
            RemoveChild(layer, "FarmSurfaceV2");
            RemoveChild(layer, "FarmSurfaceV3");
            RemoveChild(layer, "FarmSurfaceV4");
            RemoveChild(layer, FarmSurfaceName);
            ApplyFarmSurfaceMaterials(layer);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Farm Restoration/Refresh Seamless Garden Soil")]
        private static void RefreshGardenSoilInScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Debug.LogWarning("Open FarmDemo in Edit Mode before refreshing the garden soil.");
                return;
            }
            Transform valley = GameObject.Find("IllustratedValleyMap")?.transform;
            if (valley == null || valley.Find(LayerName) == null)
            {
                Debug.LogWarning("The illustrated valley or Polytope environment is missing; garden soil was not changed.");
                return;
            }
            RefreshFarmSurfaceMaterials(valley);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        internal static Material FarmDirtMaterial()
        {
            EnsureMaterialFolder();
            return GetMaterial("PT_FarmDrySoil", "PT_Ground_Generic_03.png",
                new Color(0.72f, 0.49f, 0.31f), Vector2.one);
        }

        internal static Material PathMaterial()
        {
            EnsureMaterialFolder();
            return GetMaterial("PT_DirtPath", "PT_Ground_Generic_03.png",
                new Color(0.86f, 0.73f, 0.53f), new Vector2(1f, 8f));
        }

        private static void SetPlotGround(Transform plot, string stateName, Material material, bool useSoftSoil)
        {
            Renderer renderer = plot.Find(stateName + "/Ground")?.GetComponent<Renderer>();
            if (renderer == null) return;
            if (material != null) renderer.sharedMaterial = material;
            // The soft mesh draws every cultivated state; the old cube keeps its gameplay root only.
            renderer.enabled = !useSoftSoil && stateName != "UntilledVisual";
        }

        private static Material GetSoftGardenSoilMaterial()
        {
            Shader shader = Shader.Find("FarmRestoration/SoftGardenSoil");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "PT_Ground_Generic_03.png");
            if (shader == null || texture == null)
            {
                Debug.LogWarning("Soft garden soil shader or texture is unavailable; keeping the existing soil tiles.");
                return null;
            }
            string path = MaterialFolder + "/PT_SoftGardenSoil.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(0.73f, 0.62f, 0.49f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void FenceLine(GameObject prefab, Transform parent, Vector2 a, Vector2 b, int count)
        {
            if (prefab == null) return;
            Vector3 direction = new Vector3(b.x - a.x, 0f, b.y - a.y).normalized;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, (i + 0.5f) / count);
                GameObject fence = Place(prefab, parent, "PolytopeFence_" + parent.childCount,
                    new Vector3(point.x, ReferenceLandscapeSetup.GroundHeightAt(point.x, point.y), point.y), 1f, 0f, true);
                Bounds bounds = RendererBounds(fence);
                Vector3 localAxis = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
                fence.transform.rotation = Quaternion.FromToRotation(localAxis, direction);
                float length = Mathf.Max(bounds.size.x, bounds.size.z);
                if (length > 0.01f) fence.transform.localScale *= Vector2.Distance(a, b) / count / length;
            }
        }

        private static GameObject Load(string relative) => AssetDatabase.LoadAssetAtPath<GameObject>(Nature + relative + ".prefab");

        private static GameObject Place(GameObject prefab, Transform parent, string name, Vector3 position, float scale, float yaw, bool keepColliders = false)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = Vector3.one * scale;
            if (!keepColliders)
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            return instance;
        }

        private static Bounds RendererBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        internal static void ConvertPackMaterials(Transform root)
        {
            EnsureMaterialFolder();
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Dictionary<Material, Material> cache = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                    if (!cache.TryGetValue(source, out Material converted))
                    {
                        string path = AssetDatabase.GetAssetPath(source);
                        string id = AssetDatabase.AssetPathToGUID(path);
                        string convertedPath = MaterialFolder + "/PT_" + id + ".mat";
                        converted = AssetDatabase.LoadAssetAtPath<Material>(convertedPath);
                        if (converted == null)
                        {
                            converted = new Material(lit);
                            AssetDatabase.CreateAsset(converted, convertedPath);
                        }
                        converted.shader = lit;
                        Texture map = source.HasProperty("_BaseTexture") ? source.GetTexture("_BaseTexture") : null;
                        if (map == null && source.HasProperty("_MainTex")) map = source.GetTexture("_MainTex");
                        converted.SetTexture("_BaseMap", map);
                        Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                        if (map == null && source.HasProperty("_Wood1color")) color = source.GetColor("_Wood1color");
                        converted.SetColor("_BaseColor", color);
                        bool foliage = source.name.Contains("Grass") || source.name.Contains("Leaf")
                            || source.name.Contains("Leaves") || source.name.Contains("Foliage") || source.name.Contains("Poppy")
                            || source.name.Contains("Shrub");
                        converted.SetFloat("_AlphaClip", foliage ? 1f : 0f);
                        converted.SetFloat("_Cutoff", 0.35f);
                        converted.SetFloat("_Cull", foliage ? 0f : 2f);
                        if (foliage) converted.EnableKeyword("_ALPHATEST_ON");
                        else converted.DisableKeyword("_ALPHATEST_ON");
                        EditorUtility.SetDirty(converted);
                        cache[source] = converted;
                    }
                    materials[i] = converted;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetMaterial(string name, string textureName, Color tint, Vector2 tiling)
        {
            Texture2D texture = textureName == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + textureName);
            if (textureName != null && texture == null) return null;
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", name == "PT_ClearRiver" ? 0.82f : 0.10f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "PolytopeNature");
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}

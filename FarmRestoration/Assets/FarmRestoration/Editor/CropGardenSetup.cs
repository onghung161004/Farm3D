using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Keeps three stable 3x3 save-ID groups inside one visible 27-plot garden.</summary>
    public static class CropGardenSetup
    {
        private const string FarmDemo = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string PlotPrefabPath = "Assets/FarmRestoration/Prefabs/FarmPlot.prefab";
        private const string RanchPack = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/";
        private const string LayoutMarker = "SharedGardenLayoutV3";
        private const string WoodFencePrefab = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleUpgrade()
        {
            EditorApplication.delayCall += UpgradeWhenReady;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += UpgradeWhenReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += UpgradeWhenReady;
            };
        }

        private static void UpgradeWhenReady()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != FarmDemo) return;
            Transform root = GameObject.Find("CropGardens")?.transform;
            if (root == null || root.Find(LayoutMarker) != null) return;
            if (ApplySharedLayout(scene, root)) EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Farm Restoration/Build Shared 27-Plot Garden")]
        public static void BuildSharedGarden()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != FarmDemo) return;
            Transform root = GameObject.Find("CropGardens")?.transform;
            if (root == null) { CreateGardens(); return; }
            if (ApplySharedLayout(scene, root)) EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Farm Restoration/Create 3x3 Harvest Gardens")]
        public static void CreateGardens()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject plotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlotPrefabPath);
            if (scene.path != FarmDemo || plotPrefab == null)
            {
                EditorUtility.DisplayDialog("Garden setup unavailable", "Open FarmDemo and make sure the FarmPlot prefab exists before running this command.", "OK");
                return;
            }

            GameObject oldPlot = GameObject.Find("FarmPlot");
            if (oldPlot != null) oldPlot.SetActive(false);
            HideDecorativeField("WestCropField");
            HideDecorativeField("EastWateredField");
            HideDecorativeField("WheatPatch");
            Transform root = GetOrCreateRoot(scene, "CropGardens").transform;
            ClearChildren(root);

            // Technical groups keep the original plot names and save IDs.
            CreateGarden(plotPrefab, root, "PumpkinGarden", CropType.Pumpkin, CoherentFarmsteadSetup.PumpkinCenter, "food_Pumpkin");
            CreateGarden(plotPrefab, root, "CarrotGarden", CropType.Carrot, CoherentFarmsteadSetup.CarrotCenter, "food_Carrot");
            CreateGarden(plotPrefab, root, "TomatoGarden", CropType.Tomato, CoherentFarmsteadSetup.TomatoCenter, "food_Tomato");
            ApplySharedLayout(scene, root);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Created one fenced garden with 27 independent, selectable-crop plots.", root.gameObject);
        }

        private static bool ApplySharedLayout(Scene scene, Transform root)
        {
            string[] names = { "PumpkinGarden", "CarrotGarden", "TomatoGarden" };
            Vector3[] centers = { CoherentFarmsteadSetup.PumpkinCenter, CoherentFarmsteadSetup.CarrotCenter,
                CoherentFarmsteadSetup.TomatoCenter };
            for (int i = 0; i < names.Length; i++)
            {
                Transform group = root.Find(names[i]);
                if (group == null || group.GetComponentsInChildren<FarmPlot>(true).Length != 9) return false;
            }
            GameObject plotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlotPrefabPath);
            if (plotPrefab == null) return false;
            for (int i = 0; i < names.Length; i++)
            {
                Transform group = root.Find(names[i]);
                FarmPlot[] plots = group.GetComponentsInChildren<FarmPlot>(true);
                Vector3 oldCenter = Vector3.zero;
                foreach (FarmPlot plot in plots) oldCenter += plot.transform.position;
                oldCenter /= plots.Length;
                group.position += centers[i] - oldCenter;
                for (int child = group.childCount - 1; child >= 0; child--)
                    if (group.GetChild(child).name.EndsWith(" Sign")) Object.DestroyImmediate(group.GetChild(child).gameObject);
            }
            for (int i = 0; i < 3; i++)
            {
                Transform expansion = root.Find("ExpansionGarden" + i);
                if (expansion == null)
                    expansion = CreateExpansionGarden(root, plotPrefab, i);
                FarmPlot[] expansionPlots = expansion.GetComponentsInChildren<FarmPlot>(true);
                if (expansionPlots.Length != 9) return false;
                Vector3 currentCenter = Vector3.zero;
                foreach (FarmPlot plot in expansionPlots) currentCenter += plot.transform.position;
                currentCenter /= expansionPlots.Length;
                expansion.position += ExpansionCenter(i) - currentCenter;
                foreach (FarmPlot plot in expansionPlots)
                    plot.ConfigureRequiredGarden(i);
            }
            Transform oldV1 = root.Find("SharedGardenLayoutV1");
            if (oldV1 != null) Object.DestroyImmediate(oldV1.gameObject);
            Transform oldV2 = root.Find("SharedGardenLayoutV2");
            if (oldV2 != null) Object.DestroyImmediate(oldV2.gameObject);
            Transform oldLayout = root.Find(LayoutMarker);
            if (oldLayout != null) Object.DestroyImmediate(oldLayout.gameObject);
            Transform shared = new GameObject(LayoutMarker).transform;
            shared.SetParent(root, false);
            Transform fences = new GameObject("GardenWoodFences").transform;
            fences.SetParent(shared, false);
            CreateSign(shared, "Shared Garden", new Vector3(4.4f, 0f, 5f));
            // The west side has a two-metre entrance connecting to the main farm trail.
            CreateFence(fences, new Vector3(8f, 0f, -3.5f), 8, true, "SouthFence_");
            CreateFence(fences, new Vector3(8f, 0f, 9.5f), 8, true, "NorthFence_");
            CreateFence(fences, new Vector3(12f, 0f, 3f), 13, false, "EastFence_");
            CreateFence(fences, new Vector3(4f, 0f, -0.5f), 6, false, "WestSouthFence_");
            CreateFence(fences, new Vector3(4f, 0f, 7.5f), 4, false, "WestNorthFence_");
            CreateSign(shared, "Expansion Garden", new Vector3(4.4f, 0f, -16.8f));
            CreateFence(fences, new Vector3(8f, 0f, -17.5f), 8, true, "ExpansionSouth_");
            CreateFence(fences, new Vector3(8f, 0f, -4.5f), 8, true, "ExpansionNorth_");
            CreateFence(fences, new Vector3(12f, 0f, -11f), 13, false, "ExpansionEast_");
            CreateFence(fences, new Vector3(4f, 0f, -14.5f), 6, false, "ExpansionWestSouth_");
            CreateFence(fences, new Vector3(4f, 0f, -6.5f), 4, false, "ExpansionWestNorth_");
            Material wood = WoodFenceMaterial();
            foreach (Renderer renderer in fences.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = wood;
            for (int i = 0; i < 3; i++)
                CreatePurchaseSign(shared, i, new Vector3(4.5f, 0f, ExpansionCenter(i).z));
            CoherentFarmsteadSetup.RefreshGardenApproach(scene);
            PolytopeNatureSetup.RefreshFarmSurfaceMaterials(GameObject.Find("IllustratedValleyMap")?.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static Transform CreateExpansionGarden(Transform root, GameObject plotPrefab, int index)
        {
            Transform garden = new GameObject("ExpansionGarden" + index).transform;
            garden.SetParent(root, false);
            Vector3 center = ExpansionCenter(index);
            int plotIndex = 0;
            for (int row = -1; row <= 1; row++)
            for (int column = -1; column <= 1; column++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(plotPrefab, garden);
                instance.name = "ExpansionPlot_" + plotIndex++;
                instance.transform.position = center + new Vector3(column * 1.25f, 0f, row * 1.25f);
                instance.transform.localScale = Vector3.one * 0.28f;
                FarmPlot plot = instance.GetComponent<FarmPlot>();
                plot.ConfigureRequiredGarden(index);
                plot.ConfigureCropType(CropType.Pumpkin);
                ApplyReliablePlotMaterials(instance.transform, CropType.Pumpkin);
                AddCropVisual(instance.transform, "GrowingVisual", "food_Pumpkin", 0.10f);
                AddCropVisual(instance.transform, "ReadyToHarvestVisual", "food_Pumpkin", 0.18f);
            }
            return garden;
        }

        private static Vector3 ExpansionCenter(int index) => new Vector3(8f, 0.08f, -14.5f + index * 3.75f);

        private static void CreatePurchaseSign(Transform parent, int index, Vector3 position)
        {
            GameObject sign = new GameObject("UnlockGarden" + index);
            sign.transform.SetParent(parent, false);
            sign.transform.position = new Vector3(position.x,
                ReferenceLandscapeSetup.GroundHeightAt(position.x, position.z) + 1.3f, position.z);
            TextMesh text = sign.AddComponent<TextMesh>();
            text.text = "Area " + (index + 1) + "\n30 coins [E]";
            text.characterSize = 0.12f;
            text.fontSize = 52;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            BoxCollider collider = sign.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2.2f, 2.2f, 0.8f);
            sign.AddComponent<GardenPurchasePoint>().Configure(index);
        }

        private static void CreateGarden(GameObject plotPrefab, Transform root, string gardenName, CropType cropType, Vector3 center, string cropPrefabName)
        {
            Transform garden = new GameObject(gardenName).transform;
            garden.SetParent(root, false);

            int index = 0;
            for (int row = -1; row <= 1; row++)
            for (int column = -1; column <= 1; column++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(plotPrefab, garden);
                instance.name = cropType + "Plot_" + index++;
                instance.transform.position = center + new Vector3(column * 1.25f, 0f, row * 1.25f);
                instance.transform.localScale = Vector3.one * 0.28f;
                FarmPlot plot = instance.GetComponent<FarmPlot>();
                plot.ConfigureCropType(cropType);
                ApplyReliablePlotMaterials(instance.transform, cropType);
                AddCropVisual(instance.transform, "GrowingVisual", cropPrefabName, 0.10f);
                AddCropVisual(instance.transform, "ReadyToHarvestVisual", cropPrefabName, 0.18f);
            }
        }

        private static void AddCropVisual(Transform plot, string stateName, string prefabName, float scale)
        {
            Transform state = plot.Find(stateName);
            GameObject cropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RanchPack + prefabName + ".prefab");
            if (state == null || cropPrefab == null) return;
            foreach (Transform child in state)
            {
                if (child.name.StartsWith("Crop")) Object.DestroyImmediate(child.gameObject);
            }
            GameObject crop = (GameObject)PrefabUtility.InstantiatePrefab(cropPrefab, state);
            crop.name = "" + prefabName;
            crop.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            crop.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
            crop.transform.localScale = Vector3.one * scale;
            foreach (Collider collider in crop.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Color cropColor = prefabName == "food_Pumpkin" ? new Color(0.95f, 0.28f, 0.04f)
                : prefabName == "food_Carrot" ? new Color(1f, 0.43f, 0.06f)
                : new Color(0.92f, 0.10f, 0.10f);
            foreach (Renderer renderer in crop.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = GetMaterial("Crop_" + prefabName, cropColor);
        }

        private static void ApplyReliablePlotMaterials(Transform plot, CropType cropType)
        {
            SetGroundMaterial(plot, "UntilledVisual", GetMaterial("GardenGrass", new Color(0.24f, 0.52f, 0.16f)));
            Renderer untilledGround = plot.Find("UntilledVisual/Ground")?.GetComponent<Renderer>();
            if (untilledGround != null) untilledGround.enabled = false;
            SetGroundMaterial(plot, "TilledVisual", GetMaterial("GardenTilledSoil", new Color(0.25f, 0.09f, 0.025f)));
            SetGroundMaterial(plot, "SeededVisual", GetMaterial("GardenSeededSoil", new Color(0.32f, 0.13f, 0.035f)));
            SetGroundMaterial(plot, "WateredVisual", GetMaterial("GardenWateredSoil", new Color(0.14f, 0.25f, 0.32f)));
            SetGroundMaterial(plot, "GrowingVisual", GetMaterial("GardenGrowingSoil", new Color(0.28f, 0.12f, 0.035f)));
            SetGroundMaterial(plot, "ReadyToHarvestVisual", GetMaterial("GardenReadySoil", new Color(0.31f, 0.14f, 0.045f)));
        }

        private static void SetGroundMaterial(Transform plot, string stateName, Material material)
        {
            Transform ground = plot.Find(stateName + "/Ground");
            Renderer renderer = ground == null ? null : ground.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static Material GetMaterial(string materialName, Color color)
        {
            const string folder = "Assets/FarmRestoration/Materials/Garden";
            if (!AssetDatabase.IsValidFolder("Assets/FarmRestoration/Materials")) AssetDatabase.CreateFolder("Assets/FarmRestoration", "Materials");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "Garden");
            string path = folder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material WoodFenceMaterial()
        {
            Material material = GetMaterial("GardenWoodFenceURP", new Color(0.4057f, 0.2684f, 0.1359f));
            material.SetTexture("_BaseMap", null);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateFence(Transform parent, Vector3 center, int count, bool horizontal, string prefix)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WoodFencePrefab);
            if (prefab == null) return;
            for (int i = 0; i < count; i++)
            {
                float offset = i - (count - 1) * 0.5f;
                GameObject fence = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                fence.name = prefix + i;
                Vector3 position = center + (horizontal ? Vector3.right : Vector3.forward) * offset;
                position.y = ReferenceLandscapeSetup.GroundHeightAt(position.x, position.z);
                fence.transform.position = position;
                Renderer[] renderers = fence.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);
                Vector3 axis = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
                fence.transform.rotation = Quaternion.FromToRotation(axis, horizontal ? Vector3.right : Vector3.forward);
                float length = Mathf.Max(bounds.size.x, bounds.size.z);
                if (length > 0.01f) fence.transform.localScale *= 1f / length;
            }
        }

        private static void CreateSign(Transform parent, string label, Vector3 position)
        {
            GameObject sign = new GameObject(label + " Sign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = position + new Vector3(0f, 1.3f, 0f);
            TextMesh text = sign.AddComponent<TextMesh>();
            text.text = label;
            text.characterSize = 0.22f;
            text.fontSize = 52;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
        }

        private static void HideDecorativeField(string name)
        {
            GameObject decorativeField = GameObject.Find(name);
            if (decorativeField != null) decorativeField.SetActive(false);
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects()) if (gameObject.name == name) return gameObject;
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

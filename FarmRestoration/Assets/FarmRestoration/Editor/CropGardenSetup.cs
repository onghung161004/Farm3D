using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Creates three 3x3, individually interactive crop gardens in FarmDemo.</summary>
    public static class CropGardenSetup
    {
        private const string FarmDemo = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string PlotPrefabPath = "Assets/FarmRestoration/Prefabs/FarmPlot.prefab";
        private const string RanchPack = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/";

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

            // Three separate directions from the player spawn: south-west, north, and south-east.
            CreateGarden(plotPrefab, root, "PumpkinGarden", CropType.Pumpkin, new Vector3(-12.0f, 0.08f, -6.5f), "food_Pumpkin");
            CreateGarden(plotPrefab, root, "CarrotGarden", CropType.Carrot, new Vector3(-1.5f, 0.08f, 8.0f), "food_Carrot");
            CreateGarden(plotPrefab, root, "TomatoGarden", CropType.Tomato, new Vector3(12.0f, 0.08f, -6.5f), "food_Tomato");

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Created 27 independent harvest plots: 9 pumpkin, 9 carrot, and 9 tomato.", root.gameObject);
        }

        private static void CreateGarden(GameObject plotPrefab, Transform root, string gardenName, CropType cropType, Vector3 center, string cropPrefabName)
        {
            Transform garden = new GameObject(gardenName).transform;
            garden.SetParent(root, false);
            CreateSign(garden, gardenName.Replace("Garden", " Garden"), center + new Vector3(0f, 0f, 3.0f));

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
            material.color = color;
            return material;
        }

        private static void CreateFence(Transform parent, Vector3 center, int count, bool horizontal, string prefix)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RanchPack + "Env_WoodFence_01.prefab");
            if (prefab == null) return;
            for (int i = 0; i < count; i++)
            {
                float offset = i - (count - 1) * 0.5f;
                GameObject fence = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                fence.name = prefix + i;
                fence.transform.SetPositionAndRotation(center + (horizontal ? Vector3.right : Vector3.forward) * offset, Quaternion.Euler(horizontal ? Vector3.zero : new Vector3(0f, 90f, 0f)));
                fence.transform.localScale = Vector3.one * 0.8f;
                foreach (Collider collider in fence.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
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

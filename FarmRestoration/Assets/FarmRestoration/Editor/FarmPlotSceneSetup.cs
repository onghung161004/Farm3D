using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class FarmPlotSceneSetup
    {
        private const string PrefabPath = "Assets/FarmRestoration/Prefabs/FarmPlot.prefab";

        [MenuItem("Tools/Farm Restoration/Add Farm Plot To Current Scene")]
        public static void AddFarmPlotToCurrentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            FarmPlot existingPlot = FindFarmPlotInScene(activeScene);
            if (existingPlot != null)
            {
                Selection.activeGameObject = existingPlot.gameObject;
                Debug.Log("FarmPlot already exists; the current scene was left unchanged.", existingPlot);
                return;
            }

            GameObject prefab = GetOrCreatePrefab();
            if (prefab == null)
            {
                return;
            }

            GameObject plotRoot = PrefabUtility.InstantiatePrefab(prefab, activeScene) as GameObject;
            Undo.RegisterCreatedObjectUndo(plotRoot, "Add Farm Plot");
            plotRoot.transform.position = new Vector3(0f, 0.1f, 2f);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = plotRoot;
            Debug.Log("FarmPlot added to the current scene. Save the scene with Ctrl+S.", plotRoot);
        }

        [MenuItem("Tools/Farm Restoration/Create Farm Plot Prefab (If Missing)")]
        public static void CreateFarmPlotPrefabIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                Debug.Log("FarmPlot prefab already exists and was left unchanged: " + PrefabPath);
                return;
            }

            GetOrCreatePrefab();
        }

        private static FarmPlot FindFarmPlotInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FarmPlot[] plots = root.GetComponentsInChildren<FarmPlot>(true);
                if (plots.Length > 0)
                {
                    return plots[0];
                }
            }

            return null;
        }

        private static GameObject GetOrCreatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject temporaryPlot = CreateFarmPlotHierarchy();
            prefab = PrefabUtility.SaveAsPrefabAsset(temporaryPlot, PrefabPath);
            Object.DestroyImmediate(temporaryPlot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FarmPlot prefab created at " + PrefabPath, prefab);
            return prefab;
        }

        private static GameObject CreateFarmPlotHierarchy()
        {
            GameObject plotRoot = new GameObject("FarmPlot");
            BoxCollider collider = plotRoot.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 1f, 4f);

            FarmPlot plot = plotRoot.AddComponent<FarmPlot>();
            GameObject untilled = CreateStateVisual(plotRoot.transform, "UntilledVisual", new Color(0.30f, 0.55f, 0.24f), 0.15f);
            GameObject tilled = CreateStateVisual(plotRoot.transform, "TilledVisual", new Color(0.28f, 0.14f, 0.07f), 0.15f);
            GameObject seeded = CreateStateVisual(plotRoot.transform, "SeededVisual", new Color(0.22f, 0.10f, 0.04f), 0.15f);
            AddSeedMarkers(seeded.transform, Color.black, 0.15f);
            GameObject watered = CreateStateVisual(plotRoot.transform, "WateredVisual", new Color(0.12f, 0.20f, 0.30f), 0.15f);
            GameObject growing = CreateStateVisual(plotRoot.transform, "GrowingVisual", new Color(0.28f, 0.14f, 0.07f), 0.15f);
            AddCrop(growing.transform, new Color(0.20f, 0.70f, 0.18f), 0.65f);
            GameObject ready = CreateStateVisual(plotRoot.transform, "ReadyToHarvestVisual", new Color(0.28f, 0.14f, 0.07f), 0.15f);
            AddCrop(ready.transform, new Color(0.95f, 0.72f, 0.12f), 1f);
            plot.ConfigureVisuals(untilled, tilled, seeded, watered, growing, ready);
            return plotRoot;
        }

        private static GameObject CreateStateVisual(Transform parent, string name, Color color, float height)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Ground";
            soil.transform.SetParent(root.transform, false);
            soil.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            soil.transform.localScale = new Vector3(4f, height, 4f);
            soil.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name + "Material", color);
            return root;
        }

        private static void AddSeedMarkers(Transform parent, Color color, float y)
        {
            for (int index = -1; index <= 1; index++)
            {
                GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                seed.name = "Seed";
                seed.transform.SetParent(parent, false);
                seed.transform.localPosition = new Vector3(index, y + 0.08f, 0f);
                seed.transform.localScale = Vector3.one * 0.18f;
                seed.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SeedMaterial", color);
            }
        }

        private static void AddCrop(Transform parent, Color color, float height)
        {
            for (int index = -1; index <= 1; index++)
            {
                GameObject crop = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                crop.name = "Crop";
                crop.transform.SetParent(parent, false);
                crop.transform.localPosition = new Vector3(index, 0.15f + height * 0.5f, 0f);
                crop.transform.localScale = new Vector3(0.28f, height * 0.5f, 0.28f);
                crop.GetComponent<Renderer>().sharedMaterial = CreateMaterial("CropMaterial", color);
            }
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader) { name = name, color = color };
            return material;
        }
    }
}

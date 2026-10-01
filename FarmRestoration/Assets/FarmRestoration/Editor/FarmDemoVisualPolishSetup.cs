using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class FarmDemoVisualPolishSetup
    {
        private const string FarmDemoScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string MaterialFolder = "Assets/FarmRestoration/Materials/Generated";

        [MenuItem("Tools/Farm Restoration/Add Player Tool Visuals (Safe)")]
        public static void AddPlayerToolVisuals()
        {
            if (!TryGetFarmDemoScene(out Scene scene)) return;
            GameObject player = FindByName(scene, "Player");
            PlayerToolController controller = player != null ? player.GetComponent<PlayerToolController>() : null;
            if (controller == null)
            {
                Debug.LogError("PlayerToolController is required. Run Configure FarmDemo Tools and Interaction first.");
                return;
            }

            Transform holder = player.transform.Find("ToolHolder");
            if (holder == null)
            {
                GameObject holderObject = new GameObject("ToolHolder");
                Undo.RegisterCreatedObjectUndo(holderObject, "Create Player Tool Holder");
                holderObject.transform.SetParent(player.transform, false);
                holderObject.transform.localPosition = new Vector3(0.48f, 0.25f, 0.34f);
                holderObject.transform.localRotation = Quaternion.Euler(25f, 0f, -18f);
                holder = holderObject.transform;
            }
            CreateMissingToolModels(holder);

            PlayerToolVisual visual = player.GetComponent<PlayerToolVisual>();
            if (visual == null) visual = Undo.AddComponent<PlayerToolVisual>(player);
            Undo.RecordObject(visual, "Configure Player Tool Visuals");
            visual.Configure(controller, holder);
            EditorUtility.SetDirty(visual);
            MarkDirty(scene, player);
            Debug.Log("Player now visibly holds the selected low-poly tool. Keys 1-4 swap it; successful E interactions bob it.", player);
        }

        [MenuItem("Tools/Farm Restoration/Polish Farm Plot Visuals (Safe)")]
        public static void PolishFarmPlotVisuals()
        {
            if (!TryGetFarmDemoScene(out Scene scene)) return;
            FarmPlot plot = Object.FindFirstObjectByType<FarmPlot>();
            if (plot == null)
            {
                Debug.LogError("No FarmPlot was found in FarmDemo. Add one before polishing.");
                return;
            }

            if (plot.transform.Find("VisualPolish") != null)
            {
                Selection.activeGameObject = plot.gameObject;
                Debug.Log("FarmPlot visual polish already exists; scene was left unchanged.", plot);
                return;
            }

            GameObject polishRoot = new GameObject("VisualPolish");
            Undo.RegisterCreatedObjectUndo(polishRoot, "Polish Farm Plot Visuals");
            polishRoot.transform.SetParent(plot.transform, false);
            AddPlotBorder(polishRoot.transform);
            ImproveState(plot.transform, "UntilledVisual", new Color(0.42f, 0.78f, 0.24f), PrimitiveType.Cylinder, new Color(1f, 0.84f, 0.15f), 0.48f);
            ImproveState(plot.transform, "TilledVisual", new Color(0.30f, 0.12f, 0.035f), PrimitiveType.Cube, new Color(0.72f, 0.38f, 0.13f), 0.22f);
            ImproveState(plot.transform, "SeededVisual", new Color(0.17f, 0.07f, 0.02f), PrimitiveType.Sphere, new Color(1f, 0.57f, 0.08f), 0.22f);
            ImproveState(plot.transform, "WateredVisual", new Color(0.04f, 0.22f, 0.42f), PrimitiveType.Cylinder, new Color(0.16f, 0.78f, 1f), 0.14f);
            ImproveState(plot.transform, "GrowingVisual", new Color(0.22f, 0.10f, 0.03f), PrimitiveType.Capsule, new Color(0.15f, 0.95f, 0.28f), 0.68f);
            ImproveState(plot.transform, "ReadyToHarvestVisual", new Color(0.32f, 0.15f, 0.03f), PrimitiveType.Capsule, new Color(1f, 0.82f, 0.05f), 1.04f);
            MarkDirty(scene, plot.gameObject);
            Selection.activeGameObject = plot.gameObject;
            Debug.Log("FarmPlot states now have stronger colour and shape contrast.", plot);
        }

        private static bool TryGetFarmDemoScene(out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (scene.path == FarmDemoScenePath) return true;
            EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command again.", "OK");
            return false;
        }

        private static void CreateMissingToolModels(Transform holder)
        {
            if (holder.Find(PlayerToolVisualState.GetVisualRootName(FarmTool.Hoe)) == null)
            {
                Transform hoe = CreateToolRoot(holder, FarmTool.Hoe);
                CreatePart(hoe, PrimitiveType.Cylinder, "Handle", Vector3.zero, new Vector3(0.08f, 0.60f, 0.08f), new Color(0.34f, 0.16f, 0.05f));
                CreatePart(hoe, PrimitiveType.Cube, "Blade", new Vector3(0f, -0.30f, 0.10f), new Vector3(0.42f, 0.10f, 0.16f), new Color(0.25f, 0.28f, 0.30f));
            }

            if (holder.Find(PlayerToolVisualState.GetVisualRootName(FarmTool.Seeds)) == null)
            {
                Transform seeds = CreateToolRoot(holder, FarmTool.Seeds);
                CreatePart(seeds, PrimitiveType.Cube, "SeedBag", Vector3.zero, new Vector3(0.30f, 0.40f, 0.18f), new Color(0.92f, 0.58f, 0.14f));
                CreatePart(seeds, PrimitiveType.Sphere, "Seed", new Vector3(0.10f, 0.24f, 0f), Vector3.one * 0.13f, new Color(0.18f, 0.07f, 0.01f));
            }

            if (holder.Find(PlayerToolVisualState.GetVisualRootName(FarmTool.WateringCan)) == null)
            {
                Transform watering = CreateToolRoot(holder, FarmTool.WateringCan);
                CreatePart(watering, PrimitiveType.Cube, "Can", Vector3.zero, new Vector3(0.36f, 0.30f, 0.26f), new Color(0.10f, 0.45f, 0.85f));
                CreatePart(watering, PrimitiveType.Cylinder, "Spout", new Vector3(0.28f, 0.02f, 0f), new Vector3(0.10f, 0.30f, 0.10f), new Color(0.16f, 0.68f, 1f)).localRotation = Quaternion.Euler(0f, 0f, -65f);
            }

            if (holder.Find(PlayerToolVisualState.GetVisualRootName(FarmTool.Harvest)) == null)
            {
                Transform harvest = CreateToolRoot(holder, FarmTool.Harvest);
                CreatePart(harvest, PrimitiveType.Cylinder, "Handle", Vector3.zero, new Vector3(0.08f, 0.52f, 0.08f), new Color(0.34f, 0.16f, 0.05f));
                CreatePart(harvest, PrimitiveType.Cube, "SickleBlade", new Vector3(0.18f, -0.18f, 0f), new Vector3(0.38f, 0.12f, 0.10f), new Color(0.95f, 0.75f, 0.15f));
            }
        }

        private static Transform CreateToolRoot(Transform holder, FarmTool tool)
        {
            GameObject root = new GameObject(PlayerToolVisualState.GetVisualRootName(tool));
            Undo.RegisterCreatedObjectUndo(root, "Create Tool Model");
            root.transform.SetParent(holder, false);
            return root.transform;
        }

        private static Transform CreatePart(Transform parent, PrimitiveType primitive, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            Undo.RegisterCreatedObjectUndo(part, "Create Tool Part");
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = GetMaterial("Tool_" + ColorUtility.ToHtmlStringRGB(color), color);
            return part.transform;
        }

        private static void ImproveState(Transform plot, string stateName, Color soilColor, PrimitiveType markerType, Color markerColor, float markerHeight)
        {
            Transform state = plot.Find(stateName);
            if (state == null) return;
            Renderer[] renderers = state.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Undo.RecordObject(renderers[0], "Polish Farm Plot Material");
                renderers[0].sharedMaterial = GetMaterial(stateName + "_Soil", soilColor);
            }
            Transform marker = CreatePart(state, markerType, "PolishMarker", new Vector3(0f, 0.19f + markerHeight * 0.5f, 0f), new Vector3(0.32f, markerHeight, 0.32f), markerColor);
            if (markerType == PrimitiveType.Cube) marker.localScale = new Vector3(2.8f, 0.08f, 0.12f);
        }

        private static void AddPlotBorder(Transform parent)
        {
            Color wood = new Color(0.38f, 0.18f, 0.06f);
            CreatePart(parent, PrimitiveType.Cube, "BorderNorth", new Vector3(0f, 0.18f, 2.03f), new Vector3(4.25f, 0.18f, 0.16f), wood);
            CreatePart(parent, PrimitiveType.Cube, "BorderSouth", new Vector3(0f, 0.18f, -2.03f), new Vector3(4.25f, 0.18f, 0.16f), wood);
            CreatePart(parent, PrimitiveType.Cube, "BorderEast", new Vector3(2.03f, 0.18f, 0f), new Vector3(0.16f, 0.18f, 4.25f), wood);
            CreatePart(parent, PrimitiveType.Cube, "BorderWest", new Vector3(-2.03f, 0.18f, 0f), new Vector3(0.16f, 0.18f, 4.25f), wood);
        }

        private static Material GetMaterial(string materialName, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "Generated");
            }
            string path = MaterialFolder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = materialName, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                if (transform.name == name) return transform.gameObject;
            return null;
        }

        private static void MarkDirty(Scene scene, Object selection)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = selection;
        }
    }
}

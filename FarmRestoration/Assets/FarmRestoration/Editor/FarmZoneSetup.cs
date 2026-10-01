using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    public static class FarmZoneSetup
    {
        private const string FarmDemoScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string MaterialsFolder = "Assets/FarmRestoration/Materials/Generated";

        [MenuItem("Tools/Farm Restoration/Configure Farm Zone")]
        public static void ConfigureFarmZone()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemoScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command again.", "OK");
                return;
            }

            FarmZoneLayout layout = FarmZoneLayout.CreateDefault();
            if (layout.TotalObjectCount > FarmZoneLayout.MobileObjectBudget)
            {
                Debug.LogError("Farm Zone setup was stopped because its object plan exceeds the mobile budget.");
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                const string message = "Farm Zone setup needs the URP Lit shader, but Shader.Find(\"Universal Render Pipeline/Lit\") returned null. Ensure the project uses the Universal Render Pipeline and that the URP package is installed, then run setup again.";
                Debug.LogError(message);
                EditorUtility.DisplayDialog("Farm Zone setup unavailable", message, "OK");
                return;
            }

            Transform zone = GetOrCreateSceneRoot(scene, "FarmZone").transform;
            CreateCottageWest(zone);
            CreateActiveFieldCenter(zone);
            CreateMarketEast(zone);
            CreateOrchardNorth(zone);
            CreateEntranceSouth(zone);

            VerifyGeneratedObjectBudget(zone, layout);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = zone.gameObject;
            Debug.Log("Farm Zone configured safely. Re-running this command reuses FarmZone and its named area roots.", zone.gameObject);
        }

        private static void CreateCottageWest(Transform zone)
        {
            Transform area = GetOrCreateArea(zone, "CottageWest");

            CreatePart(area, PrimitiveType.Cube, "CottageWalls", new Vector3(-10f, 1.7f, 6.5f), new Vector3(5.0f, 3.2f, 3.8f), "CottageWall", new Color(0.93f, 0.88f, 0.72f));
            CreatePart(area, PrimitiveType.Cube, "CottageRoof", new Vector3(-10f, 3.65f, 6.5f), new Vector3(5.6f, 0.5f, 4.4f), "Roof", new Color(0.31f, 0.16f, 0.08f), new Vector3(0f, 0f, 8f));
            CreatePart(area, PrimitiveType.Cube, "Door", new Vector3(-10f, 1.15f, 4.52f), new Vector3(0.82f, 2.2f, 0.12f), "Wood", new Color(0.40f, 0.18f, 0.06f));
            CreatePart(area, PrimitiveType.Cube, "WindowLeft", new Vector3(-11.45f, 2.15f, 4.50f), new Vector3(0.9f, 0.9f, 0.10f), "Window", new Color(0.25f, 0.65f, 0.9f));
            CreatePart(area, PrimitiveType.Cube, "WindowRight", new Vector3(-8.55f, 2.15f, 4.50f), new Vector3(0.9f, 0.9f, 0.10f), "Window", new Color(0.25f, 0.65f, 0.9f));
            CreatePart(area, PrimitiveType.Cylinder, "Chimney", new Vector3(-11.65f, 4.45f, 7.5f), new Vector3(0.55f, 1.7f, 0.55f), "Brick", new Color(0.54f, 0.20f, 0.10f));
            CreateWell(area, new Vector3(-5.8f, 0f, 6f));
            CreateMarker(area, "GardenerLocation", new Vector3(-5.0f, 0.05f, 4.4f), new Color(0.25f, 0.75f, 0.35f));
            CreateFencePen(area, new Vector3(-13.2f, 0f, 2.7f), 4, 3);
        }

        private static void CreateActiveFieldCenter(Transform zone)
        {
            Transform area = GetOrCreateArea(zone, "ActiveFieldCenter");

            CreateSoilBed(area, "PumpkinField", new Vector3(-0.5f, 0.08f, 7.0f), new Vector3(5.3f, 0.15f, 4.4f));
            for (int x = -2; x <= 2; x += 2)
            for (int z = 5; z <= 8; z += 2)
            {
                CreatePart(area, PrimitiveType.Sphere, "Pumpkin_" + x + "_" + z, new Vector3(x, 0.40f, z), Vector3.one * 0.62f, "Pumpkin", new Color(0.94f, 0.35f, 0.05f));
                CreatePart(area, PrimitiveType.Cylinder, "PumpkinStem_" + x + "_" + z, new Vector3(x, 0.75f, z), new Vector3(0.12f, 0.22f, 0.12f), "Leaf", new Color(0.18f, 0.45f, 0.08f));
            }

            CreateSoilBed(area, "VegetableBeds", new Vector3(5.0f, 0.08f, 5.5f), new Vector3(3.7f, 0.15f, 5.8f));
            for (int x = 4; x <= 6; x += 2)
            for (int z = 3; z <= 8; z += 2)
                CreatePart(area, PrimitiveType.Capsule, "Vegetable_" + x + "_" + z, new Vector3(x, 0.52f, z), new Vector3(0.35f, 0.62f, 0.35f), "Leaf", new Color(0.18f, 0.70f, 0.18f));

            CreateMarker(area, "ActiveFarmPlotMarker", new Vector3(0f, 0.05f, 2f), new Color(1f, 0.84f, 0.1f));
            CreateFenceLine(area, new Vector3(-3.4f, 0f, 9.5f), 8, true);
        }

        private static void CreateMarketEast(Transform zone)
        {
            Transform area = GetOrCreateArea(zone, "MarketEast");

            Vector3 position = new Vector3(11.0f, 0f, 4.8f);
            CreatePart(area, PrimitiveType.Cube, "StallCounter", position + new Vector3(0f, 1.0f, 0f), new Vector3(3.6f, 1.1f, 1.35f), "Wood", new Color(0.45f, 0.23f, 0.08f));
            CreatePart(area, PrimitiveType.Cube, "StallCanopy", position + new Vector3(0f, 3.1f, 0f), new Vector3(4.2f, 0.24f, 2.3f), "Canopy", new Color(0.92f, 0.22f, 0.13f));
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                CreatePart(area, PrimitiveType.Cylinder, "StallPost_" + x + "_" + z, position + new Vector3(x * 1.8f, 1.6f, z * 0.85f), new Vector3(0.13f, 3.1f, 0.13f), "Wood", new Color(0.45f, 0.23f, 0.08f));
            CreatePart(area, PrimitiveType.Cube, "CrateA", position + new Vector3(-2.6f, 0.5f, 0.5f), Vector3.one, "Crate", new Color(0.76f, 0.46f, 0.14f));
            CreatePart(area, PrimitiveType.Cube, "CrateB", position + new Vector3(-2.3f, 1.1f, 0.5f), Vector3.one * 0.75f, "Crate", new Color(0.76f, 0.46f, 0.14f));
            CreateMarker(area, "MerchantLocation", position + new Vector3(0f, 0.05f, -2.0f), new Color(0.98f, 0.72f, 0.12f));
        }

        private static void CreateOrchardNorth(Transform zone)
        {
            Transform area = GetOrCreateArea(zone, "OrchardNorth");

            int treeIndex = 0;
            for (int x = -12; x <= -4; x += 4)
            for (int z = 12; z <= 16; z += 4)
                CreateAppleTree(area, new Vector3(x, 0f, z), treeIndex++);

            CreateSoilBed(area, "WheatField", new Vector3(5.5f, 0.06f, 13.4f), new Vector3(7.0f, 0.12f, 5.8f));
            for (int x = 3; x <= 8; x += 1)
            for (int z = 11; z <= 15; z += 2)
                CreatePart(area, PrimitiveType.Cylinder, "Wheat_" + x + "_" + z, new Vector3(x, 0.56f, z), new Vector3(0.13f, 1.0f, 0.13f), "Wheat", new Color(0.95f, 0.72f, 0.14f));

            CreateRockCluster(area, new Vector3(11f, 0f, 14f), "RockClusterEast");
            CreateRockCluster(area, new Vector3(-15f, 0f, 13f), "RockClusterWest");
        }

        private static void CreateEntranceSouth(Transform zone)
        {
            Transform area = GetOrCreateArea(zone, "EntranceSouth");

            CreatePart(area, PrimitiveType.Cube, "DirtRoad", new Vector3(0f, 0.025f, -8.8f), new Vector3(12f, 0.05f, 7.5f), "Dirt", new Color(0.63f, 0.44f, 0.22f));
            CreateFenceLine(area, new Vector3(-6.0f, 0f, -5.3f), 7, false, "EntranceWest");
            CreateFenceLine(area, new Vector3(6.0f, 0f, -5.3f), 7, false, "EntranceEast");
            for (int x = -3; x <= 3; x += 3)
                CreateHayBale(area, new Vector3(x, 0f, -11.4f), "HayBale_" + x);
            CreateRockCluster(area, new Vector3(-8f, 0f, -11.5f), "RockClusterWest");
            CreateRockCluster(area, new Vector3(8f, 0f, -11.5f), "RockClusterEast");
        }

        private static Transform GetOrCreateArea(Transform zone, string name)
        {
            Transform existing = zone.Find(name);
            if (existing != null) return existing;
            GameObject area = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(area, "Create Farm Zone Area");
            area.transform.SetParent(zone, false);
            return area.transform;
        }

        private static GameObject GetOrCreateSceneRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            GameObject created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create Farm Zone");
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static void CreateWell(Transform parent, Vector3 position)
        {
            CreatePart(parent, PrimitiveType.Cylinder, "WellBase", position + new Vector3(0f, 0.42f, 0f), new Vector3(1.55f, 0.8f, 1.55f), "Stone", new Color(0.44f, 0.46f, 0.43f));
            CreatePart(parent, PrimitiveType.Cylinder, "WellWater", position + new Vector3(0f, 0.85f, 0f), new Vector3(1.20f, 0.08f, 1.20f), "Water", new Color(0.12f, 0.58f, 0.90f));
            CreatePart(parent, PrimitiveType.Cylinder, "WellPostLeft", position + new Vector3(-0.9f, 1.65f, 0f), new Vector3(0.12f, 1.7f, 0.12f), "Wood", new Color(0.45f, 0.23f, 0.08f));
            CreatePart(parent, PrimitiveType.Cylinder, "WellPostRight", position + new Vector3(0.9f, 1.65f, 0f), new Vector3(0.12f, 1.7f, 0.12f), "Wood", new Color(0.45f, 0.23f, 0.08f));
            CreatePart(parent, PrimitiveType.Cube, "WellRoof", position + new Vector3(0f, 2.65f, 0f), new Vector3(2.6f, 0.25f, 1.6f), "Roof", new Color(0.31f, 0.16f, 0.08f));
        }

        private static void CreateAppleTree(Transform parent, Vector3 position, int index)
        {
            string prefix = "AppleTree_" + index;
            CreatePart(parent, PrimitiveType.Cylinder, prefix + "_Trunk", position + new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 2.5f, 0.42f), "Wood", new Color(0.34f, 0.17f, 0.05f));
            CreatePart(parent, PrimitiveType.Sphere, prefix + "_Leaves", position + new Vector3(0f, 3.2f, 0f), new Vector3(2.6f, 2.2f, 2.6f), "TreeLeaf", new Color(0.18f, 0.55f, 0.12f));
            CreatePart(parent, PrimitiveType.Sphere, prefix + "_Apple", position + new Vector3(0.65f, 3.15f, -0.55f), Vector3.one * 0.34f, "Apple", new Color(0.88f, 0.08f, 0.06f));
        }

        private static void CreateSoilBed(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            CreatePart(parent, PrimitiveType.Cube, name, position, scale, "Soil", new Color(0.30f, 0.13f, 0.04f));
        }

        private static void CreateFencePen(Transform parent, Vector3 center, int width, int depth)
        {
            CreateFenceLine(parent, center + new Vector3(0f, 0f, depth * 0.5f), width, true, "PenNorth");
            CreateFenceLine(parent, center + new Vector3(0f, 0f, -depth * 0.5f), width, true, "PenSouth");
            CreateFenceLine(parent, center + new Vector3(width * 0.5f, 0f, 0f), depth, false, "PenEast");
            CreateFenceLine(parent, center + new Vector3(-width * 0.5f, 0f, 0f), depth, false, "PenWest");
        }

        private static void CreateFenceLine(Transform parent, Vector3 center, int segments, bool horizontal)
        {
            CreateFenceLine(parent, center, segments, horizontal, "Fence");
        }

        private static void CreateFenceLine(Transform parent, Vector3 center, int segments, bool horizontal, string prefix)
        {
            for (int index = 0; index <= segments; index++)
            {
                float offset = index - segments * 0.5f;
                Vector3 postPosition = center + (horizontal ? Vector3.right : Vector3.forward) * offset;
                CreatePart(parent, PrimitiveType.Cylinder, prefix + "_Post_" + index, postPosition + new Vector3(0f, 0.7f, 0f), new Vector3(0.1f, 1.4f, 0.1f), "Wood", new Color(0.45f, 0.23f, 0.08f));
                if (index < segments)
                {
                    Vector3 railPosition = postPosition + (horizontal ? Vector3.right : Vector3.forward) * 0.5f + new Vector3(0f, 0.9f, 0f);
                    Vector3 railScale = horizontal ? new Vector3(1.05f, 0.10f, 0.10f) : new Vector3(0.10f, 0.10f, 1.05f);
                    CreatePart(parent, PrimitiveType.Cube, prefix + "_Rail_" + index, railPosition, railScale, "Wood", new Color(0.45f, 0.23f, 0.08f));
                }
            }
        }

        private static void CreateHayBale(Transform parent, Vector3 position, string name)
        {
            CreatePart(parent, PrimitiveType.Cylinder, name, position + new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), "Hay", new Color(0.95f, 0.70f, 0.14f), new Vector3(90f, 0f, 0f));
        }

        private static void CreateRockCluster(Transform parent, Vector3 position, string name)
        {
            CreatePart(parent, PrimitiveType.Sphere, name + "_Large", position + new Vector3(0f, 0.35f, 0f), new Vector3(1.5f, 0.7f, 1.1f), "Stone", new Color(0.43f, 0.45f, 0.42f));
            CreatePart(parent, PrimitiveType.Sphere, name + "_Small", position + new Vector3(0.85f, 0.22f, 0.45f), new Vector3(0.65f, 0.45f, 0.60f), "Stone", new Color(0.43f, 0.45f, 0.42f));
        }

        private static void CreateMarker(Transform parent, string name, Vector3 position, Color color)
        {
            CreatePart(parent, PrimitiveType.Cylinder, name, position + new Vector3(0f, 0.06f, 0f), new Vector3(0.65f, 0.12f, 0.65f), name, color);
        }

        private static Transform CreatePart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, string materialName, Color color)
        {
            Transform existing = parent.Find(name);
            GameObject part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(part, "Create Farm Zone Prop");
                part.name = name;
                part.transform.SetParent(parent, true);
            }
            part.transform.position = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogError("Farm Zone prop '" + name + "' is missing a Renderer and could not be configured.");
                return part.transform;
            }
            renderer.sharedMaterial = GetMaterial(materialName, color);
            return part.transform;
        }

        private static Transform CreatePart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, string materialName, Color color, Vector3 eulerAngles)
        {
            Transform part = CreatePart(parent, type, name, position, scale, materialName, color);
            part.rotation = Quaternion.Euler(eulerAngles);
            return part;
        }

        private static Material GetMaterial(string materialName, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "Generated");
            string path = MaterialsFolder + "/Zone_" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("Farm Zone could not create material '" + materialName + "': URP Lit shader is unavailable. Install/enable URP and rerun Configure Farm Zone.");
                return null;
            }
            material = new Material(shader) { name = "Zone_" + materialName, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void VerifyGeneratedObjectBudget(Transform zone, FarmZoneLayout layout)
        {
            int actualTotal = 0;
            foreach (Transform area in zone)
            {
                int actualAreaCount = CountDescendants(area);
                actualTotal += actualAreaCount;
                Debug.Log("Farm Zone area " + area.name + ": " + actualAreaCount + " generated props.", area.gameObject);
            }

            if (actualTotal > FarmZoneLayout.MobileObjectBudget)
            {
                Debug.LogError("Farm Zone generated " + actualTotal + " props, exceeding the mobile budget of " + FarmZoneLayout.MobileObjectBudget + ". Reduce generated props before shipping.", zone.gameObject);
                return;
            }

            if (actualTotal != layout.TotalObjectCount)
                Debug.LogWarning("FarmZoneLayout declares " + layout.TotalObjectCount + " props, but the scene currently contains " + actualTotal + ". The editor verification uses the actual generated count for the mobile budget.", zone.gameObject);
        }

        private static int CountDescendants(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent)
                count += 1 + CountDescendants(child);
            return count;
        }
    }
}

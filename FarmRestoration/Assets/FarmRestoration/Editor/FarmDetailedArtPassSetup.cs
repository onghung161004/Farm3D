using FarmRestoration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Builds an original, reusable stylized art pass without touching gameplay objects.</summary>
    public static class FarmDetailedArtPassSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string MaterialsFolder = "Assets/FarmRestoration/Materials/Generated";
        private const string SkyTexturePath = "Assets/FarmRestoration/Textures/Generated/FarmSkyBackdrop.png";

        [MenuItem("Tools/Farm Restoration/Apply Detailed Art Pass")]
        public static void ApplyDetailedArtPass()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command again.", "OK");
                return;
            }

            Transform zone = FindRoot(scene, "FarmZone");
            if (zone == null)
            {
                EditorUtility.DisplayDialog("Create the farm zone first", "Run Tools > Farm Restoration > Configure Farm Zone, then apply the detailed art pass.", "OK");
                return;
            }

            int currentZoneCount = CountDescendants(zone);
            if (!FarmArtPassPlan.FitsMobileBudget(currentZoneCount, FarmArtPassPlan.AddedZonePropBudget))
            {
                Debug.LogError("Detailed Art Pass stopped: FarmZone has " + currentZoneCount + " objects, leaving insufficient room within the " + FarmZoneLayout.MobileObjectBudget + " mobile-object budget.", zone);
                return;
            }

            CreateSkyBackdrop(scene);
            Transform details = GetOrCreate(zone, "DetailedArt");
            CreateCottageDetails(details);
            CreateLandscapeDetails(details);
            UpgradePlayerVisual(scene);
            ConfigureWorldLighting();

            int finalZoneCount = CountDescendants(zone);
            if (finalZoneCount > FarmZoneLayout.MobileObjectBudget)
                Debug.LogError("Detailed Art Pass created " + finalZoneCount + " FarmZone objects, over the mobile budget. Revert with Ctrl+Z and report this error.", zone);
            else
                Debug.Log("Detailed Art Pass applied safely: " + finalZoneCount + "/" + FarmZoneLayout.MobileObjectBudget + " FarmZone objects. It can be re-run without duplicating named props.", zone);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = details.gameObject;
        }

        private static void CreateSkyBackdrop(Scene scene)
        {
            Transform root = FindRoot(scene, "SkyBackdrop");
            if (root == null)
            {
                GameObject created = new GameObject("SkyBackdrop");
                Undo.RegisterCreatedObjectUndo(created, "Create Sky Backdrop");
                SceneManager.MoveGameObjectToScene(created, scene);
                root = created.transform;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTexturePath);
            if (texture == null)
            {
                Debug.LogWarning("Sky image not found at " + SkyTexturePath + ". The art pass kept the existing sky colour.", root);
                return;
            }

            Transform quad = root.Find("FarmSkyBillboard");
            if (quad == null || quad.GetComponent<MeshFilter>() == null)
            {
                GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Undo.RegisterCreatedObjectUndo(primitive, "Create Sky Billboard");
                primitive.name = "FarmSkyBillboard";
                primitive.transform.SetParent(root, false);
                Object.DestroyImmediate(primitive.GetComponent<Collider>());
                quad = primitive.transform;
            }
            quad.position = new Vector3(0f, 21f, 56f);
            float aspect = texture.height > 0 ? (float)texture.width / texture.height : 1.777f;
            quad.localScale = new Vector3(62f, 62f / aspect, 1f);
            quad.rotation = Quaternion.identity;
            quad.GetComponent<Renderer>().sharedMaterial = GetSkyMaterial(texture);
        }

        private static void CreateCottageDetails(Transform details)
        {
            Transform cottage = GetOrCreate(details, "CottageDetailing");
            Color trim = new Color(0.96f, 0.94f, 0.82f);
            Color roofAccent = new Color(0.18f, 0.09f, 0.035f);
            Color flower = new Color(1f, 0.33f, 0.52f);
            Part(cottage, PrimitiveType.Cube, "FrontPorch", new Vector3(-10f, 0.28f, 4.12f), new Vector3(2.2f, 0.25f, 1.1f), "Porch", new Color(0.53f, 0.29f, 0.12f));
            Part(cottage, PrimitiveType.Cube, "RoofRidge", new Vector3(-10f, 4.08f, 6.5f), new Vector3(5.75f, 0.18f, 0.24f), "RoofAccent", roofAccent);
            Part(cottage, PrimitiveType.Cube, "WindowTrimLeft", new Vector3(-11.45f, 2.15f, 4.43f), new Vector3(1.14f, 1.14f, 0.06f), "Trim", trim);
            Part(cottage, PrimitiveType.Cube, "WindowTrimRight", new Vector3(-8.55f, 2.15f, 4.43f), new Vector3(1.14f, 1.14f, 0.06f), "Trim", trim);
            Part(cottage, PrimitiveType.Cylinder, "PorchPotLeft", new Vector3(-11.55f, 0.42f, 3.88f), new Vector3(0.32f, 0.45f, 0.32f), "Pot", new Color(0.75f, 0.24f, 0.10f));
            Part(cottage, PrimitiveType.Sphere, "PorchFlowerLeft", new Vector3(-11.55f, 0.82f, 3.88f), Vector3.one * 0.32f, "Flower", flower);
            Part(cottage, PrimitiveType.Cylinder, "PorchPotRight", new Vector3(-8.45f, 0.42f, 3.88f), new Vector3(0.32f, 0.45f, 0.32f), "Pot", new Color(0.75f, 0.24f, 0.10f));
            Part(cottage, PrimitiveType.Sphere, "PorchFlowerRight", new Vector3(-8.45f, 0.82f, 3.88f), Vector3.one * 0.32f, "Flower", flower);
        }

        private static void CreateLandscapeDetails(Transform details)
        {
            Transform landscape = GetOrCreate(details, "LandscapeClusters");
            Color bush = new Color(0.12f, 0.47f, 0.13f);
            Color bushLight = new Color(0.22f, 0.67f, 0.16f);
            Color stone = new Color(0.42f, 0.47f, 0.49f);
            Color petal = new Color(1f, 0.86f, 0.22f);
            Vector3[] bushes = {
                new Vector3(-14.5f, 0.55f, 8.3f), new Vector3(-13.1f, 0.55f, 10.0f), new Vector3(-2.9f, 0.55f, 12.8f),
                new Vector3(10.2f, 0.55f, 10.8f), new Vector3(13.0f, 0.55f, 12.2f), new Vector3(14.5f, 0.55f, 6.8f)
            };
            for (int i = 0; i < bushes.Length; i++)
            {
                Part(landscape, PrimitiveType.Sphere, "Bush_" + i + "A", bushes[i], new Vector3(1.35f, 1.1f, 1.15f), "Bush", bush);
                Part(landscape, PrimitiveType.Sphere, "Bush_" + i + "B", bushes[i] + new Vector3(0.55f, 0.22f, 0.18f), new Vector3(0.9f, 0.85f, 0.85f), "BushLight", bushLight);
            }
            Vector3[] rocks = { new Vector3(-16f, 0.25f, 4f), new Vector3(15.5f, 0.25f, 14.2f), new Vector3(12.8f, 0.2f, -2.5f), new Vector3(-9.5f, 0.2f, -8f) };
            for (int i = 0; i < rocks.Length; i++)
                Part(landscape, PrimitiveType.Sphere, "DetailRock_" + i, rocks[i], new Vector3(1.15f, 0.52f, 0.92f), "DetailStone", stone);
            Vector3[] flowers = { new Vector3(-7.2f, 0.2f, 2.3f), new Vector3(-6.5f, 0.2f, 2.7f), new Vector3(7.7f, 0.2f, 10.2f), new Vector3(8.4f, 0.2f, 10.7f), new Vector3(-3.5f, 0.2f, -3.8f), new Vector3(3.8f, 0.2f, -2.8f) };
            for (int i = 0; i < flowers.Length; i++)
            {
                Part(landscape, PrimitiveType.Cylinder, "FlowerStem_" + i, flowers[i] + new Vector3(0f, 0.18f, 0f), new Vector3(0.045f, 0.36f, 0.045f), "Stem", new Color(0.14f, 0.48f, 0.12f));
                Part(landscape, PrimitiveType.Sphere, "FlowerHead_" + i, flowers[i] + new Vector3(0f, 0.43f, 0f), Vector3.one * 0.18f, "Petal", petal);
            }
        }

        private static void UpgradePlayerVisual(Scene scene)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) return;
            Renderer original = player.GetComponent<Renderer>();
            if (original != null) original.enabled = false; // Controller, movement and tool components remain on Player.
            Transform visual = GetOrCreate(player.transform, "ChibiVisual");
            Part(visual, PrimitiveType.Capsule, "Body", new Vector3(0f, -0.12f, 0f), new Vector3(0.72f, 0.92f, 0.52f), "ChibiShirt", new Color(0.18f, 0.48f, 0.92f), true);
            Part(visual, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.86f, 0.02f), new Vector3(0.78f, 0.76f, 0.72f), "ChibiSkin", new Color(1f, 0.72f, 0.50f), true);
            Part(visual, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.14f, -0.08f), new Vector3(0.82f, 0.44f, 0.74f), "ChibiHair", new Color(0.23f, 0.09f, 0.025f), true);
            Part(visual, PrimitiveType.Capsule, "LegLeft", new Vector3(-0.22f, -0.82f, 0f), new Vector3(0.22f, 0.54f, 0.25f), "ChibiPants", new Color(0.10f, 0.17f, 0.38f), true);
            Part(visual, PrimitiveType.Capsule, "LegRight", new Vector3(0.22f, -0.82f, 0f), new Vector3(0.22f, 0.54f, 0.25f), "ChibiPants", new Color(0.10f, 0.17f, 0.38f), true);
        }

        private static void ConfigureWorldLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.63f, 0.76f, 0.88f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.66f, 0.82f, 0.91f);
            RenderSettings.fogDensity = 0.008f;
        }

        private static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, string material, Color colour, bool local = false)
        {
            Transform child = parent.Find(name);
            GameObject obj;
            if (child == null)
            {
                obj = GameObject.CreatePrimitive(type);
                Undo.RegisterCreatedObjectUndo(obj, "Create Detailed Farm Prop");
                obj.name = name;
                obj.transform.SetParent(parent, false);
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }
            else obj = child.gameObject;
            if (local) obj.transform.localPosition = position; else obj.transform.position = position;
            obj.transform.localScale = scale;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = GetLitMaterial(material, colour);
            return obj.transform;
        }

        private static Transform GetOrCreate(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            GameObject obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create Detailed Farm Group");
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root.transform;
            return null;
        }

        private static int CountDescendants(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent) count += 1 + CountDescendants(child);
            return count;
        }

        private static Material GetLitMaterial(string name, Color colour)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "Generated");
            string path = MaterialsFolder + "/Detail_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("URP Lit shader unavailable; cannot create " + name); return null; }
            material = new Material(shader) { name = "Detail_" + name, color = colour };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetSkyMaterial(Texture2D texture)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/FarmRestoration/Materials", "Generated");
            const string path = MaterialsFolder + "/Detail_SkyBackdrop.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) { Debug.LogError("URP Unlit shader unavailable; cannot create sky backdrop."); return null; }
                material = new Material(shader) { name = "Detail_SkyBackdrop" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            return material;
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>
    /// Creates the repeatable, primitive-only shell used by the FarmDemo scene.
    /// Run from Tools/Farm Restoration/Create FarmDemo Scene (If Missing).
    /// </summary>
    public static class FarmDemoSceneGenerator
    {
        private const string RootPath = "Assets/FarmRestoration";
        private const string ScenePath = RootPath + "/Scenes/FarmDemo.unity";
        private const string MaterialsPath = RootPath + "/Materials";

        [MenuItem("Tools/Farm Restoration/Create FarmDemo Scene (If Missing)")]
        public static void CreateFarmDemoIfMissing()
        {
            EnsureProjectFolders();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                Debug.Log("FarmDemo already exists and was left unchanged: " + ScenePath);
                return;
            }

            CreateFarmDemoScene();
        }

        [MenuItem("Tools/Farm Restoration/Overwrite FarmDemo Scene (Destructive)")]
        public static void OverwriteFarmDemo()
        {
            EnsureProjectFolders();

            if (!EditorUtility.DisplayDialog(
                    "Overwrite FarmDemo?",
                    "This permanently replaces Assets/FarmRestoration/Scenes/FarmDemo.unity. " +
                    "Player, plot, and UI objects in that scene will be lost.",
                    "Overwrite", "Cancel"))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }

            CreateFarmDemoScene();
        }

        private static void CreateFarmDemoScene()
        {
            Material groundMaterial = CreateOrUpdateMaterial(
                MaterialsPath + "/FarmGround.mat",
                new Color(0.30f, 0.55f, 0.24f, 1f));
            Material woodMaterial = CreateOrUpdateMaterial(
                MaterialsPath + "/FarmWood.mat",
                new Color(0.45f, 0.25f, 0.10f, 1f));
            Material soilMaterial = CreateOrUpdateMaterial(
                MaterialsPath + "/FarmSoil.mat",
                new Color(0.28f, 0.14f, 0.07f, 1f));

            if (groundMaterial == null || woodMaterial == null || soilMaterial == null)
            {
                Debug.LogError("FarmDemo scene was not created because its URP materials could not be prepared.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject bootstrap = new GameObject("FarmDemoBootstrap");
            GameObject environment = new GameObject("Environment");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FarmGround";
            ground.transform.SetParent(environment.transform);
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.82f, 1f);
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 14f, -14f);
            camera.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.72f, 0.90f, 1f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = bootstrap;
            Debug.Log("FarmDemo scene shell created at " + ScenePath);
        }

        private static void EnsureProjectFolders()
        {
            string[] folders =
            {
                "Scenes", "Scripts", "Prefabs", "Materials", "Models", "Textures",
                "Animations", "Audio", "UI", "Tests", "Tests/EditMode", "Editor"
            };

            EnsureAssetFolder(RootPath);
            foreach (string folder in folders)
            {
                EnsureAssetFolder(RootPath + "/" + folder);
            }

            AssetDatabase.Refresh();
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separatorIndex = folderPath.LastIndexOf('/');
            string parentPath = folderPath.Substring(0, separatorIndex);
            string folderName = folderPath.Substring(separatorIndex + 1);
            EnsureAssetFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }

        private static Material CreateOrUpdateMaterial(string assetPath, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("The URP Lit shader was not found. FarmDemo materials were not created.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}

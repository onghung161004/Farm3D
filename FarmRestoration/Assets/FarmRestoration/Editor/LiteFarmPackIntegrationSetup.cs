using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Places the imported Gridness prefabs into the existing playable FarmDemo scene.</summary>
    public static class LiteFarmPackIntegrationSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string Pack = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/";

        [MenuItem("Tools/Farm Restoration/Upgrade Farm Zone With Lite Farm Pack")]
        public static void UpgradeFarmZone()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }

            GameObject tree = Load("Tree");
            GameObject fence = Load("Fence_Middle");
            GameObject dirt = Load("Dirt");
            GameObject tillage = Load("Tillage_6x1");
            GameObject tomato = Load("Plant_Tomato_Large");
            GameObject plant = Load("Plant_Large");
            GameObject flower = Load("Flower");
            GameObject crate = Load("Tomato_Crate_Pack");
            GameObject waterCan = Load("WaterCan");
            if (tree == null || fence == null || dirt == null || tillage == null || tomato == null || plant == null || flower == null || crate == null || waterCan == null)
            {
                const string message = "Some Lite Farm Pack prefabs are missing. Reimport the pack, then run this command again.";
                Debug.LogError(message);
                EditorUtility.DisplayDialog("Lite Farm Pack is incomplete", message, "OK");
                return;
            }

            Transform root = GetOrCreateRoot(scene, "LiteFarmPackArt").transform;
            ClearChildren(root);

            // Dirt entrance and hand-worked tilled rows, positioned around the existing interactive plot.
            Place(dirt, root, "WarmDirtEntrance", new Vector3(0f, 0.02f, -8.7f), new Vector3(2.4f, 1f, 3.2f));
            Place(tillage, root, "TomatoBedWest", new Vector3(-0.6f, 0.02f, 7.0f), new Vector3(0.95f, 1f, 2.8f));
            Place(tillage, root, "TomatoBedEast", new Vector3(5.0f, 0.02f, 5.5f), new Vector3(0.85f, 1f, 2.5f), new Vector3(0f, 90f, 0f));

            for (int x = -2; x <= 2; x += 2)
            for (int z = 5; z <= 8; z += 2)
                Place(tomato, root, "Tomato_" + x + "_" + z, new Vector3(x, 0.03f, z), Vector3.one * 0.9f, new Vector3(0f, (x + z) * 13f, 0f));

            for (int x = 4; x <= 6; x += 2)
            for (int z = 3; z <= 8; z += 2)
                Place(plant, root, "Crop_" + x + "_" + z, new Vector3(x, 0.03f, z), Vector3.one * 0.8f, new Vector3(0f, (x - z) * 17f, 0f));

            // Orchard and a denser backdrop make the world feel enclosed from a third-person view.
            int index = 0;
            for (int x = -15; x <= 15; x += 5)
            {
                Place(tree, root, "BackdropTreeNorth_" + index, new Vector3(x, 0f, 18f + (index % 2)), Vector3.one * 1.25f, new Vector3(0f, index * 29f, 0f));
                index++;
            }
            for (int x = -12; x <= -4; x += 4)
            for (int z = 12; z <= 16; z += 4)
            {
                Place(tree, root, "OrchardTree_" + index, new Vector3(x, 0f, z), Vector3.one * 0.95f, new Vector3(0f, index * 37f, 0f));
                index++;
            }
            Place(tree, root, "WestShadeTree", new Vector3(-14.5f, 0f, 5.5f), Vector3.one * 1.15f, new Vector3(0f, 25f, 0f));
            Place(tree, root, "EastShadeTree", new Vector3(14.5f, 0f, 8f), Vector3.one * 1.1f, new Vector3(0f, -35f, 0f));

            CreateFenceRun(fence, root, "NorthFence", new Vector3(0f, 0f, 10.4f), 13, true);
            CreateFenceRun(fence, root, "WestFence", new Vector3(-7.2f, 0f, 3f), 8, false);
            CreateFenceRun(fence, root, "EastFence", new Vector3(8.7f, 0f, 4f), 8, false);
            Place(crate, root, "MarketTomatoCrates", new Vector3(10f, 0f, 4.6f), Vector3.one * 1.15f, new Vector3(0f, -20f, 0f));
            Place(waterCan, root, "WaterCanByField", new Vector3(3f, 0.04f, 2.8f), Vector3.one * 0.85f, new Vector3(0f, 25f, 0f));

            int flowerIndex = 0;
            for (int x = -13; x <= 13; x += 2)
            {
                Place(flower, root, "FlowerNorth_" + flowerIndex, new Vector3(x, 0.02f, 10.9f + (flowerIndex % 2) * 0.45f), Vector3.one * 0.65f, new Vector3(0f, flowerIndex * 23f, 0f));
                flowerIndex++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Lite Farm Pack art integrated. Gameplay objects were not changed; save the scene with Ctrl+S.", root.gameObject);
        }

        private static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Pack + name + ".prefab");

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects()) if (gameObject.name == name) return gameObject;
            GameObject created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create Lite Farm Pack Art");
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static void CreateFenceRun(GameObject prefab, Transform root, string prefix, Vector3 center, int count, bool horizontal)
        {
            for (int i = 0; i < count; i++)
            {
                float offset = i - (count - 1) * 0.5f;
                Vector3 position = center + (horizontal ? Vector3.right : Vector3.forward) * offset;
                Place(prefab, root, prefix + "_" + i, position, Vector3.one, horizontal ? Vector3.zero : new Vector3(0f, 90f, 0f));
            }
        }

        private static void Place(GameObject prefab, Transform parent, string name, Vector3 position, Vector3 scale, Vector3 euler = default)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            instance.transform.localScale = scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }
}

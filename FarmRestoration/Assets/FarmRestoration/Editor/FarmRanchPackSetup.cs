using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Builds a compact American ranch around the existing playable farm systems.</summary>
    public static class FarmRanchPackSetup
    {
        private const string FarmDemo = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string Pack = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/";

        [MenuItem("Tools/Farm Restoration/Build American Ranch With Farm Ranch Pack")]
        public static void BuildRanch()
        {
            if (PolytopeNatureSetup.HasPack())
            {
                CoherentFarmsteadSetup.Rebuild();
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemo)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null || !HasRequiredPrefabs())
            {
                EditorUtility.DisplayDialog("Farm Ranch Pack unavailable", "The required Farm Ranch Pack prefabs or URP Lit shader are missing. Wait for import to finish, then retry.", "OK");
                return;
            }

            HideOldArt();
            Transform root = GetOrCreateRoot(scene, "AmericanFarmRanch").transform;
            ClearChildren(root);

            // West homestead: American farmhouse, well and fenced garden.
            Place("Bld_FarmerHouse", root, "Farmhouse", new Vector3(-15.5f, 0f, 6.5f), new Vector3(0.62f, 0.62f, 0.62f), new Vector3(0f, 28f, 0f));
            Place("Env_Well_01", root, "FarmhouseWell", new Vector3(-9.5f, 0f, 6.0f), Vector3.one * 0.8f, Vector3.zero);
            Place("Bld_ChickenCoop", root, "ChickenCoop", new Vector3(-16.0f, 0f, -1.2f), Vector3.one * 0.7f, new Vector3(0f, 15f, 0f));
            CreateFenceLine(root, "HomeFenceNorth", new Vector3(-14.2f, 0f, 10.4f), 8, true);
            CreateFenceLine(root, "HomeFenceWest", new Vector3(-20.0f, 0f, 4.8f), 7, false);

            // North working ranch: iconic red barn, silo, greenhouse and windmill.
            Place("Bld_Barn_01", root, "RedBarn", new Vector3(-2.5f, 0f, 18.0f), new Vector3(0.72f, 0.72f, 0.72f), Vector3.zero);
            Place("Bld_Silo_01", root, "BarnSilo", new Vector3(5.2f, 0f, 18.5f), Vector3.one * 0.72f, Vector3.zero);
            Place("Bld_GreenMouse", root, "Greenhouse", new Vector3(13.8f, 0f, 16.0f), new Vector3(0.68f, 0.68f, 0.68f), new Vector3(0f, -10f, 0f));
            Place("Bld_FarmMill_01", root, "Windmill", new Vector3(20.0f, 0f, 18.0f), Vector3.one * 0.65f, new Vector3(0f, -20f, 0f));
            CreateFenceLine(root, "BarnFenceSouth", new Vector3(0.5f, 0f, 12.0f), 9, true);
            CreateFenceLine(root, "BarnFenceEast", new Vector3(8.7f, 0f, 15.0f), 6, false);

            // Central crop fields frame, but never replace, the existing interactive FarmPlot.
            Place("Env_FarmLand_10", root, "WestCropField", new Vector3(-2.8f, 0f, 6.6f), new Vector3(1.15f, 1f, 1.05f), Vector3.zero);
            Place("Env_FarmLand_09_Watered", root, "EastWateredField", new Vector3(5.6f, 0f, 6.0f), new Vector3(1.0f, 1f, 1.0f), new Vector3(0f, 90f, 0f));
            Place("Env_Wheat", root, "WheatPatch", new Vector3(6.8f, 0f, 10.0f), Vector3.one, Vector3.zero);
            Place("Prop_WateringCan_01", root, "FieldWateringCan", new Vector3(2.4f, 0.04f, 3.0f), Vector3.one * 0.8f, new Vector3(0f, 30f, 0f));
            CreateFenceLine(root, "FieldFenceWest", new Vector3(-7.2f, 0f, 6.5f), 7, false);
            CreateFenceLine(root, "FieldFenceEast", new Vector3(9.4f, 0f, 6.5f), 7, false);

            // East farm-stand and delivery props.
            Place("Bld_StoreBuilding_01", root, "FarmStand", new Vector3(17.0f, 0f, 3.4f), Vector3.one * 0.68f, new Vector3(0f, -75f, 0f));
            Place("Prop_Wheelbarrow", root, "MarketWheelbarrow", new Vector3(11.8f, 0f, 2.5f), Vector3.one * 0.85f, new Vector3(0f, -30f, 0f));
            Place("Prop_WoodenCrates_01", root, "MarketCrates", new Vector3(13.4f, 0f, 2.6f), Vector3.one * 0.8f, new Vector3(0f, 20f, 0f));
            Place("Prop_Haystack_01", root, "MarketHay", new Vector3(14.4f, 0f, 1.0f), Vector3.one * 0.8f, Vector3.zero);
            CreateFenceLine(root, "MarketFenceNorth", new Vector3(15.7f, 0f, 8.3f), 6, true);

            // Entrance lane, perimeter, orchard and small foliage clusters.
            Place("Env_GrassLand_Stright", root, "EntrancePath", new Vector3(0f, 0f, -7.5f), new Vector3(1.5f, 1f, 2.6f), Vector3.zero);
            CreateFenceLine(root, "SouthFenceWest", new Vector3(-6.6f, 0f, -4.3f), 6, false);
            CreateFenceLine(root, "SouthFenceEast", new Vector3(6.6f, 0f, -4.3f), 6, false);
            int treeIndex = 0;
            for (int x = -18; x <= 18; x += 6) Place("Env_Tree_0" + ((treeIndex % 4) + 1), root, "NorthTree_" + treeIndex++, new Vector3(x, 0f, 23f), new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0f, treeIndex * 23f, 0f));
            for (int z = 3; z <= 14; z += 4) Place("Env_Tree_0" + ((treeIndex % 4) + 1), root, "WestTree_" + treeIndex++, new Vector3(-18f, 0f, z), Vector3.one, new Vector3(0f, treeIndex * 19f, 0f));
            for (int i = 0; i < 8; i++) Place("Env_Bush_0" + ((i % 2) + 1), root, "Bush_" + i, new Vector3(-13f + i * 3.4f, 0f, 10.2f), Vector3.one * 0.8f, new Vector3(0f, i * 31f, 0f));

            int converted = LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(root, lit, "ConvertedFarmRanch");
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("American ranch built with Farm Ranch Pack. Converted " + converted + " material(s) to URP and kept Player/FarmPlot gameplay intact.", root.gameObject);
        }

        private static bool HasRequiredPrefabs() => Load("Bld_FarmerHouse") != null && Load("Bld_Barn_01") != null && Load("Env_FarmLand_10") != null && Load("Env_WoodFence_01") != null;
        private static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Pack + name + ".prefab");

        private static void HideOldArt()
        {
            string[] roots = { "LiteFarmPackArt", "DetailedArt", "FarmZone" };
            foreach (string rootName in roots)
            {
                GameObject oldRoot = GameObject.Find(rootName);
                if (oldRoot != null) oldRoot.SetActive(false);
            }
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects()) if (gameObject.name == name) return gameObject;
            GameObject created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create American Farm Ranch");
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static void CreateFenceLine(Transform root, string prefix, Vector3 center, int count, bool horizontal)
        {
            for (int i = 0; i < count; i++)
            {
                float offset = i - (count - 1) * 0.5f;
                Vector3 position = center + (horizontal ? Vector3.right : Vector3.forward) * offset;
                Place("Env_WoodFence_01", root, prefix + "_" + i, position, Vector3.one, horizontal ? Vector3.zero : new Vector3(0f, 90f, 0f));
            }
        }

        private static void Place(string prefabName, Transform parent, string instanceName, Vector3 position, Vector3 scale, Vector3 euler)
        {
            GameObject prefab = Load(prefabName);
            if (prefab == null) return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            instance.transform.localScale = scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
    }
}

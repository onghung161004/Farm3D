using FarmRestoration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmRestoration.Editor
{
    /// <summary>Adds the progression interactables to the existing valley without rebuilding the farm.</summary>
    public static class FarmProgressionSceneSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string RootName = "VillageGameplayV2";
        private const string NpcPrefab = "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Brown(Free Version).prefab";
        private const string NpcBluePrefab = "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Blue(Free Version).prefab";
        private const string NpcGreenPrefab = "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Green(Free Version).prefab";
        private const string NpcYellowPrefab = "Assets/Stylized NPC - Peasant Nolant/Prefabs/Peasant Nolant Yellow(Free Version).prefab";
        private const string CratePrefab = "Assets/Pandazole_Ultimate_Pack/Pandazole Farm Ranch Pack/Prefabs/Prop_WoodenCrates_01.prefab";
        private const string FencePrefab = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        private const string HousePrefab = "Assets/VillagePack/OldHouse/OldHousePrefab.prefab";

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            EditorApplication.delayCall += ApplyWhenReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ApplyWhenReady;
            };
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += ApplyWhenReady;
        }

        [MenuItem("Tools/Farm Restoration/Add Village Progression Gameplay")]
        public static void ApplyWhenReady()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return;
            FarmHud hud = Object.FindAnyObjectByType<FarmHud>();
            GameObject valley = GameObject.Find("IllustratedValleyMap");
            if (hud == null || valley == null) return;

            bool changed = false;
            if (hud.GetComponent<FarmProgression>() == null)
            {
                hud.gameObject.AddComponent<FarmProgression>();
                changed = true;
            }
            if (hud.transform.Find("EconomyStatus") == null)
            {
                CreateEconomyHud(hud.transform);
                changed = true;
            }
            if (valley.transform.Find(RootName) == null)
            {
                Transform previous = valley.transform.Find("VillageGameplayV1");
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                BuildActors(valley.transform);
                changed = true;
            }
            Transform gameplay = valley.transform.Find(RootName);
            if (gameplay != null && gameplay.Find("VillageWarehouse") == null)
            {
                CreateWarehouse(gameplay);
                changed = true;
            }
            if (gameplay != null && gameplay.Find("VillageResidentsV1") == null)
            {
                CreateResidents(gameplay);
                changed = true;
            }
            if (!changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Village progression ready: timed crops, trader orders, production stations and repair points.", valley);
        }

        private static void BuildActors(Transform valley)
        {
            Transform root = new GameObject(RootName).transform;
            root.SetParent(valley, false);

            Transform trader = CreateActor(root, "VillageTrader", -55f, 49f, 1.3f);
            trader.gameObject.AddComponent<VillageOrderNpc>();
            GameObject npc = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefab);
            if (npc != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(npc, trader);
                visual.name = "TraderVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 55f, 0f);
                visual.transform.localScale = Vector3.one * 1.5f;
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                PolytopeNatureSetup.ConvertPackMaterials(visual.transform);
            }
            Sign(trader, "VILLAGE ORDERS", 2.5f);

            CreateStation(root, "VillageKitchen", -59f, 66f, CropType.Pumpkin, "KITCHEN: SOUP / SAUCE", true);
            CreateStation(root, "VillageJuicePress", -77f, 52f, CropType.Carrot, "PRESS: CARROT JUICE");
            CreateWarehouse(root);
            CreateResidents(root);

            Transform houseRepair = CreateActor(root, "RepairVillageHouse", -83f, 33f, 1.4f);
            Transform houseGarden = new GameObject("RestoredHouseGarden").transform;
            houseGarden.SetParent(houseRepair, false);
            PlaceDecoration(FencePrefab, houseGarden, new Vector3(-0.9f, 0f, 1.5f), 0.8f);
            PlaceDecoration(FencePrefab, houseGarden, new Vector3(0.9f, 0f, 1.5f), 0.8f);
            Transform oldHouse = valley.Find("RiversideVillage/UniformWoodenHamlet/WoodenHouseSouth");
            GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HousePrefab);
            if (oldHouse != null && housePrefab != null)
            {
                GameObject restoredHouse = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab, houseGarden);
                restoredHouse.name = "RestoredVillageHouse";
                restoredHouse.transform.SetPositionAndRotation(oldHouse.position, oldHouse.rotation);
                restoredHouse.transform.localScale = oldHouse.localScale;
                foreach (Collider collider in restoredHouse.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(restoredHouse.transform,
                    Shader.Find("Universal Render Pipeline/Lit"), "ConvertedVillageHouses");
            }
            houseGarden.gameObject.SetActive(false);
            houseRepair.gameObject.AddComponent<VillageRepairPoint>().Configure(0, 30, "village house", houseGarden.gameObject, oldHouse == null ? null : oldHouse.gameObject);
            Sign(houseRepair, "RESTORE HOUSE", 2.2f);

            Transform bridgeRepair = CreateActor(root, "RepairBridgeApproach", -53f, 42f, 1.4f);
            Transform bridgeRail = new GameObject("RestoredBridgeApproach").transform;
            bridgeRail.SetParent(bridgeRepair, false);
            PlaceDecoration(FencePrefab, bridgeRail, new Vector3(-1.4f, 0f, 1.7f), 0.8f);
            PlaceDecoration(FencePrefab, bridgeRail, new Vector3(1.4f, 0f, 1.7f), 0.8f);
            bridgeRail.gameObject.SetActive(false);
            bridgeRepair.gameObject.AddComponent<VillageRepairPoint>().Configure(1, 45, "bridge approach", bridgeRail.gameObject);
            Sign(bridgeRepair, "RESTORE BRIDGE", 2.2f);
        }

        private static Transform CreateActor(Transform parent, string name, float x, float z, float radius)
        {
            Transform actor = new GameObject(name).transform;
            actor.SetParent(parent, false);
            actor.position = new Vector3(x, ReferenceLandscapeSetup.GroundHeightAt(x, z) + 0.05f, z);
            BoxCollider collider = actor.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.size = new Vector3(radius * 2f, 2f, radius * 2f);
            collider.isTrigger = true;
            return actor;
        }

        private static void CreateStation(Transform parent, string name, float x, float z, CropType crop, string label, bool tomatoRecipe = false)
        {
            Transform station = CreateActor(parent, name, x, z, 1.2f);
            station.gameObject.AddComponent<FarmProductionStation>().Configure(crop, 2, tomatoRecipe);
            PlaceDecoration(CratePrefab, station, Vector3.zero, 0.65f);
            Sign(station, label, 2.2f);
        }

        private static void CreateWarehouse(Transform parent)
        {
            Transform warehouse = CreateActor(parent, "VillageWarehouse", -63f, 45f, 1.15f);
            warehouse.gameObject.AddComponent<VillageWarehouse>();
            PlaceDecoration(CratePrefab, warehouse, Vector3.zero, 0.75f);
            Sign(warehouse, "WAREHOUSE  [E]", 2.2f);
        }

        private static void CreateResidents(Transform parent)
        {
            Transform residents = new GameObject("VillageResidentsV1").transform;
            residents.SetParent(parent, false);
            CreateResident(residents, "Maya", -74f, 58f, NpcGreenPrefab,
                "Hello! It is good to see you in the village.",
                "How are you?", new[] { "I am doing well, thank you!", "The gardens make every day brighter." },
                "What is new?", new[] { "The well has been busy this morning.", "Everyone is hoping for a good harvest." },
                new[] { Vector3.zero, new Vector3(-2f, 0f, 1f), new Vector3(-1f, 0f, 3f) });
            CreateResident(residents, "Bram", -71f, 68f, NpcBluePrefab,
                "Welcome! Take your time and enjoy the village.",
                "How is the village?", new[] { "It is quiet, and that suits me.", "Neighbors always lend a hand here." },
                "Any plans today?", new[] { "I may visit the well later.", "Then I will check on the old houses." },
                new[] { Vector3.zero, new Vector3(2f, 0f, 0f), new Vector3(1f, 0f, -2f) });
            CreateResident(residents, "Lina", -72f, 39f, NpcYellowPrefab,
                "Hi there! Did you cross the bridge safely?",
                "How are you?", new[] { "Quite well! The fresh air helps.", "I like walking by the village path." },
                "Tell me about here", new[] { "The bridge brings visitors to us.", "The gardens are just across the river." },
                new[] { Vector3.zero, new Vector3(-2f, 0f, 1f), new Vector3(-1f, 0f, 3f) });
        }

        private static void CreateResident(Transform parent, string name, float x, float z, string prefabPath,
            string greeting, string firstTopic, string[] firstReplies, string secondTopic, string[] secondReplies,
            Vector3[] stops)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("Village resident prefab is missing: " + prefabPath);
                return;
            }
            Transform actor = CreateActor(parent, "Resident_" + name, x, z, 0.85f);
            actor.gameObject.AddComponent<VillageResidentNpc>().Configure(name, greeting, firstTopic, firstReplies,
                secondTopic, secondReplies);
            actor.gameObject.AddComponent<VillageTraderRoutine>().ConfigureStops(stops);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, actor);
            visual.name = "ResidentVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 1.5f;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            PolytopeNatureSetup.ConvertPackMaterials(visual.transform);
        }

        private static void PlaceDecoration(string path, Transform parent, Vector3 position, float scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            model.transform.localPosition = position;
            model.transform.localScale = Vector3.one * scale;
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            PolytopeNatureSetup.ConvertPackMaterials(model.transform);
        }

        private static void Sign(Transform parent, string text, float height)
        {
            GameObject sign = new GameObject("Sign", typeof(TextMesh));
            sign.transform.SetParent(parent, false);
            sign.transform.localPosition = new Vector3(0f, height, 0f);
            TextMesh mesh = sign.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 54;
            mesh.characterSize = 0.10f;
            mesh.color = new Color(1f, 0.92f, 0.69f);
        }

        private static void CreateEconomyHud(Transform hud)
        {
            GameObject panel = new GameObject("EconomyStatus", typeof(RectTransform), typeof(Image), typeof(FarmEconomyHud));
            panel.transform.SetParent(hud, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(420f, 105f);
            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.06f, 0.12f, 0.14f, 0.96f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("EconomyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 8f);
            textRect.offsetMax = new Vector2(-10f, -7f);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 20f;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            panel.GetComponent<FarmEconomyHud>().Configure(label);
        }
    }
}

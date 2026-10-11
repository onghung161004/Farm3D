using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmRestoration.Editor
{
    /// <summary>Builds a small riverside village with the imported Village Houses Pack.</summary>
    public static class FarmVillageHousesSetup
    {
        private const string FarmDemoScene = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string OldHousePath = "Assets/VillagePack/OldHouse/OldHousePrefab.prefab";
        private const string WellPath = "Assets/VillagePack/Well/Well.prefab";
        private const string FencePath = "Assets/VillagePack/Fence/FencePrefab.prefab";
        private const string VillageRootName = "UniformWoodenHamlet";
        private const string SpacingMarker = "VillageLevelGroundV4";

        [InitializeOnLoadMethod]
        private static void ScheduleExistingVillageUpgrade()
        {
            EditorApplication.delayCall += UpgradeExistingVillage;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += UpgradeExistingVillage;
            };
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += UpgradeExistingVillage;
        }

        private static void UpgradeExistingVillage()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != FarmDemoScene) return;
            Transform landscape = GameObject.Find("IllustratedValleyMap")?.transform;
            Transform host = landscape?.Find("RiversideVillage");
            if (host == null || host.Find(VillageRootName + "/" + SpacingMarker) != null
                || !TryLoadPrefabs(out Prefabs prefabs)) return;
            if (Shader.Find("Universal Render Pipeline/Lit") == null) return;
            if (!ReferenceLandscapeSetup.RefreshTerrainHeights(landscape)) return;
            Transform village = RebuildUnder(host, ReferenceLandscapeSetup.GroundHeightAt,
                Shader.Find("Universal Render Pipeline/Lit"), prefabs);
            if (village == null) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Farm Restoration/Build Village With Village Houses Pack")]
        public static void BuildVillage()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FarmDemoScene)
            {
                EditorUtility.DisplayDialog("Open FarmDemo first", "Open Assets/FarmRestoration/Scenes/FarmDemo.unity, then run this command.", "OK");
                return;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null || !TryLoadPrefabs(out Prefabs prefabs) || !VillageHouseLayout.Validate(VillageHouseLayout.Homes))
            {
                EditorUtility.DisplayDialog("Village Houses Pack unavailable", "The VillagePack prefabs or URP Lit shader are missing. Wait for import to finish, then retry.", "OK");
                return;
            }

            Transform landscape = GameObject.Find("IllustratedValleyMap")?.transform;
            if (landscape == null)
            {
                EditorUtility.DisplayDialog("Build the map first", "Run Build Illustrated 3D Valley Map once so the village can align with the terrain.", "OK");
                return;
            }

            Transform villageHost = FindOrCreateChild(landscape, "RiversideVillage");
            if (!ReferenceLandscapeSetup.RefreshTerrainHeights(landscape)) return;
            Transform village = RebuildUnder(villageHost, ReferenceLandscapeSetup.GroundHeightAt, lit, prefabs);
            if (village == null) return;
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = village.gameObject;
            Debug.Log("Village Houses Pack village built with " + village.childCount + " placed objects. Farming and player gameplay were not changed.", village.gameObject);
        }

        internal static Transform BuildVillageUnder(Transform host, Func<float, float, float> heightAt, Shader lit)
        {
            if (host == null || heightAt == null || lit == null || !TryLoadPrefabs(out Prefabs prefabs)) return null;
            return RebuildUnder(host, heightAt, lit, prefabs);
        }

        private static Transform RebuildUnder(Transform host, Func<float, float, float> heightAt, Shader lit, Prefabs prefabs)
        {
            if (!VillageHouseLayout.Validate(VillageHouseLayout.Homes))
            {
                Debug.LogError("Village layout has overlapping or obstructing house slots; existing village was kept.");
                return null;
            }
            RemoveChild(host, "VillageHousesPackVillage");
            RemoveChild(host, VillageRootName);
            RemoveChild(host, "VillageBarn");
            Transform windmill = host.Find("HillWindmill");
            if (windmill != null)
                windmill.position = new Vector3(-80f, heightAt(-80f, 12f), 12f);
            Transform village = new GameObject(VillageRootName).transform;
            village.SetParent(host, false);

            foreach (VillageHouseSlot slot in VillageHouseLayout.Homes)
                Place(prefabs.oldHouse, village, slot.Name,
                    new Vector3(slot.Position.x, 0f, slot.Position.y), slot.Scale, slot.RotationY, heightAt);
            Place(prefabs.well, village, "VillageWell", new Vector3(-63f, 0f, 50.5f), 0.95f, 0f, heightAt);

            if (!PolytopeNatureSetup.HasPack())
            {
                CreateFenceArc(prefabs.fence, village, new Vector3(-97f, 0f, 60f), 5, true, heightAt);
                CreateFenceArc(prefabs.fence, village, new Vector3(-38f, 0f, 70f), 5, true, heightAt);
                CreateFenceArc(prefabs.fence, village, new Vector3(-65f, 0f, 94f), 5, false, heightAt);
            }

            ReplaceOldFarmhouse(village, prefabs.oldHouse, heightAt);
            Transform southHouse = village.Find("WoodenHouseSouth");
            foreach (VillageRepairPoint repair in UnityEngine.Object.FindObjectsByType<VillageRepairPoint>(FindObjectsSortMode.None))
                repair.RebindHouseVisual(southHouse == null ? null : southHouse.gameObject);
            LiteFarmPackUrpMaterialFix.ConvertMaterialsUnder(village, lit, "ConvertedVillageHouses");
            new GameObject(SpacingMarker).transform.SetParent(village, false);
            PolytopeNatureSetup.RefreshVillageFences(host.parent);
            ClearGeneratedOverlap(host.parent);
            return village;
        }

        private static void ClearGeneratedOverlap(Transform valley)
        {
            if (valley == null) return;
            string[] paths =
            {
                "ValleyForests", "ValleyBoulders", "PolytopeNatureEnvironment/Trees",
                "PolytopeNatureEnvironment/MeadowAndRiverbank"
            };
            foreach (string path in paths)
            {
                Transform group = valley.Find(path);
                if (group == null) continue;
                float clearance = path.Contains("Trees") || path == "ValleyForests" ? 8f : 5f;
                foreach (Transform item in group)
                    if (VillageHouseLayout.IsNearHome(new Vector2(item.position.x, item.position.z), clearance))
                        item.gameObject.SetActive(false);
            }
        }

        private static void ReplaceOldFarmhouse(Transform village, GameObject house, Func<float, float, float> heightAt)
        {
            GameObject ranchHouse = GameObject.Find("Farmhouse");
            if (ranchHouse != null) ranchHouse.SetActive(false);
            Transform cottage = GameObject.Find("FarmZone")?.transform.Find("CottageWest");
            if (cottage != null)
            {
                string[] oldParts = { "CottageWalls", "CottageRoof", "Door", "WindowLeft", "WindowRight", "Chimney" };
                foreach (string name in oldParts)
                    if (cottage.Find(name) != null) cottage.Find(name).gameObject.SetActive(false);
            }
            Transform details = GameObject.Find("DetailedArt")?.transform.Find("CottageDetailing");
            if (details != null) details.gameObject.SetActive(false);
            Place(house, village, "WoodenFarmResidence", new Vector3(-17f, 0f, 10f), 0.84f, 145f, heightAt);
        }

        private static bool TryLoadPrefabs(out Prefabs prefabs)
        {
            prefabs = new Prefabs
            {
                oldHouse = AssetDatabase.LoadAssetAtPath<GameObject>(OldHousePath),
                well = AssetDatabase.LoadAssetAtPath<GameObject>(WellPath),
                fence = AssetDatabase.LoadAssetAtPath<GameObject>(FencePath)
            };
            return prefabs.oldHouse != null && prefabs.well != null && prefabs.fence != null;
        }

        private static void Place(GameObject prefab, Transform parent, string name, Vector3 position, float scale, float rotationY, Func<float, float, float> heightAt)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = new Vector3(position.x, heightAt(position.x, position.z), position.z);
            instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            instance.transform.localScale = Vector3.one * scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }

        private static void CreateFenceArc(GameObject fence, Transform parent, Vector3 center, int count, bool alongZ, Func<float, float, float> heightAt)
        {
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * 2.25f;
                Vector3 position = center + (alongZ ? Vector3.forward : Vector3.right) * offset;
                Place(fence, parent, "VillageFence_" + parent.childCount, position, 0.9f, alongZ ? 90f : 0f, heightAt);
            }
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return found;
            Transform created = new GameObject(name).transform;
            created.SetParent(parent, false);
            return created;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private struct Prefabs
        {
            public GameObject oldHouse;
            public GameObject well;
            public GameObject fence;
        }
    }
}

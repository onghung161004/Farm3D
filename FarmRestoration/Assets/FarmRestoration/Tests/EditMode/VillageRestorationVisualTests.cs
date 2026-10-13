using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class VillageRestorationVisualTests
    {
        private GameObject valley;
        private Material wood;
        private FarmProgression previousGame;
        private readonly FieldInfo singleton = typeof(FarmProgression).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            previousGame = FarmProgression.Instance;
            singleton.SetValue(null, null);
            valley = new GameObject("IllustratedValleyMap");
            wood = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "TestWood" };
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(valley);
            UnityEngine.Object.DestroyImmediate(wood);
            singleton.SetValue(null, previousGame);
        }

        private Transform Child(Transform parent, string name)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }

        private VillageRepairPoint Point(int index, GameObject original = null, GameObject legacy = null)
        {
            VillageRepairPoint point = Child(valley.transform, "RepairPoint" + index).gameObject.AddComponent<VillageRepairPoint>();
            point.Configure(index, 30, index == 0 ? "house" : "bridge", legacy, original);
            return point;
        }

        [Test]
        public void HouseChangesYardAndWeatheringWithoutMovingHouseOrChangingCollisions()
        {
            Transform village = Child(Child(valley.transform, "RiversideVillage"), "UniformWoodenHamlet");
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "WoodenHouseSouth";
            house.transform.SetParent(village, false);
            house.transform.SetPositionAndRotation(new Vector3(-86f, 2f, 34f), Quaternion.Euler(0f, 55f, 0f));
            house.transform.localScale = new Vector3(4f, 5f, 6f);
            Renderer renderer = house.GetComponent<Renderer>();
            renderer.sharedMaterial = wood;
            string property = wood.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            var block = new MaterialPropertyBlock();
            Color originalColor = new Color(0.8f, 0.7f, 0.6f);
            block.SetColor(property, originalColor);
            renderer.SetPropertyBlock(block, 0);
            Vector3 position = house.transform.position, scale = house.transform.localScale;
            Quaternion rotation = house.transform.rotation;
            Collider collider = house.GetComponent<Collider>();
            VillageRepairPoint point = Point(0, house);
            VillageRestorationVisuals visual = VillageRestorationVisuals.Install(point);
            Assert.That(visual, Is.Not.Null);
            Assert.That(VillageRestorationVisuals.Install(point), Is.SameAs(visual));
            Transform before = house.transform.Find("HouseBeforeRestoration"), after = house.transform.Find("HouseAfterRestoration");
            Assert.That(before.gameObject.activeSelf, Is.True);
            Assert.That(after.gameObject.activeSelf, Is.False);
            renderer.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor(property).r, Is.LessThan(originalColor.r));
            visual.ApplyState(true);
            renderer.GetPropertyBlock(block, 0);
            Assert.That(block.GetColor(property), Is.EqualTo(originalColor));
            Assert.That(before.gameObject.activeSelf, Is.False);
            Assert.That(after.gameObject.activeSelf, Is.True);
            Assert.That(after.Find("RepairedYardBench"), Is.Not.Null);
            Assert.That(house.transform.position, Is.EqualTo(position));
            Assert.That(house.transform.rotation, Is.EqualTo(rotation));
            Assert.That(house.transform.localScale, Is.EqualTo(scale));
            Assert.That(collider.enabled, Is.True);
            foreach (Collider added in before.GetComponentsInChildren<Collider>(true)) Assert.That(added.enabled, Is.False);
            foreach (Collider added in after.GetComponentsInChildren<Collider>(true)) Assert.That(added.enabled, Is.False);
            visual.ApplyState(true);
            Assert.That(house.GetComponents<VillageRestorationVisuals>().Length, Is.EqualTo(1));
        }

        [Test]
        public void BridgeRestoresOriginalMeshesWithoutChangingDeckColliderOrTransform()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RestorationSourceBridge.fbx")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Polytope Studio/Lowpoly_Village/Sources/Meshes/Modular/Bridge/PT_Wooden_Bridge_02.fbx");
            Assert.That(model, Is.Not.Null);
            Transform nature = Child(valley.transform, "PolytopeNatureEnvironment");
            GameObject bridge = UnityEngine.Object.Instantiate(model, nature);
            bridge.name = "PolytopeWoodenBridge";
            bridge.transform.SetPositionAndRotation(new Vector3(-50f, 3f, 45f), Quaternion.Euler(7f, 35f, 0f));
            bridge.transform.localScale = new Vector3(4.5f, 1f, 1f);
            MeshFilter[] filters = bridge.GetComponentsInChildren<MeshFilter>();
            Mesh[] originals = Array.ConvertAll(filters, f => f.sharedMesh);
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Plane);
            deck.name = "PolytopeBridgeWalkway";
            deck.transform.SetParent(nature);
            MeshCollider collider = deck.GetComponent<MeshCollider>();
            Mesh originalDeck = collider.sharedMesh;
            Vector3 position = bridge.transform.position, scale = bridge.transform.localScale;
            Quaternion rotation = bridge.transform.rotation;
            VillageRestorationVisuals visual = VillageRestorationVisuals.Install(Point(1));
            Assert.That(visual, Is.Not.Null);
            for (int i = 0; i < filters.Length; i++)
            {
                Assert.That(filters[i].sharedMesh, Is.Not.SameAs(originals[i]));
                Assert.That(filters[i].sharedMesh.bounds, Is.EqualTo(originals[i].bounds));
                Assert.That(filters[i].sharedMesh.triangles.Length, Is.LessThan(originals[i].triangles.Length));
                VerifyNoDeckTrianglesRemoved(originals[i], filters[i].sharedMesh);
            }
            visual.ApplyState(true);
            for (int i = 0; i < filters.Length; i++) Assert.That(filters[i].sharedMesh, Is.SameAs(originals[i]));
            Assert.That(collider.sharedMesh, Is.SameAs(originalDeck));
            Assert.That(collider.enabled, Is.True);
            Assert.That(bridge.transform.position, Is.EqualTo(position));
            Assert.That(bridge.transform.rotation, Is.EqualTo(rotation));
            Assert.That(bridge.transform.localScale, Is.EqualTo(scale));
        }

        private void VerifyNoDeckTrianglesRemoved(Mesh original, Mesh damaged)
        {
            GameObject probe = new GameObject("OriginalDeckProbe", typeof(MeshCollider));
            try
            {
                MeshCollider collider = probe.GetComponent<MeshCollider>();
                collider.sharedMesh = original;
                Physics.SyncTransforms();
                Vector3[] vertices = original.vertices;
                int[] kept = damaged.triangles;
                var keys = new HashSet<string>();
                for (int i = 0; i < kept.Length; i += 3) keys.Add(kept[i] + "/" + kept[i + 1] + "/" + kept[i + 2]);
                int[] triangles = original.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (keys.Contains(triangles[i] + "/" + triangles[i + 1] + "/" + triangles[i + 2])) continue;
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                    Vector3 mid = (a + b + c) / 3f;
                    Ray ray = new Ray(new Vector3(mid.x, original.bounds.max.y + 2f, original.bounds.center.z), Vector3.down);
                    Assert.That(collider.Raycast(ray, out RaycastHit deck, original.bounds.size.y + 4f), Is.True);
                    Assert.That(Mathf.Min(a.y, Mathf.Min(b.y, c.y)), Is.GreaterThan(deck.point.y + 0.12f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        [Test]
        public void SavedRepairShowsRestoredHouseAndDisablesLegacyReplacement()
        {
            Transform village = Child(Child(valley.transform, "RiversideVillage"), "UniformWoodenHamlet");
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "WoodenHouseSouth";
            house.transform.SetParent(village);
            house.GetComponent<Renderer>().sharedMaterial = wood;
            GameObject legacy = Child(valley.transform, "RestoredHouseGarden").gameObject;
            GameObject gameObject = Child(valley.transform, "Game").gameObject;
            gameObject.SetActive(false);
            FarmProgression game = gameObject.AddComponent<FarmProgression>();
            singleton.SetValue(null, game);
            bool[] flags = (bool[])typeof(FarmProgression).GetField("repairs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            flags[0] = true;
            VillageRepairPoint point = Point(0, house, legacy);
            typeof(VillageRepairPoint).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(point, null);
            Assert.That(house.activeSelf, Is.True);
            Assert.That(legacy.activeSelf, Is.False);
            Assert.That(house.transform.Find("HouseAfterRestoration").gameObject.activeSelf, Is.True);
            Assert.That(house.transform.Find("HouseBeforeRestoration").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MissingStructuresAreSafeAndDialogueAcknowledgesEachRepair()
        {
            Assert.That(VillageRestorationVisuals.Install(Point(0)), Is.Null);
            Assert.That(VillageRestorationVisuals.Install(null), Is.Null);
            Assert.That(VillageResidentNpc.RestorationGreeting("Hello α", false, false), Is.EqualTo("Hello α"));
            StringAssert.Contains("house yard", VillageResidentNpc.RestorationGreeting("Hi", true, false));
            StringAssert.Contains("bridge railing", VillageResidentNpc.RestorationGreeting("Hi", false, true));
            StringAssert.Contains("village feels alive", VillageResidentNpc.RestorationGreeting("Hi", true, true));
        }
    }
}

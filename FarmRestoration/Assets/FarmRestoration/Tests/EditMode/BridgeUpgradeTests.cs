using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class BridgeUpgradeTests
    {
        private GameObject root, model;
        private FarmProgression game, previous;
        private BridgeWidening widening;
        private MeshCollider walkway;
        private MeshFilter[] filters;
        private Mesh[] originals;
        private Mesh sourceDeck;
        private VillageRestorationVisuals visual;
        private VillageRepairPoint sign;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo Singleton = typeof(FarmProgression).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            previous = FarmProgression.Instance; Singleton.SetValue(null, null);
            root = new GameObject("IllustratedValleyMap");
            var stock = new GameObject("Stock"); stock.transform.SetParent(root.transform); stock.SetActive(false);
            FarmHud hud = stock.AddComponent<FarmHud>(); game = stock.AddComponent<FarmProgression>();
            Set("hud", hud); Set("ready", true); Singleton.SetValue(null, game);
            game.Changed += () => Set("ready", false); // Isolate tests from actual disk saves.
            Transform nature = new GameObject("PolytopeNatureEnvironment").transform;
            nature.SetParent(root.transform);
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RestorationSourceBridge.fbx")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Polytope Studio/Lowpoly_Village/Sources/Meshes/Modular/Bridge/PT_Wooden_Bridge_02.fbx");
            Assert.That(imported, Is.Not.Null);
            model = UnityEngine.Object.Instantiate(imported, nature); model.name = "PolytopeWoodenBridge";
            model.transform.SetPositionAndRotation(new Vector3(-50, 3, 45), Quaternion.Euler(0, 35, 5));
            model.transform.localScale = new Vector3(4.5f, 1, 1);
            filters = model.GetComponentsInChildren<MeshFilter>();
            originals = Array.ConvertAll(filters, f => f.sharedMesh);
            sourceDeck = new Mesh { name = "Baked walkway α" };
            Bounds bounds = RestorationProps.LocalBounds(model.transform);
            var vertices = new Vector3[8];
            for (int row = 0; row < 4; row++) for (int side = 0; side < 2; side++)
                vertices[row * 2 + side] = model.transform.TransformPoint(new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, row / 3f), bounds.min.y + (row == 0 || row == 3 ? 0.1f : 0.4f),
                    bounds.center.z + (side == 0 ? -1.65f : 1.65f)));
            sourceDeck.vertices = vertices;
            sourceDeck.triangles = new[] { 0, 1, 2, 1, 3, 2, 2, 3, 4, 3, 5, 4, 4, 5, 6, 5, 7, 6 };
            sourceDeck.RecalculateNormals(); sourceDeck.RecalculateBounds();
            var deckObject = new GameObject("PolytopeBridgeWalkway", typeof(MeshCollider));
            deckObject.transform.SetParent(nature);
            walkway = deckObject.GetComponent<MeshCollider>(); walkway.sharedMesh = sourceDeck;
            var marker = new GameObject("Repair"); marker.transform.SetParent(root.transform);
            sign = marker.AddComponent<VillageRepairPoint>(); sign.Configure(1, 45, "bridge", null);
            typeof(VillageRepairPoint).GetMethod("Start", Private).Invoke(sign, null);
            visual = model.GetComponent<VillageRestorationVisuals>();
            widening = model.GetComponent<BridgeWidening>();
            Assert.That(widening, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            if (widening != null) typeof(BridgeWidening).GetMethod("OnDestroy", Private).Invoke(widening, null);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(sourceDeck);
            Singleton.SetValue(null, previous);
        }

        private void Set(string field, object value) => typeof(FarmProgression).GetField(field, Private).SetValue(game, value);
        private void Repair(bool repaired = true) => ((bool[])typeof(FarmProgression).GetField("repairs", Private).GetValue(game))[1] = repaired;

        [TestCase(39, 15)] [TestCase(40, 14)] [TestCase(0, 0)]
        public void FirstTierRequiresBothExactResources(int coins, int wood)
        {
            Repair(); Set("coins", coins); Set("wood", wood);
            Assert.That(game.TryUpgradeBridge(widening), Is.False);
            Assert.That(game.Coins, Is.EqualTo(coins)); Assert.That(game.WoodCount, Is.EqualTo(wood));
            Assert.That(game.BridgeUpgradeLevel, Is.Zero);
            Assert.That(walkway.sharedMesh, Is.SameAs(widening.CalibratedDeck));
        }

        [Test]
        public void PrerequisiteNullAndDisabledGeometryCannotCharge()
        {
            Set("coins", 100); Set("wood", 40);
            Assert.That(game.TryUpgradeBridge(widening), Is.False);
            Repair(); Assert.That(game.TryUpgradeBridge(null), Is.False);
            walkway.enabled = false;
            Assert.That(game.TryUpgradeBridge(widening), Is.False);
            Assert.That(game.Coins, Is.EqualTo(100)); Assert.That(game.WoodCount, Is.EqualTo(40));
        }

        [Test]
        public void TwoPurchasesConsume100Coins40WoodAndThirdCannotCharge()
        {
            Repair(); Set("coins", 100); Set("wood", 40);
            Assert.That(sign.TryInteract(FarmTool.Hoe), Is.True);
            Assert.That(game.Coins, Is.EqualTo(60)); Assert.That(game.WoodCount, Is.EqualTo(25));
            Assert.That(game.BridgeUpgradeLevel, Is.EqualTo(1));
            Mesh first = walkway.sharedMesh;
            Set("ready", true); Assert.That(sign.TryInteract(FarmTool.Hoe), Is.True);
            Assert.That(game.Coins, Is.Zero); Assert.That(game.WoodCount, Is.Zero);
            Assert.That(game.BridgeUpgradeLevel, Is.EqualTo(2));
            Assert.That(walkway.sharedMesh, Is.Not.SameAs(first));
            Assert.That(walkway.GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo((walkway.sharedMesh.vertexCount/2-1)*2), "Upgraded repaired bridge has physical side rails.");
            Set("ready", true); Assert.That(sign.TryInteract(FarmTool.Hoe), Is.False);
            StringAssert.Contains("3x", sign.GetInteractionPrompt(FarmTool.Hoe));
            var saved = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(game.Snapshot()));
            Assert.That(saved.version, Is.EqualTo(8)); Assert.That(saved.bridgeUpgradeLevel, Is.EqualTo(2));
        }

        [TestCase(59, 25)] [TestCase(60, 24)]
        public void SecondTierShortageKeepsPreviousSurface(int coins, int wood)
        {
            Repair(); Set("bridgeUpgradeLevel", 1); visual.ApplyState(true); Mesh previousDeck = walkway.sharedMesh;
            Set("coins", coins); Set("wood", wood);
            Assert.That(game.TryUpgradeBridge(widening), Is.False);
            Assert.That(walkway.sharedMesh, Is.SameAs(previousDeck));
            Assert.That(game.BridgeUpgradeLevel, Is.EqualTo(1));
            Assert.That(game.Coins, Is.EqualTo(coins)); Assert.That(game.WoodCount, Is.EqualTo(wood));
        }

        [TestCase(1)] [TestCase(2)]
        public void NativeLodsAndBakedColliderWidenAroundFixedCentreWithoutCompounding(int level)
        {
            Repair(); Set("bridgeUpgradeLevel", level);
            Vector3 position = model.transform.position, scale = model.transform.localScale;
            Quaternion rotation = model.transform.rotation;
            Vector3[] baseline = sourceDeck.vertices;
            float centre = model.transform.InverseTransformPoint((baseline[0] + baseline[1]) * 0.5f).z;
            visual.ApplyState(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Vector3[] before = originals[i].vertices, after = filters[i].sharedMesh.vertices;
                for (int v = 0; v < before.Length; v++)
                {
                    Vector3 a = model.transform.InverseTransformPoint(filters[i].transform.TransformPoint(before[v]));
                    Vector3 b = model.transform.InverseTransformPoint(filters[i].transform.TransformPoint(after[v]));
                    Assert.That(b.x, Is.EqualTo(a.x).Within(0.001f)); Assert.That(b.y, Is.EqualTo(a.y).Within(0.001f));
                    Assert.That(b.z, Is.EqualTo(centre + (a.z - centre) * (level + 1)).Within(0.001f));
                }
            }
            Vector3[] deck = walkway.sharedMesh.vertices, original = widening.CalibratedDeck.vertices;
            for (int i = 0; i < deck.Length; i++)
            {
                Vector3 a = model.transform.InverseTransformPoint(original[i]);
                Vector3 b = model.transform.InverseTransformPoint(deck[i]);
                Assert.That(b.x, Is.EqualTo(a.x).Within(0.001f)); Assert.That(b.y, Is.EqualTo(a.y).Within(0.001f));
                Assert.That(b.z, Is.EqualTo(centre + (a.z - centre) * (level + 1)).Within(0.001f));
            }
            // Sample the new outer surface: this used to be outside the original walkable strip.
            Vector3 test = model.transform.TransformPoint(new Vector3(1.29365f, 0, centre+0.4f*(level+1)));
            Physics.SyncTransforms();
            Assert.That(walkway.Raycast(new Ray(test + Vector3.up * 30, Vector3.down), out _, 60), Is.True);
            Mesh cached = filters[0].sharedMesh, cachedDeck = walkway.sharedMesh;
            visual.ApplyState(true); visual.ApplyState(true);
            Assert.That(filters[0].sharedMesh, Is.SameAs(cached)); Assert.That(walkway.sharedMesh, Is.SameAs(cachedDeck));
            Assert.That(model.transform.position, Is.EqualTo(position)); Assert.That(model.transform.rotation, Is.EqualTo(rotation));
            Assert.That(model.transform.localScale, Is.EqualTo(scale));
            Assert.That(sourceDeck.vertices, Is.EqualTo(baseline), "Original asset must not be mutated.");
        }

        [TestCase(-1, true, 0)] [TestCase(0, true, 0)] [TestCase(1, true, 1)] [TestCase(99, true, 2)] [TestCase(2, false, 0)]
        public void SavedTierClampAndRepairPrerequisite(int saved, bool repaired, int expected)
            => Assert.That(FarmProgression.RestoreBridgeUpgradeLevel(saved, repaired), Is.EqualTo(expected));

        [Test]
        public void LegacySaveDefaultsToOriginalWidthWithoutRepurchasingRepair()
        {
            var old = JsonUtility.FromJson<FarmSaveData>("{\"version\":7,\"coins\":55,\"wood\":12,\"repairs\":[false,true]}");
            Assert.That(old.bridgeUpgradeLevel, Is.Zero);
            Assert.That(old.repairs[1], Is.True);
            Assert.That(old.coins, Is.EqualTo(55)); Assert.That(old.wood, Is.EqualTo(12));
        }

        [Test]
        public void MissingBindingAndInvalidLevelAreSafeAndCleanupRestoresOriginals()
        {
            Assert.That(BridgeWidening.Install(null, walkway, filters, originals), Is.Null);
            Assert.That(BridgeWidening.Install(model.transform, null, filters, originals), Is.Null);
            Assert.That(widening.Apply(-1), Is.False); Assert.That(widening.Apply(3), Is.False);
            widening.Apply(2);
            typeof(BridgeWidening).GetMethod("OnDestroy", Private).Invoke(widening, null);
            Assert.That(walkway.sharedMesh, Is.SameAs(sourceDeck));
            for (int i = 0; i < filters.Length; i++) Assert.That(filters[i].sharedMesh, Is.SameAs(originals[i]));
        }
    }
}

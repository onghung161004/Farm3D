using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class WoodEconomyTests
    {
        private GameObject root;
        private FarmProgression game;
        private CowPen pen;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WoodEconomyTest");
            var hudObject = new GameObject("Inventory");
            hudObject.transform.SetParent(root.transform);
            hudObject.SetActive(false);
            FarmHud hud = hudObject.AddComponent<FarmHud>();
            game = hudObject.AddComponent<FarmProgression>();
            Set("hud", hud);
            game.Changed += () => Set("ready", false); // Do not write into a real player save.
            var penObject = new GameObject("Pen");
            penObject.transform.SetParent(root.transform);
            pen = penObject.AddComponent<CowPen>();
            Set("ready", true);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(root);
        private void Set(string name, object value) => typeof(FarmProgression).GetField(name, Fields).SetValue(game, value);

        [Test]
        public void FinalChopAwardsThreeWoodOnceAndSavesTreeState()
        {
            var obj = new GameObject("Tree");
            obj.transform.SetParent(root.transform);
            ChoppableTree tree = obj.AddComponent<ChoppableTree>();
            tree.Configure("resource-tree", null);
            Set("trees", new[] { tree });
            DateTime now = DateTime.UtcNow;
            tree.TryChop(now, Time.unscaledTime - 2f, out _);
            tree.TryChop(now, Time.unscaledTime - 1f, out _);
            Assert.That(game.WoodCount, Is.Zero);
            Assert.That(game.TryChopTree(tree), Is.True);
            Assert.That(game.WoodCount, Is.EqualTo(3));
            Set("ready", true);
            Assert.That(game.TryChopTree(tree), Is.False);
            Assert.That(game.WoodCount, Is.EqualTo(3));
            FarmSaveData saved = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(game.Snapshot()));
            Assert.That(saved.version, Is.EqualTo(FarmSaveData.CurrentVersion));
            Assert.That(saved.wood, Is.EqualTo(3));
            Assert.That(saved.trees[0].hits, Is.EqualTo(3));
            Assert.That(saved.trees[0].regrowUtcTicks, Is.GreaterThan(now.Ticks));
        }

        [TestCase(19, 30)]
        [TestCase(20, 29)]
        public void PenUpgradeRequiresBothResourcesBeforeSpending(int wood, int coins)
        {
            Set("wood", wood);
            Set("coins", coins);
            Assert.That(game.TryUpgradeCowPen(pen), Is.False);
            Assert.That(game.WoodCount, Is.EqualTo(wood));
            Assert.That(game.Coins, Is.EqualTo(coins));
            Assert.That(pen.Capacity, Is.EqualTo(8));
        }

        [Test]
        public void PenUpgradeUsesExactCostsAndCannotChargeTwice()
        {
            Vector2 before = pen.Footprint;
            Set("wood", 20);
            Set("coins", 30);
            Assert.That(game.TryUpgradeCowPen(pen), Is.True);
            Assert.That(game.WoodCount, Is.Zero);
            Assert.That(game.Coins, Is.Zero);
            Assert.That(pen.Capacity, Is.EqualTo(12));
            Assert.That(pen.Footprint.y, Is.EqualTo(before.y + CowPenExpansion.Extension));
            Assert.That(game.Snapshot().cowPenUpgraded, Is.True);
            Set("ready", true);
            Set("wood", 20);
            Set("coins", 30);
            Assert.That(game.TryUpgradeCowPen(pen), Is.False);
            Assert.That(game.WoodCount, Is.EqualTo(20));
            Assert.That(game.Coins, Is.EqualTo(30));
            Assert.That(pen.Footprint.y, Is.EqualTo(before.y + CowPenExpansion.Extension));
        }

        [TestCase(0f)]
        [TestCase(90f)]
        public void PenExpansionMovesFrontFencesAndBenchButKeepsRearAndExistingCows(float yaw)
        {
            pen.Configure(new Vector2(8f, 18f));
            pen.transform.position = new Vector3(-24f, 0f, 9f);
            pen.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform rear = Panel("CowPenFence_Rear", 0f, 9f);
            Transform front = Panel("CowPenFence_Front", -2.75f, -9f);
            Panel("CowPenFence_Left", -4f, -8f);
            Panel("CowPenFence_Right", 4f, -8f);
            Transform bench = Panel("PenUpgradeWorkbench", 2.2f, -11f);
            Transform cow = Panel("Bò con α", 0f, 4f);
            Vector3 rearPosition = rear.position;
            Vector3 frontPosition = front.position;
            Vector3 benchPosition = bench.position;
            Vector3 cowPosition = cow.position;
            Vector3 gate = pen.GatePosition;
            Vector3 forward = pen.transform.forward;
            Vector3 newlyInside = gate - forward * 4f;
            Assert.That(pen.Contains(newlyInside), Is.False);
            CowPenExpansion.Apply(pen);
            Assert.That(pen.Footprint, Is.EqualTo(new Vector2(8f, 24f)));
            Assert.That(Vector3.Distance(rear.position, rearPosition), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(cow.position, cowPosition), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(front.position, frontPosition - forward * 6f), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(bench.position, benchPosition - forward * 6f), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(pen.GatePosition, gate - forward * 6f), Is.LessThan(0.001f));
            Assert.That(pen.Contains(newlyInside, 0.6f), Is.True);
            Assert.That(pen.Contains(gate - forward * 7f), Is.False);
            Assert.That(pen.transform.childCount, Is.EqualTo(12), "Six original objects plus six added side panels.");
            CowPenExpansion.Apply(pen);
            Assert.That(pen.transform.childCount, Is.EqualTo(12), "Do not duplicate fences when restoring twice.");
            Assert.That(pen.Footprint.y, Is.EqualTo(24f));

            Transform Panel(string name, float x, float z)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = name;
                part.transform.SetParent(pen.transform, false);
                part.transform.localPosition = new Vector3(x, 0f, z);
                return part.transform;
            }
        }

        [Test]
        public void ExistingUpgradeSaveCanExpandWithoutChargingAgain()
        {
            FarmSaveData saved = JsonUtility.FromJson<FarmSaveData>("{\"version\":6,\"cowPenUpgraded\":true,\"wood\":13,\"coins\":18}");
            pen.Configure(new Vector2(8f, 18f));
            if (saved.cowPenUpgraded) CowPenExpansion.Apply(pen);
            Assert.That(pen.Footprint.y, Is.EqualTo(24f));
            Assert.That(pen.Capacity, Is.EqualTo(12));
            Assert.That(saved.wood, Is.EqualTo(13));
            Assert.That(saved.coins, Is.EqualTo(18));
            Assert.DoesNotThrow(() => CowPenExpansion.Apply(null));
        }

        [Test]
        public void RepairsRequireWoodAndAlreadyRepairedPointDoesNotChargeAgain()
        {
            Set("wood", 6);
            Set("coins", 100);
            Assert.That(game.TryRepair(0, 30), Is.True);
            Assert.That(game.WoodCount, Is.Zero);
            Assert.That(game.Coins, Is.EqualTo(70));
            Set("ready", true);
            Assert.That(game.TryRepair(1, 30), Is.False);
            Assert.That(game.TryRepair(0, 30), Is.False);
            Assert.That(game.Coins, Is.EqualTo(70));
        }

        [Test]
        public void WoodSaleUsesNinthItemAndLeavesFishUntouched()
        {
            Set("wood", 1);
            Set("fish", 2);
            Set("coins", 1);
            Assert.That((int)WarehouseItemKind.Wood, Is.EqualTo(8));
            Assert.That(game.TrySell(WarehouseItemKind.Wood), Is.True);
            Assert.That(game.WoodCount, Is.Zero);
            Assert.That(game.Coins, Is.EqualTo(4));
            Assert.That(game.FishCount, Is.EqualTo(2));
            Set("ready", true);
            Assert.That(game.TrySell(WarehouseItemKind.Wood), Is.False);
            Assert.That(game.TrySell((WarehouseItemKind)99), Is.False);
        }

        [Test]
        public void VersionFiveSaveKeepsCalvesAndDefaultsNewWoodFields()
        {
            FarmSaveData saved = JsonUtility.FromJson<FarmSaveData>(
                "{\"version\":5,\"coins\":61,\"milk\":2,\"fish\":3,\"cows\":[{\"id\":\"calf-1\",\"offspring\":true,\"grownUtcTicks\":638000000000000000}]}");
            Assert.That(saved.wood, Is.Zero);
            Assert.That(saved.cowPenUpgraded, Is.False);
            Assert.That(saved.coins, Is.EqualTo(61));
            Assert.That(saved.milk, Is.EqualTo(2));
            Assert.That(saved.fish, Is.EqualTo(3));
            Assert.That(saved.cows[0].offspring, Is.True);
            Assert.That(saved.cows[0].grownUtcTicks, Is.EqualTo(638000000000000000));
            Assert.That(game.TryChopTree(null), Is.False);
            Assert.That(game.TryUpgradeCowPen(null), Is.False);
        }
    }
}

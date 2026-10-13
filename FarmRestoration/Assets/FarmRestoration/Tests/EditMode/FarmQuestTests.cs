using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmQuestTests
    {
        private GameObject root;
        private FarmProgression game;
        private FarmHud hud;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FarmQuestContext Complete = new FarmQuestContext(1, 2, true, true);

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("QuestFixture");
            root.SetActive(false);
            hud = root.AddComponent<FarmHud>();
            game = root.AddComponent<FarmProgression>();
            Set("hud", hud);
            game.Changed += () => Set("ready", false); // Never touch a real player's save.
            Set("ready", true);
        }

        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(root);
        private void Set(string field, object value) => typeof(FarmProgression).GetField(field, Fields).SetValue(game, value);
        private void Call(string method, params object[] args) => typeof(FarmProgression).GetMethod(method, Fields).Invoke(game, args);

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void CompleteObjectivesAllowEveryStage(int stage)
        {
            var p = Full(); p.claimedStage = stage;
            Assert.That(FarmQuestRules.CanClaim(p, Complete), Is.True);
            Assert.That(FarmQuestRules.Reward(stage), Is.GreaterThan(0));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EmptyObjectivesBlockEveryStage(int stage)
        {
            Assert.That(FarmQuestRules.CanClaim(new FarmQuestProgress { claimedStage = stage }, default), Is.False);
        }

        [Test]
        public void InvalidSaveIsClampedAndCowIdsDeduplicated()
        {
            var p = FarmQuestProgress.Restore(new FarmQuestProgress { claimedStage = int.MaxValue,
                carrotsHarvested = -1, itemsProduced = 20, milkCollected = -2, fishCaught = int.MaxValue,
                fishDelivered = 4, fedCowIds = new[] { null, " ", new string('x', 257), "Bò α", "Bò α", "Bò β", "Bò γ" } });
            Assert.That(p.claimedStage, Is.EqualTo(6));
            Assert.That(p.carrotsHarvested, Is.Zero);
            Assert.That(p.itemsProduced, Is.EqualTo(1));
            Assert.That(p.milkCollected, Is.Zero);
            Assert.That(p.fishCaught, Is.EqualTo(3));
            Assert.That(p.fishDelivered, Is.EqualTo(2));
            Assert.That(p.fedCowIds, Is.EqualTo(new[] { "Bò α", "Bò β" }));
            Assert.That(FarmQuestRules.CanClaim(p, Complete), Is.False);
            Assert.That(FarmQuestProgress.Restore(null).fedCowIds, Is.Empty);
        }

        [Test]
        public void CountersSaturateAndSnapshotDoesNotShareIds()
        {
            var p = new FarmQuestProgress();
            p.RecordCarrots(2); p.RecordCarrots(int.MaxValue); p.RecordCarrots(-100);
            for (int i = 0; i < 10; i++) p.RecordFish();
            p.RecordFeed("cow"); p.RecordFeed("cow");
            var copy = p.Snapshot(); p.fedCowIds[0] = "changed";
            Assert.That(copy.fedCowIds[0], Is.EqualTo("cow"));
            Assert.That(copy.carrotsHarvested, Is.EqualTo(3));
            Assert.That(copy.fishCaught, Is.EqualTo(3));
        }

        [Test]
        public void ClaimPaysOnceAndAdvancesOnlyOneChapter()
        {
            Set("quests", Full()); Set("coins", 10);
            Assert.That(game.TryClaimQuest(), Is.True);
            Assert.That(game.Coins, Is.EqualTo(25));
            Assert.That(game.QuestProgress.claimedStage, Is.EqualTo(1));
            Set("ready", true);
            Assert.That(game.TryClaimQuest(), Is.False, "Next chapter still requires an order.");
            Assert.That(game.Coins, Is.EqualTo(25));
        }

        [Test]
        public void FinalClaimCreditsExistingRepairsAndCannotRepeat()
        {
            var p = Full(); p.claimedStage = 5; Set("quests", p);
            ((bool[])typeof(FarmProgression).GetField("repairs", Fields).GetValue(game))[0] = true;
            ((bool[])typeof(FarmProgression).GetField("repairs", Fields).GetValue(game))[1] = true;
            Assert.That(game.TryClaimQuest(), Is.True);
            Assert.That(game.Coins, Is.EqualTo(40));
            Set("ready", true);
            Assert.That(game.TryClaimQuest(), Is.False);
            Assert.That(game.QuestProgress.claimedStage, Is.EqualTo(6));
        }

        [Test]
        public void RewardOverflowDoesNotAdvanceOrSpend()
        {
            Set("quests", Full()); Set("coins", int.MaxValue);
            Assert.That(game.TryClaimQuest(), Is.False);
            Assert.That(game.QuestProgress.claimedStage, Is.Zero);
            Assert.That(game.Coins, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void DonationConsumesExactlyTwoFishAndStopsOnRapidThirdClick()
        {
            game.QuestProgress.claimedStage = 4; Set("fish", 3);
            Assert.That(game.TryGiveQuestFish(), Is.True); Set("ready", true);
            Assert.That(game.TryGiveQuestFish(), Is.True); Set("ready", true);
            Assert.That(game.TryGiveQuestFish(), Is.False);
            Assert.That(game.FishCount, Is.EqualTo(1));
            Assert.That(game.QuestProgress.fishDelivered, Is.EqualTo(2));
        }

        [TestCase(0, 3)] [TestCase(4, 0)] [TestCase(4, -1)]
        public void WrongStageOrEmptyStockCannotDonate(int stage, int stock)
        {
            game.QuestProgress.claimedStage = stage; Set("fish", stock);
            Assert.That(game.TryGiveQuestFish(), Is.False);
            Assert.That(game.FishCount, Is.EqualTo(stock));
            Assert.That(game.QuestProgress.fishDelivered, Is.Zero);
        }

        [Test]
        public void RealHarvestAndFishHooksRecordOnlySuccessfulActivities()
        {
            Call("RecordQuestHarvest", CropType.Pumpkin, 1);
            Call("RecordQuestHarvest", CropType.Carrot, 0);
            Assert.That(game.QuestProgress.carrotsHarvested, Is.Zero);
            Call("RecordQuestHarvest", CropType.Carrot, 1);
            Assert.That(game.QuestProgress.carrotsHarvested, Is.EqualTo(1));
            Set("ready", true); game.AddFish();
            Assert.That(game.QuestProgress.fishCaught, Is.EqualTo(1));
            game.AddFish(); // Not ready; no award.
            Assert.That(game.QuestProgress.fishCaught, Is.EqualTo(1));
        }

        [Test]
        public void LegacyStockGrantsEvidencedProgressWithoutClaimingOrInventingDonations()
        {
            Set("fish", 5); Set("milk", 1); Set("orderIndex", 4);
            Call("InitializeQuests");
            Assert.That(game.QuestProgress.carrotsHarvested, Is.EqualTo(3));
            Assert.That(game.QuestProgress.itemsProduced, Is.EqualTo(1));
            Assert.That(game.QuestProgress.milkCollected, Is.EqualTo(1));
            Assert.That(game.QuestProgress.fishCaught, Is.EqualTo(3));
            Assert.That(game.QuestProgress.fishDelivered, Is.Zero);
            Assert.That(game.QuestProgress.claimedStage, Is.Zero);
            Assert.That(game.FishCount, Is.EqualTo(5));
            var saved = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(game.Snapshot()));
            Assert.That(saved.version, Is.EqualTo(FarmSaveData.CurrentVersion));
            Assert.That(saved.quests.fishCaught, Is.EqualTo(3));
        }

        [Test]
        public void CurrentSaveDoesNotRecountStockOnReload()
        {
            Set("questsLoaded", true); Set("fish", 5); Set("orderIndex", 4);
            game.QuestProgress.claimedStage = 2;
            Call("InitializeQuests");
            Assert.That(game.QuestProgress.fishCaught, Is.Zero);
            Assert.That(game.QuestProgress.claimedStage, Is.EqualTo(2));
        }

        [Test]
        public void SixClaimsPay140CoinsAndKeepRestoredBuildingsAndStock()
        {
            Set("quests", Full()); Set("orderIndex", 1); Set("fish", 8);
            var repairs = (bool[])typeof(FarmProgression).GetField("repairs", Fields).GetValue(game);
            repairs[0] = repairs[1] = true;
            var cows = (System.Collections.Generic.List<CowAnimal>)typeof(FarmProgression).GetField("cows", Fields).GetValue(game);
            for (int i = 0; i < 2; i++)
            {
                var obj = new GameObject("QuestCow"); obj.transform.SetParent(root.transform);
                var cow = obj.AddComponent<CowAnimal>();
                cow.Restore(new CowSaveData { id = "cow-" + i, penned = true });
                cows.Add(cow);
            }
            for (int i = 0; i < 6; i++)
            {
                Set("ready", true);
                Assert.That(game.TryClaimQuest(), Is.True, "Chapter " + i);
                Assert.That(game.QuestProgress.claimedStage, Is.EqualTo(i + 1));
            }
            Set("ready", true);
            Assert.That(game.TryClaimQuest(), Is.False);
            Assert.That(game.Coins, Is.EqualTo(140));
            Assert.That(game.FishCount, Is.EqualTo(8));
            Assert.That(game.IsRepaired(0) && game.IsRepaired(1), Is.True);
            var saved = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(game.Snapshot()));
            Assert.That(saved.quests.claimedStage, Is.EqualTo(6));
            Assert.That(FarmQuestRules.CanClaim(FarmQuestProgress.Restore(saved.quests), game.QuestContext), Is.False);
        }

        [Test]
        public void ProductionCounterRequiresSuccessfulSpending()
        {
            Assert.That(game.TryProduce(CropType.Carrot), Is.False);
            Assert.That(game.QuestProgress.itemsProduced, Is.Zero);
            hud.CropInventory.Add(CropType.Carrot, 2);
            Assert.That(game.TryProduce(CropType.Carrot), Is.True);
            Assert.That(game.QuestProgress.itemsProduced, Is.EqualTo(1));
            Assert.That(game.CarrotCount, Is.Zero);
            Assert.That(game.ProductCount(CropType.Carrot), Is.EqualTo(1));
        }

        private static FarmQuestProgress Full() => new FarmQuestProgress { talkedToVillager = true,
            carrotsHarvested = 3, itemsProduced = 1, milkCollected = 1, fishCaught = 3, fishDelivered = 2,
            fedCowIds = new[] { "cow-a", "cow-b" } };
    }
}

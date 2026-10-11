using System;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class GameplayDepthTests
    {
        private GameObject testObject;
        private FarmProgression progression;
        private FarmHud hud;
        private CropInventory inventory;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("GameplayDepthTestRunner");
            hud = testObject.AddComponent<FarmHud>();
            inventory = hud.CropInventory;
            progression = testObject.AddComponent<FarmProgression>();
            progression.ConfigureForTests(hud, Array.Empty<FarmPlot>(), 15);
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void SeedEconomy_ScriptableObject_ConfiguresValuesProperly()
        {
            CropSeedData carrot = ScriptableObject.CreateInstance<CropSeedData>();
            carrot.Configure(CropType.Carrot, 2, 5, 35f);

            Assert.That(carrot.CropType, Is.EqualTo(CropType.Carrot));
            Assert.That(carrot.SeedCost, Is.EqualTo(2));
            Assert.That(carrot.SellPrice, Is.EqualTo(5));
            Assert.That(carrot.GrowthSeconds, Is.EqualTo(35f));
        }

        [Test]
        public void SeedInventory_InitialSeedsAllowFirstHarvest()
        {
            Assert.That(progression.SeedInventory.GetCount(CropType.Carrot), Is.GreaterThanOrEqualTo(3));
            Assert.That(progression.Coins, Is.GreaterThanOrEqualTo(15));
        }

        [Test]
        public void SeedEconomy_BuyAndSpendSeeds_DeductsCoinsAndUpdatesInventory()
        {
            int startCoins = progression.Coins;
            int startCarrotSeeds = progression.SeedInventory.GetCount(CropType.Carrot);

            bool bought = progression.TryBuySeed(CropType.Carrot, 2);
            Assert.That(bought, Is.True);
            Assert.That(progression.Coins, Is.EqualTo(startCoins - 4));
            Assert.That(progression.SeedInventory.GetCount(CropType.Carrot), Is.EqualTo(startCarrotSeeds + 2));

            bool spent = progression.TrySpendSeed(CropType.Carrot, 1);
            Assert.That(spent, Is.True);
            Assert.That(progression.SeedInventory.GetCount(CropType.Carrot), Is.EqualTo(startCarrotSeeds + 1));
        }

        [Test]
        public void SeedEconomy_CannotBuySeedWithoutEnoughCoins()
        {
            progression.TrySpendSeed(CropType.Carrot, progression.SeedInventory.GetCount(CropType.Carrot));
            progression.TrySpendSeed(CropType.Pumpkin, progression.SeedInventory.GetCount(CropType.Pumpkin));
            progression.TrySpendSeed(CropType.Tomato, progression.SeedInventory.GetCount(CropType.Tomato));

            // Spend down coins
            while (progression.Coins >= 2)
            {
                progression.TryBuySeed(CropType.Carrot, 1);
            }

            Assert.That(progression.Coins, Is.LessThan(2));
            bool bought = progression.TryBuySeed(CropType.Carrot, 1);
            Assert.That(bought, Is.False);
        }

        [Test]
        public void AntiSoftLock_GrantsSeedsWhenBankruptAndEmpty()
        {
            // Empty all seeds
            progression.SeedInventory.TrySpend(CropType.Carrot, progression.SeedInventory.GetCount(CropType.Carrot));
            progression.SeedInventory.TrySpend(CropType.Pumpkin, progression.SeedInventory.GetCount(CropType.Pumpkin));
            progression.SeedInventory.TrySpend(CropType.Tomato, progression.SeedInventory.GetCount(CropType.Tomato));

            // Spend all coins
            while (progression.Coins >= 2)
            {
                progression.TryBuySeed(CropType.Carrot, 1);
            }
            progression.SeedInventory.TrySpend(CropType.Carrot, progression.SeedInventory.GetCount(CropType.Carrot));

            Assert.That(progression.SeedInventory.TotalSeeds, Is.EqualTo(0));
            Assert.That(progression.Coins, Is.LessThan(2));
            Assert.That(inventory.Total, Is.EqualTo(0));

            bool triggered = progression.CheckAntiSoftLock();
            Assert.That(triggered, Is.True);
            Assert.That(progression.SeedInventory.GetCount(CropType.Carrot), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void GardenUnlock_CarrotStartsUnlocked_PumpkinAndTomatoStartLocked()
        {
            Assert.That(progression.IsGardenUnlocked(CropType.Carrot), Is.True);
            Assert.That(progression.IsGardenUnlocked(CropType.Pumpkin), Is.False);
            Assert.That(progression.IsGardenUnlocked(CropType.Tomato), Is.False);
        }

        [Test]
        public void GardenUnlock_UnlockWithCoins_DeductsCoinsAndUnlocks()
        {
            progression.AddCoins(50);
            int startCoins = progression.Coins;
            int pumpkinCost = progression.GetGardenUnlockCost(CropType.Pumpkin);

            bool unlocked = progression.TryUnlockGarden(CropType.Pumpkin);
            Assert.That(unlocked, Is.True);
            Assert.That(progression.IsGardenUnlocked(CropType.Pumpkin), Is.True);
            Assert.That(progression.Coins, Is.EqualTo(startCoins - pumpkinCost));
        }

        [Test]
        public void GardenUnlock_UnlockFree_UnlocksWithoutCoins()
        {
            int startCoins = progression.Coins;
            progression.UnlockGardenFree(CropType.Tomato);

            Assert.That(progression.IsGardenUnlocked(CropType.Tomato), Is.True);
            Assert.That(progression.Coins, Is.EqualTo(startCoins));
        }

        [Test]
        public void GardenUnlock_LockedPlot_PromptShowsLockedAndInteractionBlocks()
        {
            GameObject plotObj = new GameObject("PumpkinPlotTest");
            FarmPlot plot = plotObj.AddComponent<FarmPlot>();
            plot.ConfigureCropType(CropType.Pumpkin);

            Assert.That(progression.IsGardenUnlocked(CropType.Pumpkin), Is.False);
            string prompt = plot.GetInteractionPrompt(FarmTool.Hoe);
            Assert.That(prompt, Does.Contain("[Locked]"));

            // Interacting when player doesn't have enough coins does not till
            bool interacted = plot.TryInteract(FarmTool.Hoe);
            Assert.That(interacted, Is.False);
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Untilled));

            UnityEngine.Object.DestroyImmediate(plotObj);
        }

        [Test]
        public void TimedOrders_ActiveCount_Maintains2To3Orders()
        {
            Assert.That(progression.ActiveOrdersCount, Is.InRange(2, 3));
        }

        [Test]
        public void TimedOrders_OnlyAsksForUnlockedCrops()
        {
            // Initially only Carrot is unlocked
            Assert.That(progression.IsGardenUnlocked(CropType.Carrot), Is.True);
            Assert.That(progression.IsGardenUnlocked(CropType.Pumpkin), Is.False);
            Assert.That(progression.IsGardenUnlocked(CropType.Tomato), Is.False);

            foreach (ActiveTimedOrder order in progression.ActiveTimedOrders)
            {
                Assert.That(order.cropType, Is.EqualTo(CropType.Carrot));
            }
        }

        [Test]
        public void TimedOrders_ExpiredOrdersAreReplaced()
        {
            DateTime now = DateTime.UtcNow;
            progression.MaintainTimedOrders(now);

            Assert.That(progression.ActiveOrdersCount, Is.GreaterThan(0));
            string initialFirstOrderId = progression.ActiveTimedOrders[0].id;

            // Fast forward time by 1 hour (3600s) -> orders expire
            DateTime future = now.AddHours(1);
            progression.MaintainTimedOrders(future);

            Assert.That(progression.ActiveOrdersCount, Is.InRange(2, 3));
            bool stillContainsInitial = false;
            foreach (ActiveTimedOrder order in progression.ActiveTimedOrders)
            {
                if (order.id == initialFirstOrderId) stillContainsInitial = true;
            }
            Assert.That(stillContainsInitial, Is.False, "Expired order should have been replaced");
        }

        [Test]
        public void TimedOrders_SaveLoadRoundTrip_PreservesOrdersAndGardenUnlocks()
        {
            progression.UnlockGardenFree(CropType.Pumpkin);
            progression.SeedInventory.Add(CropType.Carrot, 7);

            TimedOrderSaveData[] exportedOrders = progression.ExportTimedOrders();
            int[] exportedSeeds = progression.SeedInventory.Snapshot();

            // Create new runner and restore
            GameObject newRunner = new GameObject("RestoreRunner");
            FarmHud newHud = newRunner.AddComponent<FarmHud>();
            FarmProgression newProgression = newRunner.AddComponent<FarmProgression>();
            newProgression.ConfigureForTests(newHud, Array.Empty<FarmPlot>(), 25);

            newProgression.SeedInventory.Restore(exportedSeeds);
            newProgression.RestoreTimedOrders(exportedOrders, DateTime.UtcNow);

            Assert.That(newProgression.SeedInventory.GetCount(CropType.Carrot), Is.EqualTo(exportedSeeds[1]));
            Assert.That(newProgression.ActiveOrdersCount, Is.EqualTo(exportedOrders.Length));

            UnityEngine.Object.DestroyImmediate(newRunner);
        }
    }
}

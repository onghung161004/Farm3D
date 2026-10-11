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
    }
}

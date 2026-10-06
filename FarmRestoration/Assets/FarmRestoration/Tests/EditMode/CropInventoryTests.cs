using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class CropInventoryTests
    {
        [Test]
        public void Add_PumpkinIncreasesOnlyPumpkinCount()
        {
            CropInventory inventory = new CropInventory();

            inventory.Add(CropType.Pumpkin, 2);

            Assert.That(inventory.GetCount(CropType.Pumpkin), Is.EqualTo(2));
            Assert.That(inventory.GetCount(CropType.Carrot), Is.EqualTo(0));
            Assert.That(inventory.Total, Is.EqualTo(2));
        }

        [Test]
        public void Add_NonPositiveAmountDoesNotChangeInventory()
        {
            CropInventory inventory = new CropInventory();

            inventory.Add(CropType.Tomato, 0);

            Assert.That(inventory.Total, Is.EqualTo(0));
            Assert.That(inventory.GetCount(CropType.Tomato), Is.EqualTo(0));
        }

        [Test]
        public void Summary_ListsAllCropTypesInStableOrder()
        {
            CropInventory inventory = new CropInventory();
            inventory.Add(CropType.Carrot, 1);
            inventory.Add(CropType.Pumpkin, 2);

            Assert.That(inventory.Summary(), Is.EqualTo("Pumpkin: 2  Carrot: 1  Tomato: 0"));
        }

        [Test]
        public void TrySpend_InsufficientStockLeavesInventoryUntouched()
        {
            CropInventory inventory = new CropInventory();
            inventory.Add(CropType.Pumpkin, 2);

            Assert.That(inventory.TrySpend(CropType.Pumpkin, 3), Is.False);
            Assert.That(inventory.GetCount(CropType.Pumpkin), Is.EqualTo(2));
            Assert.That(inventory.Total, Is.EqualTo(2));
        }

        [Test]
        public void RestoreAndSpend_UpdateAllCountsConsistently()
        {
            CropInventory inventory = new CropInventory();
            inventory.Restore(new[] { 4, 3, 2 });

            Assert.That(inventory.TrySpend(CropType.Carrot, 2), Is.True);
            Assert.That(inventory.Snapshot(), Is.EqualTo(new[] { 4, 1, 2 }));
            Assert.That(inventory.Total, Is.EqualTo(7));
        }

        [Test]
        public void FarmSaveData_JsonRoundTripPreservesPlotDeadlineAndEconomy()
        {
            FarmSaveData saved = new FarmSaveData
            {
                crops = new[] { 2, 3, 4 },
                products = new[] { 1, 0, 2 },
                coins = 31,
                orderIndex = 4,
                repairs = new[] { true, false },
                plots = new[] { new PlotSaveData { id = "PumpkinGarden/PumpkinPlot_0", state = (int)FarmPlotState.Watered, readyUtcTicks = 123456789L } }
            };

            FarmSaveData loaded = UnityEngine.JsonUtility.FromJson<FarmSaveData>(UnityEngine.JsonUtility.ToJson(saved));

            Assert.That(loaded.coins, Is.EqualTo(31));
            Assert.That(loaded.orderIndex, Is.EqualTo(4));
            Assert.That(loaded.plots[0].readyUtcTicks, Is.EqualTo(123456789L));
            Assert.That(loaded.repairs[0], Is.True);
        }
    }
}

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
    }
}

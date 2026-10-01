using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class InventoryStateTests
    {
        [Test]
        public void AddCrop_WithPositiveAmount_IncreasesHarvestedCropsByThatAmount()
        {
            InventoryState inventory = new InventoryState();

            inventory.AddCrop(3);

            Assert.That(inventory.HarvestedCrops, Is.EqualTo(3));
        }

        [Test]
        public void AddCrop_WithZeroAmount_LeavesHarvestedCropsUnchanged()
        {
            InventoryState inventory = new InventoryState();
            inventory.AddCrop(2);

            inventory.AddCrop(0);

            Assert.That(inventory.HarvestedCrops, Is.EqualTo(2));
        }

        [Test]
        public void AddCrop_WithPositiveAmount_PublishesTheUpdatedCropCountOnce()
        {
            InventoryState inventory = new InventoryState();
            int notificationCount = 0;
            int reportedCount = -1;
            inventory.HarvestedCropsChanged += count =>
            {
                notificationCount++;
                reportedCount = count;
            };

            inventory.AddCrop(2);

            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(reportedCount, Is.EqualTo(2));
        }

        [Test]
        public void AddCrop_WithZeroAmount_DoesNotPublishACropCountChange()
        {
            InventoryState inventory = new InventoryState();
            int notificationCount = 0;
            inventory.HarvestedCropsChanged += _ => notificationCount++;

            inventory.AddCrop(0);

            Assert.That(notificationCount, Is.EqualTo(0));
        }
    }
}

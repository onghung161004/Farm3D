using System.Collections.Generic;
using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class VillageHouseLayoutTests
    {
        [Test]
        public void PlannedSevenHomesKeepClearOfEachOtherAndReservedZones()
        {
            Assert.That(VillageHouseLayout.Homes.Count, Is.EqualTo(7));
            Assert.That(VillageHouseLayout.Validate(VillageHouseLayout.Homes), Is.True);
        }

        [Test]
        public void DuplicatePositionOrNameIsRejected()
        {
            List<VillageHouseSlot> slots = new List<VillageHouseSlot>(VillageHouseLayout.Homes);
            slots[1] = new VillageHouseSlot("WoodenHouseNorth", slots[0].Position.x,
                slots[0].Position.y, 0.8f, 0f);
            Assert.That(VillageHouseLayout.Validate(slots), Is.False);
            slots[1] = new VillageHouseSlot(slots[0].Name, -66f, 79f, 0.8f, 0f);
            Assert.That(VillageHouseLayout.Validate(slots), Is.False);
        }

        [Test]
        public void HouseOnRoadWellOrOutsideTerrainIsRejected()
        {
            List<VillageHouseSlot> slots = new List<VillageHouseSlot>(VillageHouseLayout.Homes);
            slots[0] = new VillageHouseSlot("WoodenHouseWest", -60f, 48f, 0.8f, 0f);
            Assert.That(VillageHouseLayout.Validate(slots), Is.False);
            slots[0] = new VillageHouseSlot("WoodenHouseWest", -63f, 50.5f, 0.8f, 0f);
            Assert.That(VillageHouseLayout.Validate(slots), Is.False);
            slots[0] = new VillageHouseSlot("WoodenHouseWest", -99f, 65f, 0.8f, 0f);
            Assert.That(VillageHouseLayout.Validate(slots), Is.False);
        }

        [Test]
        public void MissingRepairHouseAndEmptyLayoutAreRejected()
        {
            Assert.That(VillageHouseLayout.Validate(null), Is.False);
            Assert.That(VillageHouseLayout.Validate(new VillageHouseSlot[0]), Is.False);
            List<VillageHouseSlot> slots = new List<VillageHouseSlot>(VillageHouseLayout.Homes);
            slots[2] = new VillageHouseSlot("DifferentSouthHouse", -86f, 34f, 0.8f, 55f);
            Assert.That(VillageHouseLayout.Validate(slots), Is.False);
        }

        [Test]
        public void HouseGroundIsLevelAcrossFootprintAndUntouchedBeyondTransition()
        {
            VillageHouseSlot house = VillageHouseLayout.Homes[0];
            float Natural(float x, float z) => x * 0.3f + z * 0.2f;
            float center = Natural(house.Position.x, house.Position.y);
            Assert.That(VillageHouseLayout.LevelHouseGround(house.Position.x + 4f, house.Position.y, Natural),
                Is.EqualTo(center).Within(0.001f));
            Assert.That(VillageHouseLayout.LevelHouseGround(house.Position.x, house.Position.y + 5f, Natural),
                Is.EqualTo(center).Within(0.001f));
            Assert.That(VillageHouseLayout.LevelHouseGround(house.Position.x + 8f, house.Position.y, Natural),
                Is.EqualTo(Natural(house.Position.x + 8f, house.Position.y)).Within(0.001f));
        }
    }
}

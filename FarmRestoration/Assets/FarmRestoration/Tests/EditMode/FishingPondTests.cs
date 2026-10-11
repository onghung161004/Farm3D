using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FishingPondTests
    {
        [Test]
        public void BasinLeavesTerrainOutsidePondUnchanged()
        {
            Assert.That(FishingPondTerrain.Height(40f, 40f, 1.25f), Is.EqualTo(1.25f));
            Assert.That(FishingPondTerrain.Height(
                FishingPondTerrain.Center.x + FishingPondTerrain.OuterRadiusX,
                FishingPondTerrain.Center.y, 0.4f), Is.EqualTo(0.4f));
        }

        [Test]
        public void BasinCenterIsBelowWaterAndBlendIsContinuous()
        {
            Vector2 center = FishingPondTerrain.Center;
            float centerHeight = FishingPondTerrain.Height(center.x, center.y, 0.4f);
            float rimHeight = FishingPondTerrain.Height(center.x + FishingPondTerrain.OuterRadiusX, center.y, 0.4f);
            float nearRimHeight = FishingPondTerrain.Height(center.x + FishingPondTerrain.OuterRadiusX - 0.01f,
                center.y, 0.4f);
            Assert.That(centerHeight, Is.LessThan(FishingPondTerrain.WaterLevel - 0.5f));
            Assert.That(Mathf.Abs(rimHeight - nearRimHeight), Is.LessThan(0.02f));
        }

        [Test]
        public void PondClearingDoesNotIncludeCowSpawns()
        {
            Assert.That(FishingPondTerrain.IsNear(new Vector2(5f, 31f), 2f), Is.False);
            Assert.That(FishingPondTerrain.IsNear(new Vector2(29f, 23f), 2f), Is.False);
            Assert.That(FishingPondTerrain.IsNear(FishingPondTerrain.Center), Is.True);
        }
    }
}

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

        [Test]
        public void FishingSpotSetupIsIdempotent()
        {
            GameObject pond = new GameObject("FishingPondTest");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 10f;
            try
            {
                MeshCollider terrain = ground.GetComponent<MeshCollider>();
                FishingPondSpot first = FishingPondSpot.EnsureAt(pond.transform, terrain);
                FishingPondSpot second = FishingPondSpot.EnsureAt(pond.transform, terrain);
                Assert.That(first, Is.SameAs(second));
                Assert.That(pond.transform.childCount, Is.EqualTo(1));
                Assert.That(first.GetComponent<SphereCollider>().isTrigger, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(pond);
                Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void RiverFishingSpotsUseWaterMeshAndAreIdempotent()
        {
            GameObject valley = new GameObject("RiverTestValley");
            GameObject water = new GameObject("RiverWater", typeof(MeshFilter));
            water.transform.SetParent(valley.transform, false);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 20f;
            Mesh riverMesh = new Mesh();
            Vector3[] vertices = new Vector3[20 * 9];
            for (int row = 0; row < 20; row++)
            for (int column = 0; column < 9; column++)
                vertices[row * 9 + column] = new Vector3(column - 4f, -1f, row * 2f);
            riverMesh.vertices = vertices;
            water.GetComponent<MeshFilter>().sharedMesh = riverMesh;
            try
            {
                MeshCollider terrain = ground.GetComponent<MeshCollider>();
                Physics.SyncTransforms();
                Assert.That(FishingPondSpot.EnsureRiverSpots(valley.transform, terrain), Is.True);
                Assert.That(FishingPondSpot.EnsureRiverSpots(valley.transform, terrain), Is.False);
                for (int i = 1; i <= 3; i++)
                    Assert.That(valley.transform.Find("RiverFishingSpot_" + i), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(valley);
                Object.DestroyImmediate(ground);
                Object.DestroyImmediate(riverMesh);
            }
        }
    }
}

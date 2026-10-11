using System;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class CowHusbandryTests
    {
        [Test]
        public void PenDetectsInsideAndGateApproachCorrectly()
        {
            var pen = new GameObject("TestPen").AddComponent<CowPen>();
            try
            {
                pen.Configure(new Vector2(12f, 12f));
                pen.transform.position = new Vector3(-23f, 0f, 12f);
                Assert.That(pen.GatePosition, Is.EqualTo(new Vector3(-23f, 0f, 6f)));
                Assert.That(pen.Contains(new Vector3(-23f, 0f, 7f), 0.6f), Is.True);
                Assert.That(pen.Contains(new Vector3(-23f, 0f, 5f), 0.6f), Is.False);
                Assert.That(pen.AllowsStep(new Vector3(-23f, 0f, 5.9f), new Vector3(-23f, 0f, 6.1f)), Is.True);
                Assert.That(pen.AllowsStep(new Vector3(-18f, 0f, 12f), new Vector3(-16.9f, 0f, 12f)), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(pen.gameObject); }
        }

        [Test]
        public void ExpandedPenKeepsRearBoundaryAndAcceptsSeveralCows()
        {
            var pen = new GameObject("ExpandedPen").AddComponent<CowPen>();
            try
            {
                pen.transform.position = new Vector3(-24f, 0f, 9f);
                pen.Configure(new Vector2(8f, 18f));

                Assert.That(pen.GatePosition, Is.EqualTo(new Vector3(-24f, 0f, 0f)));
                Assert.That(pen.Footprint, Is.EqualTo(new Vector2(8f, 18f)));
                Assert.That(pen.Contains(new Vector3(-26f, 0f, 3f), 0.6f), Is.True);
                Assert.That(pen.Contains(new Vector3(-22f, 0f, 9f), 0.6f), Is.True);
                Assert.That(pen.Contains(new Vector3(-26f, 0f, 15f), 0.6f), Is.True);
                Assert.That(pen.Contains(new Vector3(-24f, 0f, -1f)), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(pen.gameObject); }
        }

        [Test]
        public void LeadRouteReachesPenThroughGateOnClearGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject penObject = new GameObject("RouteTestPen");
            try
            {
                ground.transform.localScale = Vector3.one * 10f;
                penObject.transform.position = new Vector3(8f, 0f, 8f);
                CowPen pen = penObject.AddComponent<CowPen>();
                pen.Configure(new Vector2(8f, 12f));
                Physics.SyncTransforms();
                Vector3[] route = CowLeadPathfinder.Find(new Vector3(0f, 0f, -8f), pen,
                    ground.GetComponent<MeshCollider>(), null, null, null);
                Assert.That(route.Length, Is.GreaterThanOrEqualTo(4));
                Assert.That(pen.Contains(route[route.Length - 1], 0.6f), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(penObject);
                UnityEngine.Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void LeadRouteDetoursAroundSolidObstacle()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject penObject = new GameObject("RouteObstaclePen");
            try
            {
                ground.transform.localScale = Vector3.one * 10f;
                obstacle.transform.position = new Vector3(4f, 1f, -4f);
                obstacle.transform.localScale = new Vector3(3f, 2f, 3f);
                penObject.transform.position = new Vector3(8f, 0f, 8f);
                CowPen pen = penObject.AddComponent<CowPen>();
                pen.Configure(new Vector2(8f, 12f));
                Physics.SyncTransforms();
                Vector3[] route = CowLeadPathfinder.Find(new Vector3(0f, 0f, -8f), pen,
                    ground.GetComponent<MeshCollider>(), null, null, null);
                Assert.That(route.Length, Is.GreaterThan(4));
                foreach (Vector3 point in route)
                    Assert.That(obstacle.GetComponent<Collider>().bounds.Contains(point), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(penObject);
                UnityEngine.Object.DestroyImmediate(obstacle);
                UnityEngine.Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void LeadRouteTreatsRaisedRoadAsWalkableFloor()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject penObject = new GameObject("RoadRoutePen");
            try
            {
                ground.transform.localScale = Vector3.one * 10f;
                road.transform.position = new Vector3(0f, 0.4f, 0f);
                road.transform.localScale = new Vector3(2f, 1f, 2f);
                road.transform.rotation = Quaternion.Euler(0f, 0f, 10f);
                penObject.transform.position = new Vector3(0f, 0f, 8f);
                CowPen pen = penObject.AddComponent<CowPen>();
                pen.Configure(new Vector2(8f, 12f));
                Physics.SyncTransforms();

                Vector3[] route = CowLeadPathfinder.Find(new Vector3(0f, 0f, -8f), pen,
                    ground.GetComponent<MeshCollider>(), null, null, null);

                Assert.That(route.Length, Is.GreaterThanOrEqualTo(4));
                Assert.That(pen.Contains(route[route.Length - 1], 0.6f), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(penObject);
                UnityEngine.Object.DestroyImmediate(road);
                UnityEngine.Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void LeadRouteUsesBridgeInsteadOfCrossingRiverWater()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject riverObject = new GameObject("TestRiver", typeof(MeshFilter));
            GameObject penObject = new GameObject("BridgeRoutePen");
            Mesh riverMesh = new Mesh();
            try
            {
                ground.transform.localScale = Vector3.one * 10f;
                bridge.transform.position = Vector3.up * 0.35f;
                bridge.transform.localScale = new Vector3(2f, 1f, 0.6f);
                Vector3[] vertices = new Vector3[18];
                for (int row = 0; row < 2; row++)
                for (int column = 0; column < 9; column++)
                    vertices[row * 9 + column] = new Vector3(column - 4f, 0f, row == 0 ? -30f : 30f);
                riverMesh.vertices = vertices;
                riverObject.GetComponent<MeshFilter>().sharedMesh = riverMesh;
                penObject.transform.position = new Vector3(12f, 0f, 6f);
                CowPen pen = penObject.AddComponent<CowPen>();
                pen.Configure(new Vector2(8f, 12f));
                Physics.SyncTransforms();

                Vector3[] withoutBridge = CowLeadPathfinder.Find(new Vector3(-12f, 0f, 0f),
                    pen, ground.GetComponent<MeshCollider>(), riverObject.GetComponent<MeshFilter>(), null, null);
                Vector3[] withBridge = CowLeadPathfinder.Find(new Vector3(-12f, 0f, 0f),
                    pen, ground.GetComponent<MeshCollider>(), riverObject.GetComponent<MeshFilter>(), null, null,
                    bridge.GetComponent<MeshCollider>());

                Assert.That(withoutBridge, Is.Empty);
                Assert.That(withBridge.Length, Is.GreaterThanOrEqualTo(4));
                Assert.That(pen.Contains(withBridge[withBridge.Length - 1], 0.6f), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(penObject);
                UnityEngine.Object.DestroyImmediate(riverObject);
                UnityEngine.Object.DestroyImmediate(bridge);
                UnityEngine.Object.DestroyImmediate(ground);
                UnityEngine.Object.DestroyImmediate(riverMesh);
            }
        }

        [Test]
        public void FeedingRequiresPenAndOnlyOneOutstandingMilkBatch()
        {
            var cow = new GameObject("TestCow").AddComponent<CowAnimal>();
            try
            {
                Assert.That(cow.CanFeed, Is.False);
                cow.Restore(new CowSaveData { id = "cow-1", penned = true });
                Assert.That(cow.CanFeed, Is.True);
                Assert.That(cow.Feed(DateTime.UtcNow), Is.True);
                Assert.That(cow.CanFeed, Is.False);
                Assert.That(cow.Feed(DateTime.UtcNow), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(cow.gameObject); }
        }

        [Test]
        public void MilkBecomesAvailableAfterSavedUtcDeadline()
        {
            var cow = new GameObject("TestCow").AddComponent<CowAnimal>();
            try
            {
                DateTime now = DateTime.UtcNow;
                cow.Restore(new CowSaveData { id = "cow-1", penned = true, milkReadyUtcTicks = now.AddSeconds(-1).Ticks });
                Assert.That(cow.CanMilk(now), Is.True);
                Assert.That(cow.Milk(now), Is.True);
                Assert.That(cow.Milk(now), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(cow.gameObject); }
        }

        [Test]
        public void OldSaveWithoutCowDataStillLoads()
        {
            FarmSaveData data = JsonUtility.FromJson<FarmSaveData>("{\"version\":1,\"coins\":12}");
            Assert.That(data.version, Is.EqualTo(1));
            Assert.That(data.milk, Is.Zero);
        }

        [Test]
        public void CowSaveRoundTripPreservesDeadlineAndUnicodeIdentifier()
        {
            var source = new FarmSaveData
            {
                milk = 2,
                cows = new[] { new CowSaveData { id = "bò-sữa-1", penned = true, x = 0f, z = 7f, milkReadyUtcTicks = 638000000000000000L } }
            };

            FarmSaveData restored = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(source));

            Assert.That(restored.milk, Is.EqualTo(2));
            Assert.That(restored.cows[0].id, Is.EqualTo("bò-sữa-1"));
            Assert.That(restored.cows[0].x, Is.Zero);
            Assert.That(restored.cows[0].milkReadyUtcTicks, Is.EqualTo(638000000000000000L));
        }

        [Test]
        public void RestoreNullCowSnapshotDoesNothing()
        {
            var cow = new GameObject("TestCow").AddComponent<CowAnimal>();
            try { Assert.DoesNotThrow(() => cow.Restore(null)); }
            finally { UnityEngine.Object.DestroyImmediate(cow.gameObject); }
        }
    }
}

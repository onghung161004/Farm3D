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
                Assert.That(pen.Contains(new Vector3(-23f, 0f, 7f), 0.6f), Is.True);
                Assert.That(pen.Contains(new Vector3(-23f, 0f, 5f), 0.6f), Is.False);
                Assert.That(pen.AllowsStep(new Vector3(-23f, 0f, 5.9f), new Vector3(-23f, 0f, 6.1f)), Is.True);
                Assert.That(pen.AllowsStep(new Vector3(-18f, 0f, 12f), new Vector3(-16.9f, 0f, 12f)), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(pen.gameObject); }
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

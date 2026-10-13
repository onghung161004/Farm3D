using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class PlayerMapBoundaryTests
    {
        private GameObject ground;
        private PlayerMapBoundary boundary;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Địa hình α";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            Physics.SyncTransforms();
            boundary = new PlayerMapBoundary();
            boundary.Configure(ground.GetComponent<MeshCollider>());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(ground);

        [TestCase(-100f, -100f, -18f, -18f)]
        [TestCase(100f, -100f, 18f, -18f)]
        [TestCase(-100f, 100f, -18f, 18f)]
        [TestCase(100f, 100f, 18f, 18f)]
        public void CornersStopTwoMetresBeforeMapEdgeAndPreserveHeight(float x, float z, float expectedX, float expectedZ)
        {
            Vector3 constrained = boundary.Constrain(new Vector3(x, 7f, z), 0.5f);
            Assert.That(constrained.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(constrained.z, Is.EqualTo(expectedZ).Within(0.001f));
            Assert.That(constrained.y, Is.EqualTo(7f));
        }

        [Test]
        public void TranslatedTerrainAndLargerRadiusUseActualBounds()
        {
            ground.transform.position = new Vector3(30f, 4f, -15f);
            Physics.SyncTransforms();
            Assert.That(boundary.Constrain(new Vector3(100f, 9f, -100f), 3f),
                Is.EqualTo(new Vector3(47f, 9f, -32f)));
            Vector3 exactEdge = new Vector3(12f, 4f, 3f);
            Assert.That(boundary.Constrain(exactEdge, 0.5f), Is.EqualTo(exactEdge));
        }

        [Test]
        public void FallReturnsToLastSupportedPositionAndDoesNotKeepRescuing()
        {
            Assert.That(boundary.TryRecover(new Vector3(4f, 0.2f, 6f), out _), Is.False);
            Assert.That(boundary.TryRecover(new Vector3(8f, -4f, 8f), out Vector3 recovery), Is.True);
            Assert.That(Vector3.Distance(recovery, new Vector3(4f, 0.25f, 6f)), Is.LessThan(0.001f));
            Assert.That(boundary.TryRecover(recovery, out _), Is.False);
            Assert.That(boundary.TryRecover(new Vector3(100f, -20f, 100f), out Vector3 second), Is.True);
            Assert.That(Vector3.Distance(second, recovery), Is.LessThan(0.001f));
        }

        [Test]
        public void NoPreviousSafePositionProjectsToInteriorAndExactFallThresholdDoesNotTeleport()
        {
            Assert.That(boundary.TryRecover(new Vector3(2f, -3f, 2f), out _), Is.False);
            Assert.That(boundary.TryRecover(new Vector3(100f, -20f, 100f), out Vector3 recovery), Is.True);
            Assert.That(Vector3.Distance(recovery, new Vector3(18f, 0.25f, 18f)), Is.LessThan(0.001f));
        }

        [Test]
        public void AboveTerrainAndUnsupportedButNotFallingDoNotTeleport()
        {
            Assert.That(boundary.TryRecover(new Vector3(4f, 10f, 6f), out _), Is.False);
            Assert.That(boundary.TryRecover(new Vector3(100f, 5f, 100f), out _), Is.False);
        }

        [Test]
        public void MissingOrDisabledTerrainLeavesOtherScenesUnchanged()
        {
            Vector3 origin = new Vector3(100f, -20f, 100f);
            boundary.Configure(null);
            Assert.That(boundary.Constrain(origin, 0.5f), Is.EqualTo(origin));
            Assert.That(boundary.TryRecover(origin, out _), Is.False);
            boundary.Configure(ground.GetComponent<MeshCollider>());
            ground.SetActive(false);
            Assert.That(boundary.HasTerrain, Is.False);
            Assert.That(boundary.Constrain(origin, 0.5f), Is.EqualTo(origin));
        }
    }
}

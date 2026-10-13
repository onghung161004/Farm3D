using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class BridgeRailingCollisionTests
    {
        private GameObject root;
        private MeshCollider deck;
        private BridgeRailingCollision guard;
        private readonly System.Collections.Generic.List<Mesh> meshes = new System.Collections.Generic.List<Mesh>();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Deck", typeof(MeshCollider)); deck = root.GetComponent<MeshCollider>();
            deck.sharedMesh = Mesh(1);
            guard = BridgeRailingCollision.Ensure(deck);
        }
        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); foreach (Mesh mesh in meshes) Object.DestroyImmediate(mesh); meshes.Clear(); }

        private Mesh Mesh(float width)
        {
            var mesh = new Mesh(); meshes.Add(mesh);
            float[] rows = { -10, -5, 5, 10 };
            var vertices = new Vector3[8];
            for (int i = 0; i < 4; i++) for (int s = 0; s < 2; s++)
                vertices[i * 2 + s] = new Vector3(rows[i], 0, (s == 0 ? -1 : 1) * 1.65f * width);
            mesh.vertices = vertices;
            mesh.triangles = new[] { 0, 1, 2, 1, 3, 2, 2, 3, 4, 3, 5, 4, 4, 5, 6, 5, 7, 6 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private bool SideHit(Vector3 direction, out RaycastHit hit)
        {
            Physics.SyncTransforms();
            return Physics.SphereCast(root.transform.TransformPoint(new Vector3(0, 0.8f, 0)), 0.25f,
                root.transform.TransformDirection(direction), out hit, 15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        [Test]
        public void UnrepairedHasNoSideBarrierAndRepairEnablesBothSides()
        {
            Assert.That(SideHit(Vector3.forward, out _), Is.False);
            guard.SetRestored(true);
            Assert.That(SideHit(Vector3.forward, out RaycastHit right), Is.True);
            Assert.That(SideHit(Vector3.back, out RaycastHit left), Is.True);
            Assert.That(right.collider, Is.InstanceOf<BoxCollider>());
            Assert.That(left.collider, Is.InstanceOf<BoxCollider>());
            Assert.That(right.collider.isTrigger, Is.False);
            guard.SetRestored(false);
            Assert.That(SideHit(Vector3.forward, out _), Is.False);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void UpgradedWidthMovesBarrierOutAndLeavesEntrancesOpen(int factor)
        {
            guard.SetRestored(true);
            deck.sharedMesh = Mesh(factor); guard.Refresh();
            Assert.That(SideHit(Vector3.forward, out RaycastHit hit), Is.True);
            Assert.That(hit.distance, Is.EqualTo(1.65f * factor - 0.08f - 0.25f).Within(0.03f));
            Assert.That(SideHit(Vector3.right, out _), Is.False, "No end wall at entrance/exit.");
            Assert.That(SideHit(Vector3.left, out _), Is.False);
            Assert.That(root.GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo(6));
            Assert.That(BridgeRailingCollision.Ensure(deck), Is.SameAs(guard));
            guard.Refresh(); guard.SetRestored(true);
            Assert.That(root.GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo(6));
        }

        [Test]
        public void RotatedSlopedDeckPlacesRailsOnItsSurface()
        {
            root.transform.SetPositionAndRotation(new Vector3(12, 3, -18), Quaternion.Euler(0, 37, 6));
            guard.SetRestored(true);
            foreach (BoxCollider rail in root.GetComponentsInChildren<BoxCollider>())
            {
                Assert.That(rail.bounds.size.y, Is.GreaterThan(1));
                Assert.That(rail.enabled, Is.True);
            }
            Assert.That(SideHit(Vector3.forward, out _), Is.True);
        }

        [Test]
        public void InvalidGeometryAndDisabledDeckDoNotCreateInvisibleWalls()
        {
            Assert.That(BridgeRailingCollision.Ensure(null), Is.Null);
            guard.SetRestored(true); deck.enabled = false; guard.Refresh();
            Assert.That(root.GetComponentsInChildren<BoxCollider>().Length, Is.Zero);
            var odd = new Mesh { vertices = new[] { Vector3.zero, Vector3.one, Vector3.right, Vector3.up, Vector3.forward },
                triangles = new[] { 0, 2, 1 } }; meshes.Add(odd);
            deck.enabled = true; deck.sharedMesh = odd; guard.Refresh();
            Assert.That(root.GetComponentsInChildren<BoxCollider>().Length, Is.Zero);
        }

        [TestCase(false)] [TestCase(true)]
        public void CharacterCanStepOffOnlyBeforeRailsAreRestored(bool repaired)
        {
            guard.SetRestored(repaired);
            var player = new GameObject("Character", typeof(CharacterController));
            player.transform.SetParent(root.transform, false);
            player.transform.position = new Vector3(0, 0.05f, 0);
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.25f;
            controller.center = new Vector3(0, 0.9f, 0); controller.skinWidth = 0.01f;
            Physics.SyncTransforms();
            CollisionFlags flags = controller.Move(Vector3.forward * 4);
            Assert.That((flags & CollisionFlags.Sides) != 0, Is.EqualTo(repaired));
            if (repaired) Assert.That(player.transform.position.z, Is.LessThan(1.65f));
            else Assert.That(player.transform.position.z, Is.GreaterThan(3.9f));
        }
    }
}

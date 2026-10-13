using System;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class WoodcuttingGuideTests
    {
        [Test]
        public void NearestTargetSkipsStumpsAndDisabledTreesAndReturnsRegrownTree()
        {
            GameObject a = new GameObject("NearTree");
            GameObject b = new GameObject("FarTree");
            try
            {
                ChoppableTree near = a.AddComponent<ChoppableTree>();
                ChoppableTree far = b.AddComponent<ChoppableTree>();
                a.transform.position = Vector3.right;
                b.transform.position = Vector3.right * 6f;
                var trees = new[] { far, near, null };
                Assert.That(WoodcuttingGuide.FindNearest(Vector3.zero, trees), Is.SameAs(near));
                DateTime now = DateTime.UtcNow;
                for (int i = 0; i < 3; i++) near.TryChop(now, i, out _);
                Assert.That(WoodcuttingGuide.IsAvailable(near), Is.False);
                Assert.That(WoodcuttingGuide.FindNearest(Vector3.zero, trees), Is.SameAs(far));
                b.SetActive(false);
                Assert.That(WoodcuttingGuide.FindNearest(Vector3.zero, trees), Is.Null);
                near.State.Tick(now.AddSeconds(TreeHarvestState.RegrowthSeconds));
                Assert.That(WoodcuttingGuide.FindNearest(Vector3.zero, trees), Is.SameAs(near));
                near.enabled = false;
                Assert.That(WoodcuttingGuide.FindNearest(Vector3.zero, trees), Is.Null);
                Assert.That(WoodcuttingGuide.FindNearest(Vector3.zero, null), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }

        [Test]
        public void GroundRouteSamplesSlopedSurfaceAndEndsAtTreeApproach()
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            try
            {
                plane.transform.localScale = Vector3.one * 4f;
                plane.transform.rotation = Quaternion.Euler(0f, 0f, 10f);
                Physics.SyncTransforms();
                Vector3 destination = new Vector3(8f, 0f, 0f);
                Vector3[] route = CowLeadPathfinder.FindTo(new Vector3(-8f, 0f, 0f), destination,
                    plane.GetComponent<MeshCollider>(), null, null, null);
                Assert.That(route.Length, Is.GreaterThan(10));
                foreach (Vector3 point in route)
                    Assert.That(point.y, Is.EqualTo(point.x * Mathf.Tan(10f * Mathf.Deg2Rad) + 0.35f).Within(0.02f));
                Assert.That(route[route.Length - 1].x, Is.EqualTo(destination.x).Within(0.01f));
                Assert.That(CowLeadPathfinder.FindTo(Vector3.zero, destination, null, null, null, null), Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(plane); }
        }

        [Test]
        public void HudGuideInstallerIsIdempotentAndDoesNotInterceptTouch()
        {
            GameObject canvas = new GameObject("GuideCanvas", typeof(Canvas));
            GameObject player = new GameObject("GuidePlayer");
            try
            {
                PlayerToolController tools = player.AddComponent<PlayerToolController>();
                GameObject hudObject = new GameObject("Hud");
                hudObject.transform.SetParent(canvas.transform);
                hudObject.SetActive(false);
                FarmHud hud = hudObject.AddComponent<FarmHud>();
                WoodcuttingGuide.Ensure(hud, tools);
                WoodcuttingGuide.Ensure(hud, tools);
                Assert.That(canvas.GetComponentsInChildren<WoodcuttingGuide>().Length, Is.EqualTo(1));
                var guide = canvas.GetComponentInChildren<WoodcuttingGuide>();
                Assert.That(guide.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.False);
                foreach (UnityEngine.UI.Graphic graphic in guide.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                    Assert.That(graphic.raycastTarget, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); UnityEngine.Object.DestroyImmediate(player); }
        }
    }
}

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class WoodcuttingTests
    {
        private readonly DateTime now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ThirdHitFellsTreeExactlyOnceAndDeadlineRegrowsIt()
        {
            var state = new TreeHarvestState();
            Assert.That(state.TryHit(now, out bool felled), Is.True);
            Assert.That(felled, Is.False);
            Assert.That(state.TryHit(now, out felled), Is.True);
            Assert.That(felled, Is.False);
            Assert.That(state.TryHit(now, out felled), Is.True);
            Assert.That(felled, Is.True);
            Assert.That(state.TryHit(now, out felled), Is.False);
            Assert.That(felled, Is.False, "A stump must not award a second batch of wood.");
            Assert.That(state.Tick(now.AddSeconds(299.99)), Is.False);
            Assert.That(state.Tick(now.AddSeconds(300)), Is.True);
            Assert.That(state.HitsRemaining, Is.EqualTo(3));
            Assert.That(state.TryHit(now.AddSeconds(300), out felled), Is.True);
            Assert.That(felled, Is.False);
        }

        [Test]
        public void RestorePartialAndFelledUnicodeTreesDoesNotAwardWood()
        {
            var source = new TreeHarvestState();
            source.TryHit(now, out _);
            var data = new FarmSaveData { trees = new[] { source.Snapshot("cây-thông-α") }, wood = 7 };
            FarmSaveData restored = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(data));
            var state = new TreeHarvestState();
            state.Restore(restored.trees[0], now);
            Assert.That(state.HitsRemaining, Is.EqualTo(2));
            Assert.That(restored.wood, Is.EqualTo(7));
            Assert.That(restored.trees[0].id, Is.EqualTo("cây-thông-α"));
            source.TryHit(now, out _);
            source.TryHit(now, out _);
            state.Restore(source.Snapshot("tree"), now.AddSeconds(100));
            Assert.That(state.IsStump, Is.True);
            Assert.That(state.TryHit(now.AddSeconds(100), out _), Is.False);
            state.Restore(source.Snapshot("tree"), now.AddSeconds(301));
            Assert.That(state.IsStump, Is.False, "Offline elapsed time should restore a grown tree.");
        }

        [Test]
        public void ChopRejectsSpamAndWrongToolWithoutTakingAnotherHit()
        {
            var obj = new GameObject("TreeTest");
            try
            {
                ChoppableTree tree = obj.AddComponent<ChoppableTree>();
                Assert.That(tree.TryInteract(FarmTool.Hoe), Is.False);
                Assert.That(tree.State.Hits, Is.Zero);
                Assert.That(tree.TryChop(now, 0f, out _), Is.True);
                Assert.That(tree.TryChop(now, 0.1f, out _), Is.False);
                Assert.That(tree.State.Hits, Is.EqualTo(1));
                Assert.That(tree.TryChop(now, ChoppableTree.HitInterval, out _), Is.True);
                Assert.That(tree.State.Hits, Is.EqualTo(2));
                Assert.DoesNotThrow(() => tree.Restore(null));
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }

        [Test]
        public void ForestSetupWrapsTwelvePinesOnceAndLeavesFruitTreesAlone()
        {
            var root = new GameObject("ForestSetupTest");
            try
            {
                var forestObject = new GameObject("Trees");
                forestObject.transform.SetParent(root.transform);
                for (int i = 0; i < 14; i++)
                {
                    GameObject pine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pine.name = "PT_Pine_" + i;
                    pine.transform.SetParent(forestObject.transform);
                    pine.transform.position = new Vector3(i * 2f, 1f, i * 2f);
                }
                var fruit = new GameObject("PT_Fruit_Apple");
                fruit.transform.SetParent(forestObject.transform);
                WoodcuttingSetup.EnsureTrees(forestObject.transform, root.transform);
                WoodcuttingSetup.EnsureTrees(forestObject.transform, root.transform);
                ChoppableTree[] trees = root.GetComponentsInChildren<ChoppableTree>();
                Assert.That(trees.Length, Is.EqualTo(12));
                Assert.That(fruit.transform.parent, Is.SameAs(forestObject.transform));
                Assert.That(forestObject.transform.childCount, Is.EqualTo(3));
                Assert.That(trees[0].TreeId, Is.EqualTo("wood-tree/PT_Pine_0"));
                Assert.That(trees[0].GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(trees[0].transform.Find("TreeFallPivot"), Is.Not.Null);
                Assert.DoesNotThrow(() => WoodcuttingSetup.EnsureTrees(null, root.transform));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void HeldAxeAndTouchSlotAreCreatedOnceAndSelectAxe()
        {
            var root = new GameObject("AxePlayerTest");
            var hudObject = new GameObject("AxeHudTest");
            var eventsObject = new GameObject("AxeEvents", typeof(EventSystem));
            try
            {
                PlayerToolController tools = root.AddComponent<PlayerToolController>();
                var holder = new GameObject("ToolHolder");
                holder.transform.SetParent(root.transform);
                AxeToolVisual.Ensure(holder.transform);
                AxeToolVisual.Ensure(holder.transform);
                Assert.That(holder.transform.childCount, Is.EqualTo(1));
                Assert.That(holder.transform.Find("AxeTool"), Is.Not.Null);
                FarmHud hud = hudObject.AddComponent<FarmHud>();
                var bar = new GameObject("InventoryHotbar", typeof(RectTransform));
                bar.transform.SetParent(hudObject.transform);
                AxeHotbarButton.Ensure(hud, tools);
                AxeHotbarButton.Ensure(hud, tools);
                Assert.That(bar.transform.childCount, Is.EqualTo(1));
                Transform slot = bar.transform.Find("Slot_Axe");
                slot.GetComponent<HotbarToolButton>().OnPointerClick(new PointerEventData(eventsObject.GetComponent<EventSystem>()));
                Assert.That(tools.SelectedTool, Is.EqualTo(FarmTool.Axe));
                Assert.That(slot.localScale.x, Is.GreaterThan(1f));
                Assert.That(slot.GetComponent<Image>().raycastTarget, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(eventsObject);
                UnityEngine.Object.DestroyImmediate(hudObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}

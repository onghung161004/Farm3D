using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmQuestViewTests
    {
        private GameObject root;
        private FarmProgression game;
        private FarmHud hud;
        private PlayerMovement player;
        private FarmProgression previous;
        private static readonly FieldInfo Singleton = typeof(FarmProgression).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            previous = FarmProgression.Instance;
            Singleton.SetValue(null, null);
            root = new GameObject("QuestUiFixture", typeof(Canvas));
            var stock = new GameObject("Stock"); stock.transform.SetParent(root.transform); stock.SetActive(false);
            hud = stock.AddComponent<FarmHud>(); game = stock.AddComponent<FarmProgression>();
            typeof(FarmProgression).GetField("ready", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, true);
            var obj = new GameObject("QuestPlayer"); obj.transform.SetParent(root.transform);
            player = obj.AddComponent<PlayerMovement>();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); Singleton.SetValue(null, previous); }

        [Test]
        public void EnsureIsIdempotentAndModalOwnsOnlyItsMovementLock()
        {
            FarmQuestView.Ensure(game, hud); FarmQuestView.Ensure(game, hud);
            Assert.That(root.GetComponentsInChildren<FarmQuestView>(true).Length, Is.EqualTo(1));
            Assert.That(FarmQuestView.Show(), Is.True);
            Assert.That(FarmQuestView.IsOpen, Is.True);
            Assert.That(player.ControlsLocked, Is.True);
            Close();
            Assert.That(player.ControlsLocked, Is.False);
            player.SetControlsLocked(true);
            FarmQuestView.Show(); Close();
            Assert.That(player.ControlsLocked, Is.True, "Do not undo another system's lock.");
        }

        [Test]
        public void MissingHudDoesNotCreateUnusableModal()
        {
            FarmQuestView.Ensure(game, null);
            Assert.That(FarmQuestView.Show(), Is.False);
            Assert.That(FarmQuestView.IsOpen, Is.False);
        }

        [Test]
        public void OpenQuestPanelBlocksToolSelectionActionAndRecipe()
        {
            var tools = player.gameObject.AddComponent<PlayerToolController>();
            FarmQuestView.Ensure(game, hud);
            Assert.That(FarmQuestView.Show(), Is.True);
            tools.SelectTool(8);
            tools.SelectCrop(CropType.Carrot);
            tools.SetMobileInteractHeld(true);
            Assert.That(tools.SelectedTool, Is.EqualTo(FarmTool.Hoe));
            Assert.That(tools.TryUseTool(), Is.False);
            Assert.That(tools.TryCycleRecipe(), Is.False);
            Close();
        }

        [Test]
        public void WorldBoardUsesOneInstanceAndCompletionRevealsBenchWithoutMovingLandmark()
        {
            var well = new GameObject("VillageWell"); well.transform.SetParent(root.transform);
            Vector3 original = well.transform.position = new Vector3(10, 0, 20);
            FarmQuestBoard.Ensure(game); FarmQuestBoard.Ensure(game);
            var boards = root.GetComponentsInChildren<FarmQuestBoard>();
            Assert.That(boards.Length, Is.EqualTo(1));
            Transform bench = boards[0].transform.Find("RestoredVillageBench");
            Assert.That(bench.gameObject.activeSelf, Is.False);
            game.QuestProgress.claimedStage = 6;
            typeof(FarmQuestBoard).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boards[0], null);
            Assert.That(bench.gameObject.activeSelf, Is.True);
            Assert.That(well.transform.position, Is.EqualTo(original));
            foreach (Collider collider in bench.GetComponentsInChildren<Collider>()) Assert.That(collider.enabled, Is.False);
        }

        [Test]
        public void LifecycleHandlersReleaseLockAndStaticModalState()
        {
            FarmQuestView.Ensure(game, hud); FarmQuestView.Show();
            var view = root.GetComponentInChildren<FarmQuestView>();
            // EditMode does not reliably dispatch non-ExecuteAlways lifecycle messages.
            typeof(FarmQuestView).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            typeof(FarmQuestView).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            Object.DestroyImmediate(view.gameObject);
            Assert.That(player.ControlsLocked, Is.False);
            Assert.That(FarmQuestView.IsOpen, Is.False);
            Assert.That(game.GetComponent<FarmQuestNavigation>(), Is.Null);
        }

        private void Close()
        {
            var view = root.GetComponentInChildren<FarmQuestView>();
            typeof(FarmQuestView).GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
        }
    }
}

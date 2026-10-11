using System;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmPlotTests
    {
        private GameObject plotObject;
        private FarmPlot plot;
        private GameObject[] stateVisuals;

        [SetUp]
        public void SetUp()
        {
            plotObject = new GameObject("FarmPlotTest");
            plot = plotObject.AddComponent<FarmPlot>();
            stateVisuals = new[]
            {
                CreateVisual("UntilledVisual"),
                CreateVisual("TilledVisual"),
                CreateVisual("SeededVisual"),
                CreateVisual("WateredVisual"),
                CreateVisual("GrowingVisual"),
                CreateVisual("ReadyVisual")
            };
            plot.ConfigureVisuals(
                stateVisuals[0], stateVisuals[1], stateVisuals[2],
                stateVisuals[3], stateVisuals[4], stateVisuals[5]);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(plotObject);
        }

        [Test]
        public void TryInteract_HoeOnUntilledPlot_ChangesStateToTilled()
        {
            bool didInteract = plot.TryInteract(FarmTool.Hoe);

            Assert.That(didInteract, Is.True);
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Tilled));
            AssertOnlyVisualIsActive(1);
        }

        [Test]
        public void TryInteract_WrongTool_DoesNotChangeStateOrVisual()
        {
            FarmPlotState initialState = plot.CurrentState;

            bool didInteract = plot.TryInteract(FarmTool.Seeds);

            Assert.That(didInteract, Is.False);
            Assert.That(plot.CurrentState, Is.EqualTo(initialState));
            AssertOnlyVisualIsActive(0);
        }

        [Test]
        public void TryPlant_UsesChosenCropOnlyAfterTilling()
        {
            Assert.That(plot.TryPlant(CropType.Tomato), Is.False);
            Assert.That(plot.TryInteract(FarmTool.Hoe), Is.True);
            Assert.That(plot.TryPlant(CropType.Tomato), Is.True);
            Assert.That(plot.CropType, Is.EqualTo(CropType.Tomato));
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Seeded));
            Assert.That(plot.TryPlant(CropType.Carrot), Is.False);
            Assert.That(plot.CropType, Is.EqualTo(CropType.Tomato));
        }

        [Test]
        public void LockedExpansionPlot_RejectsAllFarmingActions()
        {
            GameObject garden = new GameObject("ExpansionGarden0");
            try
            {
                plotObject.transform.SetParent(garden.transform);
                plot.ConfigureRequiredGarden(0);
                Assert.That(plot.IsUnlocked, Is.False);
                Assert.That(plot.TryInteract(FarmTool.Hoe), Is.False);
                Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Untilled));
                Assert.That(plot.GetInteractionPrompt(FarmTool.Hoe), Does.Contain("30 coins"));
            }
            finally
            {
                plotObject.transform.SetParent(null);
                UnityEngine.Object.DestroyImmediate(garden);
            }
        }

        [Test]
        public void FullFarmingLoop_HarvestsExactlyOneCropAndResetsPlot()
        {
            int harvestEvents = 0;
            int harvestedAmount = 0;
            plot.Harvested += amount =>
            {
                harvestEvents++;
                harvestedAmount += amount;
            };

            Assert.That(plot.TryInteract(FarmTool.Hoe), Is.True);
            Assert.That(plot.TryInteract(FarmTool.Seeds), Is.True);
            Assert.That(plot.TryInteract(FarmTool.WateringCan), Is.True);
            Assert.That(plot.AdvanceGrowth(), Is.True);
            Assert.That(plot.AdvanceGrowth(), Is.True);
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.ReadyToHarvest));

            Assert.That(plot.TryInteract(FarmTool.Harvest), Is.True);
            Assert.That(plot.TryInteract(FarmTool.Harvest), Is.False);

            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Untilled));
            Assert.That(harvestEvents, Is.EqualTo(1));
            Assert.That(harvestedAmount, Is.EqualTo(1));
            AssertOnlyVisualIsActive(0);
        }

        [Test]
        public void AdvanceGrowth_WhenPlotIsNotWateredOrGrowing_DoesNotChangeStateOrVisual()
        {
            bool didAdvance = plot.AdvanceGrowth();

            Assert.That(didAdvance, Is.False);
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Untilled));
            AssertOnlyVisualIsActive(0);
        }

        [Test]
        public void WateredPlot_BecomesHarvestableAfterDeadlineWithoutKeyboardShortcut()
        {
            plot.TryInteract(FarmTool.Hoe);
            plot.TryInteract(FarmTool.Seeds);
            plot.TryInteract(FarmTool.WateringCan);
            Assert.That(plot.GrowthReadyUtcTicks, Is.GreaterThan(DateTime.UtcNow.Ticks));

            plot.TickGrowth(new DateTime(plot.GrowthReadyUtcTicks + 1, DateTimeKind.Utc));

            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.ReadyToHarvest));
            AssertOnlyVisualIsActive(5);
        }

        [Test]
        public void RainOnSeededPlot_StartsGrowthAndNotifiesOnce()
        {
            plot.TryInteract(FarmTool.Hoe);
            plot.TryInteract(FarmTool.Seeds);
            int changes = 0;
            plot.StateChanged += _ => changes++;
            DateTime now = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

            Assert.That(plot.TryWaterFromRain(now), Is.True);
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Watered));
            Assert.That(plot.GrowthReadyUtcTicks, Is.EqualTo(now.AddSeconds(75).Ticks));
            Assert.That(plot.TryWaterFromRain(now.AddSeconds(5)), Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void RainOnNonSeededPlot_DoesNotModifyStateOrDeadline()
        {
            DateTime now = DateTime.UtcNow;
            Assert.That(plot.TryWaterFromRain(now), Is.False);
            plot.TryInteract(FarmTool.Hoe);
            Assert.That(plot.TryWaterFromRain(now), Is.False);
            plot.TryInteract(FarmTool.Seeds);
            plot.TryInteract(FarmTool.WateringCan);
            long deadline = plot.GrowthReadyUtcTicks;
            Assert.That(plot.TryWaterFromRain(now), Is.False);
            Assert.That(plot.GrowthReadyUtcTicks, Is.EqualTo(deadline));
        }

        [TestCase(CropType.Pumpkin, 75)]
        [TestCase(CropType.Carrot, 45)]
        [TestCase(CropType.Tomato, 60)]
        public void RainWatering_KeepsCropSpecificGrowthTime(CropType type, int seconds)
        {
            plot.ConfigureCropType(type);
            plot.TryInteract(FarmTool.Hoe);
            plot.TryInteract(FarmTool.Seeds);
            DateTime now = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

            Assert.That(plot.TryWaterFromRain(now), Is.True);
            Assert.That(plot.GrowthReadyUtcTicks, Is.EqualTo(now.AddSeconds(seconds).Ticks));

            plot.Restore(FarmPlotState.Watered, plot.GrowthReadyUtcTicks, now.AddSeconds(1));
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Watered));
            Assert.That(plot.GrowthReadyUtcTicks, Is.EqualTo(now.AddSeconds(seconds).Ticks));
        }

        [Test]
        public void RestoreWateredPlot_AppliesTimeElapsedWhileGameWasClosed()
        {
            plot.Restore(FarmPlotState.Watered, DateTime.UtcNow.AddSeconds(-10).Ticks, DateTime.UtcNow);

            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.ReadyToHarvest));
            Assert.That(plot.GrowthReadyUtcTicks, Is.Zero);
        }

        private GameObject CreateVisual(string name)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(plotObject.transform);
            return visual;
        }

        private void AssertOnlyVisualIsActive(int activeIndex)
        {
            for (int index = 0; index < stateVisuals.Length; index++)
            {
                Assert.That(stateVisuals[index].activeSelf, Is.EqualTo(index == activeIndex),
                    "Unexpected active visual at index " + index);
            }
        }
    }
}

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
            Object.DestroyImmediate(plotObject);
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

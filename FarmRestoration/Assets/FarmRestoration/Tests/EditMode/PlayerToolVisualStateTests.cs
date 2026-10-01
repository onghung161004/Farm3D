using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class PlayerToolVisualStateTests
    {
        [TestCase(FarmTool.Hoe, "HoeTool")]
        [TestCase(FarmTool.Seeds, "SeedsTool")]
        [TestCase(FarmTool.WateringCan, "WateringCanTool")]
        [TestCase(FarmTool.Harvest, "HarvestTool")]
        public void GetVisualRootName_ReturnsStableNameForEveryTool(FarmTool tool, string expectedName)
        {
            Assert.That(PlayerToolVisualState.GetVisualRootName(tool), Is.EqualTo(expectedName));
        }

        [Test]
        public void GetUseBobOffset_AtMiddleOfUse_IsVisibleAndDownward()
        {
            Vector3 offset = PlayerToolVisualState.GetUseBobOffset(0.5f, 0.25f);

            Assert.That(offset.y, Is.LessThan(-0.2f));
        }

        [Test]
        public void TryAdvanceDemoGrowth_OnlyAdvancesFarmPlots()
        {
            GameObject plotObject = new GameObject("Plot");
            FarmPlot plot = plotObject.AddComponent<FarmPlot>();
            plot.TryInteract(FarmTool.Hoe);
            plot.TryInteract(FarmTool.Seeds);
            plot.TryInteract(FarmTool.WateringCan);

            bool advanced = FarmGrowthDemo.TryAdvance(plot);

            Assert.That(advanced, Is.True);
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Growing));
            Object.DestroyImmediate(plotObject);
        }

        [Test]
        public void Configure_AfterComponentCreation_LeavesVisualEnabledAndConfigured()
        {
            GameObject player = new GameObject("Player");
            PlayerToolController controller = player.AddComponent<PlayerToolController>();
            PlayerToolVisual visual = player.AddComponent<PlayerToolVisual>();
            GameObject holder = new GameObject("ToolHolder");
            holder.transform.SetParent(player.transform, false);

            visual.Configure(controller, holder.transform);

            Assert.That(visual.enabled, Is.True);
            Assert.That(visual.IsConfigured, Is.True);
            Object.DestroyImmediate(player);
        }
    }
}

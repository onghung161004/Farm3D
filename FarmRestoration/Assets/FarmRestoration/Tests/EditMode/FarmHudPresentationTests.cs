using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmHudPresentationTests
    {
        [Test]
        public void ToolText_WateringCan_UsesTheReadableToolName()
        {
            string text = FarmHudPresentation.ToolText(FarmTool.WateringCan);

            Assert.That(text, Is.EqualTo("Tool: Watering Can"));
        }

        [TestCase(FarmTool.Hoe, "Tool: Hoe")]
        [TestCase(FarmTool.Seeds, "Tool: Seeds")]
        [TestCase(FarmTool.WateringCan, "Tool: Watering Can")]
        [TestCase(FarmTool.Harvest, "Tool: Harvest")]
        public void ToolText_EachSupportedTool_UsesItsReadableName(FarmTool tool, string expected)
        {
            Assert.That(FarmHudPresentation.ToolText(tool), Is.EqualTo(expected));
        }

        [Test]
        public void PromptText_WithPlotPrompt_PrefixesTheAction()
        {
            string text = FarmHudPresentation.PromptText("Water seeds");

            Assert.That(text, Is.EqualTo("Action: Water seeds"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void PromptText_WithMissingPrompt_UsesTheMoveCloserGuidance(string prompt)
        {
            Assert.That(FarmHudPresentation.PromptText(prompt),
                Is.EqualTo("Action: Move closer to a farm plot"));
        }

        [Test]
        public void CropText_WithOneHarvest_DisplaysTheCropCount()
        {
            string text = FarmHudPresentation.CropText(1);

            Assert.That(text, Is.EqualTo("Crops: 1"));
        }

        [Test]
        public void CropText_WithNegativeCount_ClampsTheDisplayToZero()
        {
            Assert.That(FarmHudPresentation.CropText(-1), Is.EqualTo("Crops: 0"));
        }

        [Test]
        public void ReadabilityStyle_UsesLargeTypeAndTwoLineControls()
        {
            FarmHudReadabilityStyle style = FarmHudReadabilityStyle.Default;

            Assert.That(style.ToolFontSize, Is.GreaterThanOrEqualTo(42f));
            Assert.That(style.ActionFontSize, Is.GreaterThanOrEqualTo(34f));
            Assert.That(style.CropsFontSize, Is.GreaterThanOrEqualTo(32f));
            Assert.That(style.ControlsFontSize, Is.GreaterThanOrEqualTo(26f));
            Assert.That(style.ControlsText, Does.Contain("\n"));
        }
    }
}

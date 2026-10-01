using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class PlayerToolSelectionTests
    {
        [TestCase(1, FarmTool.Hoe)]
        [TestCase(2, FarmTool.Seeds)]
        [TestCase(3, FarmTool.WateringCan)]
        [TestCase(4, FarmTool.Harvest)]
        public void TryGetTool_ValidShortcut_ReturnsMappedTool(int shortcutIndex, FarmTool expectedTool)
        {
            bool mapped = PlayerToolSelection.TryGetTool(shortcutIndex, out FarmTool tool);

            Assert.That(mapped, Is.True);
            Assert.That(tool, Is.EqualTo(expectedTool));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void TryGetTool_InvalidShortcut_ReturnsFalseAndHoeFallback(int shortcutIndex)
        {
            bool mapped = PlayerToolSelection.TryGetTool(shortcutIndex, out FarmTool tool);

            Assert.That(mapped, Is.False);
            Assert.That(tool, Is.EqualTo(FarmTool.Hoe));
        }
    }
}

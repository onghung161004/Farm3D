using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmZoneLayoutTests
    {
        [Test]
        public void DefaultPlan_StaysWithinMobileObjectBudget()
        {
            FarmZoneLayout plan = FarmZoneLayout.CreateDefault();

            Assert.That(plan.TotalObjectCount, Is.LessThanOrEqualTo(FarmZoneLayout.MobileObjectBudget));
            Assert.That(plan.TotalObjectCount, Is.GreaterThan(0));
        }

        [Test]
        public void DefaultPlan_ContainsAllRequiredNamedAreas()
        {
            FarmZoneLayout plan = FarmZoneLayout.CreateDefault();

            Assert.That(plan.ContainsArea(FarmZoneArea.CottageWest), Is.True);
            Assert.That(plan.ContainsArea(FarmZoneArea.ActiveFieldCenter), Is.True);
            Assert.That(plan.ContainsArea(FarmZoneArea.MarketEast), Is.True);
            Assert.That(plan.ContainsArea(FarmZoneArea.OrchardNorth), Is.True);
            Assert.That(plan.ContainsArea(FarmZoneArea.EntranceSouth), Is.True);
        }
    }
}

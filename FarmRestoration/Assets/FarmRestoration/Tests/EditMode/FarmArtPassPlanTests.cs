using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmArtPassPlanTests
    {
        [Test]
        public void DetailedArtPlan_StaysWithinMobileObjectBudget()
        {
            FarmZoneLayout basePlan = FarmZoneLayout.CreateDefault();

            Assert.That(FarmArtPassPlan.FitsMobileBudget(basePlan.TotalObjectCount, FarmArtPassPlan.AddedZonePropBudget), Is.True);
            Assert.That(FarmArtPassPlan.ExpectedDetailedZonePropCount, Is.LessThanOrEqualTo(FarmZoneLayout.MobileObjectBudget));
        }

        [Test]
        public void DetailedArtPlan_RejectsAnOverBudgetAddition()
        {
            Assert.That(FarmArtPassPlan.FitsMobileBudget(220, 31), Is.False);
        }
    }
}

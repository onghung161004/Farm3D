namespace FarmRestoration
{
    /// <summary>Pure limits used by the editor art pass so the decorative scene stays mobile-friendly.</summary>
    public static class FarmArtPassPlan
    {
        public const int AddedZonePropBudget = 48;
        public const int ExpectedDetailedZonePropCount = 219;

        public static bool FitsMobileBudget(int existingZonePropCount, int addedZonePropCount)
        {
            return existingZonePropCount >= 0
                && addedZonePropCount >= 0
                && addedZonePropCount <= AddedZonePropBudget
                && existingZonePropCount + addedZonePropCount <= FarmZoneLayout.MobileObjectBudget;
        }
    }
}

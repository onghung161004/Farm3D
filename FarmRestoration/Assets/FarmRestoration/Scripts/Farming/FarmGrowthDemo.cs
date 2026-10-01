namespace FarmRestoration
{
    /// <summary>Temporary keyboard-demo growth control. It advances only the nearby FarmPlot.</summary>
    public static class FarmGrowthDemo
    {
        public static bool TryAdvance(IInteractable target)
        {
            FarmPlot plot = target as FarmPlot;
            return plot != null && plot.AdvanceGrowth();
        }
    }
}

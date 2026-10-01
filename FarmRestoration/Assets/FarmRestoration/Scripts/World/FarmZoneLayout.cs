using System.Collections.Generic;

namespace FarmRestoration
{
    public enum FarmZoneArea
    {
        CottageWest,
        ActiveFieldCenter,
        MarketEast,
        OrchardNorth,
        EntranceSouth
    }

    public sealed class FarmZoneLayout
    {
        public const int MobileObjectBudget = 250;

        private readonly Dictionary<FarmZoneArea, int> objectCounts;

        private FarmZoneLayout(Dictionary<FarmZoneArea, int> objectCounts)
        {
            this.objectCounts = objectCounts;
        }

        public int TotalObjectCount
        {
            get
            {
                int total = 0;
                foreach (int count in objectCounts.Values) total += count;
                return total;
            }
        }

        public bool ContainsArea(FarmZoneArea area)
        {
            return objectCounts.ContainsKey(area);
        }

        public static FarmZoneLayout CreateDefault()
        {
            return new FarmZoneLayout(new Dictionary<FarmZoneArea, int>
            {
                { FarmZoneArea.CottageWest, 27 },
                { FarmZoneArea.ActiveFieldCenter, 43 },
                { FarmZoneArea.MarketEast, 16 },
                { FarmZoneArea.OrchardNorth, 48 },
                { FarmZoneArea.EntranceSouth, 37 }
            });
        }
    }
}

using System;

namespace FarmRestoration
{
    public sealed class InventoryState
    {
        public int HarvestedCrops { get; private set; }

        public event Action<int> HarvestedCropsChanged;

        public void AddCrop(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            HarvestedCrops += amount;
            HarvestedCropsChanged?.Invoke(HarvestedCrops);
        }
    }
}

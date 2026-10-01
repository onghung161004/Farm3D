using System;

namespace FarmRestoration
{
    public sealed class CropInventory
    {
        private readonly int[] counts = new int[3];

        public int Total { get; private set; }
        public event Action<CropType, int> Changed;

        public void Add(CropType cropType, int amount)
        {
            if (amount <= 0) return;
            int index = (int)cropType;
            counts[index] += amount;
            Total += amount;
            Changed?.Invoke(cropType, counts[index]);
        }

        public int GetCount(CropType cropType) => counts[(int)cropType];

        public string Summary()
        {
            return "Pumpkin: " + GetCount(CropType.Pumpkin)
                + "  Carrot: " + GetCount(CropType.Carrot)
                + "  Tomato: " + GetCount(CropType.Tomato);
        }
    }
}

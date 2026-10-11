using UnityEngine;

namespace FarmRestoration
{
    [CreateAssetMenu(fileName = "NewCropSeedData", menuName = "Farm Restoration/Crop Seed Data")]
    public sealed class CropSeedData : ScriptableObject
    {
        [SerializeField] private CropType cropType = CropType.Carrot;
        [SerializeField, Min(1)] private int seedCost = 2;
        [SerializeField, Min(1)] private int sellPrice = 5;
        [SerializeField, Min(5f)] private float growthSeconds = 45f;

        public CropType CropType => cropType;
        public int SeedCost => Mathf.Max(1, seedCost);
        public int SellPrice => Mathf.Max(1, sellPrice);
        public float GrowthSeconds => Mathf.Max(5f, growthSeconds);

        public void Configure(CropType crop, int cost, int sell, float growth)
        {
            cropType = crop;
            seedCost = Mathf.Max(1, cost);
            sellPrice = Mathf.Max(1, sell);
            growthSeconds = Mathf.Max(5f, growth);
        }
    }
}

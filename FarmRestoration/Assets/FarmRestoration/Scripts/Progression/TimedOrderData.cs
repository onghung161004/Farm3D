using System;
using UnityEngine;

namespace FarmRestoration
{
    [CreateAssetMenu(fileName = "NewTimedOrder", menuName = "Farm Restoration/Timed Order Data")]
    public sealed class TimedOrderData : ScriptableObject
    {
        [SerializeField] private string orderId = "Order_1";
        [SerializeField] private string customerName = "Village Market";
        [SerializeField] private CropType cropType = CropType.Carrot;
        [SerializeField] private bool isProduct;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(1)] private int baseReward = 15;
        [SerializeField, Min(1)] private int fastDeliveryBonus = 8;
        [SerializeField, Min(10f)] private float timeLimitSeconds = 120f;
        [SerializeField, Min(5f)] private float fastBonusSeconds = 45f;

        public string OrderId => orderId;
        public string CustomerName => customerName;
        public CropType CropType => cropType;
        public bool IsProduct => isProduct;
        public int Amount => Mathf.Max(1, amount);
        public int BaseReward => Mathf.Max(1, baseReward);
        public int FastDeliveryBonus => Mathf.Max(1, fastDeliveryBonus);
        public float TimeLimitSeconds => Mathf.Max(10f, timeLimitSeconds);
        public float FastBonusSeconds => Mathf.Max(5f, fastBonusSeconds);

        public void Configure(
            string id,
            string customer,
            CropType crop,
            bool product,
            int count,
            int reward,
            int bonus,
            float limit,
            float fastLimit)
        {
            orderId = id;
            customerName = customer;
            cropType = crop;
            isProduct = product;
            amount = Mathf.Max(1, count);
            baseReward = Mathf.Max(1, reward);
            fastDeliveryBonus = Mathf.Max(1, bonus);
            timeLimitSeconds = Mathf.Max(10f, limit);
            fastBonusSeconds = Mathf.Max(5f, fastLimit);
        }

        public string GetItemDescription()
        {
            if (!isProduct) return $"{Amount} {CropType}";
            switch (CropType)
            {
                case CropType.Carrot: return $"{Amount} Carrot Juice";
                case CropType.Tomato: return $"{Amount} Tomato Sauce";
                default: return $"{Amount} Pumpkin Soup";
            }
        }
    }

    [Serializable]
    public sealed class ActiveTimedOrder
    {
        public string id;
        public string customer;
        public CropType cropType;
        public bool isProduct;
        public int amount;
        public int baseReward;
        public int fastBonus;
        public long expiryUtcTicks;
        public long fastBonusUtcTicks;

        public bool IsExpired(DateTime now) => now.Ticks >= expiryUtcTicks;
        public bool IsFastDelivery(DateTime now) => now.Ticks <= fastBonusUtcTicks;
        public int RemainingSeconds(DateTime now) => Math.Max(0, (int)Math.Ceiling((expiryUtcTicks - now.Ticks) / (double)TimeSpan.TicksPerSecond));
        public int FastBonusRemainingSeconds(DateTime now) => Math.Max(0, (int)Math.Ceiling((fastBonusUtcTicks - now.Ticks) / (double)TimeSpan.TicksPerSecond));
        public int TotalReward(DateTime now) => baseReward + (IsFastDelivery(now) ? fastBonus : 0);

        public string ItemDescription
        {
            get
            {
                if (!isProduct) return $"{amount} {cropType}";
                switch (cropType)
                {
                    case CropType.Carrot: return $"{amount} Carrot Juice";
                    case CropType.Tomato: return $"{amount} Tomato Sauce";
                    default: return $"{amount} Pumpkin Soup";
                }
            }
        }
    }
}

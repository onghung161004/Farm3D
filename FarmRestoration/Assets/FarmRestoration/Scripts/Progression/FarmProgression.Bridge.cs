using System;

namespace FarmRestoration
{
    public sealed partial class FarmProgression
    {
        private int bridgeUpgradeLevel;
        public int BridgeUpgradeLevel => bridgeUpgradeLevel;
        public static int BridgeCoinCost(int level) => level == 0 ? 40 : level == 1 ? 60 : 0;
        public static int BridgeWoodCost(int level) => level == 0 ? 15 : level == 1 ? 25 : 0;
        public static int RestoreBridgeUpgradeLevel(int saved, bool repaired) => repaired ? Math.Max(0, Math.Min(2, saved)) : 0;

        public bool TryUpgradeBridge(BridgeWidening bridge)
        {
            if (!ready || !IsRepaired(1) || bridgeUpgradeLevel < 0 || bridgeUpgradeLevel >= 2 || bridge == null || !bridge.CanUpgrade) return false;
            int coinCost = BridgeCoinCost(bridgeUpgradeLevel);
            int woodCost = BridgeWoodCost(bridgeUpgradeLevel);
            if (coins < coinCost || wood < woodCost || !bridge.Apply(bridgeUpgradeLevel + 1)) return false;
            coins -= coinCost;
            wood -= woodCost;
            bridgeUpgradeLevel++;
            CowBreedingHud.Show("Bridge upgraded: " + (bridgeUpgradeLevel + 1) + "x width!");
            NotifyAndSave();
            return true;
        }
    }
}

using UnityEngine;
using TMPro;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageRepairPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private int repairIndex;
        [SerializeField, Min(0)] private int price = 30;
        [SerializeField] private string repairName = "Village garden";
        [SerializeField] private GameObject oldVisual;
        [SerializeField] private GameObject repairedVisual;
        private VillageRestorationVisuals restoration;
        public int RepairIndex => repairIndex;

        public void Configure(int index, int cost, string label, GameObject visual, GameObject original = null)
        {
            repairIndex = index;
            price = Mathf.Max(0, cost);
            repairName = label;
            repairedVisual = visual;
            oldVisual = original;
        }

        public void RebindHouseVisual(GameObject original)
        {
            if (repairIndex == 0) oldVisual = original;
        }

        private FarmProgression game;

        private void Start()
        {
            restoration = VillageRestorationVisuals.Install(this);
            game = FarmProgression.Instance;
            if (game != null) game.Changed += RefreshVisual;
            RefreshVisual();
        }

        private void OnDestroy() { if (game != null) game.Changed -= RefreshVisual; }

        public bool TryInteract(FarmTool tool)
        {
            if (restoration == null && Application.isPlaying)
            {
                restoration = VillageRestorationVisuals.Install(this);
                if (restoration == null) return false; // Do not charge when the real visual cannot be repaired.
            }
            FarmProgression game = FarmProgression.Instance;
            if (game != null && repairIndex == 1 && game.IsRepaired(1))
            {
                BridgeWidening bridge = restoration != null ? restoration.GetComponent<BridgeWidening>() : null;
                return game.TryUpgradeBridge(bridge);
            }
            if (game == null || !game.TryRepair(repairIndex, price)) return false;
            RefreshVisual();
            return true;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null) return "Village repairs are loading";
            if (game.IsRepaired(repairIndex))
            {
                if (repairIndex != 1) return repairName + " restored";
                int level = game.BridgeUpgradeLevel;
                return level >= 2 ? "Bridge fully upgraded: 3x width"
                    : "Upgrade bridge to " + (level + 2) + "x width: " + FarmProgression.BridgeCoinCost(level)
                        + " coins + " + FarmProgression.BridgeWoodCost(level) + " wood (you have "
                        + game.Coins + " coins, " + game.WoodCount + " wood) [E / ACTION]";
            }
            return "Restore " + repairName + ": " + price + " coins + " + FarmProgression.RepairWoodCost(repairIndex)
                + " wood (you have " + game.Coins + " coins, " + game.WoodCount + " wood) [E]";
        }

        private void RefreshVisual()
        {
            bool isRestored = FarmProgression.Instance != null && FarmProgression.Instance.IsRepaired(repairIndex);
            FarmProgression current = FarmProgression.Instance;
            int level = current != null ? current.BridgeUpgradeLevel : 0;
            string status = repairIndex == 1 && isRestored
                    ? level >= 2 ? "BRIDGE UPGRADED\n3x WIDTH" : "UPGRADE BRIDGE\n" + (level + 2) + "x WIDTH\n"
                        + FarmProgression.BridgeCoinCost(level) + " COINS + " + FarmProgression.BridgeWoodCost(level) + " WOOD\n[E / ACTION]"
                    : (isRestored ? "RESTORED " : "RESTORE ") + (repairIndex == 0 ? "HOUSE" : "BRIDGE");
            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true)) label.text = status;
            // The original scene's floating signs still include legacy TextMesh components.
            foreach (TextMesh label in GetComponentsInChildren<TextMesh>(true)) label.text = status;
            if (restoration != null)
            {
                // Keep the real house/bridge; never enable the old replacement prefab or misplaced approach rails.
                if (repairedVisual != null) repairedVisual.SetActive(false);
                if (oldVisual != null) oldVisual.SetActive(true);
                restoration.ApplyState(isRestored);
                return;
            }
            if (repairedVisual != null) repairedVisual.SetActive(isRestored);
            if (oldVisual != null) oldVisual.SetActive(!isRestored);
        }
    }
}

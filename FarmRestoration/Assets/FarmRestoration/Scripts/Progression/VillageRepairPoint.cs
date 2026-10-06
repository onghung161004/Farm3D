using UnityEngine;

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

        public void Configure(int index, int cost, string label, GameObject visual, GameObject original = null)
        {
            repairIndex = index;
            price = Mathf.Max(0, cost);
            repairName = label;
            repairedVisual = visual;
            oldVisual = original;
        }

        private FarmProgression game;

        private void Start()
        {
            game = FarmProgression.Instance;
            if (game != null) game.Changed += RefreshVisual;
            RefreshVisual();
        }

        private void OnDestroy() { if (game != null) game.Changed -= RefreshVisual; }

        public bool TryInteract(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null || !game.TryRepair(repairIndex, price)) return false;
            RefreshVisual();
            return true;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null) return "Village repairs are loading";
            if (game.IsRepaired(repairIndex)) return repairName + " restored";
            return "Restore " + repairName + ": " + price + " coins (you have " + game.Coins + ")  [E]";
        }

        private void RefreshVisual()
        {
            bool isRestored = FarmProgression.Instance != null && FarmProgression.Instance.IsRepaired(repairIndex);
            if (repairedVisual != null) repairedVisual.SetActive(isRestored);
            if (oldVisual != null) oldVisual.SetActive(!isRestored);
        }
    }
}

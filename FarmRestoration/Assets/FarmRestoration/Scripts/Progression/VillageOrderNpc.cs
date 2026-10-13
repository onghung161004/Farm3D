using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageOrderNpc : MonoBehaviour, IInteractable
    {
        public bool TryInteract(FarmTool tool)
        {
            if (FarmProgression.Instance == null) return false;
            bool opened = VillageOrderDialogue.Show(this);
            if (opened) FarmProgression.Instance.RecordQuestTalk();
            return opened;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null) return "Orders are loading";

            System.DateTime now = System.DateTime.UtcNow;
            game.MaintainTimedOrders(now);

            ActiveTimedOrder deliverable = null;
            for (int i = 0; i < game.ActiveTimedOrders.Count; i++)
            {
                if (game.CanFulfillOrder(game.ActiveTimedOrders[i]))
                {
                    deliverable = game.ActiveTimedOrders[i];
                    break;
                }
            }

            if (deliverable != null)
            {
                return $"Trader: [E] Deliver {deliverable.ItemDescription} -> {deliverable.TotalReward(now)}c  |  [1/2/3] Seeds";
            }

            ActiveTimedOrder nearest = game.GetNearestDeadlineOrder(now);
            if (nearest != null)
            {
                return $"Trader: {nearest.ItemDescription} -> {nearest.TotalReward(now)}c ({nearest.RemainingSeconds(now)}s) [E] | Seeds [1/2/3]";
            }

            return "Village orders are loading...";
            return game == null ? "Orders are loading"
                : "Village order: " + game.OrderDescription() + " -> " + game.OrderReward + " coins  [E talk]";
        }

        private void Awake()
        {
            if (GetComponent<VillageTraderRoutine>() == null)
                gameObject.AddComponent<VillageTraderRoutine>();
        }
    }
}

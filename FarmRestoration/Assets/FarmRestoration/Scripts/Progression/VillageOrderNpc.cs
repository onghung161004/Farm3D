using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageOrderNpc : MonoBehaviour, IInteractable
    {
        public bool TryInteract(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            return game != null && game.TryDeliverOrder();
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            return game == null ? "Orders are loading"
                : "Village order: " + game.OrderDescription() + " -> " + game.OrderReward + " coins  [E deliver]";
        }
    }
}

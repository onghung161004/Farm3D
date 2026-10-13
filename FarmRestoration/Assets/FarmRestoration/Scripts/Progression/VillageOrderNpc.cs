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

using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmProductionStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private CropType crop = CropType.Pumpkin;
        [SerializeField] private bool servesTomatoToo;
        [SerializeField, Min(1)] private int cropCost = 2;
        private bool tomatoTurn;

        public void Configure(CropType type, int cost, bool addTomatoRecipe = false)
        {
            crop = type;
            cropCost = Mathf.Max(1, cost);
            servesTomatoToo = addTomatoRecipe;
        }

        private CropType CurrentCrop => servesTomatoToo && tomatoTurn ? CropType.Tomato : crop;

        public bool CycleRecipe()
        {
            if (!servesTomatoToo) return false;
            tomatoTurn = !tomatoTurn;
            return true;
        }

        public bool TryInteract(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            return game != null && game.TryProduce(CurrentCrop, cropCost);
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            FarmProgression game = FarmProgression.Instance;
            int stock = game == null ? 0 : game.ProductCount(CurrentCrop);
            return "Make " + FarmProgression.ProductName(CurrentCrop) + ": " + cropCost + " " + CurrentCrop
                + " [E]" + (servesTomatoToo ? " [R change]" : "") + " Stock: " + stock;
        }
    }
}

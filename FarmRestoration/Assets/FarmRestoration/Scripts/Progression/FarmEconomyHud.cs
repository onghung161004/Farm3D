using TMPro;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmEconomyHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        private FarmProgression game;

        public void Configure(TMP_Text text) { label = text; Refresh(); }

        private void Start()
        {
            game = FarmProgression.Instance;
            if (game != null) game.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy() { if (game != null) game.Changed -= Refresh; }

        private void Refresh()
        {
            if (label == null) return;
            FarmProgression state = game != null ? game : FarmProgression.Instance;
            label.text = state == null ? "Village loading..."
                : "COINS  " + state.Coins + "  MILK " + state.MilkCount
                + "\nSoup " + state.ProductCount(CropType.Pumpkin)
                + "  Juice " + state.ProductCount(CropType.Carrot)
                + "  Sauce " + state.ProductCount(CropType.Tomato)
                + "\nORDER  " + state.OrderDescription();
        }
    }
}

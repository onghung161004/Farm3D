using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class GardenPurchasePoint : MonoBehaviour, IInteractable
    {
        private const int Price = 30;
        [SerializeField, Range(0, 2)] private int gardenIndex;
        private TextMesh label;
        private FarmProgression progression;

        public void Configure(int index) => gardenIndex = Mathf.Clamp(index, 0, 2);

        private void Start()
        {
            label = GetComponent<TextMesh>();
            progression = FarmProgression.Instance;
            if (progression != null) progression.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (progression != null) progression.Changed -= Refresh;
        }

        public bool TryInteract(FarmTool tool)
        {
            if (progression == null || !progression.TryUnlockGarden(gardenIndex, Price)) return false;
            Refresh();
            return true;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            if (progression == null) return "Garden expansion is loading";
            if (progression.IsGardenUnlocked(gardenIndex)) return "Garden area " + (gardenIndex + 1) + " unlocked";
            return "Unlock 9 plots: " + Price + " coins (you have " + progression.Coins + ") [E]";
        }

        private void Refresh()
        {
            if (label == null) return;
            bool unlocked = progression != null && progression.IsGardenUnlocked(gardenIndex);
            label.text = unlocked ? "Area " + (gardenIndex + 1) + "\nunlocked"
                : "Area " + (gardenIndex + 1) + (Application.isMobilePlatform ? "\n30 coins [ACTION]" : "\n30 coins [E]");
            label.color = unlocked ? new Color(0.65f, 1f, 0.6f) : Color.white;
        }
    }
}

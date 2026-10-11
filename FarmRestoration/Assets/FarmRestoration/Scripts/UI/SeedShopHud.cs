using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class SeedShopHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject panelRoot;

        public static SeedShopHud Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (FarmProgression.Instance != null)
            {
                FarmProgression.Instance.Changed += RefreshDisplay;
                FarmProgression.Instance.SeedInventory.Changed += (crop, count) => RefreshDisplay();
            }
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Functional hotkeys for buying seeds directly
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame)
            {
                if (FarmProgression.Instance != null) FarmProgression.Instance.TryBuySeed(CropType.Carrot);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.gKey.wasPressedThisFrame)
            {
                if (FarmProgression.Instance != null) FarmProgression.Instance.TryBuySeed(CropType.Pumpkin);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame)
            {
                if (FarmProgression.Instance != null) FarmProgression.Instance.TryBuySeed(CropType.Tomato);
            }
        }

        public void RefreshDisplay()
        {
            if (statusText == null) statusText = GetComponentInChildren<TextMeshProUGUI>();
            if (statusText == null || FarmProgression.Instance == null) return;

            FarmProgression p = FarmProgression.Instance;
            statusText.text = $"Seeds: Carrot({p.SeedInventory.GetCount(CropType.Carrot)}) [F/1: 2c] | " +
                              $"Pumpkin({p.SeedInventory.GetCount(CropType.Pumpkin)}) [G/2: 4c] | " +
                              $"Tomato({p.SeedInventory.GetCount(CropType.Tomato)}) [H/3: 3c]";
        }
    }
}

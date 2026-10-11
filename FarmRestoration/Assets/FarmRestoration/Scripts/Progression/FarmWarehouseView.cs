using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmWarehouseView : MonoBehaviour
    {
        private static readonly string[] Names =
        {
            "Pumpkin", "Carrot", "Tomato", "Pumpkin soup", "Carrot juice", "Tomato sauce", "Milk", "Fish"
        };
        private static FarmWarehouseView instance;
        private TMP_Text summary;
        private TMP_Text[] rows;
        private Button sellButton;
        private int selected;
        private FarmProgression game;
        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        public static bool Show()
        {
            if (FarmProgression.Instance == null) return false;
            if (instance == null) instance = Create();
            if (instance == null) return false;
            instance.Open();
            return true;
        }

        private static FarmWarehouseView Create()
        {
            FarmHud hud = FindAnyObjectByType<FarmHud>();
            Canvas canvas = hud == null ? FindAnyObjectByType<Canvas>() : hud.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            GameObject panel = new GameObject("WarehousePanel", typeof(RectTransform), typeof(Image), typeof(FarmWarehouseView));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(490f, 500f);
            panel.GetComponent<Image>().color = new Color(0.055f, 0.11f, 0.13f, 0.98f);
            FarmWarehouseView view = panel.GetComponent<FarmWarehouseView>();
            view.summary = CreateText(panel.transform, "Summary", new Vector2(0f, 200f), new Vector2(455f, 72f), 18f);
            view.rows = new TMP_Text[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                int itemIndex = i;
                Button button = CreateButton(panel.transform, "Item" + i, new Vector2(0f, 137f - i * 40f), new Vector2(450f, 36f), () => view.Select(itemIndex));
                view.rows[i] = CreateText(button.transform, "Label", Vector2.zero, new Vector2(438f, 34f), 18f);
            }
            view.sellButton = CreateButton(panel.transform, "Sell", new Vector2(-112f, -208f), new Vector2(210f, 43f), view.SellSelected);
            CreateText(view.sellButton.transform, "Label", Vector2.zero, new Vector2(208f, 42f), 18f).text = "Sell 1";
            Button close = CreateButton(panel.transform, "Close", new Vector2(112f, -208f), new Vector2(210f, 43f), view.Close);
            CreateText(close.transform, "Label", Vector2.zero, new Vector2(208f, 42f), 18f).text = "Close";
            panel.SetActive(false);
            return view;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.20f, 0.34f, 0.29f, 0.96f);
            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(action);
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private void Open()
        {
            game = FarmProgression.Instance;
            game.Changed -= Refresh;
            game.Changed += Refresh;
            gameObject.SetActive(true);
            Refresh();
        }

        private void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, Names.Length - 1);
            Refresh();
        }

        private void SellSelected()
        {
            if (game != null && game.TrySell((WarehouseItemKind)selected)) Refresh();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void Close()
        {
            if (game != null) game.Changed -= Refresh;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (game != null) game.Changed -= Refresh;
            if (instance == this) instance = null;
        }

        private void Refresh()
        {
            if (game == null || rows == null) return;
            summary.text = "WAREHOUSE  |  Coins: " + game.Coins + "\nCurrent order: " + game.OrderDescription();
            for (int i = 0; i < rows.Length; i++)
            {
                WarehouseItemKind item = (WarehouseItemKind)i;
                rows[i].text = (i == selected ? "> " : "   ") + Names[i] + "  x" + game.StockCount(item)
                    + "     Sell 1: +" + FarmProgression.SellPrice(item) + " coins";
            }
            sellButton.interactable = game.StockCount((WarehouseItemKind)selected) > 0;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageOrderDialogue : MonoBehaviour
    {
        private static VillageOrderDialogue instance;
        private TMP_Text message;
        private VillageOrderNpc currentNpc;
        private VillageTraderRoutine routine;
        private string result = "";
        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        public static bool Show(VillageOrderNpc npc)
        {
            if (npc == null) return false;
            if (instance == null) instance = Create();
            if (instance == null) return false;
            instance.Open(npc);
            return true;
        }

        private static VillageOrderDialogue Create()
        {
            FarmHud hud = FindAnyObjectByType<FarmHud>();
            Canvas canvas = hud == null ? FindAnyObjectByType<Canvas>() : hud.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            GameObject panel = new GameObject("VillageOrderDialogue", typeof(RectTransform), typeof(Image), typeof(VillageOrderDialogue));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 360f);
            panel.GetComponent<Image>().color = new Color(0.055f, 0.11f, 0.13f, 0.98f);
            VillageOrderDialogue view = panel.GetComponent<VillageOrderDialogue>();
            view.message = CreateText(panel.transform, new Vector2(0f, 85f), new Vector2(560f, 135f));

            // Seed purchasing buttons (Row 1)
            CreateButton(panel.transform, "Carrot (2c) [1]", new Vector2(-190f, -25f), new Vector2(175f, 44f), view.BuyCarrot, new Color(0.18f, 0.35f, 0.25f), 19f);
            CreateButton(panel.transform, "Pumpkin (4c) [2]", new Vector2(0f, -25f), new Vector2(175f, 44f), view.BuyPumpkin, new Color(0.35f, 0.28f, 0.15f), 19f);
            CreateButton(panel.transform, "Tomato (3c) [3]", new Vector2(190f, -25f), new Vector2(175f, 44f), view.BuyTomato, new Color(0.38f, 0.20f, 0.20f), 19f);

            // Order delivery and close buttons (Row 2)
            CreateButton(panel.transform, "Deliver Order", new Vector2(-130f, -95f), new Vector2(230f, 48f), view.Deliver, new Color(0.22f, 0.40f, 0.32f), 20f);
            CreateButton(panel.transform, "Close [Esc]", new Vector2(130f, -95f), new Vector2(230f, 48f), view.Close, new Color(0.32f, 0.25f, 0.25f), 20f);

            panel.SetActive(false);
            return view;
        }

        private static TMP_Text CreateText(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.fontSize = 22f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.fontStyle = FontStyles.Bold;
            text.extraPadding = true;
            text.lineSpacing = 5f;
            FarmHud.OptimizeLabelRendering(text, 20f);
            return text;
        }

        private static void CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color color, float fontSize = 19f)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Button>().onClick.AddListener(action);
            TMP_Text text = CreateText(go.transform, Vector2.zero, rect.sizeDelta);
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.extraPadding = true;
            text.text = label;
            FarmHud.OptimizeLabelRendering(text, fontSize);
        }

        private void Open(VillageOrderNpc npc)
        {
            if (currentNpc != null && routine != null) routine.SetPaused(false);
            currentNpc = npc;
            routine = npc.GetComponent<VillageTraderRoutine>();
            if (routine != null) routine.SetPaused(true);
            result = "";
            gameObject.SetActive(true);
            Refresh();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Close();
            else if (keyboard.enterKey.wasPressedThisFrame) Deliver();
            else if (keyboard.digit1Key.wasPressedThisFrame) BuyCarrot();
            else if (keyboard.digit2Key.wasPressedThisFrame) BuyPumpkin();
            else if (keyboard.digit3Key.wasPressedThisFrame) BuyTomato();
        }

        public void BuyCarrot() => BuySeed(CropType.Carrot);
        public void BuyPumpkin() => BuySeed(CropType.Pumpkin);
        public void BuyTomato() => BuySeed(CropType.Tomato);

        private void BuySeed(CropType crop)
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null || !gameObject.activeSelf) return;
            int cost = game.GetSeedCost(crop);
            if (game.Coins < cost)
            {
                result = $"Not enough coins for {crop} seed! Need {cost} coins.";
            }
            else
            {
                bool bought = game.TryBuySeed(crop, 1);
                result = bought ? $"Bought 1 {crop} seed (-{cost} coins)" : "Could not buy seed!";
            }
            Refresh();
        }

        public void Deliver()
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null || !gameObject.activeSelf) return;
            int reward = game.OrderReward;
            result = game.TryDeliverOrder() ? "Delivered! +" + reward + " coins" : game.OrderMissingDescription();
            Refresh();
        }

        public void Close()
        {
            if (routine != null) routine.SetPaused(false);
            currentNpc = null;
            routine = null;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (routine != null) routine.SetPaused(false);
        }

        private void OnDestroy()
        {
            if (routine != null) routine.SetPaused(false);
            if (instance == this) instance = null;
        }

        private void Refresh()
        {
            FarmProgression game = FarmProgression.Instance;
            if (game == null)
            {
                message.text = "Orders are loading...";
                return;
            }
            string seedStock = game.SeedInventory != null
                ? $"Seeds: Carrot({game.SeedInventory.GetCount(CropType.Carrot)})  Pumpkin({game.SeedInventory.GetCount(CropType.Pumpkin)})  Tomato({game.SeedInventory.GetCount(CropType.Tomato)})"
                : "";
            string statusLine = string.IsNullOrEmpty(result) ? "" : $"\n<color=#FFD54F>{result}</color>";
            message.text = $"VILLAGE TRADER & SEED SHOP  (Coins: {game.Coins})\n" +
                $"{seedStock}\n" +
                $"Order: {game.OrderDescription()} -> {game.OrderReward} coins" +
                statusLine;
        }
    }
}

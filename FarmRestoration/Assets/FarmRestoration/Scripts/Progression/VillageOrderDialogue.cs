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
            rect.sizeDelta = new Vector2(440f, 215f);
            panel.GetComponent<Image>().color = new Color(0.055f, 0.11f, 0.13f, 0.97f);
            VillageOrderDialogue view = panel.GetComponent<VillageOrderDialogue>();
            view.message = CreateText(panel.transform, new Vector2(0f, 37f), new Vector2(408f, 125f));
            CreateButton(panel.transform, "Deliver", new Vector2(-100f, -76f), view.Deliver);
            CreateButton(panel.transform, "Close", new Vector2(100f, -76f), view.Close);
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
            text.fontSize = 20f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(170f, 42f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.37f, 0.31f);
            go.GetComponent<Button>().onClick.AddListener(action);
            TMP_Text text = CreateText(go.transform, Vector2.zero, rect.sizeDelta);
            text.text = label;
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
            message.text = game == null ? "Orders are loading..." :
                "VILLAGE TRADER\nOrder: " + game.OrderDescription() + "\nReward: " + game.OrderReward + " coins\n" + result;
        }
    }
}

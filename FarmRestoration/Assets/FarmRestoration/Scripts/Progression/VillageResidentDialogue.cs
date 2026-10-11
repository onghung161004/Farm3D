using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class VillageResidentDialogue : MonoBehaviour
    {
        private static VillageResidentDialogue instance;
        private VillageResidentNpc resident;
        private VillageTraderRoutine routine;
        private TMP_Text text;
        private TMP_Text firstLabel;
        private TMP_Text secondLabel;
        private int topic = -1;
        private int line;

        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        public static bool Show(VillageResidentNpc target)
        {
            if (target == null) return false;
            if (instance == null) instance = Create();
            if (instance == null) return false;
            instance.Open(target);
            return true;
        }

        private static VillageResidentDialogue Create()
        {
            FarmHud hud = FindAnyObjectByType<FarmHud>();
            Canvas canvas = hud == null ? FindAnyObjectByType<Canvas>() : hud.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            GameObject panel = new GameObject("ResidentDialogue", typeof(RectTransform), typeof(Image), typeof(VillageResidentDialogue));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(490f, 285f);
            panel.GetComponent<Image>().color = new Color(0.055f, 0.11f, 0.13f, 0.98f);
            VillageResidentDialogue view = panel.GetComponent<VillageResidentDialogue>();
            view.text = CreateText(panel.transform, new Vector2(0f, 58f), new Vector2(455f, 135f), 21f);
            view.firstLabel = CreateButton(panel.transform, new Vector2(-108f, -43f), "", () => view.SelectTopic(0));
            view.secondLabel = CreateButton(panel.transform, new Vector2(108f, -43f), "", () => view.SelectTopic(1));
            CreateButton(panel.transform, new Vector2(-108f, -105f),
                Application.isMobilePlatform ? "Next" : "Next  [Enter]", view.Next);
            CreateButton(panel.transform, new Vector2(108f, -105f),
                Application.isMobilePlatform ? "Close" : "Close  [Esc]", view.Close);
            panel.SetActive(false);
            return view;
        }

        private static TMP_Text CreateText(Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text label = go.GetComponent<TMP_Text>();
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static TMP_Text CreateButton(Transform parent, Vector2 position, string caption,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(205f, 44f);
            go.GetComponent<Image>().color = new Color(0.20f, 0.34f, 0.29f, 0.96f);
            go.GetComponent<Button>().onClick.AddListener(action);
            TMP_Text label = CreateText(go.transform, Vector2.zero, rect.sizeDelta, 17f);
            label.text = caption;
            return label;
        }

        private void Open(VillageResidentNpc target)
        {
            if (routine != null) routine.SetPaused(false);
            resident = target;
            routine = target.GetComponent<VillageTraderRoutine>();
            if (routine != null) routine.SetPaused(true);
            topic = -1;
            line = 0;
            firstLabel.text = target.TopicLabel(0);
            secondLabel.text = target.TopicLabel(1);
            gameObject.SetActive(true);
            Refresh();
        }

        private void SelectTopic(int selection)
        {
            if (resident == null || resident.ReplyCount(selection) == 0) return;
            topic = selection;
            line = 0;
            Refresh();
        }

        private void Next()
        {
            if (resident == null || topic < 0) return;
            line++;
            if (line >= resident.ReplyCount(topic))
            {
                topic = -1;
                line = 0;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (resident == null) return;
            text.text = resident.ResidentName + "\n\n" + (topic < 0 ? resident.Greeting : resident.Reply(topic, line));
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Close();
            else if (keyboard.enterKey.wasPressedThisFrame) Next();
        }

        private void Close()
        {
            if (routine != null) routine.SetPaused(false);
            routine = null;
            resident = null;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (routine != null) routine.SetPaused(false);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class AxeHotbarButton : MonoBehaviour
    {
        private PlayerToolController controller;
        private Image frame;
        private bool subscribed;

        public static void Ensure(FarmHud hud, PlayerToolController tools)
        {
            if (hud == null || tools == null) return;
            RectTransform bar = hud.transform.Find("InventoryHotbar") as RectTransform;
            if (bar == null) return;
            Transform existing = bar.Find("Slot_Axe");
            AxeHotbarButton slot;
            if (existing != null) slot = existing.GetComponent<AxeHotbarButton>();
            else
            {
                GameObject root = new GameObject("Slot_Axe", typeof(RectTransform), typeof(Image), typeof(AxeHotbarButton));
                root.transform.SetParent(bar, false);
                RectTransform rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(846f, 0f);
                rect.sizeDelta = new Vector2(100f, 112f);
                slot = root.GetComponent<AxeHotbarButton>();
                slot.frame = root.GetComponent<Image>();
                slot.frame.raycastTarget = true;
                root.AddComponent<HotbarToolButton>().Configure(tools, 8);
                Outline border = root.AddComponent<Outline>();
                border.effectColor = new Color(0.02f, 0.06f, 0.06f, 0.95f);
                border.effectDistance = new Vector2(3f, -3f);
                Label(root.transform, "Shortcut", "8", new Vector2(14f, 98f), new Vector2(25f, 22f), 18f);
                Label(root.transform, "Name", "Axe", new Vector2(50f, 14f), new Vector2(95f, 28f), 18f);
                IconPart(root.transform, "Handle", new Vector2(48f, 63f), new Vector2(8f, 49f), new Color(0.6f, 0.36f, 0.16f));
                IconPart(root.transform, "Head", new Vector2(59f, 82f), new Vector2(37f, 17f), new Color(0.72f, 0.80f, 0.82f));
            }
            if (slot == null) return;
            bar.sizeDelta = new Vector2(Mathf.Max(bar.sizeDelta.x, 970f), bar.sizeDelta.y);
            slot.Bind(tools);
        }

        private void Bind(PlayerToolController tools)
        {
            Unsubscribe();
            controller = tools;
            frame = GetComponent<Image>();
            GetComponent<HotbarToolButton>()?.Configure(tools, 8);
            Subscribe();
            Refresh(tools.SelectedTool);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void Subscribe()
        {
            if (controller == null || subscribed) return;
            controller.SelectedToolChanged += Refresh;
            subscribed = true;
        }
        private void Unsubscribe()
        {
            if (controller != null && subscribed) controller.SelectedToolChanged -= Refresh;
            subscribed = false;
        }
        private void Refresh(FarmTool tool)
        {
            if (frame == null) return;
            bool selected = tool == FarmTool.Axe;
            frame.color = selected ? new Color(1f, 0.76f, 0.25f, 1f) : new Color(0.20f, 0.27f, 0.29f, 0.96f);
            transform.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
        }

        private static void Label(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(parent, false);
            TMP_Text text = label.GetComponent<TextMeshProUGUI>();
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.text = value;
            text.color = new Color(1f, 0.9f, 0.65f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
        private static void IconPart(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject part = new GameObject(name, typeof(RectTransform), typeof(Image));
            part.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)part.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            part.GetComponent<Image>().color = color;
            part.GetComponent<Image>().raycastTarget = false;
        }
    }
}

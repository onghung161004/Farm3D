using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmRestoration
{
    internal static class QuestUiFactory
    {
        public static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.055f, 0.11f, 0.10f, 0.96f);
            return rect;
        }

        public static TMP_Text Text(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityAction action)
        {
            RectTransform rect = Panel(parent, label.Replace(" ", ""), position, size);
            rect.GetComponent<Image>().color = new Color(0.22f, 0.34f, 0.23f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            TMP_Text text = Text(rect, "Label", Vector2.zero, size - Vector2.one * 8f, 22f);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmRestoration
{
    internal static class AudioSettingsUi
    {
        public static Slider Slider(Transform parent, Vector2 position, UnityAction<float> changed)
        {
            RectTransform root = QuestUiFactory.Panel(parent, "VolumeSlider", position, new Vector2(300, 48));
            Slider slider = root.gameObject.AddComponent<Slider>();
            RectTransform fillArea = QuestUiFactory.Panel(root, "FillArea", Vector2.zero, new Vector2(270, 12));
            RectTransform fill = QuestUiFactory.Panel(fillArea, "Fill", Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.9f, 0.7f, 0.25f);
            fill.GetComponent<Image>().raycastTarget = false;
            fillArea.GetComponent<Image>().raycastTarget = false;
            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root, false); handleArea.sizeDelta = new Vector2(270, 48);
            RectTransform handle = QuestUiFactory.Panel(handleArea, "Handle", Vector2.zero, new Vector2(30, 40));
            handle.GetComponent<Image>().color = new Color(1f, 0.86f, 0.43f);
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = 1;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.AddListener(changed);
            return slider;
        }

        public static TMP_Text Label(Transform parent, string name, Vector2 position)
            => QuestUiFactory.Text(parent, name, position, new Vector2(440, 38), 25);
    }
}

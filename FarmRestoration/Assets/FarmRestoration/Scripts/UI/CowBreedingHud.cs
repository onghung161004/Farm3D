using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class CowBreedingHud : MonoBehaviour
    {
        private TMP_Text messageText;
        private float secondsLeft;

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            FarmHud hud = FindAnyObjectByType<FarmHud>();
            if (hud == null) return;
            CowBreedingHud toast = hud.GetComponentInChildren<CowBreedingHud>(true);
            if (toast == null) toast = Create(hud.transform);
            toast.gameObject.SetActive(true);
            toast.messageText.text = message;
            toast.secondsLeft = 6f;
        }

        private static CowBreedingHud Create(Transform parent)
        {
            GameObject root = new GameObject("CowBreedingNotice", typeof(RectTransform), typeof(Image), typeof(CowBreedingHud));
            root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -175f);
            rect.sizeDelta = new Vector2(730f, 76f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.07f, 0.24f, 0.14f, 0.94f);
            background.raycastTarget = false;
            GameObject label = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(root.transform, false);
            TMP_Text text = label.GetComponent<TextMeshProUGUI>();
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(14f, 5f);
            text.rectTransform.offsetMax = new Vector2(-14f, -5f);
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            CowBreedingHud toast = root.GetComponent<CowBreedingHud>();
            toast.messageText = text;
            return toast;
        }

        private void Update()
        {
            secondsLeft -= Time.unscaledDeltaTime;
            if (secondsLeft <= 0f) gameObject.SetActive(false);
        }
    }
}

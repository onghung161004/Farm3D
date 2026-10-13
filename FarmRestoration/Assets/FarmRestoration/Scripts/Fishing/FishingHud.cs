using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FishingHud : MonoBehaviour
    {
        private static FishingHud instance;
        private const float BarWidth = 470f;
        private FishingPondSpot nearbySpot;
        private FishingSession session;
        private GameObject panel;
        private GameObject castButton;
        private GameObject pullButton;
        private GameObject resultToast;
        private TMP_Text status;
        private TMP_Text resultText;
        private float resultSeconds;
        private RectTransform safeZone;
        private RectTransform needle;
        private RectTransform progressFill;

        public static FishingHud Ensure()
        {
            if (instance != null) return instance;
            FarmHud farmHud = FindAnyObjectByType<FarmHud>();
            Canvas canvas = farmHud != null ? farmHud.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            if (canvas == null) return null;
            GameObject root = new GameObject("FishingHud", typeof(RectTransform), typeof(FishingHud));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            instance = root.GetComponent<FishingHud>();
            instance.Build();
            return instance;
        }

        private void Build()
        {
            panel = CreateImage(transform, "FishingPanel", new Vector2(0f, 205f),
                new Vector2(650f, 230f), new Color(0.04f, 0.10f, 0.12f, 0.94f), false).gameObject;
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            status = CreateText(panel.transform, "Status", new Vector2(-35f, 83f), new Vector2(510f, 40f), 23f);
            CreateButton(panel.transform, "Cancel", new Vector2(280f, 83f),
                new Vector2(56f, 40f), "X", Cancel);
            Image tensionBar = CreateImage(panel.transform, "Tension", new Vector2(0f, 27f),
                new Vector2(BarWidth, 26f), new Color(0.2f, 0.3f, 0.32f, 1f), false);
            safeZone = CreateImage(tensionBar.transform, "SafeZone", Vector2.zero,
                new Vector2(BarWidth * FishingPullModel.SafeHalfWidth * 2f, 26f),
                new Color(0.22f, 0.72f, 0.40f, 1f), false).rectTransform;
            needle = CreateImage(tensionBar.transform, "Needle", Vector2.zero,
                new Vector2(8f, 39f), new Color(1f, 0.91f, 0.48f, 1f), false).rectTransform;
            Image progressBar = CreateImage(panel.transform, "Progress", new Vector2(0f, -21f),
                new Vector2(BarWidth, 15f), new Color(0.19f, 0.28f, 0.3f, 1f), false);
            progressFill = CreateImage(progressBar.transform, "Fill", Vector2.zero, Vector2.zero,
                new Color(0.28f, 0.7f, 0.92f, 1f), false).rectTransform;
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
            pullButton = CreateButton(panel.transform, "Pull", new Vector2(0f, -75f),
                new Vector2(210f, 55f), "HOLD TO REEL", null).gameObject;
            pullButton.AddComponent<FishingPullTouch>();
            castButton = CreateButton(transform, "Cast", new Vector2(-115f, 147f),
                new Vector2(190f, 68f), "CAST ROD", Cast).gameObject;
            RectTransform castRect = (RectTransform)castButton.transform;
            castRect.anchorMin = castRect.anchorMax = new Vector2(1f, 0f);
            if (Application.isMobilePlatform) castRect.anchoredPosition = new Vector2(-135f, 340f);
            resultToast = CreateImage(transform, "FishingResult", new Vector2(0f, 355f),
                new Vector2(650f, 56f), new Color(0.08f, 0.32f, 0.20f, 0.97f), false).gameObject;
            RectTransform resultRect = (RectTransform)resultToast.transform;
            resultRect.anchorMin = resultRect.anchorMax = new Vector2(0.5f, 0f);
            resultText = CreateText(resultToast.transform, "ResultText", Vector2.zero,
                new Vector2(620f, 50f), 22f);
            panel.SetActive(false);
            castButton.SetActive(false);
            resultToast.SetActive(false);
        }

        private void Update()
        {
            if (resultSeconds <= 0f) return;
            resultSeconds -= Time.unscaledDeltaTime;
            if (resultSeconds <= 0f && resultToast != null) resultToast.SetActive(false);
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool raycast)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
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

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size,
            string caption, UnityEngine.Events.UnityAction action)
        {
            Image image = CreateImage(parent, name, position, size, new Color(0.21f, 0.44f, 0.39f), true);
            Button button = image.gameObject.AddComponent<Button>();
            if (action != null) button.onClick.AddListener(action);
            CreateText(button.transform, "Caption", Vector2.zero, size, 23f).text = caption;
            return button;
        }

        /// <summary>Mobile ACTION shares the exact destination shown by CAST ROD, not the world raycast.</summary>
        public static bool TryCastFromAction()
        {
            return instance != null && instance.TryCastShownSpot();
        }

        private void Cast() => TryCastShownSpot();

        private bool TryCastShownSpot()
        {
            if (session != null || FishingSession.IsFishing || nearbySpot == null
                || castButton == null || !castButton.activeInHierarchy) return false;
            return nearbySpot.TryBeginFromTouch();
        }

        private void Cancel()
        {
            if (session != null) session.Cancel();
        }

        public void SetSpot(FishingPondSpot spot, bool canStart)
        {
            if (!canStart)
            {
                if (nearbySpot == spot) ClearSpot(spot);
                return;
            }
            if (nearbySpot == null || nearbySpot == spot
                || spot.DistanceToPlayerSquared < nearbySpot.DistanceToPlayerSquared)
                nearbySpot = spot;
            if (castButton != null) castButton.SetActive(nearbySpot != null && session == null);
        }

        public void ClearSpot(FishingPondSpot spot)
        {
            if (nearbySpot != spot) return;
            nearbySpot = null;
            if (castButton != null) castButton.SetActive(false);
        }

        public void Show(FishingSession current)
        {
            session = current;
            resultSeconds = 0f;
            resultToast.SetActive(false);
            panel.SetActive(true);
            castButton.SetActive(false);
            Refresh(current);
        }

        public void Refresh(FishingSession current)
        {
            if (session != current || status == null) return;
            switch (current.Phase)
            {
                case FishingPhase.Casting: status.text = "Casting..."; break;
                case FishingPhase.Waiting: status.text = "Waiting for a bite..."; break;
                case FishingPhase.Bite: status.text = Application.isMobilePlatform
                    ? "FISH ON! Hold REEL" : "FISH ON! Hold REEL / Space"; break;
                case FishingPhase.Reeling: status.text = "Keep tension inside the green zone"; break;
                case FishingPhase.Caught:
                case FishingPhase.Lost: status.text = current.ResultMessage; break;
            }
            bool reeling = current.Phase == FishingPhase.Reeling || current.Phase == FishingPhase.Bite;
            pullButton.SetActive(reeling);
            float center = Mathf.Clamp01(current.SafeCenter);
            safeZone.anchoredPosition = new Vector2((center - 0.5f) * BarWidth, 0f);
            needle.anchoredPosition = new Vector2((current.Pull.tension - 0.5f) * BarWidth, 0f);
            progressFill.anchorMax = new Vector2(current.Pull.progress, 1f);
        }

        public void ShowResult(string message, bool success)
        {
            if (resultToast == null) return;
            resultToast.GetComponent<Image>().color = success
                ? new Color(0.08f, 0.32f, 0.20f, 0.97f)
                : new Color(0.43f, 0.16f, 0.12f, 0.97f);
            resultText.text = message;
            resultSeconds = 5f;
            resultToast.SetActive(true);
        }

        public void Hide(FishingSession current)
        {
            if (session != current) return;
            session = null;
            panel.SetActive(false);
            if (castButton != null) castButton.SetActive(false);
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}

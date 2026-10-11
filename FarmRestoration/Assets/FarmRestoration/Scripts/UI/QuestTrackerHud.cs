using TMPro;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class QuestTrackerHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI trackerText;

        public void Configure(TextMeshProUGUI label)
        {
            trackerText = label;
            RefreshDisplay();
        }

        private void Start()
        {
            if (trackerText == null)
            {
                trackerText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.Changed += RefreshDisplay;
            }

            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.Changed -= RefreshDisplay;
            }
        }

        public void RefreshDisplay()
        {
            if (trackerText == null) return;

            if (QuestManager.Instance != null)
            {
                trackerText.text = QuestManager.Instance.GetTrackerSummaryText();
            }
            else
            {
                trackerText.text = "";
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmHud : MonoBehaviour
    {
        private static readonly int SharpnessProperty = Shader.PropertyToID("_Sharpness");
        private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");
        [SerializeField] private PlayerToolController toolController;
        [SerializeField] private FarmPlot farmPlot;
        [SerializeField] private TMP_Text toolLabel;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private TMP_Text cropsLabel;
        [SerializeField] private Image[] toolSlotFrames;
        [SerializeField] private TMP_Text[] cropSlotCounts;

        private readonly InventoryState inventory = new InventoryState();
        private readonly CropInventory cropInventory = new CropInventory();
        private FarmPlot[] subscribedPlots;
        private bool isSubscribed;

        public InventoryState Inventory => inventory;
        public CropInventory CropInventory => cropInventory;

        public void RefreshInventory() => SetCropSummary();

        private void OnEnable()
        {
            CowLeadGuide.Ensure(this);
            MobileGameControls.Ensure(this, toolController);
            EnsureCropSelectionButtons();
            ConfigureTextRendering();
            Subscribe();
            cropInventory.Changed += OnCropInventoryChanged;
            Refresh();
        }

        private void EnsureCropSelectionButtons()
        {
            if (toolController == null || cropSlotCounts == null) return;
            for (int i = 0; i < cropSlotCounts.Length && i < 3; i++)
            {
                TMP_Text count = cropSlotCounts[i];
                if (count == null) continue;
                Image frame = count.transform.parent.GetComponent<Image>();
                if (frame == null) continue;
                frame.raycastTarget = true;
                HotbarToolButton button = frame.GetComponent<HotbarToolButton>();
                if (button == null) button = frame.gameObject.AddComponent<HotbarToolButton>();
                button.Configure(toolController, i + 5);
                Transform shortcut = frame.transform.Find("Shortcut");
                if (shortcut == null)
                {
                    GameObject label = new GameObject("Shortcut", typeof(RectTransform), typeof(TextMeshProUGUI));
                    label.transform.SetParent(frame.transform, false);
                    RectTransform rect = label.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(8f, -5f);
                    rect.sizeDelta = new Vector2(25f, 22f);
                    TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
                    text.text = (i + 5).ToString();
                    text.fontSize = 20f;
                    text.fontStyle = FontStyles.Bold;
                    text.extraPadding = true;
                    text.color = new Color(1f, 0.83f, 0.36f);
                    text.raycastTarget = false;
                }
            }
        }

        private void OnDisable()
        {
            cropInventory.Changed -= OnCropInventoryChanged;
            Unsubscribe();
        }

        private void OnCropInventoryChanged(CropType type, int count) => SetCropSummary();

        public static void OptimizeLabelRendering(TMP_Text label, float minimumFontSize = 20f)
        {
            if (label == null) return;
            label.extraPadding = true;
            if (label.fontSize < minimumFontSize)
            {
                label.fontSize = minimumFontSize;
            }
            label.fontStyle |= FontStyles.Bold;
            Material material = label.fontSharedMaterial;
            if (material != null && material.HasProperty(SharpnessProperty))
            {
                material.SetFloat(SharpnessProperty, 0f);
            }
            label.SetMaterialDirty();
        }

        private void ConfigureTextRendering()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.pixelPerfect = false;

            foreach (TextMeshProUGUI label in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                OptimizeLabelRendering(label, 20f);
            }
        }

        public void ConfigureSources(PlayerToolController controller, FarmPlot plot)
        {
            Unsubscribe();
            toolController = controller;
            farmPlot = plot;
            Subscribe();
            Refresh();
        }

        public void ConfigureLabels(TMP_Text tool, TMP_Text prompt, TMP_Text crops)
        {
            toolLabel = tool;
            promptLabel = prompt;
            cropsLabel = crops;
            Refresh();
        }

        public void ConfigureHotbar(Image[] toolFrames, TMP_Text[] cropCounts, TMP_Text prompt)
        {
            toolSlotFrames = toolFrames;
            cropSlotCounts = cropCounts;
            promptLabel = prompt;
            toolLabel = null;
            cropsLabel = null;
            Refresh();
        }

        public void Refresh()
        {
            if (toolController != null)
            {
                SetTool(toolController.SelectedTool);
                SetCropSelection(toolController.SelectedCropType);
                SetPrompt(toolController.CurrentPrompt);
            }
            else
            {
                SetTool(FarmTool.Hoe);
                SetPrompt("Move closer to a farm plot");
            }

            SetCropSummary();
        }

        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            if (toolController != null)
            {
                toolController.SelectedToolChanged += SetTool;
                toolController.SelectedCropChanged += SetCropSelection;
                toolController.InteractionPromptChanged += SetPrompt;
            }

            subscribedPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
            foreach (FarmPlot plot in subscribedPlots)
            {
                plot.CropHarvested += OnCropHarvested;
            }

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (toolController != null)
            {
                toolController.SelectedToolChanged -= SetTool;
                toolController.SelectedCropChanged -= SetCropSelection;
                toolController.InteractionPromptChanged -= SetPrompt;
            }

            if (subscribedPlots != null)
            {
                foreach (FarmPlot plot in subscribedPlots)
                {
                    if (plot != null) plot.CropHarvested -= OnCropHarvested;
                }
            }

            subscribedPlots = null;
            isSubscribed = false;
        }

        private void SetTool(FarmTool tool)
        {
            if (toolController != null) SetCropSelection(toolController.SelectedCropType);
            if (toolLabel != null)
            {
                toolLabel.text = FarmHudPresentation.ToolText(tool);
            }
            if (toolSlotFrames == null) return;
            for (int i = 0; i < toolSlotFrames.Length; i++)
            {
                Image frame = toolSlotFrames[i];
                if (frame == null) continue;
                bool selected = i == (int)tool;
                frame.color = selected ? new Color(1f, 0.76f, 0.25f, 1f) : new Color(0.20f, 0.27f, 0.29f, 0.96f);
                frame.rectTransform.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
            }
        }

        private void SetPrompt(string prompt)
        {
            if (promptLabel != null)
            {
                if (Application.isMobilePlatform && !string.IsNullOrEmpty(prompt))
                    prompt = prompt.Replace("Hold E", "Hold ACTION")
                        .Replace("[E", "[ACTION")
                        .Replace("[R change]", "[RECIPE]")
                        .Replace(" (5/6/7)", " (tap a crop)");
                promptLabel.text = FarmHudPresentation.PromptText(prompt);
            }
        }

        private void SetCropSelection(CropType crop)
        {
            if (cropSlotCounts == null || toolController == null) return;
            for (int i = 0; i < cropSlotCounts.Length && i < 3; i++)
            {
                Image frame = cropSlotCounts[i] == null ? null : cropSlotCounts[i].transform.parent.GetComponent<Image>();
                if (frame == null) continue;
                bool selected = toolController.SelectedTool == FarmTool.Seeds && i == (int)crop;
                frame.color = selected ? new Color(0.45f, 0.82f, 0.36f, 1f) : new Color(0.20f, 0.27f, 0.29f, 0.96f);
            }
        }

        private void OnCropHarvested(CropType cropType, int amount)
        {
            inventory.AddCrop(amount);
            cropInventory.Add(cropType, amount);
            SetCropSummary();
        }

        private void SetCropSummary()
        {
            if (cropsLabel != null)
            {
                cropsLabel.text = cropInventory.Summary().Replace("  ", "\n") + "\nTotal: " + inventory.HarvestedCrops;
            }
            if (cropSlotCounts == null) return;
            for (int i = 0; i < cropSlotCounts.Length && i < 3; i++)
            {
                if (cropSlotCounts[i] != null) cropSlotCounts[i].text = cropInventory.GetCount((CropType)i).ToString();
            }
        }
    }
}

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
            ConfigureTextRendering();
            Subscribe();
            cropInventory.Changed += OnCropInventoryChanged;
            Refresh();
        }

        private void OnDisable()
        {
            cropInventory.Changed -= OnCropInventoryChanged;
            Unsubscribe();
        }

        private void OnCropInventoryChanged(CropType type, int count) => SetCropSummary();

        private void ConfigureTextRendering()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                canvas.pixelPerfect = true;

            foreach (TextMeshProUGUI label in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Material material = label.fontMaterial;
                if (material == null) continue;
                if (material.HasProperty(SharpnessProperty)) material.SetFloat(SharpnessProperty, 0.65f);
                if (material.HasProperty(OutlineWidthProperty))
                    material.SetFloat(OutlineWidthProperty, Mathf.Min(material.GetFloat(OutlineWidthProperty), 0.06f));
                label.SetMaterialDirty();
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
                promptLabel.text = FarmHudPresentation.PromptText(prompt);
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

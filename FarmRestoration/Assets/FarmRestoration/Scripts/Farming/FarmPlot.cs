using System;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmPlot : MonoBehaviour, IInteractable
    {
        [Header("One root object for each state")]
        [SerializeField] private GameObject untilledVisual;
        [SerializeField] private GameObject tilledVisual;
        [SerializeField] private GameObject seededVisual;
        [SerializeField] private GameObject wateredVisual;
        [SerializeField] private GameObject growingVisual;
        [SerializeField] private GameObject readyToHarvestVisual;
        [SerializeField] private CropType cropType = CropType.Pumpkin;

        public FarmPlotState CurrentState { get; private set; } = FarmPlotState.Untilled;
        public CropType CropType => cropType;

        public event Action<int> Harvested;
        public event Action<CropType, int> CropHarvested;

        private void Start()
        {
            if (!HasVisualConfiguration())
            {
                Debug.LogError("FarmPlot requires one visual root for every state.", this);
                enabled = false;
                return;
            }

            ApplyVisuals();
        }

        public bool TryInteract(FarmTool tool)
        {
            if (CurrentState == FarmPlotState.Untilled && tool == FarmTool.Hoe)
            {
                SetState(FarmPlotState.Tilled);
                return true;
            }

            if (CurrentState == FarmPlotState.Tilled && tool == FarmTool.Seeds)
            {
                SetState(FarmPlotState.Seeded);
                return true;
            }

            if (CurrentState == FarmPlotState.Seeded && tool == FarmTool.WateringCan)
            {
                SetState(FarmPlotState.Watered);
                return true;
            }

            if (CurrentState == FarmPlotState.ReadyToHarvest && tool == FarmTool.Harvest)
            {
                SetState(FarmPlotState.Untilled);
                Harvested?.Invoke(1);
                CropHarvested?.Invoke(cropType, 1);
                return true;
            }

            return false;
        }

        public bool AdvanceGrowth()
        {
            if (CurrentState == FarmPlotState.Watered)
            {
                SetState(FarmPlotState.Growing);
                return true;
            }

            if (CurrentState == FarmPlotState.Growing)
            {
                SetState(FarmPlotState.ReadyToHarvest);
                return true;
            }

            return false;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            if (CurrentState == FarmPlotState.Untilled)
            {
                return tool == FarmTool.Hoe ? "Till soil" : "Select Hoe";
            }

            if (CurrentState == FarmPlotState.Tilled)
            {
                return tool == FarmTool.Seeds ? "Plant seeds" : "Select Seeds";
            }

            if (CurrentState == FarmPlotState.Seeded)
            {
                return tool == FarmTool.WateringCan ? "Water seeds" : "Select Watering Can";
            }

            if (CurrentState == FarmPlotState.Watered || CurrentState == FarmPlotState.Growing)
            {
                return "Crop is growing - press G to advance demo growth";
            }

            return tool == FarmTool.Harvest ? "Harvest crop" : "Select Harvest";
        }

        public void ConfigureVisuals(
            GameObject untilled,
            GameObject tilled,
            GameObject seeded,
            GameObject watered,
            GameObject growing,
            GameObject ready)
        {
            untilledVisual = untilled;
            tilledVisual = tilled;
            seededVisual = seeded;
            wateredVisual = watered;
            growingVisual = growing;
            readyToHarvestVisual = ready;
            ApplyVisuals();
        }

        public void ConfigureCropType(CropType type)
        {
            cropType = type;
        }

        private void SetState(FarmPlotState nextState)
        {
            CurrentState = nextState;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            SetActive(untilledVisual, CurrentState == FarmPlotState.Untilled);
            SetActive(tilledVisual, CurrentState == FarmPlotState.Tilled);
            SetActive(seededVisual, CurrentState == FarmPlotState.Seeded);
            SetActive(wateredVisual, CurrentState == FarmPlotState.Watered);
            SetActive(growingVisual, CurrentState == FarmPlotState.Growing);
            SetActive(readyToHarvestVisual, CurrentState == FarmPlotState.ReadyToHarvest);
        }

        private bool HasVisualConfiguration()
        {
            return untilledVisual != null && tilledVisual != null && seededVisual != null
                && wateredVisual != null && growingVisual != null && readyToHarvestVisual != null;
        }

        private static void SetActive(GameObject visual, bool isActive)
        {
            if (visual != null)
            {
                visual.SetActive(isActive);
            }
        }
    }
}

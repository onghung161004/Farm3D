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
        public long GrowthReadyUtcTicks { get; private set; }

        public event Action<int> Harvested;
        public event Action<CropType, int> CropHarvested;
        public event Action<FarmPlot> StateChanged;

        private int GrowthSeconds => cropType == CropType.Carrot ? 45 : cropType == CropType.Tomato ? 60 : 75;

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

        private void Update()
        {
            TickGrowth(DateTime.UtcNow);
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
                GrowthReadyUtcTicks = DateTime.UtcNow.AddSeconds(GrowthSeconds).Ticks;
                SetState(FarmPlotState.Watered);
                return true;
            }

            if (CurrentState == FarmPlotState.ReadyToHarvest && tool == FarmTool.Harvest)
            {
                GrowthReadyUtcTicks = 0;
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
                GrowthReadyUtcTicks = 0;
                SetState(FarmPlotState.ReadyToHarvest);
                return true;
            }

            return false;
        }

        public void TickGrowth(DateTime nowUtc)
        {
            if (GrowthReadyUtcTicks <= 0 || (CurrentState != FarmPlotState.Watered && CurrentState != FarmPlotState.Growing)) return;
            if (nowUtc.Ticks >= GrowthReadyUtcTicks)
            {
                GrowthReadyUtcTicks = 0;
                SetState(FarmPlotState.ReadyToHarvest);
            }
            else if (CurrentState == FarmPlotState.Watered
                && nowUtc.Ticks >= GrowthReadyUtcTicks - TimeSpan.FromSeconds(GrowthSeconds / 2f).Ticks)
            {
                SetState(FarmPlotState.Growing);
            }
        }

        public void Restore(FarmPlotState state, long readyUtcTicks, DateTime nowUtc)
        {
            if (!Enum.IsDefined(typeof(FarmPlotState), state)) state = FarmPlotState.Untilled;
            GrowthReadyUtcTicks = state == FarmPlotState.Watered || state == FarmPlotState.Growing
                ? (readyUtcTicks > 0 ? readyUtcTicks : nowUtc.AddSeconds(GrowthSeconds).Ticks) : 0;
            SetState(state);
            TickGrowth(nowUtc);
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
                int seconds = Math.Max(0, (int)Math.Ceiling((GrowthReadyUtcTicks - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond));
                return "Crop growing: " + seconds + "s remaining";
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
            StateChanged?.Invoke(this);
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

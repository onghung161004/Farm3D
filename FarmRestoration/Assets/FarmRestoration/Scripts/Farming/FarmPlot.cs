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
        [SerializeField] private int requiredGardenIndex = -1;

        public FarmPlotState CurrentState { get; private set; } = FarmPlotState.Untilled;
        public CropType CropType => cropType;
        public long GrowthReadyUtcTicks { get; private set; }
        public bool IsUnlocked => transform.parent == null
            || !transform.parent.name.StartsWith("ExpansionGarden", StringComparison.Ordinal)
            || requiredGardenIndex < 0 || FarmProgression.Instance != null
            && FarmProgression.Instance.IsGardenUnlocked(requiredGardenIndex);

        public event Action<int> Harvested;
        public event Action<CropType, int> CropHarvested;
        public event Action<CropType> CropPlanted;
        public static event Action<CropType> AnyCropPlanted;
        public static event Action<CropType, int> AnyCropHarvested;
        public event Action<FarmPlot> StateChanged;
        public event Action<CropType> CropTypeChanged;

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
            if (FarmProgression.Instance != null && !FarmProgression.Instance.IsGardenUnlocked(cropType))
            {
                if (FarmProgression.Instance.TryUnlockGarden(cropType))
                {
                    return true;
                }
                return false;
            }

            if (!IsUnlocked) return false;
            if (CurrentState == FarmPlotState.Untilled && tool == FarmTool.Hoe)
            {
                SetState(FarmPlotState.Tilled);
                return true;
            }

            if (CurrentState == FarmPlotState.Tilled && tool == FarmTool.Seeds)
            {
                if (FarmProgression.Instance != null && !FarmProgression.Instance.TrySpendSeed(cropType))
                {
                    return false;
                }
                SetState(FarmPlotState.Seeded);
                CropPlanted?.Invoke(cropType);
                AnyCropPlanted?.Invoke(cropType);
                return true;
                return TryPlant(cropType);
            }

            if (CurrentState == FarmPlotState.Seeded && tool == FarmTool.WateringCan)
            {
                return TryWaterFromRain(DateTime.UtcNow);
            }

            if (CurrentState == FarmPlotState.ReadyToHarvest && tool == FarmTool.Harvest)
            {
                GrowthReadyUtcTicks = 0;
                SetState(FarmPlotState.Untilled);
                Harvested?.Invoke(1);
                CropHarvested?.Invoke(cropType, 1);
                AnyCropHarvested?.Invoke(cropType, 1);
                return true;
            }

            return false;
        }

        public bool TryPlant(CropType type)
        {
            if (!IsUnlocked) return false;
            if (CurrentState != FarmPlotState.Tilled || !Enum.IsDefined(typeof(CropType), type)) return false;
            ConfigureCropType(type);
            SetState(FarmPlotState.Seeded);
            return true;
        }

        /// <summary>Rain uses the same one-time watering transition as the watering can.</summary>
        public bool TryWaterFromRain(DateTime nowUtc)
        {
            if (!IsUnlocked) return false;
            if (CurrentState != FarmPlotState.Seeded) return false;
            GrowthReadyUtcTicks = nowUtc.AddSeconds(GrowthSeconds).Ticks;
            SetState(FarmPlotState.Watered);
            return true;
        }

        public bool AdvanceGrowth()
        {
            if (!IsUnlocked) return false;
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
            if (!IsUnlocked) return;
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
            if (FarmProgression.Instance != null && !FarmProgression.Instance.IsGardenUnlocked(cropType))
            {
                int cost = FarmProgression.Instance.GetGardenUnlockCost(cropType);
                if (FarmProgression.Instance.Coins >= cost)
                    return $"[Locked] {cropType} Garden  [E: Unlock for {cost} coins]";
                return $"[Locked] {cropType} Garden (Unlock: {cost} coins or Quest)";
            }

            if (!IsUnlocked) return "Garden locked: buy this 9-plot area for 30 coins at its sign";
            if (CurrentState == FarmPlotState.Untilled)
            {
                return tool == FarmTool.Hoe ? "Till soil" : "Select Hoe";
            }

            if (CurrentState == FarmPlotState.Tilled)
            {
                if (tool == FarmTool.Seeds)
                {
                    if (FarmProgression.Instance != null && FarmProgression.Instance.SeedInventory.GetCount(cropType) <= 0)
                        return $"No {cropType} seeds! Buy at Trader";
                    return "Plant seeds";
                }
                return "Select Seeds";
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
            if (!Enum.IsDefined(typeof(CropType), type) || cropType == type) return;
            cropType = type;
            CropTypeChanged?.Invoke(type);
        }

        public void ConfigureRequiredGarden(int index)
        {
            requiredGardenIndex = index;
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

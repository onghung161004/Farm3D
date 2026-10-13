using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed partial class FarmProgression : MonoBehaviour
    {
        private const string SaveFile = "FarmDemoSave.json";
        private readonly int[] products = new int[3];
        private readonly bool[] repairs = new bool[2];
        private readonly bool[] gardenUnlocks = new bool[3];
        private FarmPlot[] plots = Array.Empty<FarmPlot>();
        private ChoppableTree[] trees = Array.Empty<ChoppableTree>();
        private readonly List<CowAnimal> cows = new List<CowAnimal>();
        private FarmHud hud;
        private int coins;
        private int orderIndex;
        private int milk;
        private int fish;
        private int wood;
        private bool cowPenUpgraded;
        private bool ready;
        private bool inTransaction;

        public static FarmProgression Instance { get; private set; }
        public int Coins => coins;
        public int OrderIndex => orderIndex;
        public int MilkCount => milk;
        public int FishCount => fish;
        public int WoodCount => wood;
        public bool CowPenUpgraded => cowPenUpgraded;
        public int CarrotCount => hud == null ? 0 : hud.CropInventory.GetCount(CropType.Carrot);
        private int OrderCycleLength => cows.Count > 0 || milk > 0 ? 7 : 6;
        public IReadOnlyList<CowAnimal> Cows => cows;
        public event Action Changed;

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            hud = GetComponent<FarmHud>() ?? FindAnyObjectByType<FarmHud>();
            if (hud == null) { Debug.LogError("FarmProgression needs the FarmHud inventory.", this); enabled = false; return; }
            WoodcuttingSetup.EnsureInFarm();
            plots = FindObjectsByType<FarmPlot>();
            trees = FindObjectsByType<ChoppableTree>();
            cows.AddRange(FindObjectsByType<CowAnimal>());
            RestoreGame();
            InitializeQuests();
            foreach (FarmPlot plot in plots) plot.StateChanged += OnPlotChanged;
            foreach (CowAnimal cow in cows) cow.StateChanged += OnCowChanged;
            foreach (ChoppableTree tree in trees) tree.StateChanged += OnTreeChanged;
            hud.CropInventory.Changed += OnInventoryChanged;
            ready = true;
            Changed?.Invoke();
            VillageWarehouse.EnsurePresent();
            FarmQuestBoard.Ensure(this);
            FarmQuestView.Ensure(this, hud);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (FarmPlot plot in plots) if (plot != null) plot.StateChanged -= OnPlotChanged;
            foreach (FarmPlot plot in plots) if (plot != null) plot.CropHarvested -= RecordQuestHarvest;
            foreach (CowAnimal cow in cows) if (cow != null) cow.StateChanged -= OnCowChanged;
            foreach (ChoppableTree tree in trees) if (tree != null) tree.StateChanged -= OnTreeChanged;
            if (hud != null) hud.CropInventory.Changed -= OnInventoryChanged;
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveGame(); }
        private void OnApplicationQuit() => SaveGame();

        public int ProductCount(CropType type) => products[(int)type];
        public int StockCount(WarehouseItemKind item)
        {
            int index = (int)item;
            if (index < 0 || index > 8) return 0;
            if (index < 3) return hud == null ? 0 : hud.CropInventory.GetCount((CropType)index);
            if (index < 6) return products[index - 3];
            return index == 6 ? milk : index == 7 ? fish : wood;
        }

        public static int SellPrice(WarehouseItemKind item)
        {
            int index = (int)item;
            if (index >= 0 && index < 3) return 2;
            if (index >= 3 && index < 6) return 15;
            return index == 6 ? 10 : index == 7 ? 8 : index == 8 ? 3 : 0;
        }

        public bool TrySell(WarehouseItemKind item)
        {
            int index = (int)item;
            int price = SellPrice(item);
            if (!ready || price == 0 || StockCount(item) < 1) return false;
            if (index < 3)
            {
                inTransaction = true;
                bool spent = hud.CropInventory.TrySpend((CropType)index, 1);
                inTransaction = false;
                if (!spent) return false;
            }
            else if (index < 6) products[index - 3]--;
            else if (index == 6) milk--;
            else if (index == 7) fish--;
            else wood--;
            coins += price;
            NotifyAndSave();
            return true;
        }
        public bool IsRepaired(int index) => index >= 0 && index < repairs.Length && repairs[index];
        public bool IsGardenUnlocked(int index) => index >= 0 && index < gardenUnlocks.Length && gardenUnlocks[index];

        public bool TryUnlockGarden(int index, int price)
        {
            if (!ready || index < 0 || index >= gardenUnlocks.Length || gardenUnlocks[index]
                || price < 0 || coins < price) return false;
            coins -= price;
            gardenUnlocks[index] = true;
            NotifyAndSave();
            return true;
        }

        public bool TryFeedCow(CowAnimal cow)
        {
            if (!ready || cow == null || !cow.CanFeed) return false;
            inTransaction = true;
            bool spent;
            try
            {
                spent = hud.CropInventory.TrySpend(CropType.Carrot, 1);
                if (spent)
                {
                    DateTime now = DateTime.UtcNow;
                    cow.Feed(now);
                    quests.RecordFeed(cow.CowId);
                    CowAnimal calf = CowBreeding.TryBreed(cow, cows, now, out string message);
                    if (calf != null) RegisterCow(calf);
                    CowBreedingHud.Show(message);
                }
            }
            finally { inTransaction = false; }
            if (spent) NotifyAndSave();
            return spent;
        }

        private void RegisterCow(CowAnimal cow)
        {
            if (cow == null || cows.Contains(cow)) return;
            cows.Add(cow);
            if (ready) cow.StateChanged += OnCowChanged;
        }

        public bool TryMilkCow(CowAnimal cow)
        {
            if (!ready || cow == null) return false;
            inTransaction = true;
            bool collected = cow.Milk(DateTime.UtcNow);
            inTransaction = false;
            if (!collected) return false;
            milk++;
            quests.RecordMilk();
            NotifyAndSave();
            return true;
        }

        public bool AddFish()
        {
            if (!ready || fish == int.MaxValue) return false;
            fish++;
            quests.RecordFish();
            NotifyAndSave();
            return true;
        }

        public bool TryChopTree(ChoppableTree tree)
        {
            if (!ready || tree == null || wood > int.MaxValue - TreeHarvestState.WoodYield) return false;
            bool chopped;
            bool felled;
            inTransaction = true;
            try
            {
                chopped = tree.TryChop(DateTime.UtcNow, Time.unscaledTime, out felled);
                if (felled) wood += TreeHarvestState.WoodYield;
            }
            finally { inTransaction = false; }
            if (!chopped) return false;
            if (felled) CowBreedingHud.Show("+3 Wood | Warehouse: " + wood + " wood. Sell it or upgrade your pen!");
            NotifyAndSave();
            return true;
        }

        public bool TryUpgradeCowPen(CowPen pen)
        {
            if (!ready || pen == null || cowPenUpgraded || wood < LumberWorkshop.WoodCost
                || coins < LumberWorkshop.CoinCost) return false;
            wood -= LumberWorkshop.WoodCost;
            coins -= LumberWorkshop.CoinCost;
            cowPenUpgraded = true;
            CowPenExpansion.Apply(pen);
            CowBreedingHud.Show("Pen expanded towards the gate! Space for 12 cows.");
            NotifyAndSave();
            return true;
        }

        public bool TryProduce(CropType crop, int cropCost = 2)
        {
            if (!ready || cropCost <= 0) return false;
            inTransaction = true;
            bool spent = hud.CropInventory.TrySpend(crop, cropCost);
            inTransaction = false;
            if (!spent) return false;
            products[(int)crop]++;
            quests.RecordProduction();
            NotifyAndSave();
            return true;
        }

        public bool TryDeliverOrder()
        {
            if (!ready) return false;
            int step = orderIndex % OrderCycleLength;
            if (step == 6)
            {
                if (milk < 1) return false;
                milk--;
                coins += 20;
                orderIndex++;
                NotifyAndSave();
                return true;
            }
            int cropIndex = step % 3;
            bool productOrder = step >= 3;
            if (productOrder)
            {
                if (products[cropIndex] < 1) return false;
                products[cropIndex]--;
            }
            else
            {
                inTransaction = true;
                bool spent = hud.CropInventory.TrySpend((CropType)cropIndex, 3);
                inTransaction = false;
                if (!spent) return false;
            }
            coins += productOrder ? 25 : 12;
            orderIndex++;
            NotifyAndSave();
            return true;
        }

        public string OrderDescription()
        {
            int step = orderIndex % OrderCycleLength;
            if (step == 6) return "1 milk";
            CropType crop = (CropType)(step % 3);
            bool productOrder = step >= 3;
            return productOrder ? "1 " + ProductName(crop) : "3 " + crop;
        }

        public string OrderMissingDescription()
        {
            int step = orderIndex % OrderCycleLength;
            if (step == 6) return milk >= 1 ? "" : "Need 1 milk";
            int cropIndex = step % 3;
            if (step >= 3)
                return products[cropIndex] >= 1 ? "" : "Need 1 " + ProductName((CropType)cropIndex);
            int missing = 3 - StockCount((WarehouseItemKind)cropIndex);
            return missing <= 0 ? "" : "Need " + missing + " more " + (CropType)cropIndex;
        }

        public int OrderReward => orderIndex % OrderCycleLength == 6 ? 20
            : orderIndex % OrderCycleLength >= 3 ? 25 : 12;

        public bool TryRepair(int index, int price)
        {
            int woodCost = RepairWoodCost(index);
            if (!ready || index < 0 || index >= repairs.Length || repairs[index] || price < 0
                || coins < price || wood < woodCost) return false;
            coins -= price;
            wood -= woodCost;
            repairs[index] = true;
            NotifyAndSave();
            return true;
        }

        public static int RepairWoodCost(int index) => index == 0 ? 6 : index == 1 ? 10 : 0;

        public static string ProductName(CropType crop) => crop == CropType.Pumpkin ? "pumpkin soup"
            : crop == CropType.Carrot ? "carrot juice" : "tomato sauce";

        private void OnPlotChanged(FarmPlot plot) => SaveGame();
        private void OnCowChanged(CowAnimal cow) { if (!inTransaction) NotifyAndSave(); }
        private void OnTreeChanged(ChoppableTree tree) { if (!inTransaction) SaveGame(); }
        private void OnInventoryChanged(CropType crop, int count) { if (!inTransaction) { NotifyAndSave(); } }
        private void NotifyAndSave() { Changed?.Invoke(); SaveGame(); }

        public void SaveGame()
        {
            if (!ready || hud == null) return;
            FarmSaveData data = Snapshot();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(SavePath))
                {
                    try { File.Replace(temp, SavePath, null); }
                    catch (PlatformNotSupportedException) { File.Copy(temp, SavePath, true); File.Delete(temp); }
                }
                else File.Move(temp, SavePath);
            }
            catch (Exception ex) { Debug.LogWarning("Could not save FarmDemo: " + ex.Message, this); }
        }

        public FarmSaveData Snapshot()
        {
            if (hud == null) return null;
            FarmSaveData data = new FarmSaveData
            {
                crops = hud.CropInventory.Snapshot(),
                products = (int[])products.Clone(),
                coins = coins,
                orderIndex = orderIndex,
                milk = milk,
                fish = fish,
                wood = wood,
                cowPenUpgraded = cowPenUpgraded,
                bridgeUpgradeLevel = bridgeUpgradeLevel,
                repairs = (bool[])repairs.Clone(),
                gardenUnlocks = (bool[])gardenUnlocks.Clone(),
                plots = new PlotSaveData[plots.Length],
                cows = new CowSaveData[cows.Count],
                trees = new TreeSaveData[trees.Length],
                quests = quests.Snapshot()
            };
            for (int i = 0; i < plots.Length; i++)
            {
                FarmPlot plot = plots[i];
                data.plots[i] = new PlotSaveData { id = PlotId(plot), state = (int)plot.CurrentState, readyUtcTicks = plot.GrowthReadyUtcTicks, cropType = (int)plot.CropType };
            }
            for (int i = 0; i < cows.Count; i++) data.cows[i] = cows[i].Snapshot();
            for (int i = 0; i < trees.Length; i++) data.trees[i] = trees[i].Snapshot();
            return data;
        }

        private void RestoreGame()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(File.ReadAllText(SavePath));
                if (data == null || (data.version < 1 || data.version > FarmSaveData.CurrentVersion)) throw new InvalidDataException("Unsupported save version");
                hud.CropInventory.Restore(data.crops);
                for (int i = 0; i < products.Length; i++) products[i] = data.products != null && i < data.products.Length ? Math.Max(0, data.products[i]) : 0;
                for (int i = 0; i < repairs.Length; i++) repairs[i] = data.repairs != null && i < data.repairs.Length && data.repairs[i];
                for (int i = 0; i < gardenUnlocks.Length; i++)
                    gardenUnlocks[i] = data.gardenUnlocks != null && i < data.gardenUnlocks.Length && data.gardenUnlocks[i];
                coins = Math.Max(0, data.coins);
                orderIndex = Math.Max(0, data.orderIndex);
                milk = Math.Max(0, data.milk);
                fish = Math.Max(0, data.fish);
                wood = Math.Max(0, data.wood);
                cowPenUpgraded = data.cowPenUpgraded;
                bridgeUpgradeLevel = RestoreBridgeUpgradeLevel(data.bridgeUpgradeLevel, IsRepaired(1));
                questsLoaded = data.quests != null;
                quests = FarmQuestProgress.Restore(data.quests);
                if (cowPenUpgraded)
                    foreach (CowPen pen in FindObjectsByType<CowPen>()) CowPenExpansion.Apply(pen);
                var savedTrees = new Dictionary<string, TreeSaveData>();
                if (data.trees != null)
                    foreach (TreeSaveData saved in data.trees)
                        if (saved != null && !string.IsNullOrEmpty(saved.id)) savedTrees[saved.id] = saved;
                foreach (ChoppableTree tree in trees)
                    if (savedTrees.TryGetValue(tree.TreeId, out TreeSaveData saved)) tree.Restore(saved);
                Dictionary<string, PlotSaveData> savedPlots = new Dictionary<string, PlotSaveData>();
                if (data.plots != null)
                    foreach (PlotSaveData saved in data.plots)
                        if (saved != null && !string.IsNullOrEmpty(saved.id)) savedPlots[saved.id] = saved;
                foreach (FarmPlot plot in plots)
                    if (savedPlots.TryGetValue(PlotId(plot), out PlotSaveData saved))
                    {
                        if (data.version >= 2) plot.ConfigureCropType((CropType)saved.cropType);
                        plot.Restore((FarmPlotState)saved.state, saved.readyUtcTicks, DateTime.UtcNow);
                    }
                Dictionary<string, CowSaveData> savedCows = new Dictionary<string, CowSaveData>();
                if (data.cows != null)
                    foreach (CowSaveData saved in data.cows)
                        if (saved != null && !string.IsNullOrEmpty(saved.id)) savedCows[saved.id] = saved;
                foreach (CowAnimal cow in cows)
                    if (savedCows.TryGetValue(cow.CowId, out CowSaveData saved)) cow.Restore(saved);
                CowBreeding.RestoreOffspring(data.cows, cows);
                hud.RefreshInventory();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FarmDemo save was not loaded; starting fresh: " + ex.Message, this);
                hud.CropInventory.Restore(null);
                Array.Clear(products, 0, products.Length);
                Array.Clear(repairs, 0, repairs.Length);
                Array.Clear(gardenUnlocks, 0, gardenUnlocks.Length);
                coins = 0;
                orderIndex = 0;
                milk = 0;
                fish = 0;
                wood = 0;
                cowPenUpgraded = false;
                bridgeUpgradeLevel = 0;
                questsLoaded = false;
                quests = new FarmQuestProgress();
                foreach (FarmPlot plot in plots) plot.Restore(FarmPlotState.Untilled, 0, DateTime.UtcNow);
                hud.RefreshInventory();
            }
        }

        private static string PlotId(FarmPlot plot) => plot.transform.parent.name + "/" + plot.name;
    }
}

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
        private int coins = 15;
        private int orderIndex;
        private int milk;
        private int fish;
        private int wood;
        private bool cowPenUpgraded;
        private bool ready;
        private bool inTransaction;
        private readonly bool[] unlockedGardens = new bool[] { false, true, false };
        private readonly List<ActiveTimedOrder> activeTimedOrders = new List<ActiveTimedOrder>();

        public static FarmProgression Instance { get; set; }
        public SeedInventory SeedInventory { get; } = new SeedInventory();
        public IReadOnlyList<ActiveTimedOrder> ActiveTimedOrders => activeTimedOrders;
        public int ActiveOrdersCount => activeTimedOrders.Count;
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
        public event Action<CropType, int> RecipeProcessed;
        public static event Action<CropType, int> AnyRecipeProcessed;
        public event Action<int> OrderDelivered;
        public static event Action<int> AnyOrderDelivered;
        public event Action<int> MoneyEarned;
        public static event Action<int> AnyMoneyEarned;
        public event Action<int> RepairCompleted;
        public static event Action<int> AnyRepairCompleted;

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
            UpdateWorldGardenLabels();
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

        private void Update()
        {
            if (ready) MaintainTimedOrders(DateTime.UtcNow);
        }

        public void ConfigureForTests(FarmHud testHud, FarmPlot[] testPlots, int initialCoins = 15)
        {
            hud = testHud;
            plots = testPlots ?? Array.Empty<FarmPlot>();
            cows.Clear();
            coins = initialCoins;
            ready = true;
            MaintainTimedOrders(DateTime.UtcNow);
        }

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
            MoneyEarned?.Invoke(price);
            AnyMoneyEarned?.Invoke(price);
            FarmAudioFeedback.PlayCoin();
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

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            coins += amount;
            MoneyEarned?.Invoke(amount);
            AnyMoneyEarned?.Invoke(amount);
            NotifyAndSave();
        }

        public int GetSeedCost(CropType crop) => crop == CropType.Carrot ? 2 : crop == CropType.Tomato ? 3 : 4;
        public int GetCropSellPrice(CropType crop) => crop == CropType.Carrot ? 5 : crop == CropType.Tomato ? 8 : 10;

        public bool TryBuySeed(CropType crop, int amount = 1)
        {
            if (amount <= 0) return false;
            int totalCost = GetSeedCost(crop) * amount;
            if (coins < totalCost) return false;
            coins -= totalCost;
            SeedInventory.Add(crop, amount);
            FarmAudioFeedback.PlayCoin();
            NotifyAndSave();
            return true;
        }

        public bool TrySellCrop(CropType crop, int amount = 1)
        {
            if (amount <= 0 || hud == null || hud.CropInventory == null) return false;
            if (hud.CropInventory.GetCount(crop) < amount) return false;
            hud.CropInventory.TrySpend(crop, amount);
            int earnings = GetCropSellPrice(crop) * amount;
            AddCoins(earnings);
            FarmAudioFeedback.PlayCoin();
            return true;
        }

        public bool TrySpendSeed(CropType crop, int amount = 1)
        {
            bool spent = SeedInventory.TrySpend(crop, amount);
            if (spent)
            {
                NotifyAndSave();
                CheckAntiSoftLock();
            }
            return spent;
        }

        public bool CheckAntiSoftLock()
        {
            if (!ready) return false;
            if (coins >= 2) return false;
            if (SeedInventory.TotalSeeds > 0) return false;
            if (hud != null && hud.CropInventory.Total > 0) return false;
            if (plots != null)
            {
                for (int i = 0; i < plots.Length; i++)
                {
                    if (plots[i] != null && plots[i].CurrentState != FarmPlotState.Untilled && plots[i].CurrentState != FarmPlotState.Tilled)
                    {
                        return false;
                    }
                }
            }
            SeedInventory.Add(CropType.Carrot, 2);
            Debug.Log("[Anti-Soft-Lock] Granted 2 free Carrot seeds to prevent soft-lock.");
            NotifyAndSave();
            return true;
        }

        public bool IsGardenUnlocked(CropType crop)
        {
            int idx = (int)crop;
            return idx >= 0 && idx < unlockedGardens.Length && unlockedGardens[idx];
        }

        public int GetGardenUnlockCost(CropType crop) => crop == CropType.Pumpkin ? 20 : crop == CropType.Tomato ? 35 : 0;

        public bool TryUnlockGarden(CropType crop)
        {
            if (IsGardenUnlocked(crop)) return true;
            int cost = GetGardenUnlockCost(crop);
            if (coins < cost) return false;
            coins -= cost;
            unlockedGardens[(int)crop] = true;
            FarmAudioFeedback.PlayCoin();
            NotifyAndSave();
            return true;
        }

        public void UnlockGardenFree(CropType crop)
        {
            int idx = (int)crop;
            if (idx >= 0 && idx < unlockedGardens.Length)
            {
                unlockedGardens[idx] = true;
                NotifyAndSave();
            }
        }

        public void UpdateWorldGardenLabels()
        {
            UpdateGardenSign("PumpkinGarden", CropType.Pumpkin);
            UpdateGardenSign("CarrotGarden", CropType.Carrot);
            UpdateGardenSign("TomatoGarden", CropType.Tomato);
        }

        private void UpdateGardenSign(string gardenName, CropType crop)
        {
            GameObject gardenObj = GameObject.Find(gardenName);
            if (gardenObj == null) return;
            TextMesh sign = gardenObj.GetComponentInChildren<TextMesh>();
            if (sign == null) return;
            bool unlocked = IsGardenUnlocked(crop);
            string baseName = crop.ToString() + " Garden";
            sign.text = unlocked ? baseName : $"{baseName} [Locked]";
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

        private static readonly TimedOrderTemplate[] OrderTemplates = new[]
        {
            new TimedOrderTemplate("Carrot_Small", CropType.Carrot, false, 2, 12, 6, 90f, 40f),
            new TimedOrderTemplate("Carrot_Juice", CropType.Carrot, true, 1, 18, 8, 120f, 50f),
            new TimedOrderTemplate("Carrot_Bulk", CropType.Carrot, false, 3, 18, 8, 110f, 45f),
            new TimedOrderTemplate("Pumpkin_Small", CropType.Pumpkin, false, 2, 22, 10, 110f, 45f),
            new TimedOrderTemplate("Pumpkin_Soup", CropType.Pumpkin, true, 1, 28, 12, 140f, 60f),
            new TimedOrderTemplate("Tomato_Small", CropType.Tomato, false, 2, 20, 10, 100f, 45f),
            new TimedOrderTemplate("Tomato_Sauce", CropType.Tomato, true, 1, 26, 12, 130f, 55f)
        };

        public readonly struct TimedOrderTemplate
        {
            public readonly string id;
            public readonly CropType crop;
            public readonly bool product;
            public readonly int amount;
            public readonly int reward;
            public readonly int bonus;
            public readonly float timeLimit;
            public readonly float fastLimit;

            public TimedOrderTemplate(string id, CropType crop, bool product, int amount, int reward, int bonus, float timeLimit, float fastLimit)
            {
                this.id = id;
                this.crop = crop;
                this.product = product;
                this.amount = amount;
                this.reward = reward;
                this.bonus = bonus;
                this.timeLimit = timeLimit;
                this.fastLimit = fastLimit;
            }
        }

        public void MaintainTimedOrders(DateTime nowUtc)
        {
            for (int i = activeTimedOrders.Count - 1; i >= 0; i--)
            {
                if (activeTimedOrders[i].IsExpired(nowUtc))
                {
                    activeTimedOrders.RemoveAt(i);
                }
            }

            List<TimedOrderTemplate> available = new List<TimedOrderTemplate>();
            for (int i = 0; i < OrderTemplates.Length; i++)
            {
                if (IsGardenUnlocked(OrderTemplates[i].crop))
                {
                    available.Add(OrderTemplates[i]);
                }
            }

            if (available.Count > 0)
            {
                int attempts = 0;
                while (activeTimedOrders.Count < 3 && attempts < 15)
                {
                    attempts++;
                    TimedOrderTemplate template = available[(orderIndex + activeTimedOrders.Count + attempts) % available.Count];
                    ActiveTimedOrder newOrder = new ActiveTimedOrder
                    {
                        id = template.id + "_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                        customer = "Village Trader",
                        cropType = template.crop,
                        isProduct = template.product,
                        amount = template.amount,
                        baseReward = template.reward,
                        fastBonus = template.bonus,
                        expiryUtcTicks = nowUtc.AddSeconds(template.timeLimit).Ticks,
                        fastBonusUtcTicks = nowUtc.AddSeconds(template.fastLimit).Ticks
                    };
                    activeTimedOrders.Add(newOrder);
                }
            }
        }

        public ActiveTimedOrder GetNearestDeadlineOrder(DateTime nowUtc)
        {
            MaintainTimedOrders(nowUtc);
            ActiveTimedOrder nearest = null;
            long minTicks = long.MaxValue;
            for (int i = 0; i < activeTimedOrders.Count; i++)
            {
                if (activeTimedOrders[i].expiryUtcTicks < minTicks)
                {
                    minTicks = activeTimedOrders[i].expiryUtcTicks;
                    nearest = activeTimedOrders[i];
                }
            }
            return nearest;
        }

        public string GetNearestDeadlineSummary(DateTime nowUtc)
        {
            ActiveTimedOrder nearest = GetNearestDeadlineOrder(nowUtc);
            if (nearest == null) return "None";
            int sec = nearest.RemainingSeconds(nowUtc);
            bool fast = nearest.IsFastDelivery(nowUtc);
            return $"{nearest.ItemDescription} in {sec}s" + (fast ? $" (+{nearest.fastBonus}c bonus)" : "");
        }

        public bool CanFulfillOrder(ActiveTimedOrder o)
        {
            if (o == null) return false;
            if (o.isProduct) return products[(int)o.cropType] >= o.amount;
            return hud != null && hud.CropInventory.GetCount(o.cropType) >= o.amount;
        }

        public bool TryDeliverOrder()
        {
            if (!ready) return false;
            DateTime now = DateTime.UtcNow;
            MaintainTimedOrders(now);

            int targetIndex = -1;
            for (int i = 0; i < activeTimedOrders.Count; i++)
            {
                if (CanFulfillOrder(activeTimedOrders[i]))
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0) return false;
            ActiveTimedOrder order = activeTimedOrders[targetIndex];

            if (order.isProduct)
            {
                products[(int)order.cropType] -= order.amount;
            }
            else
            {
                inTransaction = true;
                bool spent = hud.CropInventory.TrySpend(order.cropType, order.amount);
                inTransaction = false;
                if (!spent) return false;
            }

            int reward = order.TotalReward(now);
            activeTimedOrders.RemoveAt(targetIndex);
            MaintainTimedOrders(now);

            coins += reward;
            orderIndex++;
            MoneyEarned?.Invoke(reward);
            AnyMoneyEarned?.Invoke(reward);
            OrderDelivered?.Invoke(orderIndex);
            AnyOrderDelivered?.Invoke(orderIndex);
            FarmAudioFeedback.PlayCoin();
            NotifyAndSave();
            return true;
        }

        public string OrderDescription()
        {
            ActiveTimedOrder nearest = GetNearestDeadlineOrder(DateTime.UtcNow);
            if (nearest == null) return "No orders";
            return $"{nearest.ItemDescription} ({nearest.RemainingSeconds(DateTime.UtcNow)}s left)";
        }

        public int OrderReward
        {
            get
            {
                ActiveTimedOrder nearest = GetNearestDeadlineOrder(DateTime.UtcNow);
                return nearest != null ? nearest.TotalReward(DateTime.UtcNow) : 15;
            }
        }
        public string OrderMissingDescription()
        {
            ActiveTimedOrder nearest = GetNearestDeadlineOrder(DateTime.UtcNow);
            if (nearest == null) return "No active orders";
            if (nearest.isProduct)
            {
                int have = products[(int)nearest.cropType];
                int missing = nearest.amount - have;
                return missing <= 0 ? "" : "Need " + missing + " " + ProductName(nearest.cropType);
            }
            else
            {
                int have = hud != null ? hud.CropInventory.GetCount(nearest.cropType) : 0;
                int missing = nearest.amount - have;
                return missing <= 0 ? "" : "Need " + missing + " more " + nearest.cropType;
            }
        }

        public bool TryRepair(int index, int price)
        {
            int woodCost = RepairWoodCost(index);
            if (!ready || index < 0 || index >= repairs.Length || repairs[index] || price < 0
                || coins < price || wood < woodCost) return false;
            coins -= price;
            wood -= woodCost;
            repairs[index] = true;
            RepairCompleted?.Invoke(index);
            AnyRepairCompleted?.Invoke(index);
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
        private void NotifyAndSave() { Changed?.Invoke(); UpdateWorldGardenLabels(); SaveGame(); }

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
                coins = 15;
                orderIndex = 0;
                milk = 0;
                SeedInventory.Restore(null);
                unlockedGardens[0] = false;
                unlockedGardens[1] = true;
                unlockedGardens[2] = false;
                RestoreTimedOrders(null, DateTime.UtcNow);
                fish = 0;
                wood = 0;
                cowPenUpgraded = false;
                bridgeUpgradeLevel = 0;
                questsLoaded = false;
                quests = new FarmQuestProgress();
                foreach (FarmPlot plot in plots) plot.Restore(FarmPlotState.Untilled, 0, DateTime.UtcNow);
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.RestoreSaveData(null);
                }
                hud.RefreshInventory();
            }
        }

        public TimedOrderSaveData[] ExportTimedOrders()
        {
            TimedOrderSaveData[] saves = new TimedOrderSaveData[activeTimedOrders.Count];
            for (int i = 0; i < activeTimedOrders.Count; i++)
            {
                ActiveTimedOrder o = activeTimedOrders[i];
                saves[i] = new TimedOrderSaveData
                {
                    id = o.id,
                    cropType = (int)o.cropType,
                    isProduct = o.isProduct,
                    amount = o.amount,
                    reward = o.baseReward,
                    bonus = o.fastBonus,
                    expiryUtcTicks = o.expiryUtcTicks,
                    fastBonusUtcTicks = o.fastBonusUtcTicks
                };
            }
            return saves;
        }

        public void RestoreTimedOrders(TimedOrderSaveData[] saves, DateTime nowUtc)
        {
            activeTimedOrders.Clear();
            if (saves != null && saves.Length > 0)
            {
                for (int i = 0; i < saves.Length; i++)
                {
                    TimedOrderSaveData s = saves[i];
                    if (s == null) continue;
                    activeTimedOrders.Add(new ActiveTimedOrder
                    {
                        id = s.id,
                        customer = "Village Trader",
                        cropType = (CropType)s.cropType,
                        isProduct = s.isProduct,
                        amount = s.amount,
                        baseReward = s.reward,
                        fastBonus = s.bonus,
                        expiryUtcTicks = s.expiryUtcTicks,
                        fastBonusUtcTicks = s.fastBonusUtcTicks
                    });
                }
            }
            MaintainTimedOrders(nowUtc);
        }

        private static string PlotId(FarmPlot plot) => plot.transform.parent.name + "/" + plot.name;
    }
}

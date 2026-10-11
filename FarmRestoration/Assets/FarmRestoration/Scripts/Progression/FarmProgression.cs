using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmProgression : MonoBehaviour
    {
        private const string SaveFile = "FarmDemoSave.json";
        private readonly int[] products = new int[3];
        private readonly bool[] repairs = new bool[2];
        private FarmPlot[] plots = Array.Empty<FarmPlot>();
        private CowAnimal[] cows = Array.Empty<CowAnimal>();
        private FarmHud hud;
        private int coins = 15;
        private int orderIndex;
        private int milk;
        private bool ready;
        private bool inTransaction;
        private readonly bool[] unlockedGardens = new bool[] { false, true, false };

        public static FarmProgression Instance { get; private set; }
        public SeedInventory SeedInventory { get; } = new SeedInventory();
        public int Coins => coins;
        public int OrderIndex => orderIndex;
        public int MilkCount => milk;
        public int CarrotCount => hud == null ? 0 : hud.CropInventory.GetCount(CropType.Carrot);
        private int OrderCycleLength => cows.Length > 0 || milk > 0 ? 7 : 6;
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
            plots = FindObjectsByType<FarmPlot>();
            cows = FindObjectsByType<CowAnimal>();
            RestoreGame();
            foreach (FarmPlot plot in plots) plot.StateChanged += OnPlotChanged;
            foreach (CowAnimal cow in cows) cow.StateChanged += OnCowChanged;
            hud.CropInventory.Changed += OnInventoryChanged;
            ready = true;
            Changed?.Invoke();
            UpdateWorldGardenLabels();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (FarmPlot plot in plots) if (plot != null) plot.StateChanged -= OnPlotChanged;
            foreach (CowAnimal cow in cows) if (cow != null) cow.StateChanged -= OnCowChanged;
            if (hud != null) hud.CropInventory.Changed -= OnInventoryChanged;
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveGame(); }
        private void OnApplicationQuit() => SaveGame();

        public void ConfigureForTests(FarmHud testHud, FarmPlot[] testPlots, int initialCoins = 15)
        {
            hud = testHud;
            plots = testPlots ?? Array.Empty<FarmPlot>();
            cows = Array.Empty<CowAnimal>();
            coins = initialCoins;
            ready = true;
        }

        public int ProductCount(CropType type) => products[(int)type];
        public bool IsRepaired(int index) => index >= 0 && index < repairs.Length && repairs[index];

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
            bool spent = hud.CropInventory.TrySpend(CropType.Carrot, 1);
            if (spent) cow.Feed(DateTime.UtcNow);
            inTransaction = false;
            if (spent) NotifyAndSave();
            return spent;
        }

        public bool TryMilkCow(CowAnimal cow)
        {
            if (!ready || cow == null) return false;
            inTransaction = true;
            bool collected = cow.Milk(DateTime.UtcNow);
            inTransaction = false;
            if (!collected) return false;
            milk++;
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
            RecipeProcessed?.Invoke(crop, 1);
            AnyRecipeProcessed?.Invoke(crop, 1);
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
                MoneyEarned?.Invoke(20);
                AnyMoneyEarned?.Invoke(20);
                OrderDelivered?.Invoke(orderIndex);
                AnyOrderDelivered?.Invoke(orderIndex);
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
            int earned = productOrder ? 25 : 12;
            coins += earned;
            orderIndex++;
            MoneyEarned?.Invoke(earned);
            AnyMoneyEarned?.Invoke(earned);
            OrderDelivered?.Invoke(orderIndex);
            AnyOrderDelivered?.Invoke(orderIndex);
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

        public int OrderReward => orderIndex % OrderCycleLength == 6 ? 20
            : orderIndex % OrderCycleLength >= 3 ? 25 : 12;

        public bool TryRepair(int index, int price)
        {
            if (!ready || index < 0 || index >= repairs.Length || repairs[index] || price < 0 || coins < price) return false;
            coins -= price;
            repairs[index] = true;
            RepairCompleted?.Invoke(index);
            AnyRepairCompleted?.Invoke(index);
            NotifyAndSave();
            return true;
        }

        public static string ProductName(CropType crop) => crop == CropType.Pumpkin ? "pumpkin soup"
            : crop == CropType.Carrot ? "carrot juice" : "tomato sauce";

        private void OnPlotChanged(FarmPlot plot) => SaveGame();
        private void OnCowChanged(CowAnimal cow) { if (!inTransaction) SaveGame(); }
        private void OnInventoryChanged(CropType crop, int count) { if (!inTransaction) { NotifyAndSave(); } }
        private void NotifyAndSave() { Changed?.Invoke(); UpdateWorldGardenLabels(); SaveGame(); }

        public void SaveGame()
        {
            if (!ready || hud == null) return;
            FarmSaveData data = new FarmSaveData
            {
                crops = hud.CropInventory.Snapshot(),
                products = (int[])products.Clone(),
                coins = coins,
                orderIndex = orderIndex,
                milk = milk,
                repairs = (bool[])repairs.Clone(),
                plots = new PlotSaveData[plots.Length],
                cows = new CowSaveData[cows.Length],
                questData = QuestManager.Instance != null ? QuestManager.Instance.ExportSaveData() : null,
                seeds = SeedInventory.Snapshot(),
                unlockedGardens = (bool[])unlockedGardens.Clone()
            };
            for (int i = 0; i < plots.Length; i++)
            {
                FarmPlot plot = plots[i];
                data.plots[i] = new PlotSaveData { id = PlotId(plot), state = (int)plot.CurrentState, readyUtcTicks = plot.GrowthReadyUtcTicks };
            }
            for (int i = 0; i < cows.Length; i++) data.cows[i] = cows[i].Snapshot();
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

        private void RestoreGame()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(File.ReadAllText(SavePath));
                if (data == null || data.version != 1) throw new InvalidDataException("Unsupported save version");
                hud.CropInventory.Restore(data.crops);
                for (int i = 0; i < products.Length; i++) products[i] = data.products != null && i < data.products.Length ? Math.Max(0, data.products[i]) : 0;
                for (int i = 0; i < repairs.Length; i++) repairs[i] = data.repairs != null && i < data.repairs.Length && data.repairs[i];
                coins = Math.Max(0, data.coins);
                orderIndex = Math.Max(0, data.orderIndex);
                milk = Math.Max(0, data.milk);
                Dictionary<string, PlotSaveData> savedPlots = new Dictionary<string, PlotSaveData>();
                if (data.plots != null)
                    foreach (PlotSaveData saved in data.plots)
                        if (saved != null && !string.IsNullOrEmpty(saved.id)) savedPlots[saved.id] = saved;
                foreach (FarmPlot plot in plots)
                    if (savedPlots.TryGetValue(PlotId(plot), out PlotSaveData saved))
                        plot.Restore((FarmPlotState)saved.state, saved.readyUtcTicks, DateTime.UtcNow);
                Dictionary<string, CowSaveData> savedCows = new Dictionary<string, CowSaveData>();
                if (data.cows != null)
                    foreach (CowSaveData saved in data.cows)
                        if (saved != null && !string.IsNullOrEmpty(saved.id)) savedCows[saved.id] = saved;
                foreach (CowAnimal cow in cows)
                    if (savedCows.TryGetValue(cow.CowId, out CowSaveData saved)) cow.Restore(saved);
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.RestoreSaveData(data.questData);
                }
                SeedInventory.Restore(data.seeds);
                if (data.unlockedGardens != null)
                {
                    for (int i = 0; i < unlockedGardens.Length && i < data.unlockedGardens.Length; i++)
                        unlockedGardens[i] = data.unlockedGardens[i];
                }
                else
                {
                    unlockedGardens[0] = false;
                    unlockedGardens[1] = true;
                    unlockedGardens[2] = false;
                }
                hud.RefreshInventory();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FarmDemo save was not loaded; starting fresh: " + ex.Message, this);
                hud.CropInventory.Restore(null);
                Array.Clear(products, 0, products.Length);
                Array.Clear(repairs, 0, repairs.Length);
                coins = 15;
                orderIndex = 0;
                milk = 0;
                SeedInventory.Restore(null);
                unlockedGardens[0] = false;
                unlockedGardens[1] = true;
                unlockedGardens[2] = false;
                foreach (FarmPlot plot in plots) plot.Restore(FarmPlotState.Untilled, 0, DateTime.UtcNow);
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.RestoreSaveData(null);
                }
                hud.RefreshInventory();
            }
        }

        private static string PlotId(FarmPlot plot) => plot.transform.parent.name + "/" + plot.name;
    }
}

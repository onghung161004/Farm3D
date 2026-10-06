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
        private FarmHud hud;
        private int coins;
        private int orderIndex;
        private bool ready;
        private bool inTransaction;

        public static FarmProgression Instance { get; private set; }
        public int Coins => coins;
        public int OrderIndex => orderIndex;
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
            plots = FindObjectsByType<FarmPlot>();
            RestoreGame();
            foreach (FarmPlot plot in plots) plot.StateChanged += OnPlotChanged;
            hud.CropInventory.Changed += OnInventoryChanged;
            ready = true;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (FarmPlot plot in plots) if (plot != null) plot.StateChanged -= OnPlotChanged;
            if (hud != null) hud.CropInventory.Changed -= OnInventoryChanged;
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveGame(); }
        private void OnApplicationQuit() => SaveGame();

        public int ProductCount(CropType type) => products[(int)type];
        public bool IsRepaired(int index) => index >= 0 && index < repairs.Length && repairs[index];

        public bool TryProduce(CropType crop, int cropCost = 2)
        {
            if (!ready || cropCost <= 0) return false;
            inTransaction = true;
            bool spent = hud.CropInventory.TrySpend(crop, cropCost);
            inTransaction = false;
            if (!spent) return false;
            products[(int)crop]++;
            NotifyAndSave();
            return true;
        }

        public bool TryDeliverOrder()
        {
            if (!ready) return false;
            int cropIndex = orderIndex % 3;
            bool productOrder = orderIndex % 6 >= 3;
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
            CropType crop = (CropType)(orderIndex % 3);
            bool productOrder = orderIndex % 6 >= 3;
            return productOrder ? "1 " + ProductName(crop) : "3 " + crop;
        }

        public int OrderReward => orderIndex % 6 >= 3 ? 25 : 12;

        public bool TryRepair(int index, int price)
        {
            if (!ready || index < 0 || index >= repairs.Length || repairs[index] || price < 0 || coins < price) return false;
            coins -= price;
            repairs[index] = true;
            NotifyAndSave();
            return true;
        }

        public static string ProductName(CropType crop) => crop == CropType.Pumpkin ? "pumpkin soup"
            : crop == CropType.Carrot ? "carrot juice" : "tomato sauce";

        private void OnPlotChanged(FarmPlot plot) => SaveGame();
        private void OnInventoryChanged(CropType crop, int count) { if (!inTransaction) { NotifyAndSave(); } }
        private void NotifyAndSave() { Changed?.Invoke(); SaveGame(); }

        public void SaveGame()
        {
            if (!ready || hud == null) return;
            FarmSaveData data = new FarmSaveData
            {
                crops = hud.CropInventory.Snapshot(),
                products = (int[])products.Clone(),
                coins = coins,
                orderIndex = orderIndex,
                repairs = (bool[])repairs.Clone(),
                plots = new PlotSaveData[plots.Length]
            };
            for (int i = 0; i < plots.Length; i++)
            {
                FarmPlot plot = plots[i];
                data.plots[i] = new PlotSaveData { id = PlotId(plot), state = (int)plot.CurrentState, readyUtcTicks = plot.GrowthReadyUtcTicks };
            }
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
                Dictionary<string, PlotSaveData> savedPlots = new Dictionary<string, PlotSaveData>();
                if (data.plots != null)
                    foreach (PlotSaveData saved in data.plots)
                        if (saved != null && !string.IsNullOrEmpty(saved.id)) savedPlots[saved.id] = saved;
                foreach (FarmPlot plot in plots)
                    if (savedPlots.TryGetValue(PlotId(plot), out PlotSaveData saved))
                        plot.Restore((FarmPlotState)saved.state, saved.readyUtcTicks, DateTime.UtcNow);
                hud.RefreshInventory();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FarmDemo save was not loaded; starting fresh: " + ex.Message, this);
                hud.CropInventory.Restore(null);
                Array.Clear(products, 0, products.Length);
                Array.Clear(repairs, 0, repairs.Length);
                coins = 0;
                orderIndex = 0;
                foreach (FarmPlot plot in plots) plot.Restore(FarmPlotState.Untilled, 0, DateTime.UtcNow);
                hud.RefreshInventory();
            }
        }

        private static string PlotId(FarmPlot plot) => plot.transform.parent.name + "/" + plot.name;
    }
}

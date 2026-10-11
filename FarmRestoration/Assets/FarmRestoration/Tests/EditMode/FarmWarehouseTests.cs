using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmWarehouseTests
    {
        [Test]
        public void DirectSalePricesStayBelowOrderRewards()
        {
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.Pumpkin), Is.EqualTo(2));
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.Carrot), Is.EqualTo(2));
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.Tomato), Is.EqualTo(2));
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.PumpkinSoup), Is.EqualTo(15));
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.CarrotJuice), Is.EqualTo(15));
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.TomatoSauce), Is.EqualTo(15));
            Assert.That(FarmProgression.SellPrice(WarehouseItemKind.Milk), Is.EqualTo(10));
            Assert.That(FarmProgression.SellPrice((WarehouseItemKind)99), Is.Zero);
            Assert.That(3 * FarmProgression.SellPrice(WarehouseItemKind.Pumpkin), Is.LessThan(12));
        }

        [Test]
        public void StockQueriesReadExistingInventoryRatherThanSecondStorage()
        {
            GameObject hudObject = new GameObject("WarehouseHudTest");
            FarmHud hud = hudObject.AddComponent<FarmHud>();
            FarmProgression game = hudObject.AddComponent<FarmProgression>();
            try
            {
                typeof(FarmProgression).GetField("hud", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(game, hud);
                hud.CropInventory.Add(CropType.Pumpkin, 4);
                int[] products = (int[])typeof(FarmProgression).GetField("products", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(game);
                products[0] = 2;
                typeof(FarmProgression).GetField("milk", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(game, 3);
                Assert.That(game.StockCount(WarehouseItemKind.Pumpkin), Is.EqualTo(4));
                Assert.That(game.StockCount(WarehouseItemKind.PumpkinSoup), Is.EqualTo(2));
                Assert.That(game.StockCount(WarehouseItemKind.Milk), Is.EqualTo(3));
                Assert.That(game.StockCount((WarehouseItemKind)(-1)), Is.Zero);
                Assert.That(game.TrySell((WarehouseItemKind)99), Is.False);
                Assert.That(game.Coins, Is.Zero);
            }
            finally { Object.DestroyImmediate(hudObject); }
        }
    }
}

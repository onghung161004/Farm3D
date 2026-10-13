using System;
using FarmRestoration.World;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmWeatherControllerTests
    {
        private GameObject weatherObject;
        private GameObject plotObject;
        private FarmWeatherController weather;
        private FarmPlot plot;

        [SetUp]
        public void SetUp()
        {
            weatherObject = new GameObject("WeatherTest");
            weather = weatherObject.AddComponent<FarmWeatherController>();
            weather.ConfigureDurations(2f, 1f);
            plotObject = new GameObject("PlotTest");
            plot = plotObject.AddComponent<FarmPlot>();
            weather.ConfigurePlots(new[] { plot });
            plot.TryInteract(FarmTool.Hoe);
            plot.TryInteract(FarmTool.Seeds);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(weatherObject);
            UnityEngine.Object.DestroyImmediate(plotObject);
        }

        [Test]
        public void RainTransition_WatersSeededPlotOnlyOnce()
        {
            DateTime now = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);
            int events = 0;
            weather.WeatherChanged += _ => events++;

            weather.Advance(1.9f, now);
            Assert.That(weather.CurrentWeather, Is.EqualTo(FarmWeatherKind.Clear));
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Seeded));
            weather.Advance(0.1f, now);

            Assert.That(weather.CurrentWeather, Is.EqualTo(FarmWeatherKind.Rain));
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Watered));
            Assert.That(events, Is.EqualTo(1));
            long deadline = plot.GrowthReadyUtcTicks;
            weather.Advance(3f, now.AddSeconds(3));
            Assert.That(plot.GrowthReadyUtcTicks, Is.EqualTo(deadline));
        }

        [Test]
        public void InvalidOrLongDelta_DoesNotHangOrChangeFinalPhase()
        {
            DateTime now = DateTime.UtcNow;
            weather.Advance(-1f, now);
            weather.Advance(float.NaN, now);
            Assert.That(weather.CurrentWeather, Is.EqualTo(FarmWeatherKind.Clear));

            weather.Advance(3002f, now);
            Assert.That(weather.CurrentWeather, Is.EqualTo(FarmWeatherKind.Rain));
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Watered));
        }

        [Test]
        public void EmptyPlotList_StillCyclesWeather()
        {
            weather.ConfigurePlots(null);
            weather.Advance(2f, DateTime.UtcNow);
            Assert.That(weather.CurrentWeather, Is.EqualTo(FarmWeatherKind.Rain));
            Assert.That(plot.CurrentState, Is.EqualTo(FarmPlotState.Seeded));
        }
    }
}

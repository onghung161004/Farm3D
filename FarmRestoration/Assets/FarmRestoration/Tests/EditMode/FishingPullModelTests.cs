using NUnit.Framework;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FishingPullModelTests
    {
        [Test]
        public void HoldingRaisesTensionAndReleasingLowersIt()
        {
            FishingPullState initial = FishingPullState.Initial;
            FishingPullState held = FishingPullModel.Step(initial, true, 0.1f, 0.5f);
            FishingPullState released = FishingPullModel.Step(held, false, 0.1f, 0.5f);
            Assert.That(held.tension, Is.GreaterThan(initial.tension));
            Assert.That(released.tension, Is.LessThan(held.tension));
        }

        [Test]
        public void ProgressOnlyGrowsWithinSafeZone()
        {
            FishingPullState state = FishingPullState.Initial;
            FishingPullState inZone = FishingPullModel.Step(state, false, 0.01f, 0.5f);
            FishingPullState outside = FishingPullModel.Step(state, false, 0.01f, 0.8f);
            Assert.That(inZone.progress, Is.GreaterThan(0f));
            Assert.That(outside.progress, Is.Zero);
        }

        [Test]
        public void OverpullAndSlackEventuallyLoseFish()
        {
            FishingPullState over = new FishingPullState { tension = 1f };
            FishingPullState slack = new FishingPullState { tension = 0f };
            for (int i = 0; i < 30; i++)
            {
                over = FishingPullModel.Step(over, true, 0.1f, 0.5f);
                slack = FishingPullModel.Step(slack, false, 0.1f, 0.5f);
            }
            Assert.That(over.lost, Is.True);
            Assert.That(slack.lost, Is.True);
        }

        [Test]
        public void FullProgressCatchesExactlyOnce()
        {
            FishingPullState state = new FishingPullState { tension = 0.5f, progress = 0.99f };
            state = FishingPullModel.Step(state, false, 0.1f, 0.5f);
            Assert.That(state.caught, Is.True);
            Assert.That(FishingPullModel.Step(state, true, 0.1f, 0.5f).progress, Is.EqualTo(1f));
        }
    }
}

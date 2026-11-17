using BlinkExpress.Effects.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// 粉砕の「時間が止まる演出」のタイマーを確かめるテスト。
    /// </summary>
    public sealed class HitStopTimerTests
    {
        [Test]
        public void IsNotFrozenAtFirst()
        {
            var timer = new HitStopTimer();
            Assert.That(timer.IsFrozen, Is.False);
            Assert.That(timer.WorldScale, Is.EqualTo(1f));
        }

        [Test]
        public void FreezesWorldAfterTrigger()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.09f);
            Assert.That(timer.IsFrozen, Is.True);
            Assert.That(timer.WorldScale, Is.EqualTo(0f));
        }

        [Test]
        public void ThawsAfterDuration()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.09f);
            timer.Tick(0.05f);
            Assert.That(timer.IsFrozen, Is.True);
            timer.Tick(0.05f);
            Assert.That(timer.IsFrozen, Is.False);
            Assert.That(timer.WorldScale, Is.EqualTo(1f));
        }

        [Test]
        public void LongerTriggerWinsAndShorterDoesNotShorten()
        {
            // 重なったときは、足さずに残りが長いほうを使う
            var timer = new HitStopTimer();
            timer.Trigger(0.07f);
            timer.Trigger(0.11f);
            timer.Tick(0.08f);
            Assert.That(timer.IsFrozen, Is.True);
            timer.Trigger(0.01f);
            timer.Tick(0.04f);
            Assert.That(timer.IsFrozen, Is.False);
        }

        [Test]
        public void ClearThawsImmediately()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.11f);
            timer.Clear();
            Assert.That(timer.IsFrozen, Is.False);
        }

        [Test]
        public void ZeroOrNegativeTriggerDoesNothing()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0f);
            timer.Trigger(-1f);
            Assert.That(timer.IsFrozen, Is.False);
        }
    }
}

using BlinkExpress.Enemy.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// 逃げる敵の動きが仕様書どおりかを確かめるテスト。
    /// </summary>
    public sealed class EnemyFleeTests
    {
        [Test]
        public void DoesNotFleeWhenPlayerIsFarAway()
        {
            // 前方 35 m 以上離れているうちは動かない
            Assert.That(EnemyFlee.StepX(3f, 135f, 0f, 100f, 1f), Is.EqualTo(3f));
        }

        [Test]
        public void DoesNotFleeFromPlayerAlreadyPassed()
        {
            // プレイヤーより後ろに下がった敵は逃げない
            Assert.That(EnemyFlee.StepX(3f, 90f, 0f, 100f, 1f), Is.EqualTo(3f));
        }

        [Test]
        public void FleesToTheRightWhenOnTheRightOfPlayer()
        {
            // 毎秒 7 m、プレイヤーから離れる向き
            Assert.That(EnemyFlee.StepX(3f, 120f, 0f, 100f, 1f), Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void FleesToTheLeftWhenOnTheLeftOfPlayer()
        {
            Assert.That(EnemyFlee.StepX(-3f, 120f, 0f, 100f, 1f), Is.EqualTo(-10f).Within(0.001f));
        }

        [Test]
        public void FleesToTheRightWhenDirectlyAhead()
        {
            Assert.That(EnemyFlee.StepX(0f, 120f, 0f, 100f, 1f), Is.EqualTo(7f).Within(0.001f));
        }

        [Test]
        public void DistanceScalesWithWorldTime()
        {
            // 照準中は世界が 0.25 倍速なので、逃げる量も 0.25 倍になる
            Assert.That(EnemyFlee.StepX(0f, 120f, 0f, 100f, 0.25f), Is.EqualTo(1.75f).Within(0.001f));
        }

        [Test]
        public void StopsAtTheEdgeOfTheRoad()
        {
            Assert.That(EnemyFlee.StepX(18f, 120f, 0f, 100f, 1f), Is.EqualTo(19f));
            Assert.That(EnemyFlee.StepX(-18f, 120f, 0f, 100f, 1f), Is.EqualTo(-19f));
        }

        [Test]
        public void TriggerRangeIsExclusive()
        {
            // ちょうど 35 m のときは、まだ逃げない（仕様書「35 m以内」はデモ版の < 35 に合わせた）
            Assert.That(EnemyFlee.StepX(3f, 135f, 0f, 100f, 1f), Is.EqualTo(3f));
            Assert.That(EnemyFlee.StepX(3f, 134.9f, 0f, 100f, 1f), Is.GreaterThan(3f));
        }
    }
}

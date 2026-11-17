using BlinkExpress.Effects.Model;
using BlinkExpress.Enemy.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// 粉砕の演出の数値を確かめるテスト。止まる時間は仕様書「敵・コイン・トラップ」の表の値。
    /// </summary>
    public sealed class SmashEffectRulesTests
    {
        [Test]
        public void HitStopMatchesSpecTable()
        {
            Assert.That(EnemyRules.HitStopSec(EnemyTier.Soft), Is.EqualTo(0.07f));
            Assert.That(EnemyRules.HitStopSec(EnemyTier.Normal), Is.EqualTo(0.09f));
            Assert.That(EnemyRules.HitStopSec(EnemyTier.Hard), Is.EqualTo(0.11f));
        }

        [Test]
        public void HarderEnemiesMakeMoreDebris()
        {
            Assert.That(SmashEffectRules.DebrisCount(EnemyTier.Normal), Is.GreaterThan(SmashEffectRules.DebrisCount(EnemyTier.Soft)));
            Assert.That(SmashEffectRules.DebrisCount(EnemyTier.Hard), Is.GreaterThan(SmashEffectRules.DebrisCount(EnemyTier.Normal)));
        }

        [Test]
        public void RingIsLargerThanTheEnemy()
        {
            foreach (EnemyTier tier in System.Enum.GetValues(typeof(EnemyTier)))
            {
                Assert.That(SmashEffectRules.RingMaxRadiusM(tier), Is.GreaterThan(EnemyRules.RadiusM(tier)));
            }
        }

        [Test]
        public void CoinFlyProgressGoesFromZeroToOne()
        {
            Assert.That(SmashEffectRules.CoinFlyProgress(0f), Is.EqualTo(0f));
            Assert.That(SmashEffectRules.CoinFlyProgress(0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(SmashEffectRules.CoinFlyProgress(1f), Is.EqualTo(1f));
        }

        [Test]
        public void CoinFlyProgressIsClamped()
        {
            Assert.That(SmashEffectRules.CoinFlyProgress(-1f), Is.EqualTo(0f));
            Assert.That(SmashEffectRules.CoinFlyProgress(2f), Is.EqualTo(1f));
        }

        [Test]
        public void CoinFlyStartsSlowly()
        {
            // 最初の 25% の時間では、進みは 25% より小さい（ゆっくり始まる）
            Assert.That(SmashEffectRules.CoinFlyProgress(0.25f), Is.LessThan(0.25f));
        }
    }
}

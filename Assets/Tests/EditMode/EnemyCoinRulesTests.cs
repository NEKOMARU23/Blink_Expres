using BlinkExpress.Enemy.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// 敵の粉砕の判定と、敵・コインの数値が仕様書どおりかを確かめるテスト。
    /// </summary>
    public sealed class EnemyCoinRulesTests
    {
        [Test]
        public void SoftEnemyCanAlwaysBeSmashed()
        {
            // やわらかい敵は、速さ 0 でも粉砕できる
            Assert.That(EnemyRules.CanSmash(0f, EnemyTier.Soft), Is.True);
        }

        [Test]
        public void NormalEnemyNeedsTwentySixMetersPerSecond()
        {
            Assert.That(EnemyRules.CanSmash(25f, EnemyTier.Normal), Is.False);
            Assert.That(EnemyRules.CanSmash(26f, EnemyTier.Normal), Is.True);
        }

        [Test]
        public void HardEnemyNeedsFortyEightMetersPerSecond()
        {
            // かたい敵は上限近く（115 km/h 相当）が必要
            Assert.That(EnemyRules.CanSmash(47f, EnemyTier.Hard), Is.False);
            Assert.That(EnemyRules.CanSmash(48f, EnemyTier.Hard), Is.True);
        }

        [Test]
        public void SmashRewardsMatchSpecTable()
        {
            // 仕様書の表：加速（+3、+4、+6）、コイン（1、2、4）、ゲージ（+0.15、+0.2、+0.3）
            Assert.That(EnemyRules.SpeedGainMps(EnemyTier.Soft), Is.EqualTo(3f));
            Assert.That(EnemyRules.SpeedGainMps(EnemyTier.Normal), Is.EqualTo(4f));
            Assert.That(EnemyRules.SpeedGainMps(EnemyTier.Hard), Is.EqualTo(6f));
            Assert.That(EnemyRules.CoinCount(EnemyTier.Hard), Is.EqualTo(4));
            Assert.That(EnemyRules.GaugeGain(EnemyTier.Normal), Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void EnemyRadiusGrowsWithTier()
        {
            // 仕様書の表：当たりの半径は 1.1、1.7、2.8 m
            Assert.That(EnemyRules.RadiusM(EnemyTier.Soft), Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(EnemyRules.RadiusM(EnemyTier.Normal), Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(EnemyRules.RadiusM(EnemyTier.Hard), Is.EqualTo(2.8f).Within(0.0001f));
        }

        [Test]
        public void CoinRulesMatchSpec()
        {
            // コインは体の中心から 1.9 m 以内で取れ、1枚で +1.2 m/s、ゲージ +0.05
            Assert.That(CoinRules.PickupRangeM, Is.EqualTo(1.9f).Within(0.0001f));
            Assert.That(CoinRules.SpeedGainMps, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(CoinRules.GaugeGain, Is.EqualTo(0.05f).Within(0.0001f));
        }
    }
}

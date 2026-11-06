using BlinkExpress.Beacon.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// ビーコン関連の Model（チェイン判定、弾道、ゲージ）が仕様書どおりかを確かめるテスト。
    /// </summary>
    public sealed class BeaconModelTests
    {
        [Test]
        public void ThrowWithin0_3SecondsIsEarlyAndGivesNoChain()
        {
            // 連打は加速なし、チェインは 0 に戻る
            Assert.That(ChainRule.NextChain(0.2f, 3), Is.EqualTo(0));
        }

        [Test]
        public void ThrowWithinChainWindowContinuesChain()
        {
            // 0.3〜1.6 秒の間に投げると、チェインが 1 つ増える
            Assert.That(ChainRule.NextChain(0.5f, 1), Is.EqualTo(2));
        }

        [Test]
        public void ThrowAfterChainWindowStartsSingle()
        {
            // 1.6 秒以上あけると単発になり、チェインは 1 から
            Assert.That(ChainRule.NextChain(2f, 4), Is.EqualTo(1));
        }

        [Test]
        public void WarpBoostGrowsWithChain()
        {
            // 単発は +3、チェイン 2 は +3 + 3 = +6、連打は加速なし
            Assert.That(ChainRule.WarpBoostMps(1, 3f, 3f), Is.EqualTo(3f));
            Assert.That(ChainRule.WarpBoostMps(2, 3f, 3f), Is.EqualTo(6f));
            Assert.That(ChainRule.WarpBoostMps(0, 3f, 3f), Is.EqualTo(0f));
        }

        [Test]
        public void InitialSpeedAddsHalfOfPlayerSpeed()
        {
            // 初速は 70 + 自分の速さ × 0.5
            Assert.That(BeaconFlight.InitialSpeedMps(30f, 70f), Is.EqualTo(85f).Within(0.001f));
        }

        [Test]
        public void HorizontalThrowLandsAboutTwentyMetersAway()
        {
            // 水平に投げ、高さ 1.6 m から落ちたとき、着地まで約 20 m（仕様書の目安）
            var flight = new BeaconFlight(0f, 1.6f, 0f, 0f, 0f, 1f, 70f, 45f, 2f);

            var dt = 1f / 120f;
            while (flight.IsAlive && flight.Y > 0f)
            {
                flight.Advance(dt);
            }

            Assert.That(flight.Z, Is.InRange(15f, 25f));
        }

        [Test]
        public void BeaconExpiresAfterLifetime()
        {
            var flight = new BeaconFlight(0f, 1.6f, 0f, 0f, 1f, 0f, 70f, 45f, 2f);

            // 2 秒を少し超えるまで進める（浮動小数点の誤差で残りが僅かに残らないように）
            for (var i = 0; i < 250; i++)
            {
                flight.Advance(1f / 120f);
            }

            Assert.That(flight.IsAlive, Is.False);
        }

        [Test]
        public void AimCannotBeginWhenGaugeIsTooLow()
        {
            // ゲージが照準の消費分以下だと、照準を始められない
            var gauge = new AimGauge(0.08f, 0.08f, 0.15f, 0.05f);

            Assert.That(gauge.TryBeginAim(), Is.False);
            Assert.That(gauge.Value, Is.EqualTo(0.08f).Within(0.0001f));
        }

        [Test]
        public void BeginningAimConsumesCost()
        {
            var gauge = new AimGauge(0.7f, 0.08f, 0.15f, 0.05f);

            Assert.That(gauge.TryBeginAim(), Is.True);
            Assert.That(gauge.Value, Is.EqualTo(0.62f).Within(0.0001f));
        }

        [Test]
        public void AimingDrainsGaugeUntilEmpty()
        {
            // 満タンから約 6 秒（実時間）で 0 になる
            var gauge = new AimGauge(1f, 0.08f, 0.15f, 0.05f);

            var stillAiming = true;
            for (var i = 0; i < 70 && stillAiming; i++)
            {
                stillAiming = gauge.DrainWhileAiming(0.1f);
            }

            Assert.That(stillAiming, Is.False);
            Assert.That(gauge.Value, Is.EqualTo(0f));
        }
    }
}

using BlinkExpress.Beacon.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// ワープの結果、予測の文言、クールタイムが仕様書どおりかを確かめるテスト。
    /// </summary>
    public sealed class WarpRulesTests
    {
        [Test]
        public void EnemyIsSmashedWhenSpeedIsEnoughAfterBoost()
        {
            // 加速後の速さが必要速度以上なら粉砕（ちょうど同じ値も粉砕）
            Assert.That(WarpRules.Classify(HitKind.Enemy, 48f, 48f), Is.EqualTo(WarpOutcome.Smash));
        }

        [Test]
        public void EnemyBouncesWhenSpeedIsNotEnough()
        {
            Assert.That(WarpRules.Classify(HitKind.Enemy, 40f, 48f), Is.EqualTo(WarpOutcome.Bounce));
        }

        [Test]
        public void NoWarpSurfaceDoesNotMovePlayer()
        {
            var outcome = WarpRules.Classify(HitKind.NoWarpSurface, 30f, 0f);

            Assert.That(outcome, Is.EqualTo(WarpOutcome.NoWarp));
            Assert.That(WarpRules.MovesPlayer(outcome), Is.False);
        }

        [Test]
        public void CeilingScalesSpeedDown()
        {
            // 天井に当たると速さが 0.55 倍になる
            var after = WarpRules.SpeedAfter(WarpOutcome.Ceiling, 40f);

            Assert.That(after, Is.EqualTo(22f).Within(0.001f));
        }

        [Test]
        public void BounceHalvesSpeedButGroundKeepsSpeed()
        {
            Assert.That(WarpRules.SpeedAfter(WarpOutcome.Bounce, 40f), Is.EqualTo(20f).Within(0.001f));
            Assert.That(WarpRules.SpeedAfter(WarpOutcome.Ground, 40f), Is.EqualTo(40f).Within(0.001f));
        }

        [Test]
        public void MissAndNoWarpAndBounceDoNotMovePlayer()
        {
            Assert.That(WarpRules.MovesPlayer(WarpOutcome.Miss), Is.False);
            Assert.That(WarpRules.MovesPlayer(WarpOutcome.Bounce), Is.False);
            Assert.That(WarpRules.MovesPlayer(WarpOutcome.Ground), Is.True);
            Assert.That(WarpRules.MovesPlayer(WarpOutcome.WallSlide), Is.True);
        }

        [Test]
        public void AimFeedbackMatchesSpecTable()
        {
            // 仕様書の表どおり：着地は水色、粉砕は緑、速度不足は黄、ワープ不可は赤、届かないは灰
            Assert.That(AimFeedback.For(WarpOutcome.Ground).Text, Is.EqualTo("着地"));
            Assert.That(AimFeedback.For(WarpOutcome.Ground).Tone, Is.EqualTo(AimTone.Cyan));
            Assert.That(AimFeedback.For(WarpOutcome.Smash).Tone, Is.EqualTo(AimTone.Green));
            Assert.That(AimFeedback.For(WarpOutcome.Bounce).Tone, Is.EqualTo(AimTone.Yellow));
            Assert.That(AimFeedback.For(WarpOutcome.NoWarp).Tone, Is.EqualTo(AimTone.Red));
            Assert.That(AimFeedback.For(WarpOutcome.Miss).Tone, Is.EqualTo(AimTone.Gray));
        }

        [Test]
        public void ThrowIsBlockedDuringCooldownAndFlight()
        {
            var cooldown = new ThrowCooldown(0.35f);
            Assert.That(cooldown.CanThrow(false), Is.True);

            cooldown.OnThrown();
            Assert.That(cooldown.CanThrow(false), Is.False);

            cooldown.Tick(0.4f);
            Assert.That(cooldown.CanThrow(false), Is.True);
            Assert.That(cooldown.CanThrow(true), Is.False);
        }
    }
}

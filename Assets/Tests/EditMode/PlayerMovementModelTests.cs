using System;
using BlinkExpress.Player.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// PlayerMovementModel の計算が仕様書どおりになっているかを確かめるテスト。
    /// </summary>
    public sealed class PlayerMovementModelTests
    {
        private const float Dt = 0.1f;

        /// <summary>
        /// 仕様書「速度」「操作」「ジャンプと壁」の初期値を入れた設定を作る。
        /// </summary>
        private static MovementSettings CreateSettings()
        {
            return new MovementSettings
            {
                MinSpeedMps = 8f,
                CruiseSpeedMps = 30f,
                MaxSpeedMps = 60f,
                StartSpeedMps = 14f,
                AccelMps2 = 3f,
                BrakeMps2 = 28f,
                SteerRadPerSec = 5f,
                LeanRad = 0.6f,
                GravityMps2 = 26f,
                AirSteerRatio = 0.35f,
            };
        }

        /// <summary>
        /// 入力なし（ブレーキなし、寄せなし）の入力を作る。
        /// </summary>
        private static MovementInput Idle(float cameraYawRad = 0f)
        {
            return new MovementInput(false, cameraYawRad, 0f);
        }

        [Test]
        public void GroundSpeedStopsAtCruiseSpeed()
        {
            // 地上でスケートだけで加速すると、クルーズ速度（30 m/s）で止まる
            var model = new PlayerMovementModel(CreateSettings());

            for (var i = 0; i < 300; i++)
            {
                model.Tick(Idle(), true, Dt);
            }

            Assert.That(model.SpeedMps, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void BrakeNeverGoesBelowMinSpeed()
        {
            // ブレーキをかけ続けても、最低速度（8 m/s）を下回らない
            var model = new PlayerMovementModel(CreateSettings());
            var brake = new MovementInput(true, 0f, 0f);

            for (var i = 0; i < 300; i++)
            {
                model.Tick(brake, true, Dt);
            }

            Assert.That(model.SpeedMps, Is.EqualTo(8f).Within(0.01f));
        }

        [Test]
        public void SteerTurnsNoMoreThanSteerRatePerFrame()
        {
            // 真後ろを向いたカメラに対して、1フレームで曲がれるのは 5 rad/s × 0.1 s = 0.5 rad まで
            var model = new PlayerMovementModel(CreateSettings());

            model.Tick(Idle(cameraYawRad: (float)Math.PI), true, Dt);

            Assert.That(Math.Abs(model.YawRad), Is.LessThanOrEqualTo(0.5f + 0.01f));
        }

        [Test]
        public void AirFallKeepsSpeedAndBendsDownward()
        {
            // 空中では重力で向きが下がっても、速さは変わらない（仕様書「重力のルール」）
            var model = new PlayerMovementModel(CreateSettings());
            var before = model.SpeedMps;

            for (var i = 0; i < 200; i++)
            {
                model.Tick(Idle(), false, 0.02f);
            }

            Assert.That(model.SpeedMps, Is.EqualTo(before).Within(0.01f));
            Assert.That(model.VelocityY, Is.LessThan(0f));
        }

        [Test]
        public void GroundJumpSetsUpwardSpeed()
        {
            // 速さ 30 m/s の地上ジャンプは、上向きの速さが 12.5 m/s になる
            var settings = CreateSettings();
            settings.StartSpeedMps = 30f;
            var model = new PlayerMovementModel(settings);

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), true, Dt);

            Assert.That(model.VelocityY, Is.EqualTo(12.5f).Within(0.01f));
            Assert.That(model.SpeedMps, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void SlowJumpIsCappedAtMaxUpRatio()
        {
            // 速さ 14 m/s では上向きの割合の上限（0.8）で止まる
            var model = new PlayerMovementModel(CreateSettings());

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), true, Dt);

            // 地上の1フレームでスケートの加速も入るので、期待値はジャンプ後の速さから求める
            Assert.That(model.VelocityY, Is.EqualTo(model.SpeedMps * 0.8f).Within(0.01f));
        }

        [Test]
        public void DoubleJumpWorksOnceInAir()
        {
            // 空中で 40 m/s 以上なら二段ジャンプができる。2 回目は、空中では使えない
            var settings = CreateSettings();
            settings.StartSpeedMps = 45f;
            var model = new PlayerMovementModel(settings);

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);
            Assert.That(model.VelocityY, Is.EqualTo(12.5f).Within(0.01f));

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);
            Assert.That(model.VelocityY, Is.LessThan(12.5f));
        }

        [Test]
        public void DoubleJumpNeedsMinimumSpeed()
        {
            // 30 m/s では二段ジャンプできない（上向きの速さは生まれず、重力で下がる）
            var settings = CreateSettings();
            settings.StartSpeedMps = 30f;
            var model = new PlayerMovementModel(settings);

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);

            Assert.That(model.VelocityY, Is.LessThan(0f));
        }

        [Test]
        public void LandingRestoresDoubleJump()
        {
            var settings = CreateSettings();
            settings.StartSpeedMps = 45f;
            var model = new PlayerMovementModel(settings);

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);
            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);
            model.Tick(new MovementInput(false, 0f, 0f), true, 0.1f);
            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);

            Assert.That(model.VelocityY, Is.EqualTo(12.5f).Within(0.01f));
        }

        [Test]
        public void SetDirectionSetsHeadingAndSpeed()
        {
            // ワープの着地で、向きと速さを設定できる（+X 方向、速さ 20 m/s）
            var model = new PlayerMovementModel(CreateSettings());

            model.SetDirection(1f, 0f, 0f, 20f);

            Assert.That(model.SpeedMps, Is.EqualTo(20f).Within(0.01f));
            Assert.That(model.YawRad, Is.EqualTo((float)(Math.PI / 2)).Within(0.001f));
        }

        [Test]
        public void SetSpeedKeepsHeading()
        {
            var model = new PlayerMovementModel(CreateSettings());

            model.SetSpeed(40f);

            Assert.That(model.SpeedMps, Is.EqualTo(40f).Within(0.01f));
            Assert.That(model.YawRad, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void LandingKeepsSpeedAboveCruiseSpeed()
        {
            // ワープなどで上限より速いまま着地しても、減速要因がなければ速さは保たれる
            var settings = CreateSettings();
            settings.StartSpeedMps = 45f;
            var model = new PlayerMovementModel(settings);

            model.Tick(Idle(), true, Dt);

            Assert.That(model.SpeedMps, Is.EqualTo(45f).Within(0.01f));
        }
    }
}

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
        public void SlidingStopsSkateAcceleration()
        {
            // スライディング中は、スケートの加速が止まる（速さは 14 m/s のまま）
            var model = new PlayerMovementModel(CreateSettings());

            for (var i = 0; i < 20; i++)
            {
                model.Tick(new MovementInput(false, 0f, 0f, slideHeld: true), true, Dt);
            }

            Assert.That(model.IsSliding, Is.True);
            Assert.That(model.SpeedMps, Is.EqualTo(14f).Within(0.01f));
        }

        [Test]
        public void SlidingNeedsMinimumSpeed()
        {
            // 12 m/s より遅いとスライディングにならない
            var settings = CreateSettings();
            settings.StartSpeedMps = 10f;
            var model = new PlayerMovementModel(settings);

            model.Tick(new MovementInput(false, 0f, 0f, slideHeld: true), true, Dt);

            Assert.That(model.IsSliding, Is.False);
        }

        [Test]
        public void SlidingContinuesUnderCeilingAfterReleasingKey()
        {
            // キーを離しても、頭の上に天井があれば立ち上がれず、スライディングを続ける
            var model = new PlayerMovementModel(CreateSettings());
            model.Tick(new MovementInput(false, 0f, 0f, slideHeld: true), true, Dt);

            model.Tick(new MovementInput(false, 0f, 0f, slideHeld: false, canStand: false), true, Dt);
            Assert.That(model.IsSliding, Is.True);

            model.Tick(new MovementInput(false, 0f, 0f, slideHeld: false, canStand: true), true, Dt);
            Assert.That(model.IsSliding, Is.False);
        }

        /// <summary>
        /// 正面の壁（+Z 方向に向かって、壁の法線は -Z）に触れている状態を作る。
        /// </summary>
        private static WallContact FrontWall(bool touching, bool topReached = false)
        {
            return new WallContact(touching, 0f, -1f, topReached, 0f);
        }

        [Test]
        public void ClimbRisesAboutSpeedSquaredOver40()
        {
            // 速さ 30 m/s で前から壁に当たると、約 30² ÷ 40 = 22.5 m 登る（仕様書「壁」の節）
            var settings = CreateSettings();
            settings.StartSpeedMps = 30f;
            var model = new PlayerMovementModel(settings);

            model.Tick(new MovementInput(false, 0f, 0f, wall: FrontWall(true)), false, 0.001f);
            var height = 0f;
            for (var i = 0; i < 5000 && model.IsClimbing; i++)
            {
                // 壁に触れている間だけ、壁のぼりを続ける
                model.Tick(new MovementInput(false, 0f, 0f, wall: FrontWall(model.IsClimbing)), false, 0.001f);
                height += model.VelocityY * 0.001f;
            }

            Assert.That(height, Is.EqualTo(22.5f).Within(0.5f));
        }

        [Test]
        public void ClimbKickAddsSixteenOnlyOnce()
        {
            // 壁のぼり中のジャンプは、上昇速度に +16 m/s を一度だけ足す
            var settings = CreateSettings();
            settings.StartSpeedMps = 30f;
            var model = new PlayerMovementModel(settings);
            model.Tick(new MovementInput(false, 0f, 0f, wall: FrontWall(true)), false, 0.1f);
            var before = model.VelocityY;

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true, wall: FrontWall(true)), false, 0.1f);
            Assert.That(model.VelocityY, Is.EqualTo(before + 16f).Within(0.01f));

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true, wall: FrontWall(true)), false, 0.1f);
            Assert.That(model.VelocityY, Is.EqualTo(before + 16f).Within(0.01f));
        }

        [Test]
        public void WallRunKeepsHeightWhileRunTimeLasts()
        {
            // 横から空中で壁に当たると壁走り。最初の 1.2 秒は重力がかからない
            var settings = CreateSettings();
            settings.StartSpeedMps = 20f;
            var model = new PlayerMovementModel(settings);
            var sideWall = new WallContact(true, 1f, 0f, false, 3f);

            model.Tick(new MovementInput(false, 0f, 0f, wall: sideWall), false, 0.1f);
            Assert.That(model.IsRunning, Is.True);
            var yAtStart = model.VelocityY;

            for (var i = 0; i < 10; i++)
            {
                model.Tick(new MovementInput(false, 0f, 0f, wall: sideWall), false, 0.1f);
            }

            Assert.That(model.VelocityY, Is.EqualTo(yAtStart).Within(0.01f));
            Assert.That(model.SpeedMps, Is.EqualTo(20f).Within(0.01f));
        }

        [Test]
        public void WallRunJumpLeavesWallAndRestoresDoubleJump()
        {
            // 壁走りのジャンプで壁から離れ、二段ジャンプが戻る
            var settings = CreateSettings();
            settings.StartSpeedMps = 45f;
            var model = new PlayerMovementModel(settings);
            var sideWall = new WallContact(true, 1f, 0f, false, 3f);

            model.Tick(new MovementInput(false, 0f, 0f, wall: sideWall), false, 0.1f);
            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true, wall: sideWall), false, 0.1f);

            Assert.That(model.IsRunning, Is.False);
            Assert.That(model.VelocityX, Is.GreaterThan(0f));
            Assert.That(model.VelocityY, Is.GreaterThan(0f));

            model.Tick(new MovementInput(false, 0f, 0f, jumpPressed: true), false, 0.1f);
            Assert.That(model.VelocityY, Is.EqualTo(12.5f).Within(0.01f));
        }

        [Test]
        public void SideWallOnGroundDoesNotStartRun()
        {
            // 地面にいるときは、横の壁では壁走りにならない
            var model = new PlayerMovementModel(CreateSettings());

            model.Tick(new MovementInput(false, 0f, 0f, wall: new WallContact(true, 1f, 0f, false, 3f)), true, Dt);

            Assert.That(model.IsRunning, Is.False);
        }

        [Test]
        public void MudSlowsByAboutSixteenPerSecond()
        {
            // 泥の上では毎秒 16 m/s 減る（スケートの加速は打ち消される）。0.5 秒で 20 → 12 m/s
            var settings = CreateSettings();
            settings.StartSpeedMps = 20f;
            var model = new PlayerMovementModel(settings);
            var mud = new HazardContact(true, false, false, 0f, 0f, 0f, false);

            for (var i = 0; i < 5; i++)
            {
                model.Tick(new MovementInput(false, 0f, 0f, hazard: mud), true, 0.1f);
            }

            Assert.That(model.SpeedMps, Is.EqualTo(12f).Within(0.05f));
        }

        [Test]
        public void MushroomBouncesUpAndKeepsSpeed()
        {
            // 速さ 30 m/s のキノコは、上向きの速さを min(30 × 0.9, 24) = 24 m/s にする。速さは保つ
            var settings = CreateSettings();
            settings.StartSpeedMps = 30f;
            var model = new PlayerMovementModel(settings);
            var mushroom = new HazardContact(false, true, false, 0f, 0f, 0f, false);

            model.Tick(new MovementInput(false, 0f, 0f, hazard: mushroom), true, Dt);

            Assert.That(model.VelocityY, Is.EqualTo(24f).Within(0.01f));
            Assert.That(model.SpeedMps, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void RailAcceleratesAlongRailDirection()
        {
            // レールの上では、レールの向き（+Z）へ、毎秒 4 m/s ずつ加速する
            var settings = CreateSettings();
            settings.StartSpeedMps = 20f;
            var model = new PlayerMovementModel(settings);
            var rail = new HazardContact(false, false, true, 0f, 1f, 0f, false);

            for (var i = 0; i < 10; i++)
            {
                model.Tick(new MovementInput(false, 0f, 0f, hazard: rail), true, 0.1f);
            }

            Assert.That(model.SpeedMps, Is.EqualTo(24f).Within(0.01f));
            Assert.That(model.VelocityY, Is.EqualTo(0f).Within(0.01f));
            Assert.That(model.VelocityZ, Is.GreaterThan(0f));
        }

        [Test]
        public void ObstacleSlowsByFormula()
        {
            // 登れない障害物に正面から当たると、速さは × (1 − 0.45 × 内積)。内積 1.0 なら × 0.55
            var settings = CreateSettings();
            settings.StartSpeedMps = 20f;
            var model = new PlayerMovementModel(settings);
            var obstacle = new HazardContact(false, false, false, 0f, 0f, 1f, false);

            model.Tick(new MovementInput(false, 0f, 0f, hazard: obstacle), false, 0.001f);

            Assert.That(model.SpeedMps, Is.EqualTo(11f).Within(0.05f));
        }

        [Test]
        public void CeilingHitLowersSpeedAndDropsDown()
        {
            // 天井に当たると速さが × 0.55 になり、下へ向きを変える
            var settings = CreateSettings();
            settings.StartSpeedMps = 20f;
            var model = new PlayerMovementModel(settings);
            var ceiling = new HazardContact(false, false, false, 0f, 0f, 0f, true);

            model.Tick(new MovementInput(false, 0f, 0f, hazard: ceiling), false, 0.001f);

            Assert.That(model.SpeedMps, Is.EqualTo(11f).Within(0.05f));
            Assert.That(model.VelocityY, Is.LessThan(0f));
        }

        [Test]
        public void AddSpeedNeverExceedsMaximum()
        {
            // コインや粉砕で速さを足しても、上限（60 m/s）を超えない
            var settings = CreateSettings();
            settings.StartSpeedMps = 58f;
            var model = new PlayerMovementModel(settings);

            model.AddSpeed(6f);

            Assert.That(model.SpeedMps, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void BounceHalvesSpeedAndTurnsAway()
        {
            // 弾かれると、速さが半分になり、指定の向き（+X）へ逸れる
            var settings = CreateSettings();
            settings.StartSpeedMps = 30f;
            var model = new PlayerMovementModel(settings);

            model.Bounce(1f, 0f);

            Assert.That(model.SpeedMps, Is.EqualTo(15f).Within(0.01f));
            Assert.That(model.VelocityX, Is.GreaterThan(0f));
        }

        [Test]
        public void WallSlideKeepsSpeedAndFallsAlongWall()
        {
            // 壁滑走：速さ 20 m/s のまま、重力（13 m/s²）で下へ滑る。壁から離れる向きには出ない
            var model = new PlayerMovementModel(CreateSettings());
            model.StartWallSlide(-1f, 0f, 0f, 0f, 1f, 20f);
            var before = model.VelocityY;

            model.Tick(new MovementInput(false, 0f, 0f), false, 0.5f);

            Assert.That(model.SpeedMps, Is.EqualTo(20f).Within(0.05f));
            Assert.That(model.VelocityY, Is.LessThan(before));
            Assert.That(model.VelocityX, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void WallSlideEndsOnGround()
        {
            var model = new PlayerMovementModel(CreateSettings());
            model.StartWallSlide(-1f, 0f, 0f, -0.5f, 1f, 20f);

            model.Tick(new MovementInput(false, 0f, 0f), true, 0.1f);

            Assert.That(model.IsRunning, Is.False);
            Assert.That(model.IsClimbing, Is.False);
        }

        [Test]
        public void AirSteerTurnsTowardLean()
        {
            // 空中でも、寄せ（A）の向きへ曲がる（地上の 0.35 倍の速さで）。1 秒で横の速さが生まれる
            var settings = CreateSettings();
            settings.StartSpeedMps = 25f;
            var model = new PlayerMovementModel(settings);

            for (var i = 0; i < 100; i++)
            {
                model.Tick(new MovementInput(false, 0f, 1f), false, 0.01f);
            }

            Assert.That(model.VelocityX, Is.GreaterThan(1f));
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

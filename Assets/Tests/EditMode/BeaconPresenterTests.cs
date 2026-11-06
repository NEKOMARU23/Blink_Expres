using System.Collections.Generic;
using BlinkExpress.Beacon.Model;
using BlinkExpress.Beacon.Presenter;
using NUnit.Framework;
using UnityEngine;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// BeaconPresenter が、照準・投げ・着地・チェインを仕様どおりに進めるかを確かめるテスト。
    /// 床（y = 0）だけを持つ偽の View を使う。
    /// </summary>
    public sealed class BeaconPresenterTests
    {
        private const float Frame = 1f / 60f;

        /// <summary>
        /// 床（y = 0）だけに当たる偽の View。呼ばれた内容を記録する。
        /// </summary>
        private sealed class FakeBeaconView : IBeaconView
        {
            public readonly List<(Vector3 Position, Vector3 Direction, float Speed, WarpOutcome Outcome)> Warps = new List<(Vector3, Vector3, float, WarpOutcome)>();
            public AimFeedback LastFeedback;
            public bool PreviewShown;

            public bool Raycast(Vector3 from, Vector3 to, out BeaconHit hit)
            {
                hit = default;
                if (from.y > 0f && to.y <= 0f)
                {
                    var t = from.y / (from.y - to.y);
                    var point = Vector3.Lerp(from, to, t);
                    point.y = 0f;
                    hit = new BeaconHit(HitKind.Ground, point, Vector3.up, 0f);
                    return true;
                }

                return false;
            }

            public void ShowAimPreview(IReadOnlyList<Vector3> points, AimFeedback feedback)
            {
                PreviewShown = true;
                LastFeedback = feedback;
            }

            public void HideAimPreview()
            {
                PreviewShown = false;
            }

            public void ShowBeacon(Vector3 position)
            {
            }

            public void HideBeacon()
            {
            }

            public void WarpPlayer(Vector3 position, Vector3 direction, float speedMps, WarpOutcome outcome)
            {
                Warps.Add((position, direction, speedMps, outcome));
            }

            public void SetPlayerSpeed(float speedMps)
            {
            }
        }

        private static readonly BeaconPlayerState Player = new BeaconPlayerState(Vector3.zero, 14f);

        /// <summary>
        /// 入力を与えずに、指定の時間だけ進める。
        /// </summary>
        private static void Idle(BeaconPresenter presenter, float seconds)
        {
            var frames = Mathf.RoundToInt(seconds / Frame);
            for (var i = 0; i < frames; i++)
            {
                presenter.Tick(new BeaconInput(false, false, false, Vector3.forward), Player, Frame);
            }
        }

        /// <summary>
        /// 照準を始めて、少し待ってから投げる。
        /// </summary>
        private static void AimAndThrow(BeaconPresenter presenter)
        {
            var forward = new Vector3(0f, 0f, 1f);
            presenter.Tick(new BeaconInput(true, false, false, forward), Player, Frame);
            Idle(presenter, 0.3f);
            presenter.Tick(new BeaconInput(false, true, false, forward), Player, Frame);
        }

        [Test]
        public void ThrowOntoGroundWarpsPlayerToGround()
        {
            var view = new FakeBeaconView();
            var presenter = new BeaconPresenter(view, new BeaconSettings());

            AimAndThrow(presenter);
            Idle(presenter, 2f);

            Assert.That(view.Warps.Count, Is.EqualTo(1));
            Assert.That(view.Warps[0].Outcome, Is.EqualTo(WarpOutcome.Ground));
            Assert.That(view.Warps[0].Position.y, Is.EqualTo(0f).Within(0.001f));
            // 水平に投げた（+Z）ので、着地は前方にある
            Assert.That(view.Warps[0].Position.z, Is.GreaterThan(15f));
        }

        [Test]
        public void SecondThrowWithinWindowGetsChainBoost()
        {
            // 1 回目のワープから 0.6 秒後に投げると、チェイン 2 になり加速が上乗せされる
            var view = new FakeBeaconView();
            var presenter = new BeaconPresenter(view, new BeaconSettings());

            // 着地（約 0.27 秒後）してから 0.6 秒待ち、さらに照準の 0.3 秒を足して投げる（前のワープからおよそ 0.9 秒）
            AimAndThrow(presenter);
            Idle(presenter, 0.5f);
            Idle(presenter, 0.6f);
            AimAndThrow(presenter);
            Idle(presenter, 2f);

            Assert.That(view.Warps.Count, Is.EqualTo(2));
            // 1 回目：14 + 3 = 17 m/s。2 回目（チェイン 2）：14 + 6 = 20 m/s
            Assert.That(view.Warps[0].Speed, Is.EqualTo(17f).Within(0.001f));
            Assert.That(view.Warps[1].Speed, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void CancelAimThrowsNothing()
        {
            var view = new FakeBeaconView();
            var presenter = new BeaconPresenter(view, new BeaconSettings());

            presenter.Tick(new BeaconInput(true, false, false, Vector3.forward), Player, Frame);
            Assert.That(presenter.IsAiming, Is.True);
            Assert.That(presenter.WorldTimeScale, Is.EqualTo(0.25f).Within(0.001f));

            presenter.Tick(new BeaconInput(false, false, true, Vector3.forward), Player, Frame);
            Idle(presenter, 2f);

            Assert.That(presenter.IsAiming, Is.False);
            Assert.That(view.Warps.Count, Is.EqualTo(0));
        }
    }
}

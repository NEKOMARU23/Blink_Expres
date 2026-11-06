using System.Collections.Generic;
using BlinkExpress.Beacon.Model;
using UnityEngine;

namespace BlinkExpress.Beacon.Presenter
{
    /// <summary>
    /// ビーコンの Presenter。照準、投げ、飛行、着地までを進め、結果を IBeaconView に反映する。
    /// 予測線と実際の飛行は同じ刻みで計算するので、予測どおりに着地する。
    /// 仕様書「照準とゲージ」「ワープ」「ビーコンの弾道」の節に対応する。
    /// </summary>
    public sealed class BeaconPresenter
    {
        private readonly IBeaconView _view;
        private readonly BeaconSettings _settings;
        private readonly AimGauge _gauge;
        private readonly ThrowCooldown _cooldown;
        private readonly List<Vector3> _previewPoints = new List<Vector3>();

        private float _gameTimeSec;
        private float _lastWarpTimeSec = -100f;
        private int _chain;
        private float _playerSpeedMps;

        private bool _aiming;
        private BeaconFlight _flight;
        private float _flightAccumulatorSec;
        private int _flightChain;
        private float _flightBoostMps;
        private Vector3 _flightDirection;

        /// <summary>
        /// Presenter を作る。
        /// </summary>
        /// <param name="view">ビーコンの View。</param>
        /// <param name="settings">ビーコンの数値。</param>
        public BeaconPresenter(IBeaconView view, BeaconSettings settings)
        {
            _view = view;
            _settings = settings;
            _gauge = new AimGauge(settings.GaugeStartValue, settings.AimCost, settings.AimDrainPerSec, settings.AimRegenPerSec);
            _cooldown = new ThrowCooldown(settings.CooldownSec);
        }

        /// <summary>照準中か。</summary>
        public bool IsAiming => _aiming;

        /// <summary>ゲーム内時間の速さ（0〜1）。照準中は遅くなる。プレイヤーの移動にも使う。</summary>
        public float WorldTimeScale => _aiming ? _settings.SlowFactor : 1f;

        /// <summary>ゲージの値（0〜1）。HUD に表示するときに使う。</summary>
        public float GaugeValue => _gauge.Value;

        /// <summary>
        /// ゲージを回復する（コイン、粉砕など）。
        /// </summary>
        /// <param name="amount">回復する量（0〜1）。</param>
        public void RefillGauge(float amount)
        {
            _gauge.Refill(amount);
        }

        /// <summary>
        /// 1フレーム分の処理を進める。
        /// </summary>
        /// <param name="input">このフレームの入力。</param>
        /// <param name="player">プレイヤーの現在の状態。</param>
        /// <param name="realDt">実時間の経過（秒）。</param>
        public void Tick(BeaconInput input, BeaconPlayerState player, float realDt)
        {
            _playerSpeedMps = player.SpeedMps;
            var worldDt = realDt * WorldTimeScale;

            _gameTimeSec += worldDt;
            _cooldown.Tick(worldDt);
            if (!_aiming)
            {
                _gauge.Regenerate(realDt);
            }

            if (_aiming)
            {
                UpdateAiming(input, player, realDt);
            }
            else if (input.AimStarted && _cooldown.CanThrow(_flight != null) && _gauge.TryBeginAim())
            {
                _aiming = true;
                ShowPreview(input.AimDirection, player);
            }

            if (_flight != null)
            {
                StepFlight(worldDt);
            }
        }

        private void UpdateAiming(BeaconInput input, BeaconPlayerState player, float realDt)
        {
            if (input.CancelPressed || !_gauge.DrainWhileAiming(realDt))
            {
                CancelAim();
                return;
            }

            if (input.AimReleased)
            {
                Throw(input.AimDirection, player);
                return;
            }

            ShowPreview(input.AimDirection, player);
        }

        /// <summary>
        /// 今投げた場合のチェイン数と、加速量を求める。
        /// </summary>
        private int NextChain()
        {
            return ChainRule.NextChain(_gameTimeSec - _lastWarpTimeSec, _chain);
        }

        private float BoostFor(int chain)
        {
            return ChainRule.WarpBoostMps(chain, _settings.WarpBoostMps, _settings.ChainBoostMps);
        }

        private Vector3 OriginOf(BeaconPlayerState player)
        {
            return player.Position + Vector3.up * _settings.ThrowOriginHeightM;
        }

        private BeaconFlight CreateFlight(Vector3 origin, Vector3 direction, float playerSpeedMps)
        {
            var speed = BeaconFlight.InitialSpeedMps(playerSpeedMps, _settings.BaseSpeedMps);
            return new BeaconFlight(origin.x, origin.y, origin.z, direction.x, direction.y, direction.z, speed, _settings.GravityMps2, _settings.LifetimeSec);
        }

        /// <summary>
        /// 照準の予測線を作って表示する。実際の飛行と同じ刻みで、最初に当たるまで進める。
        /// </summary>
        private void ShowPreview(Vector3 direction, BeaconPlayerState player)
        {
            var origin = OriginOf(player);
            var preview = CreateFlight(origin, direction.normalized, player.SpeedMps);
            var chain = NextChain();
            var boostedSpeed = player.SpeedMps + BoostFor(chain);

            _previewPoints.Clear();
            _previewPoints.Add(origin);

            var outcome = WarpOutcome.Miss;
            var previous = origin;
            while (preview.IsAlive)
            {
                preview.Advance(_settings.StepSec);
                var current = new Vector3(preview.X, preview.Y, preview.Z);

                if (_view.Raycast(previous, current, out var hit))
                {
                    _previewPoints.Add(hit.Point);
                    outcome = WarpRules.Classify(hit.Kind, boostedSpeed, hit.EnemyRequiredSpeedMps);
                    break;
                }

                _previewPoints.Add(current);
                previous = current;
            }

            _view.ShowAimPreview(_previewPoints, AimFeedback.For(outcome));
        }

        /// <summary>
        /// 投げる。チェインは投げた瞬間に確定し、着弾したときに加速として適用する。
        /// </summary>
        private void Throw(Vector3 direction, BeaconPlayerState player)
        {
            _aiming = false;
            _view.HideAimPreview();

            _flightChain = NextChain();
            _flightBoostMps = BoostFor(_flightChain);
            _flightDirection = direction.normalized;
            _flight = CreateFlight(OriginOf(player), _flightDirection, player.SpeedMps);
            _flightAccumulatorSec = 0f;
            _cooldown.OnThrown();
            _view.ShowBeacon(OriginOf(player));
        }

        private void CancelAim()
        {
            _aiming = false;
            _view.HideAimPreview();
        }

        /// <summary>
        /// 飛んでいるビーコンを、ゲーム内時間で進める。各刻みで当たりを調べ、当たったら着地させる。
        /// </summary>
        private void StepFlight(float worldDt)
        {
            _flightAccumulatorSec += worldDt;

            while (_flight != null && _flightAccumulatorSec >= _settings.StepSec)
            {
                _flightAccumulatorSec -= _settings.StepSec;

                var previous = new Vector3(_flight.X, _flight.Y, _flight.Z);
                _flight.Advance(_settings.StepSec);
                var current = new Vector3(_flight.X, _flight.Y, _flight.Z);

                if (_view.Raycast(previous, current, out var hit))
                {
                    Land(hit);
                    return;
                }

                if (!_flight.IsAlive)
                {
                    EndFlight();
                    return;
                }

                _view.ShowBeacon(current);
            }
        }

        private void EndFlight()
        {
            _flight = null;
            _view.HideBeacon();
        }

        /// <summary>
        /// ビーコンが当たった結果を適用する。ワープできるものだけプレイヤーを動かす。
        /// </summary>
        private void Land(BeaconHit hit)
        {
            EndFlight();

            var boostedSpeed = _playerSpeedMps + _flightBoostMps;
            var outcome = WarpRules.Classify(hit.Kind, boostedSpeed, hit.EnemyRequiredSpeedMps);

            switch (outcome)
            {
                case WarpOutcome.Ground:
                case WarpOutcome.Ceiling:
                    _chain = _flightChain;
                    _lastWarpTimeSec = _gameTimeSec;
                    var direction = LandingDirection(outcome, _flightDirection);
                    _view.WarpPlayer(hit.Point, direction, WarpRules.SpeedAfter(outcome, boostedSpeed), outcome);
                    break;

                case WarpOutcome.NoWarp:
                    _chain = 0;
                    break;

                case WarpOutcome.Bounce:
                    _view.SetPlayerSpeed(WarpRules.SpeedAfter(outcome, _playerSpeedMps));
                    break;

                case WarpOutcome.WallSlide:
                    _chain = _flightChain;
                    _lastWarpTimeSec = _gameTimeSec;
                    var slideDirection = WallSlideDirection(hit.Normal, _flightDirection);
                    _view.StartWallSlide(hit.Point, hit.Normal, slideDirection, boostedSpeed);
                    break;

                default:
                    // レールは、まだ実装していない（次の作業で扱う）
                    break;
            }
        }

        /// <summary>
        /// 壁滑走の向きを求める。投げた向きを壁の面に投影する（真正面に投げたときは、水平の前方を使う）。
        /// </summary>
        /// <param name="normal">壁の法線（壁から体へ向かう向き）。</param>
        /// <param name="throwDirection">投げた向き。</param>
        /// <returns>壁に沿った、正規化した向き。</returns>
        private static Vector3 WallSlideDirection(Vector3 normal, Vector3 throwDirection)
        {
            var projected = Vector3.ProjectOnPlane(throwDirection, normal);
            if (projected.sqrMagnitude < 1e-4f)
            {
                projected = Vector3.ProjectOnPlane(Vector3.forward, normal);
            }

            return projected.normalized;
        }

        /// <summary>
        /// 着地後の進む向きを求める。
        /// 地面は投げた向きの水平成分。天井は水平成分を弱めて、下向きに落ちる。
        /// </summary>
        private static Vector3 LandingDirection(WarpOutcome outcome, Vector3 throwDirection)
        {
            var horizontal = new Vector3(throwDirection.x, 0f, throwDirection.z);
            if (horizontal.sqrMagnitude < 1e-4f)
            {
                horizontal = Vector3.forward;
            }

            if (outcome == WarpOutcome.Ceiling)
            {
                return (horizontal.normalized * 0.35f + Vector3.down).normalized;
            }

            return horizontal.normalized;
        }
    }
}

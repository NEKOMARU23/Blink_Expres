using System;

namespace BlinkExpress.Player.Model
{
    /// <summary>
    /// プレイヤーの移動のルールと状態を持つ Model。Unity に依存しない。
    /// 速度は X・Y・Z の成分で持ち、速さと向きは成分から求める。
    /// 仕様書「速度」「ジャンプと壁」の節に対応する。
    /// </summary>
    public sealed class PlayerMovementModel
    {
        private const float Pi = (float)Math.PI;
        private const float TwoPi = (float)(Math.PI * 2);

        /// <summary>ジャンプの上向きの割合の上限（仕様書「ジャンプ」の節：最大 0.8）。</summary>
        private const float JumpMaxUpRatio = 0.8f;

        private readonly MovementSettings _settings;
        private float _vx;
        private float _vy;
        private float _vz;
        private bool _doubleJumpUsed;
        private bool _sliding;

        /// <summary>
        /// 開始時の速さと向き（+Z 方向）で作る。
        /// </summary>
        /// <param name="settings">移動の数値。</param>
        public PlayerMovementModel(MovementSettings settings)
        {
            _settings = settings;
            SetVelocity(settings.StartSpeedMps, 0f, 0f);
        }

        /// <summary>現在の速さ（m/s）。表示の km/h は、この値に 2.4 を掛けて求める。</summary>
        public float SpeedMps => (float)Math.Sqrt(_vx * _vx + _vy * _vy + _vz * _vz);

        /// <summary>進む向きの水平角（rad）。0 は +Z 方向。</summary>
        public float YawRad => (float)Math.Atan2(_vx, _vz);

        /// <summary>現在の速度の X 成分（m/s）。</summary>
        public float VelocityX => _vx;

        /// <summary>現在の速度の Y 成分（m/s）。上がプラス。</summary>
        public float VelocityY => _vy;

        /// <summary>現在の速度の Z 成分（m/s）。</summary>
        public float VelocityZ => _vz;

        /// <summary>
        /// 向きと速さを外から設定する（ワープの着地など）。向きは正規化して使う。
        /// </summary>
        /// <param name="x">向きの X 成分。</param>
        /// <param name="y">向きの Y 成分。</param>
        /// <param name="z">向きの Z 成分。</param>
        /// <param name="speedMps">設定する速さ（m/s）。</param>
        public void SetDirection(float x, float y, float z, float speedMps)
        {
            var horizontal = (float)Math.Sqrt(x * x + z * z);
            if (horizontal <= 0f && y == 0f)
            {
                SetVelocity(speedMps, 0f, 0f);
                return;
            }

            var yaw = (float)Math.Atan2(x, z);
            var pitch = (float)Math.Atan2(y, horizontal);
            SetVelocity(speedMps, yaw, pitch);
        }

        /// <summary>
        /// 向きはそのままで、速さだけを設定する。
        /// </summary>
        /// <param name="speedMps">設定する速さ（m/s）。</param>
        public void SetSpeed(float speedMps)
        {
            SetDirection(_vx, _vy, _vz, speedMps);
        }

        /// <summary>
        /// 1フレーム分の移動を計算して、速度を更新する。
        /// </summary>
        /// <param name="input">このフレームの入力。</param>
        /// <param name="grounded">地面に立っているか。</param>
        /// <param name="dt">経過時間（秒）。</param>
        public void Tick(MovementInput input, bool grounded, float dt)
        {
            if (grounded)
            {
                // 着地したら、二段ジャンプを使えるようにする
                _doubleJumpUsed = false;
                UpdateSliding(input);
                TickGround(input, dt);
            }
            else
            {
                _sliding = false;
                TickAir(input, dt);
            }

            if (input.JumpPressed)
            {
                _sliding = false;
                TryJump(grounded);
            }

            ClampSpeed();
        }

        /// <summary>スライディングの状態を更新する。キーを押している間、または天井の下にいる間は続ける。</summary>
        private void UpdateSliding(MovementInput input)
        {
            var wantSliding = input.SlideHeld || (_sliding && !input.CanStand);
            _sliding = wantSliding && SpeedMps >= _settings.SlideMinSpeedMps;
        }

        /// <summary>スライディング中か（体を低くしている状態）。</summary>
        public bool IsSliding => _sliding;

        /// <summary>
        /// ジャンプを試す。地上なら通常のジャンプ、空中なら一度だけ二段ジャンプができる（速さが足りるとき）。
        /// </summary>
        private void TryJump(bool grounded)
        {
            if (grounded)
            {
                Jump();
                return;
            }

            if (!_doubleJumpUsed && SpeedMps >= _settings.DoubleJumpMinSpeedMps)
            {
                _doubleJumpUsed = true;
                Jump();
            }
        }

        /// <summary>
        /// 速さは変えずに、向きを上へ傾ける。上向きの速さは JumpSpeedMps にする（速さが足りないときは割合の上限まで）。
        /// </summary>
        private void Jump()
        {
            var speed = SpeedMps;
            if (speed <= 0f)
            {
                return;
            }

            var upRatio = Math.Min(JumpMaxUpRatio, _settings.JumpSpeedMps / speed);
            SetVelocity(speed, YawRad, (float)Math.Asin(upRatio));
        }

        /// <summary>
        /// 地上の移動。ブレーキは速さを下げ、スケートは上限まで加速する。
        /// 上限を超えた速さ（ワープなど）は、減速要因がなければ保つ。
        /// </summary>
        private void TickGround(MovementInput input, float dt)
        {
            var speed = SpeedMps;
            if (input.Brake)
            {
                speed = Math.Max(_settings.MinSpeedMps, speed - _settings.BrakeMps2 * dt);
            }
            else if (!_sliding && speed < _settings.CruiseSpeedMps)
            {
                // スライディング中はスケートの加速が止まる（仕様書「スライディング」）
                speed = Math.Min(_settings.CruiseSpeedMps, speed + _settings.AccelMps2 * dt);
            }

            var steerRatio = _sliding ? _settings.SlideSteerRatio : 1f;
            var yaw = SteerTowards(YawRad, TargetYaw(input), _settings.SteerRadPerSec * steerRatio * dt);
            SetVelocity(speed, yaw, 0f);
        }

        /// <summary>
        /// 空中の移動。重力は速さを変えず、向きだけを下へ曲げる（仕様書「重力のルール」）。
        /// 空中の旋回は地上より遅い。
        /// </summary>
        private void TickAir(MovementInput input, float dt)
        {
            var speed = SpeedMps;
            var yaw = SteerTowards(YawRad, TargetYaw(input), _settings.SteerRadPerSec * _settings.AirSteerRatio * dt);

            var vy = _vy - _settings.GravityMps2 * dt;
            var horizontal = (float)Math.Sqrt(_vx * _vx + _vz * _vz);
            var pitch = (float)Math.Atan2(vy, horizontal);
            SetVelocity(speed, yaw, pitch);
        }

        /// <summary>
        /// 進む向きの目標。カメラの向きに、寄せの角度を足す。
        /// </summary>
        private float TargetYaw(MovementInput input)
        {
            return input.CameraYawRad + input.LeanDirection * _settings.LeanRad;
        }

        /// <summary>
        /// 速さ・水平角・上下角から、速度の成分を求めて設定する。
        /// </summary>
        /// <param name="speed">速さ（m/s）。</param>
        /// <param name="yaw">水平角（rad）。0 は +Z 方向。</param>
        /// <param name="pitch">上下角（rad）。上がプラス。</param>
        private void SetVelocity(float speed, float yaw, float pitch)
        {
            var horizontal = speed * (float)Math.Cos(pitch);
            _vx = horizontal * (float)Math.Sin(yaw);
            _vy = speed * (float)Math.Sin(pitch);
            _vz = horizontal * (float)Math.Cos(yaw);
        }

        /// <summary>
        /// 速さが上限を超えていたら、向きを保ったまま上限に揃える。
        /// </summary>
        private void ClampSpeed()
        {
            var speed = SpeedMps;
            if (speed <= _settings.MaxSpeedMps || speed <= 0f)
            {
                return;
            }

            var scale = _settings.MaxSpeedMps / speed;
            _vx *= scale;
            _vy *= scale;
            _vz *= scale;
        }

        /// <summary>
        /// 現在の角度から目標角へ、1フレームに maxStep（rad）までだけ近づける。
        /// </summary>
        /// <param name="current">現在の水平角（rad）。</param>
        /// <param name="target">目標の水平角（rad）。</param>
        /// <param name="maxStep">1フレームに曲がれる最大の角度（rad）。</param>
        /// <returns>次の水平角（rad）。</returns>
        private static float SteerTowards(float current, float target, float maxStep)
        {
            var diff = target - current;
            // −π〜π の範囲に収め、遠回りせず近い方へ曲がる
            diff -= TwoPi * (float)Math.Floor((diff + Pi) / TwoPi);
            var step = Math.Max(-maxStep, Math.Min(maxStep, diff));
            return current + step;
        }
    }
}

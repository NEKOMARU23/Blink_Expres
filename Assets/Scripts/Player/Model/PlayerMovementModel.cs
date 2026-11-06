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
        private WallMode _wallMode;
        private float _wallNormalX;
        private float _wallNormalZ;
        private float _runTimeLeft;
        private bool _climbKickUsed;
        private bool _mushroomLatched;
        private bool _obstacleLatched;
        private bool _ceilingLatched;

        /// <summary>壁の上での状態。</summary>
        private enum WallMode
        {
            None,
            Climb,
            Run,
        }

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
        /// 速さを増やす（コインや粉砕など）。上限を超えない。
        /// </summary>
        /// <param name="deltaMps">増える速さ（m/s）。</param>
        public void AddSpeed(float deltaMps)
        {
            SetSpeed(Math.Min(_settings.MaxSpeedMps, SpeedMps + deltaMps));
        }

        /// <summary>
        /// 敵に弾かれる。速さは半分になり、指定の向き（水平）へ逸れる。
        /// </summary>
        /// <param name="awayX">弾かれる向きの X 成分（水平）。</param>
        /// <param name="awayZ">弾かれる向きの Z 成分（水平）。</param>
        public void Bounce(float awayX, float awayZ)
        {
            var speed = SpeedMps * 0.5f;
            SetDirection(awayX, 0f, awayZ, speed);
        }

        /// <summary>
        /// 1フレーム分の移動を計算して、速度を更新する。
        /// </summary>
        /// <param name="input">このフレームの入力。</param>
        /// <param name="grounded">地面に立っているか。</param>
        /// <param name="dt">経過時間（秒）。</param>
        public void Tick(MovementInput input, bool grounded, float dt)
        {
            // 横の壁から離れたら、壁走りを終える
            if (_wallMode == WallMode.Run && (grounded || !input.Wall.Touching))
            {
                _wallMode = WallMode.None;
            }

            if (_wallMode != WallMode.None)
            {
                TickOnWall(input, dt);
                ClampSpeed();
                return;
            }

            // レールに乗っている間は、レールの向きへ進む（ジャンプで降りる）
            if (input.Hazard.OnRail && !input.JumpPressed)
            {
                TickRail(input, dt);
                ClampSpeed();
                return;
            }

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

            TryStartWall(input, grounded);
            TryMushroom(input.Hazard);
            TryObstacle(input.Hazard);
            TryCeiling(input.Hazard);
            ClampSpeed();
        }

        /// <summary>
        /// レールの上の移動。レールの向きへ進み、速さは毎秒 RailAccelMps2 ずつ増える（上限まで）。
        /// </summary>
        private void TickRail(MovementInput input, float dt)
        {
            var speed = Math.Min(_settings.MaxSpeedMps, SpeedMps + _settings.RailAccelMps2 * dt);
            _sliding = false;
            _doubleJumpUsed = false;
            var yaw = (float)Math.Atan2(input.Hazard.RailDirX, input.Hazard.RailDirZ);
            SetVelocity(speed, yaw, 0f);
        }

        /// <summary>
        /// 跳ねるキノコを踏んだ瞬間に跳ねる。速さは保ち、上向きの速さは min(速さ × 0.9, 24) m/s。
        /// 二段ジャンプも戻る。踏み続けている間は一度だけ。
        /// </summary>
        private void TryMushroom(HazardContact hazard)
        {
            if (!hazard.MushroomNear)
            {
                _mushroomLatched = false;
                return;
            }

            if (_mushroomLatched)
            {
                return;
            }

            _mushroomLatched = true;
            var speed = SpeedMps;
            if (speed <= 0f)
            {
                return;
            }

            var bounce = Math.Min(speed * _settings.MushroomBounceSpeedRatio, _settings.MushroomBounceMaxMps);
            var upRatio = Math.Min(_settings.MushroomMaxUpRatio, bounce / speed);
            _sliding = false;
            _doubleJumpUsed = false;
            SetVelocity(speed, YawRad, (float)Math.Asin(upRatio));
        }

        /// <summary>
        /// 登れない障害物に正面から当たったとき、速さが減る。当たっている間は一度だけ。
        /// </summary>
        private void TryObstacle(HazardContact hazard)
        {
            if (hazard.ObstacleIntoDot < _settings.ObstacleMinIntoDot)
            {
                _obstacleLatched = false;
                return;
            }

            if (_obstacleLatched)
            {
                return;
            }

            _obstacleLatched = true;
            var scale = 1f - _settings.ObstacleSlowFactor * hazard.ObstacleIntoDot;
            SetSpeed(Math.Max(_settings.MinSpeedMps, SpeedMps * scale));
        }

        /// <summary>
        /// 天井に当たると、速さが減って下へ落ちる。当たっている間は一度だけ。
        /// </summary>
        private void TryCeiling(HazardContact hazard)
        {
            if (!hazard.CeilingHit)
            {
                _ceilingLatched = false;
                return;
            }

            if (_ceilingLatched)
            {
                return;
            }

            _ceilingLatched = true;
            var speed = Math.Max(_settings.MinSpeedMps, SpeedMps * _settings.CeilingSpeedRatio);
            SetDirection(_vx * 0.35f, -1f, _vz * 0.35f, speed);
        }

        /// <summary>壁のぼり中か（前から壁を登っている）。</summary>
        public bool IsClimbing => _wallMode == WallMode.Climb;

        /// <summary>壁走り中か（横から壁に沿って走っている）。</summary>
        public bool IsRunning => _wallMode == WallMode.Run;

        /// <summary>
        /// 壁に触れていれば、前からなら壁のぼり、横からで空中なら壁走りを始める。
        /// </summary>
        private void TryStartWall(MovementInput input, bool grounded)
        {
            if (!input.Wall.Touching)
            {
                return;
            }

            var horizontal = (float)Math.Sqrt(_vx * _vx + _vz * _vz);
            var facing = horizontal > 0f ? (-(_vx * input.Wall.NormalX + _vz * input.Wall.NormalZ) / horizontal) : 0f;
            if (facing > _settings.FrontDot)
            {
                StartClimb();
                return;
            }

            if (!grounded && input.Wall.HeightAboveGroundM >= _settings.WallRunMinHeightM)
            {
                StartRun(input.Wall);
            }
        }

        /// <summary>壁のぼりを始める。上昇速度は今の速さから始まる（仕様書「壁」の節）。</summary>
        private void StartClimb()
        {
            // 水平の成分を消す前に、今の速さを取っておく
            var speed = SpeedMps;
            _wallMode = WallMode.Climb;
            _climbKickUsed = false;
            _doubleJumpUsed = false;
            _sliding = false;
            _vx = 0f;
            _vz = 0f;
            _vy = speed;
        }

        /// <summary>壁走りを始める。速さは保ったまま、壁に沿った向きにする。</summary>
        private void StartRun(WallContact wall)
        {
            _wallMode = WallMode.Run;
            _wallNormalX = wall.NormalX;
            _wallNormalZ = wall.NormalZ;
            _runTimeLeft = _settings.WallRunTimeSec;
            _doubleJumpUsed = false;
            _sliding = false;
            AlongWall(SpeedMps);
        }

        /// <summary>
        /// 壁の法線に沿った成分を消し、向きだけを壁に沿わせる。速さは指定の値にする。
        /// </summary>
        /// <param name="targetSpeedMps">保つ速さ（m/s）。</param>
        private void AlongWall(float targetSpeedMps)
        {
            var along = _vx * _wallNormalX + _vz * _wallNormalZ;
            _vx -= along * _wallNormalX;
            _vz -= along * _wallNormalZ;

            var projected = SpeedMps;
            if (projected > 0f)
            {
                var scale = targetSpeedMps / projected;
                _vx *= scale;
                _vy *= scale;
                _vz *= scale;
            }
        }

        /// <summary>
        /// 壁の上での移動。ジャンプは壁ジャンプになる。
        /// </summary>
        private void TickOnWall(MovementInput input, float dt)
        {
            if (input.JumpPressed)
            {
                WallJump(input);
                return;
            }

            if (_wallMode == WallMode.Climb)
            {
                TickClimb(input, dt);
                return;
            }

            TickRun(dt);
        }

        /// <summary>
        /// 壁のぼり。真上へ上がり、上昇速度が 0 になると下りる。上端に着いたら乗り越える。
        /// </summary>
        private void TickClimb(MovementInput input, float dt)
        {
            _vy -= _settings.ClimbDecelMps2 * dt;
            _vx = 0f;
            _vz = 0f;

            if (input.Wall.TopReached)
            {
                LeaveWall(0.2f, Math.Max(SpeedMps, _settings.MinSpeedMps), input);
                return;
            }

            if (_vy <= 0f)
            {
                LeaveWall(-0.3f, _settings.MinSpeedMps, input);
            }
        }

        /// <summary>
        /// 壁走り。まっすぐ走る時間は重力がなく、その後は重力で下へ曲がる。速さは保つ。
        /// </summary>
        private void TickRun(float dt)
        {
            var speed = SpeedMps;
            if (_runTimeLeft > 0f)
            {
                _runTimeLeft -= dt;
            }
            else
            {
                _vy -= _settings.WallGravityMps2 * dt;
            }

            AlongWall(speed);
        }

        /// <summary>
        /// 壁から離れる。カメラの向きへ、指定の上下の傾きで進む。
        /// </summary>
        /// <param name="pitchRatio">上下の割合（プラスで上、マイナスで下）。</param>
        /// <param name="speedMps">離れた後の速さ（m/s）。</param>
        /// <param name="input">このフレームの入力（カメラの向きを使う）。</param>
        private void LeaveWall(float pitchRatio, float speedMps, MovementInput input)
        {
            _wallMode = WallMode.None;
            _sliding = false;
            SetDirection((float)Math.Sin(input.CameraYawRad), pitchRatio, (float)Math.Cos(input.CameraYawRad), speedMps);
        }

        /// <summary>
        /// 壁ジャンプ。壁のぼり中は上昇速度が一度だけ上がり、壁走り中は壁から離れる向きへ跳ぶ。
        /// 離れたあとは二段ジャンプが使える。
        /// </summary>
        private void WallJump(MovementInput input)
        {
            if (_wallMode == WallMode.Climb)
            {
                if (!_climbKickUsed)
                {
                    _climbKickUsed = true;
                    _vy += _settings.ClimbKickMps;
                }

                return;
            }

            var speed = SpeedMps;
            var horizontal = (float)Math.Sqrt(_vx * _vx + _vz * _vz);
            var forwardX = horizontal > 0f ? _vx / horizontal : 0f;
            var forwardZ = horizontal > 0f ? _vz / horizontal : 0f;

            var x = _wallNormalX * _settings.WallJumpNormalRatio + forwardX * _settings.WallJumpForwardRatio;
            var y = _settings.WallJumpUpRatio;
            var z = _wallNormalZ * _settings.WallJumpNormalRatio + forwardZ * _settings.WallJumpForwardRatio;

            _wallMode = WallMode.None;
            _doubleJumpUsed = false;
            SetDirection(x, y, z, speed);
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
            else if (input.Hazard.OnMud)
            {
                // 泥の上では、スケートの加速が打ち消されて、毎秒 16 m/s 減る
                speed = Math.Max(_settings.MinSpeedMps, speed - _settings.MudDecelMps2 * dt);
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

using BlinkExpress.Beacon.Model;
using BlinkExpress.Beacon.View;
using BlinkExpress.Enemy.Model;
using BlinkExpress.Enemy.View;
using BlinkExpress.Player.Model;
using BlinkExpress.Player.Presenter;
using BlinkExpress.Stage.View;
using BlinkExpress.Tuning;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlinkExpress.Player.View
{
    /// <summary>
    /// プレイヤーの View。CharacterController で体を動かし、キーボードの入力を読んで Presenter に渡す。
    /// 移動の判断はしない。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerView : MonoBehaviour, IPlayerView
    {
        /// <summary>天井にワープしたとき、体を天井からどれだけ下げて置くか（m）。</summary>
        private const float CeilingDropM = 2.1f;

        /// <summary>立っているときの体の高さ（m）。</summary>
        private const float StandHeightM = 2f;

        /// <summary>登れる壁の高さの下限（m）。仕様書「壁」の節：高さ 2.5 m より高い。</summary>
        private const float ClimbableWallHeightM = 2.5f;

        /// <summary>壁から離れたあと、同じ壁に再び張り付かない時間（秒）。</summary>
        private const float NoStickSec = 1.2f;

        /// <summary>スライディング中の体の高さ（m）。仕様書「スライディング」：1.8 m → 0.9 m。</summary>
        private const float SlideHeightM = 0.9f;

        [SerializeField] private PlayerTuning _tuning;
        [SerializeField] private ChaseCameraView _chaseCamera;
        [SerializeField] private BeaconView _beacon;

        private CharacterController _controller;
        private PlayerPresenter _presenter;

        // 直前の移動で当たった壁の情報（次のフレームの入力になる）
        private bool _wallTouching;
        private float _wallNormalX;
        private float _wallNormalZ;
        private float _wallTopY;

        // 壁から離れた直後は、同じ壁に張り付かないようにする
        private float _ignoreWallUntilSec;
        private bool _wasOnWall;

        // 仕掛け：登れない障害物と天井は、移動の中の衝突で記録し、次のフレームに渡す
        private float _obstacleIntoDot;
        private bool _ceilingHit;
        private GrindRail[] _grindRails;
        private bool _onRail;
        private Vector3 _railSnapPoint;
        private float _railBlockedUntilSec;

        /// <summary>レールから降りたあと、同じレールに乗り直さない時間（秒）。</summary>
        private const float RailRelockSec = 0.4f;

        /// <summary>敵を探す範囲（m）。最も大きい敵の半径と、接触の余裕を足した値より大きくする。</summary>
        private const float MaxEnemyReachM = 6f;

        // 敵に当たった直後は、少しの間は当たらない
        private float _invincibleUntilSec;

        /// <summary>地面に立っているか（CharacterController の判定）。</summary>
        public bool IsGrounded => _controller.isGrounded;

        /// <summary>プレイヤーの現在の位置（m）。</summary>
        public Vector3 Position => transform.position;

        /// <summary>プレイヤーの現在の速さ（m/s）。</summary>
        public float SpeedMps => _presenter != null ? _presenter.SpeedMps : 0f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            LiftBodyMesh();

            // 無いと移動もカメラも動かないため、ここで止めて原因を出す
            if (_tuning == null || _chaseCamera == null)
            {
                Debug.LogError($"{name}: PlayerTuning か ChaseCameraView が設定されていません。", this);
                enabled = false;
                return;
            }

            _presenter = new PlayerPresenter(this, _tuning.ToMovementSettings());
            _grindRails = FindObjectsByType<GrindRail>(FindObjectsSortMode.None);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var lean = (keyboard.aKey.isPressed ? 1f : 0f) - (keyboard.dKey.isPressed ? 1f : 0f);
            var slideHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed || keyboard.cKey.isPressed;
            var jumpPressed = keyboard.spaceKey.wasPressedThisFrame;

            // レールの上でジャンプしたら、少しの間は同じレールに乗り直さない
            if (_onRail && jumpPressed)
            {
                _railBlockedUntilSec = Time.time + RailRelockSec;
            }

            var hazard = ReadHazardContact();
            var input = new MovementInput(
                keyboard.sKey.isPressed,
                _chaseCamera.YawRad,
                lean,
                jumpPressed,
                slideHeld,
                HeadClear(),
                ReadWallContact(),
                hazard);

            // レールの上では、レールの線の上に体を置いてから動かす
            if (_onRail)
            {
                Teleport(_railSnapPoint);
            }

            // 照準中はゲーム内時間が遅くなるので、プレイヤーの移動もそれに合わせる
            var worldDt = Time.deltaTime * (_beacon != null ? _beacon.WorldTimeScale : 1f);
            _presenter.Tick(input, worldDt);

            ApplyBodyHeight();
            UpdateNoStick();
            ApplyCoinsAndEnemies();
        }

        /// <summary>
        /// 体の中心に近いコインを取り、敵に当たったら粉砕するか弾かれる。
        /// </summary>
        private void ApplyCoinsAndEnemies()
        {
            var body = transform.position + Vector3.up * (_presenter.IsSliding ? 0.5f : 1f);

            var coins = Physics.OverlapSphere(body, CoinRules.PickupRangeM, ~0, QueryTriggerInteraction.Collide);
            foreach (var collider in coins)
            {
                if (collider.TryGetComponent(out CoinView coin) && coin.TryCollect())
                {
                    _presenter.AddSpeed(CoinRules.SpeedGainMps);
                    if (_beacon != null)
                    {
                        _beacon.RefillGauge(CoinRules.GaugeGain);
                    }
                }
            }

            if (Time.time < _invincibleUntilSec)
            {
                return;
            }

            var enemies = Physics.OverlapSphere(body, MaxEnemyReachM, ~0, QueryTriggerInteraction.Collide);
            foreach (var collider in enemies)
            {
                if (!collider.TryGetComponent(out EnemyView enemy) || !enemy.IsAlive)
                {
                    continue;
                }

                var center = enemy.Center;
                if (Vector3.Distance(body, center) >= enemy.RadiusM + EnemyRules.ContactMarginM)
                {
                    continue;
                }

                _invincibleUntilSec = Time.time + EnemyRules.InvincibleSec;
                ApplyEnemyContact(enemy, body, center);
                return;
            }
        }

        /// <summary>
        /// 敵に触れたときの結果。速さが足りれば粉砕して加速し、足りなければ弾かれる。
        /// </summary>
        private void ApplyEnemyContact(EnemyView enemy, Vector3 body, Vector3 center)
        {
            if (EnemyRules.CanSmash(SpeedMps, enemy.Tier))
            {
                enemy.Smash();
                _presenter.AddSpeed(EnemyRules.SpeedGainMps(enemy.Tier));
                if (_beacon != null)
                {
                    _beacon.RefillGauge(EnemyRules.GaugeGain(enemy.Tier));
                }

                if (StageProgress.Instance != null)
                {
                    StageProgress.Instance.AddCoins(EnemyRules.CoinCount(enemy.Tier));
                }

                return;
            }

            var away = new Vector3(body.x - center.x, 0f, body.z - center.z);
            if (away.sqrMagnitude < 1e-4f)
            {
                away = Vector3.back;
            }

            away.Normalize();
            _presenter.Bounce(away.x, away.z);
        }

        /// <summary>
        /// 直前の移動で当たった壁から、壁の接触の情報を作る。壁から離れた直後は無視する。
        /// </summary>
        private WallContact ReadWallContact()
        {
            var touching = _wallTouching && Time.time >= _ignoreWallUntilSec;
            _wallTouching = false;
            if (!touching)
            {
                return default;
            }

            var topReached = transform.position.y >= _wallTopY - 0.5f;
            return new WallContact(true, _wallNormalX, _wallNormalZ, topReached, HeightAboveGroundM());
        }

        /// <summary>
        /// 壁のぼりや壁走りを終えたとき、その直後は同じ壁に張り付かないように時間を置く。
        /// </summary>
        private void UpdateNoStick()
        {
            var onWall = _presenter.IsClimbing || _presenter.IsRunning;
            if (_wasOnWall && !onWall)
            {
                _ignoreWallUntilSec = Time.time + NoStickSec;
            }

            _wasOnWall = onWall;
        }

        /// <summary>
        /// 体が当たったときに呼ばれる（CharacterController の移動の中）。
        /// 天井、登れる壁、登れない障害物（紫の壁や倒木）のどれかを記録する。
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < -0.5f)
            {
                _ceilingHit = true;
                return;
            }

            // 地面は壁ではない
            if (hit.normal.y > 0.5f)
            {
                return;
            }

            var bounds = hit.collider.bounds;
            var noWarp = hit.collider.TryGetComponent(out WarpSurface surface) && surface.Kind == HitKind.NoWarpSurface;
            var climbable = !noWarp && bounds.size.y > ClimbableWallHeightM && transform.position.y < bounds.max.y - 1f;
            if (climbable)
            {
                _wallTouching = true;
                _wallNormalX = hit.normal.x;
                _wallNormalZ = hit.normal.z;
                _wallTopY = bounds.max.y;
                return;
            }

            // 登れない障害物に正面から当たったときは、進む向きと面の内積を記録する
            var yaw = _presenter.YawRad;
            var forward = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var into = -Vector3.Dot(forward, new Vector3(hit.normal.x, 0f, hit.normal.z));
            if (into > _obstacleIntoDot)
            {
                _obstacleIntoDot = into;
            }
        }

        /// <summary>
        /// 仕掛けに触れている状態を作る。障害物と天井の記録は、読んだ後に消す。
        /// </summary>
        private HazardContact ReadHazardContact()
        {
            var feet = transform.position;
            var onMud = TouchesZone(feet + Vector3.up * 0.2f, 0.3f, HazardZone.Kind.Mud);
            var nearMushroom = TouchesZone(feet + Vector3.up * 0.6f, 0.5f, HazardZone.Kind.Mushroom);
            var onRail = FindRail(out var railDirX, out var railDirZ);
            _onRail = onRail;

            var obstacle = _obstacleIntoDot;
            var ceiling = _ceilingHit;
            _obstacleIntoDot = 0f;
            _ceilingHit = false;

            return new HazardContact(onMud, nearMushroom, onRail, railDirX, railDirZ, obstacle, ceiling);
        }

        /// <summary>
        /// 指定の範囲に、指定の種類の仕掛けが重なっているか。
        /// </summary>
        private bool TouchesZone(Vector3 center, float radius, HazardZone.Kind kind)
        {
            var colliders = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
            foreach (var collider in colliders)
            {
                if (collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (collider.TryGetComponent(out HazardZone zone) && zone.KindOfZone == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 体がレールの近くにあるか。近ければ、レールに沿った進む向きと、レールの上の点を記録する。
        /// </summary>
        private bool FindRail(out float dirX, out float dirZ)
        {
            dirX = 0f;
            dirZ = 0f;
            if (Time.time < _railBlockedUntilSec)
            {
                return false;
            }

            var p = transform.position;
            foreach (var rail in _grindRails)
            {
                var a = rail.StartPoint;
                var d = rail.EndPoint - a;
                var length2 = d.sqrMagnitude;
                if (length2 < 1e-4f)
                {
                    continue;
                }

                var t = Mathf.Clamp01(Vector3.Dot(p - a, d) / length2);
                var q = a + d * t;
                var horizontal = new Vector2(q.x - p.x, q.z - p.z).magnitude;
                var dy = p.y - q.y;
                if (horizontal > 1.1f || dy < -1f || dy > 1.4f)
                {
                    continue;
                }

                // 乗ったときの向きに近い側へ進む
                var heading = new Vector3(d.x, 0f, d.z).normalized;
                var yaw = _presenter.YawRad;
                var forward = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                if (Vector3.Dot(heading, forward) < 0f)
                {
                    heading = -heading;
                }

                dirX = heading.x;
                dirZ = heading.z;
                _railSnapPoint = q;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 体の真下の地面までの高さ（m）。地面が見つからなければ大きな値を返す。
        /// </summary>
        private float HeightAboveGroundM()
        {
            var origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                return transform.position.y - hit.point.y;
            }

            return 100f;
        }

        /// <summary>
        /// スライディング中は体を低くする。立っているときは元の高さに戻す。
        /// </summary>
        private void ApplyBodyHeight()
        {
            var height = _presenter.IsSliding ? SlideHeightM : StandHeightM;
            _controller.height = height;
            _controller.center = new Vector3(0f, height / 2f, 0f);
        }

        /// <summary>
        /// 体の見た目（メッシュ）を、足元の上へ持ち上げる。
        /// 見た目の中心は足元にあるため、そのままだと下半分が地面に埋まって見える。
        /// 見た目は子オブジェクトに移し、当たり判定（CharacterController）は今のまま足元を基準にする。
        /// </summary>
        private void LiftBodyMesh()
        {
            var ownFilter = GetComponent<MeshFilter>();
            var ownRenderer = GetComponent<MeshRenderer>();
            if (ownFilter == null || ownRenderer == null)
            {
                return;
            }

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, StandHeightM / 2f, 0f);
            body.AddComponent<MeshFilter>().sharedMesh = ownFilter.sharedMesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = ownRenderer.sharedMaterial;
            ownRenderer.enabled = false;
        }

        /// <summary>
        /// 頭の上に天井が無く、立ち上がれるかを調べる。自分自身は無視する。
        /// </summary>
        private bool HeadClear()
        {
            var origin = transform.position + Vector3.up * SlideHeightM;
            var length = StandHeightM - SlideHeightM;
            var hits = Physics.RaycastAll(origin, Vector3.up, length, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// ワープで壁に着弾したとき、壁から体の半径だけ離して張り付け、壁滑走を始める。
        /// </summary>
        /// <param name="position">着弾の位置（m）。</param>
        /// <param name="normal">壁の法線（壁から体へ向かう向き）。</param>
        /// <param name="direction">壁に沿った、着弾後の進む向き。</param>
        /// <param name="speedMps">保つ速さ（m/s）。</param>
        public void StartWallSlide(Vector3 position, Vector3 normal, Vector3 direction, float speedMps)
        {
            if (_presenter == null)
            {
                return;
            }

            var target = position + normal.normalized * 0.6f;
            _presenter.StartWallSlide(target, normal, direction, speedMps);
        }

        /// <summary>
        /// ワープで着地する。天井の場合は、体を天井から少し下げて置く。
        /// </summary>
        /// <param name="position">着地の位置（m）。</param>
        /// <param name="direction">着地後の進む向き。</param>
        /// <param name="speedMps">着地後の速さ（m/s）。</param>
        /// <param name="outcome">ワープの結果。</param>
        public void WarpTo(Vector3 position, Vector3 direction, float speedMps, WarpOutcome outcome)
        {
            if (_presenter == null)
            {
                return;
            }

            var target = outcome == WarpOutcome.Ceiling ? position + Vector3.down * CeilingDropM : position;
            _presenter.Warp(target, direction, speedMps);
        }

        /// <summary>
        /// 速さだけを設定する（向きは変えない）。
        /// </summary>
        /// <param name="speedMps">設定する速さ（m/s）。</param>
        public void SetSpeed(float speedMps)
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.SetSpeed(speedMps);
        }

        /// <summary>
        /// CharacterController を一度止めてから位置を変える（移動のずれを防ぐため）。
        /// </summary>
        /// <param name="position">移動先の位置（m）。</param>
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }

        /// <summary>進む向きの水平角（rad）。0 は +Z 方向。</summary>
        public float YawRad => _presenter != null ? _presenter.YawRad : 0f;

        /// <summary>
        /// 走りを最初からやり直す。速さと向きを開始時の値に戻し、指定の位置に置く。
        /// </summary>
        /// <param name="position">開始の位置（m）。</param>
        public void ResetToStart(Vector3 position)
        {
            if (_tuning == null)
            {
                return;
            }

            _presenter = new PlayerPresenter(this, _tuning.ToMovementSettings());
            Teleport(position);
        }

        /// <summary>
        /// CharacterController で体を動かす。
        /// </summary>
        /// <param name="dx">X 方向の移動量（m）。</param>
        /// <param name="dy">Y 方向の移動量（m）。</param>
        /// <param name="dz">Z 方向の移動量（m）。</param>
        public void Move(float dx, float dy, float dz)
        {
            _controller.Move(new Vector3(dx, dy, dz));
        }

        /// <summary>
        /// 体の向きを水平角に合わせる。
        /// </summary>
        /// <param name="yawRad">水平角（rad）。0 は +Z 方向。</param>
        public void Face(float yawRad)
        {
            transform.rotation = Quaternion.Euler(0f, yawRad * Mathf.Rad2Deg, 0f);
        }

        /// <summary>
        /// 速さの割合をカメラに渡し、速いほど引きを縮める。
        /// </summary>
        /// <param name="speedMps">速さ（m/s）。</param>
        public void ShowSpeed(float speedMps)
        {
            _chaseCamera.SetSpeedRatio(speedMps / _tuning.MaxSpeedMps);
        }
    }
}

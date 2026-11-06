using BlinkExpress.Beacon.Model;
using BlinkExpress.Beacon.View;
using BlinkExpress.Player.Model;
using BlinkExpress.Player.Presenter;
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

        /// <summary>地面に立っているか（CharacterController の判定）。</summary>
        public bool IsGrounded => _controller.isGrounded;

        /// <summary>プレイヤーの現在の位置（m）。</summary>
        public Vector3 Position => transform.position;

        /// <summary>プレイヤーの現在の速さ（m/s）。</summary>
        public float SpeedMps => _presenter != null ? _presenter.SpeedMps : 0f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // 無いと移動もカメラも動かないため、ここで止めて原因を出す
            if (_tuning == null || _chaseCamera == null)
            {
                Debug.LogError($"{name}: PlayerTuning か ChaseCameraView が設定されていません。", this);
                enabled = false;
                return;
            }

            _presenter = new PlayerPresenter(this, _tuning.ToMovementSettings());
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
            var input = new MovementInput(
                keyboard.sKey.isPressed,
                _chaseCamera.YawRad,
                lean,
                keyboard.spaceKey.wasPressedThisFrame,
                slideHeld,
                HeadClear(),
                ReadWallContact());

            // 照準中はゲーム内時間が遅くなるので、プレイヤーの移動もそれに合わせる
            var worldDt = Time.deltaTime * (_beacon != null ? _beacon.WorldTimeScale : 1f);
            _presenter.Tick(input, worldDt);

            ApplyBodyHeight();
            UpdateNoStick();
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
        /// 体が当たったときに呼ばれる（CharacterController の移動の中）。登れる壁なら記録する。
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // 地面と天井は壁ではない
            if (hit.normal.y > 0.5f || hit.normal.y < -0.5f)
            {
                return;
            }

            var bounds = hit.collider.bounds;
            var climbable = bounds.size.y > ClimbableWallHeightM && transform.position.y < bounds.max.y - 1f;
            if (!climbable)
            {
                return;
            }

            _wallTouching = true;
            _wallNormalX = hit.normal.x;
            _wallNormalZ = hit.normal.z;
            _wallTopY = bounds.max.y;
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

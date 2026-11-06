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

        [SerializeField] private PlayerTuning _tuning;
        [SerializeField] private ChaseCameraView _chaseCamera;
        [SerializeField] private BeaconView _beacon;

        private CharacterController _controller;
        private PlayerPresenter _presenter;

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
            var input = new MovementInput(
                keyboard.sKey.isPressed,
                _chaseCamera.YawRad,
                lean,
                keyboard.spaceKey.wasPressedThisFrame);

            // 照準中はゲーム内時間が遅くなるので、プレイヤーの移動もそれに合わせる
            var worldDt = Time.deltaTime * (_beacon != null ? _beacon.WorldTimeScale : 1f);
            _presenter.Tick(input, worldDt);
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

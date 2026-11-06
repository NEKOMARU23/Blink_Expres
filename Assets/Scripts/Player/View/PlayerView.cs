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
        [SerializeField] private PlayerTuning _tuning;
        [SerializeField] private ChaseCameraView _chaseCamera;

        private CharacterController _controller;
        private PlayerPresenter _presenter;

        /// <summary>地面に立っているか（CharacterController の判定）。</summary>
        public bool IsGrounded => _controller.isGrounded;

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
            var input = new MovementInput(keyboard.sKey.isPressed, _chaseCamera.YawRad, lean);
            _presenter.Tick(input, Time.deltaTime);
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

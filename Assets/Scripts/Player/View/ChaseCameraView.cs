using UnityEngine;
using UnityEngine.InputSystem;

namespace BlinkExpress.Player.View
{
    /// <summary>
    /// プレイヤーの後ろから追うカメラ。マウスで向きを変え、その向きへプレイヤーが曲がる。
    /// 速さに応じて引きが変わる（仕様書「カメラ」の節）。
    /// </summary>
    public sealed class ChaseCameraView : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _mouseRadPerPixel = 0.0026f;
        [SerializeField] private float _minPitchRad = -0.6f;
        [SerializeField] private float _maxPitchRad = 0.55f;
        [SerializeField] private float _heightM = 2.2f;
        [SerializeField] private float _baseDistanceM = 8.5f;
        [SerializeField] private float _distanceShrinkM = 2f;

        private float _yawRad;
        private float _pitchRad;
        private float _speedRatio;

        /// <summary>カメラの水平角（rad）。0 は +Z 方向。プレイヤーの進む向きはここへ曲がる。</summary>
        public float YawRad => _yawRad;

        /// <summary>
        /// 速さの割合（0〜1）を受け取り、カメラの引きに反映する。
        /// </summary>
        /// <param name="ratio">現在の速さ ÷ 速さの上限。</param>
        public void SetSpeedRatio(float ratio)
        {
            _speedRatio = Mathf.Clamp01(ratio);
        }

        private void Awake()
        {
            if (_target == null)
            {
                Debug.LogError($"{name}: 追う対象（_target）が設定されていません。", this);
                enabled = false;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var delta = mouse.delta.ReadValue();
            _yawRad -= delta.x * _mouseRadPerPixel;
            _pitchRad = Mathf.Clamp(_pitchRad + delta.y * _mouseRadPerPixel, _minPitchRad, _maxPitchRad);
        }

        private void LateUpdate()
        {
            var cos = Mathf.Cos(_pitchRad);
            var forward = new Vector3(Mathf.Sin(_yawRad) * cos, Mathf.Sin(_pitchRad), Mathf.Cos(_yawRad) * cos);

            var focus = _target.position + Vector3.up * _heightM;
            var distance = _baseDistanceM - _distanceShrinkM * _speedRatio;
            transform.position = focus - forward * distance;
            transform.LookAt(focus + forward * 10f);
        }
    }
}

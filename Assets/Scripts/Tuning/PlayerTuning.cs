using BlinkExpress.Player.Model;
using UnityEngine;

namespace BlinkExpress.Tuning
{
    /// <summary>
    /// プレイヤーの移動の数値。仕様書「速度」「操作」「ジャンプと壁」の値を初期値にしている。
    /// インスペクターで調整する。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerTuning", menuName = "BlinkExpress/Tuning/Player")]
    public sealed class PlayerTuning : ScriptableObject
    {
        [Header("速度（m/s）")]
        [SerializeField] private float _minSpeedMps = 8f;
        [SerializeField] private float _cruiseSpeedMps = 30f;
        [SerializeField] private float _maxSpeedMps = 60f;
        [SerializeField] private float _startSpeedMps = 14f;
        [SerializeField] private float _accelMps2 = 3f;
        [SerializeField] private float _brakeMps2 = 28f;

        [Header("操作")]
        [SerializeField] private float _steerRadPerSec = 5f;
        [SerializeField] private float _leanRad = 0.6f;

        [Header("空中")]
        [SerializeField] private float _gravityMps2 = 26f;
        [SerializeField] private float _airSteerRatio = 0.35f;

        [Header("ジャンプ")]
        [SerializeField] private float _jumpSpeedMps = 12.5f;
        [SerializeField] private float _doubleJumpMinSpeedMps = 40f;

        /// <summary>速さの上限（m/s）。カメラの引きを求めるときの基準に使う。</summary>
        public float MaxSpeedMps => _maxSpeedMps;

        /// <summary>
        /// この設定を Model が使う形に変えて返す。
        /// </summary>
        /// <returns>移動の数値。</returns>
        public MovementSettings ToMovementSettings()
        {
            return new MovementSettings
            {
                MinSpeedMps = _minSpeedMps,
                CruiseSpeedMps = _cruiseSpeedMps,
                MaxSpeedMps = _maxSpeedMps,
                StartSpeedMps = _startSpeedMps,
                AccelMps2 = _accelMps2,
                BrakeMps2 = _brakeMps2,
                SteerRadPerSec = _steerRadPerSec,
                LeanRad = _leanRad,
                GravityMps2 = _gravityMps2,
                AirSteerRatio = _airSteerRatio,
                JumpSpeedMps = _jumpSpeedMps,
                DoubleJumpMinSpeedMps = _doubleJumpMinSpeedMps,
            };
        }
    }
}

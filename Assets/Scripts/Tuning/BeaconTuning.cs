using BlinkExpress.Beacon.Model;
using UnityEngine;

namespace BlinkExpress.Tuning
{
    /// <summary>
    /// ビーコンとゲージの数値。仕様書「ワープ」「照準とゲージ」の値を初期値にしている。
    /// インスペクターで調整する。
    /// </summary>
    [CreateAssetMenu(fileName = "BeaconTuning", menuName = "BlinkExpress/Tuning/Beacon")]
    public sealed class BeaconTuning : ScriptableObject
    {
        [Header("弾道（m、m/s、秒）")]
        [SerializeField] private float _baseSpeedMps = 70f;
        [SerializeField] private float _gravityMps2 = 45f;
        [SerializeField] private float _lifetimeSec = 2f;
        [SerializeField] private float _cooldownSec = 0.35f;
        [SerializeField] private float _throwOriginHeightM = 1.6f;

        [Header("ワープ")]
        [SerializeField] private float _warpBoostMps = 3f;
        [SerializeField] private float _chainBoostMps = 3f;

        [Header("照準とゲージ（0〜1）")]
        [SerializeField] private float _slowFactor = 0.25f;
        [SerializeField] private float _gaugeStartValue = 0.7f;
        [SerializeField] private float _aimCost = 0.08f;
        [SerializeField] private float _aimDrainPerSec = 0.15f;
        [SerializeField] private float _aimRegenPerSec = 0.05f;

        /// <summary>
        /// この設定を Model が使う形に変えて返す。
        /// </summary>
        /// <returns>ビーコンの数値。</returns>
        public BeaconSettings ToBeaconSettings()
        {
            return new BeaconSettings
            {
                BaseSpeedMps = _baseSpeedMps,
                GravityMps2 = _gravityMps2,
                LifetimeSec = _lifetimeSec,
                CooldownSec = _cooldownSec,
                ThrowOriginHeightM = _throwOriginHeightM,
                WarpBoostMps = _warpBoostMps,
                ChainBoostMps = _chainBoostMps,
                SlowFactor = _slowFactor,
                GaugeStartValue = _gaugeStartValue,
                AimCost = _aimCost,
                AimDrainPerSec = _aimDrainPerSec,
                AimRegenPerSec = _aimRegenPerSec,
            };
        }
    }
}

using UnityEngine;

namespace BlinkExpress.Beacon.Presenter
{
    /// <summary>
    /// ビーコンの計算に必要な、プレイヤーの現在の状態。
    /// </summary>
    public readonly struct BeaconPlayerState
    {
        /// <summary>
        /// 状態を作る。
        /// </summary>
        /// <param name="position">プレイヤーの位置（m）。</param>
        /// <param name="speedMps">プレイヤーの速さ（m/s）。</param>
        public BeaconPlayerState(Vector3 position, float speedMps)
        {
            Position = position;
            SpeedMps = speedMps;
        }

        /// <summary>プレイヤーの位置（m）。</summary>
        public Vector3 Position { get; }

        /// <summary>プレイヤーの速さ（m/s）。</summary>
        public float SpeedMps { get; }
    }
}

using BlinkExpress.Beacon.Model;
using UnityEngine;

namespace BlinkExpress.Beacon.Presenter
{
    /// <summary>
    /// ビーコンが当たった結果。View が Raycast の結果を、この形に変えて Presenter に渡す。
    /// </summary>
    public readonly struct BeaconHit
    {
        /// <summary>
        /// 当たりの結果を作る。
        /// </summary>
        /// <param name="kind">当たったものの種類。</param>
        /// <param name="point">当たった位置（m）。</param>
        /// <param name="normal">当たった面の法線。</param>
        /// <param name="enemyRequiredSpeedMps">敵の場合、粉砕に必要な速さ（m/s）。敵以外は 0。</param>
        public BeaconHit(HitKind kind, Vector3 point, Vector3 normal, float enemyRequiredSpeedMps)
        {
            Kind = kind;
            Point = point;
            Normal = normal;
            EnemyRequiredSpeedMps = enemyRequiredSpeedMps;
        }

        /// <summary>当たったものの種類。</summary>
        public HitKind Kind { get; }

        /// <summary>当たった位置（m）。</summary>
        public Vector3 Point { get; }

        /// <summary>当たった面の法線。</summary>
        public Vector3 Normal { get; }

        /// <summary>敵の場合、粉砕に必要な速さ（m/s）。</summary>
        public float EnemyRequiredSpeedMps { get; }
    }
}

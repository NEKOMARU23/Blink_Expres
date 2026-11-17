using UnityEngine;

namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// ビーコンがグラインドレールに「当たった」かの判定（仕様書「着弾したものごとの結果」の節）。
    /// レールは細い線なので、Raycast では当てにくい。そのため、ビーコンの位置がレールの線分から
    /// <see cref="CatchRadiusM"/> 以内に入ったら、レールに着弾したことにする（デモ版と同じ）。
    /// </summary>
    public static class RailWarp
    {
        /// <summary>ビーコンがレールに着弾したとみなす、線分からの距離（m）。</summary>
        public const float CatchRadiusM = 0.9f;

        /// <summary>
        /// ビーコンの位置が、レールの線分に着弾する距離にあるかを調べる。
        /// </summary>
        /// <param name="railStart">レールの始点（m）。</param>
        /// <param name="railEnd">レールの終点（m）。</param>
        /// <param name="beacon">ビーコンの位置（m）。</param>
        /// <param name="closest">着弾する場合、レール上の最も近い点（m）。着弾しなければ既定値。</param>
        /// <param name="distanceM">着弾する場合、その点までの距離（m）。</param>
        /// <returns>着弾するか。長さが 0 のレールは着弾しない。</returns>
        public static bool TryCatch(Vector3 railStart, Vector3 railEnd, Vector3 beacon, out Vector3 closest, out float distanceM)
        {
            closest = default;
            distanceM = 0f;

            var along = railEnd - railStart;
            var lengthSqr = along.sqrMagnitude;
            if (lengthSqr < 1e-4f)
            {
                return false;
            }

            // 線分の上で、ビーコンに最も近い点（端を越えたら端）
            var t = Mathf.Clamp01(Vector3.Dot(beacon - railStart, along) / lengthSqr);
            var point = railStart + along * t;
            var distance = Vector3.Distance(point, beacon);
            if (distance >= CatchRadiusM)
            {
                return false;
            }

            closest = point;
            distanceM = distance;
            return true;
        }
    }
}

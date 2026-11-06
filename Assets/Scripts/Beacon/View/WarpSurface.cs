using BlinkExpress.Beacon.Model;
using UnityEngine;

namespace BlinkExpress.Beacon.View
{
    /// <summary>
    /// 面の種類を指定するコンポーネント。ビーコンが当たったときの結果を決める。
    /// 付けていない面は、法線の向きで地面・天井・壁として扱う。
    /// </summary>
    public sealed class WarpSurface : MonoBehaviour
    {
        [SerializeField] private HitKind _kind = HitKind.Ground;

        /// <summary>この面の種類。</summary>
        public HitKind Kind => _kind;
    }
}

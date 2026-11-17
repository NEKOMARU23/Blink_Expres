using UnityEngine;

namespace BlinkExpress.Stage.View
{
    /// <summary>
    /// 泥（減速床）や跳ねるキノコなど、触れると効果がある範囲を表すコンポーネント。
    /// 範囲は Trigger のコライダーで作る。PlayerView が重なりを調べて、その効果を適用する。
    /// </summary>
    public sealed class HazardZone : MonoBehaviour
    {
        /// <summary>仕掛けの種類。</summary>
        public enum Kind
        {
            /// <summary>泥（減速床）。上にいる間、速さが減る。</summary>
            Mud,

            /// <summary>跳ねるキノコ。踏むと跳ねる。</summary>
            Mushroom,
        }

        [SerializeField] private Kind _kind = Kind.Mud;

        /// <summary>この仕掛けの種類。</summary>
        public Kind KindOfZone => _kind;
    }
}

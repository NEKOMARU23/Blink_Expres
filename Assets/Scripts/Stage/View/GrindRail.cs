using UnityEngine;

namespace BlinkExpress.Stage.View
{
    /// <summary>
    /// グラインドレール。直線の区間を表し、近くに来たプレイヤーが乗れる。
    /// 始点と終点は子の Transform で指定する。
    /// </summary>
    public sealed class GrindRail : MonoBehaviour
    {
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;

        /// <summary>レールの始点（ワールド座標）。</summary>
        public Vector3 StartPoint => _start != null ? _start.position : transform.position;

        /// <summary>レールの終点（ワールド座標）。</summary>
        public Vector3 EndPoint => _end != null ? _end.position : transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(StartPoint, EndPoint);
        }
    }
}

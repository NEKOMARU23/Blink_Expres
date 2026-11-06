using BlinkExpress.Ghost.Model;
using UnityEngine;

namespace BlinkExpress.Ghost.View
{
    /// <summary>
    /// ゴーストの View。自己ベストの走りを、半透明のキャラとして同じ時間の位置に描く。
    /// 見た目は仮の立方体（半透明は後で仕上げる）。
    /// </summary>
    public sealed class GhostView : MonoBehaviour
    {
        /// <summary>
        /// 指定の位置と向きに、ゴーストを置く。
        /// </summary>
        /// <param name="pose">位置と向き。</param>
        public void Show(GhostSample pose)
        {
            gameObject.SetActive(true);
            transform.position = new Vector3(pose.X, pose.Y, pose.Z);
            transform.rotation = Quaternion.Euler(0f, pose.YawRad * Mathf.Rad2Deg, 0f);
        }

        /// <summary>ゴーストを隠す（再生が終わったとき、記録が無いとき）。</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

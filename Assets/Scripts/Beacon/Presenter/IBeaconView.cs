using System.Collections.Generic;
using BlinkExpress.Beacon.Model;
using UnityEngine;

namespace BlinkExpress.Beacon.Presenter
{
    /// <summary>
    /// ビーコンの View に求める機能。
    /// Presenter はこの型だけを通して Unity に触るので、テストで差し替えられる。
    /// </summary>
    public interface IBeaconView
    {
        /// <summary>
        /// 2 点の間で最初に当たったものを調べる。プレイヤー自身は無視する。
        /// </summary>
        /// <param name="from">調べ始めの位置（m）。</param>
        /// <param name="to">調べ終わりの位置（m）。</param>
        /// <param name="hit">当たった結果。当たらなければ既定値。</param>
        /// <returns>当たったかどうか。</returns>
        bool Raycast(Vector3 from, Vector3 to, out BeaconHit hit);

        /// <summary>
        /// 照準の予測線と、着弾の文言と色を表示する。
        /// </summary>
        /// <param name="points">予測線の点（順番どおり）。</param>
        /// <param name="feedback">表示する文言と色。</param>
        void ShowAimPreview(IReadOnlyList<Vector3> points, AimFeedback feedback);

        /// <summary>照準の予測線と文言を消す。</summary>
        void HideAimPreview();

        /// <summary>飛んでいるビーコンを、指定の位置に表示する。</summary>
        /// <param name="position">ビーコンの位置（m）。</param>
        void ShowBeacon(Vector3 position);

        /// <summary>飛んでいるビーコンの表示を消す。</summary>
        void HideBeacon();

        /// <summary>
        /// ワープでプレイヤーを移動させる。
        /// </summary>
        /// <param name="position">着地の位置（m）。</param>
        /// <param name="direction">着地後の進む向き。</param>
        /// <param name="speedMps">着地後の速さ（m/s）。</param>
        /// <param name="outcome">ワープの結果。</param>
        void WarpPlayer(Vector3 position, Vector3 direction, float speedMps, WarpOutcome outcome);

        /// <summary>プレイヤーの速さだけを設定する（弾かれたときなど）。</summary>
        /// <param name="speedMps">設定する速さ（m/s）。</param>
        void SetPlayerSpeed(float speedMps);
    }
}

using System;

namespace BlinkExpress.Enemy.Model
{
    /// <summary>
    /// 「逃げる」敵の動き（仕様書「敵・コイン・トラップ」の節）。
    /// プレイヤーが前方 35 m 以内に来たら、横へ毎秒 7 m、プレイヤーから離れる向きに動く。
    /// 計算だけを行い、位置は View が持つ。
    /// </summary>
    public static class EnemyFlee
    {
        /// <summary>逃げ始める距離（m）。プレイヤーより前（Z が大きい側）にいる敵だけが、この距離以内で逃げる。</summary>
        public const float TriggerRangeM = 35f;

        /// <summary>逃げる横の速さ（m/s）。</summary>
        public const float SpeedMps = 7f;

        /// <summary>横の位置の限界（m）。道の外へは逃げない。</summary>
        public const float LimitXM = 19f;

        /// <summary>
        /// 1 回分の逃げる動きを進めて、敵の新しい横の位置を返す。
        /// </summary>
        /// <param name="enemyX">敵の今の横の位置（m）。</param>
        /// <param name="enemyZ">敵の前後の位置（m）。</param>
        /// <param name="playerX">プレイヤーの横の位置（m）。</param>
        /// <param name="playerZ">プレイヤーの前後の位置（m）。</param>
        /// <param name="worldDt">ゲーム内時間の経過（秒）。照準中の遅くなった分を含む。</param>
        /// <returns>敵の新しい横の位置（m）。逃げないときは、今の位置のまま。</returns>
        public static float StepX(float enemyX, float enemyZ, float playerX, float playerZ, float worldDt)
        {
            var ahead = enemyZ - playerZ;
            if (ahead <= 0f || ahead >= TriggerRangeM)
            {
                return enemyX;
            }

            // 真後ろ（横の位置が同じ）のときは、右へ逃げる
            var direction = enemyX >= playerX ? 1f : -1f;
            var moved = enemyX + direction * SpeedMps * worldDt;
            return Math.Max(-LimitXM, Math.Min(LimitXM, moved));
        }
    }
}

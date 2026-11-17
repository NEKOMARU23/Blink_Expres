using BlinkExpress.Enemy.Model;

namespace BlinkExpress.Effects.Model
{
    /// <summary>
    /// 粉砕の演出（破片、衝撃波の輪、コインの飛び）の数値。
    /// 仕様書には止まる時間（<see cref="EnemyRules.HitStopSec"/>）しか書かれていないので、
    /// ここは見た目の仮の値。手触りを見て調整する。
    /// </summary>
    public static class SmashEffectRules
    {
        /// <summary>コインが画面の表示へ飛んでいく時間（秒）。</summary>
        public const float CoinFlySec = 0.6f;

        /// <summary>衝撃波の輪が広がりきるまでの時間（秒）。</summary>
        public const float RingLifeSec = 0.35f;

        /// <summary>破片が残る時間（秒）。</summary>
        public const float DebrisLifeSec = 0.8f;

        /// <summary>
        /// コインが飛ぶ動きの進み具合を、ゆっくり始まってゆっくり着く形（なめらかな補間）にする。
        /// </summary>
        /// <param name="t">経過の割合（0〜1。範囲の外は丸める）。</param>
        /// <returns>位置の割合（0〜1）。</returns>
        public static float CoinFlyProgress(float t)
        {
            var clamped = t < 0f ? 0f : (t > 1f ? 1f : t);
            return clamped * clamped * (3f - 2f * clamped);
        }

        /// <summary>
        /// 出る破片の数。硬い敵ほど多い。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>破片の数。</returns>
        public static int DebrisCount(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 16;
                case EnemyTier.Hard:
                    return 26;
                default:
                    return 10;
            }
        }

        /// <summary>
        /// 衝撃波の輪の、広がりきったときの半径（m）。敵の半径の倍率で決める。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>輪の最大の半径（m）。</returns>
        public static float RingMaxRadiusM(EnemyTier tier)
        {
            return EnemyRules.RadiusM(tier) * 2.5f;
        }

        /// <summary>
        /// 破片の飛ぶ速さ（m/s）。硬い敵ほど速い。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>破片の速さ（m/s）。</returns>
        public static float DebrisSpeedMps(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 9f;
                case EnemyTier.Hard:
                    return 11f;
                default:
                    return 7f;
            }
        }
    }
}

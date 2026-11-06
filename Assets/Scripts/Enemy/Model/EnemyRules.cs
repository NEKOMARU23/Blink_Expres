namespace BlinkExpress.Enemy.Model
{
    /// <summary>
    /// 敵の硬さの3段階。見た目の大きさと硬さを一致させる（仕様書「敵・コイン・トラップ」）。
    /// </summary>
    public enum EnemyTier
    {
        /// <summary>やわらかい。ほぼいつでも壊せる。</summary>
        Soft,

        /// <summary>ふつう。中くらいの速さが必要。</summary>
        Normal,

        /// <summary>かたい。上限近くの速さが必要。</summary>
        Hard,
    }

    /// <summary>
    /// 敵の硬さごとの数値と、粉砕できるかの判定。
    /// 単位は距離 m、速さ m/s、時間 秒。仕様書の表の値を使う。
    /// </summary>
    public static class EnemyRules
    {
        /// <summary>プレイヤーとの接触の判定で、敵の半径に加える距離（m）。</summary>
        public const float ContactMarginM = 0.9f;

        /// <summary>接触の後、もう一度当たらない時間（秒）。</summary>
        public const float InvincibleSec = 0.35f;

        /// <summary>
        /// 敵の当たりの半径（m）。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>当たりの半径（m）。</returns>
        public static float RadiusM(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 1.7f;
                case EnemyTier.Hard:
                    return 2.8f;
                default:
                    return 1.1f;
            }
        }

        /// <summary>
        /// 粉砕に必要な速さ（m/s）。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>必要な速さ（m/s）。</returns>
        public static float RequiredSpeedMps(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 26f;
                case EnemyTier.Hard:
                    return 48f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 粉砕したときに増える速さ（m/s）。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>増える速さ（m/s）。</returns>
        public static float SpeedGainMps(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 4f;
                case EnemyTier.Hard:
                    return 6f;
                default:
                    return 3f;
            }
        }

        /// <summary>
        /// 粉砕したときに出るコインの数。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>コインの数。</returns>
        public static int CoinCount(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 2;
                case EnemyTier.Hard:
                    return 4;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// 粉砕したときに回復するゲージ（0〜1）。
        /// </summary>
        /// <param name="tier">硬さ。</param>
        /// <returns>回復する量（0〜1）。</returns>
        public static float GaugeGain(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return 0.2f;
                case EnemyTier.Hard:
                    return 0.3f;
                default:
                    return 0.15f;
            }
        }

        /// <summary>
        /// 当たりの判定。速さが必要な速さ以上なら粉砕できる（ちょうど同じ値も粉砕）。
        /// </summary>
        /// <param name="speedMps">当たった時点の速さ（m/s）。ワープの加速を含めた値を渡す。</param>
        /// <param name="tier">硬さ。</param>
        /// <returns>粉砕できるか。</returns>
        public static bool CanSmash(float speedMps, EnemyTier tier)
        {
            return speedMps >= RequiredSpeedMps(tier);
        }
    }
}

namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// 当たったものと速さから、ワープの結果と、その後の速さを決める。
    /// 仕様書「ワープ」の「着弾したものごとの結果」と、敵の「粉砕できるか」の節に対応する。
    /// </summary>
    public static class WarpRules
    {
        /// <summary>天井に当たったあとの速さの倍率。</summary>
        public const float CeilingSpeedScale = 0.55f;

        /// <summary>敵に弾かれたあとの速さの倍率。</summary>
        public const float BounceSpeedScale = 0.5f;

        /// <summary>
        /// 当たったものの種類と速さから、ワープの結果を決める。
        /// 敵は、ワープの加速を足した後の速さで粉砕できるかを判定する。
        /// </summary>
        /// <param name="hit">当たったものの種類。</param>
        /// <param name="speedAfterBoostMps">ワープの加速を足した後の速さ（m/s）。</param>
        /// <param name="enemyRequiredSpeedMps">敵を壊すのに必要な速さ（m/s）。敵以外では使わない。</param>
        /// <returns>ワープの結果。</returns>
        public static WarpOutcome Classify(HitKind hit, float speedAfterBoostMps, float enemyRequiredSpeedMps)
        {
            switch (hit)
            {
                case HitKind.Ground:
                    return WarpOutcome.Ground;
                case HitKind.Wall:
                    return WarpOutcome.WallSlide;
                case HitKind.Ceiling:
                    return WarpOutcome.Ceiling;
                case HitKind.Rail:
                    return WarpOutcome.Rail;
                case HitKind.NoWarpSurface:
                    return WarpOutcome.NoWarp;
                case HitKind.Enemy:
                    return speedAfterBoostMps >= enemyRequiredSpeedMps ? WarpOutcome.Smash : WarpOutcome.Bounce;
                default:
                    return WarpOutcome.Miss;
            }
        }

        /// <summary>
        /// 結果に応じて、ワープ後の速さを求める。
        /// 粉砕の加速は敵の種類で変わるので、ここでは扱わない（敵の実装で加える）。
        /// </summary>
        /// <param name="outcome">ワープの結果。</param>
        /// <param name="speedAfterBoostMps">ワープの加速を足した後の速さ（m/s）。</param>
        /// <returns>ワープ後の速さ（m/s）。</returns>
        public static float SpeedAfter(WarpOutcome outcome, float speedAfterBoostMps)
        {
            switch (outcome)
            {
                case WarpOutcome.Ceiling:
                    return speedAfterBoostMps * CeilingSpeedScale;
                case WarpOutcome.Bounce:
                    return speedAfterBoostMps * BounceSpeedScale;
                default:
                    return speedAfterBoostMps;
            }
        }

        /// <summary>
        /// ワープが実際に起きるか。届かない・ワープ不可・弾かれる場合は起きない（弾かれるのは速さだけ変わる）。
        /// </summary>
        /// <param name="outcome">ワープの結果。</param>
        /// <returns>プレイヤーが移動するか。</returns>
        public static bool MovesPlayer(WarpOutcome outcome)
        {
            return outcome != WarpOutcome.Miss && outcome != WarpOutcome.NoWarp && outcome != WarpOutcome.Bounce;
        }
    }
}

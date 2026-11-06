namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// ワープのチェインの判定。前のワープから次を投げるまでの時間で、加速の大きさが変わる。
    /// 仕様書「ワープ」の「チェイン」の節に対応する。
    /// </summary>
    public static class ChainRule
    {
        /// <summary>これより早く投げると連打とみなし、加速しない（秒）。</summary>
        public const float EarlyLimitSec = 0.3f;

        /// <summary>これ以内に投げるとチェインが続く（秒）。</summary>
        public const float ChainWindowSec = 1.6f;

        /// <summary>
        /// 次のワープのチェイン数を求める。0 は連打（加速なし）、1 は単発、2 以上はチェイン。
        /// </summary>
        /// <param name="secondsSincePreviousWarp">前のワープからの経過時間（秒）。</param>
        /// <param name="currentChain">前のワープ時点のチェイン数。</param>
        /// <returns>次のワープのチェイン数。</returns>
        public static int NextChain(float secondsSincePreviousWarp, int currentChain)
        {
            if (secondsSincePreviousWarp < EarlyLimitSec)
            {
                return 0;
            }

            if (secondsSincePreviousWarp < ChainWindowSec)
            {
                return currentChain + 1;
            }

            return 1;
        }

        /// <summary>
        /// チェイン数に応じた、ワープの加速（m/s）を求める。
        /// 単発は warpBoostMps、チェインは chainBoostMps ずつ上乗せされる。
        /// </summary>
        /// <param name="chain">チェイン数（0 は加速なし）。</param>
        /// <param name="warpBoostMps">ワープの基本の加速（m/s）。</param>
        /// <param name="chainBoostMps">チェインごとの上乗せ（m/s）。</param>
        /// <returns>加速（m/s）。</returns>
        public static float WarpBoostMps(int chain, float warpBoostMps, float chainBoostMps)
        {
            if (chain <= 0)
            {
                return 0f;
            }

            return warpBoostMps + (chain - 1) * chainBoostMps;
        }
    }
}

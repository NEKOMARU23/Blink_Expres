namespace BlinkExpress.Enemy.Model
{
    /// <summary>
    /// コインの取得の数値。仕様書「コイン」の節の値。
    /// </summary>
    public static class CoinRules
    {
        /// <summary>取得できる範囲（体の中心からの距離、m）。</summary>
        public const float PickupRangeM = 1.9f;

        /// <summary>1枚で増える速さ（m/s）。</summary>
        public const float SpeedGainMps = 1.2f;

        /// <summary>1枚で回復するゲージ（0〜1）。</summary>
        public const float GaugeGain = 0.05f;
    }
}

namespace BlinkExpress.Stage.Model
{
    /// <summary>
    /// クリアの評価（ランク S〜C）。クリアタイムとコインの取得率で決める。
    /// 仕様書「追ってくるもの・ゴール・評価・ゴースト」の表の値（平原）を使う。
    /// </summary>
    public static class StageRank
    {
        /// <summary>
        /// ランクを決める。
        /// S：48 秒未満かつコイン 60% 以上。A：58 秒未満かつ 35% 以上。B：75 秒未満。それ以外は C。
        /// </summary>
        /// <param name="clearTimeSec">クリアタイム（秒）。</param>
        /// <param name="coinRatio">コインの取得率（0〜1）。</param>
        /// <returns>ランクの文字（S、A、B、C）。</returns>
        public static string Decide(float clearTimeSec, float coinRatio)
        {
            if (clearTimeSec < 48f && coinRatio >= 0.6f)
            {
                return "S";
            }

            if (clearTimeSec < 58f && coinRatio >= 0.35f)
            {
                return "A";
            }

            if (clearTimeSec < 75f)
            {
                return "B";
            }

            return "C";
        }
    }
}

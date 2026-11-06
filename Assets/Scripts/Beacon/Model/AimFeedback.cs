namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// 予測線の色の種類。View がこれを実際の色に変える。
    /// </summary>
    public enum AimTone
    {
        Cyan,
        Green,
        Yellow,
        Red,
        White,
        Orange,
        Gray,
    }

    /// <summary>
    /// 照準中に表示する結果の文言と色。
    /// </summary>
    public readonly struct AimFeedback
    {
        /// <summary>
        /// 文言と色を組み合わせて作る。
        /// </summary>
        /// <param name="text">画面に出す文言。</param>
        /// <param name="tone">色の種類。</param>
        public AimFeedback(string text, AimTone tone)
        {
            Text = text;
            Tone = tone;
        }

        /// <summary>画面に出す文言（例：「着地」「粉砕できる」）。</summary>
        public string Text { get; }

        /// <summary>予測線と着弾点の色の種類。</summary>
        public AimTone Tone { get; }

        /// <summary>
        /// ワープの結果から、照準中に表示する文言と色を求める。
        /// 仕様書「着弾したものごとの結果」の表と同じ対応にする。
        /// </summary>
        /// <param name="outcome">ワープの結果。</param>
        /// <returns>表示する文言と色。</returns>
        public static AimFeedback For(WarpOutcome outcome)
        {
            switch (outcome)
            {
                case WarpOutcome.Ground:
                    return new AimFeedback("着地", AimTone.Cyan);
                case WarpOutcome.WallSlide:
                    return new AimFeedback("壁滑走", AimTone.Cyan);
                case WarpOutcome.Ceiling:
                    return new AimFeedback("天井・減速", AimTone.Orange);
                case WarpOutcome.Smash:
                    return new AimFeedback("粉砕できる", AimTone.Green);
                case WarpOutcome.Bounce:
                    return new AimFeedback("速度不足", AimTone.Yellow);
                case WarpOutcome.Rail:
                    return new AimFeedback("レール", AimTone.White);
                case WarpOutcome.NoWarp:
                    return new AimFeedback("ワープ不可", AimTone.Red);
                default:
                    return new AimFeedback("届かない", AimTone.Gray);
            }
        }
    }
}

namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// ビーコンとゲージの数値。単位は距離 m、速さ m/s、時間 秒。
    /// 値は BeaconTuning（ScriptableObject）から受け取る。仕様書「ワープ」の節の値を初期値にする。
    /// </summary>
    public sealed class BeaconSettings
    {
        /// <summary>投げるときの基本の初速（m/s）。自分の速さの半分が上乗せされる。</summary>
        public float BaseSpeedMps { get; set; } = 70f;

        /// <summary>ビーコンの重力（m/s²）。</summary>
        public float GravityMps2 { get; set; } = 45f;

        /// <summary>ビーコンの寿命（秒）。</summary>
        public float LifetimeSec { get; set; } = 2f;

        /// <summary>投げた後のクールタイム（秒）。</summary>
        public float CooldownSec { get; set; } = 0.35f;

        /// <summary>ワープの基本の加速（m/s）。</summary>
        public float WarpBoostMps { get; set; } = 3f;

        /// <summary>チェインごとの上乗せ（m/s）。</summary>
        public float ChainBoostMps { get; set; } = 3f;

        /// <summary>照準中の時間の速さ（0〜1）。</summary>
        public float SlowFactor { get; set; } = 0.25f;

        /// <summary>弾道の計算の刻み（秒）。予測と実際の飛行で同じ値を使う。</summary>
        public float StepSec { get; set; } = 1f / 120f;

        /// <summary>投げる位置の高さ（m）。プレイヤーの肩の高さに合わせる。</summary>
        public float ThrowOriginHeightM { get; set; } = 1.6f;

        /// <summary>ゲージの開始時の値（0〜1）。</summary>
        public float GaugeStartValue { get; set; } = 0.7f;

        /// <summary>照準を始めるたびに消費する量（0〜1）。</summary>
        public float AimCost { get; set; } = 0.08f;

        /// <summary>照準中に毎秒減る量（実時間、0〜1）。</summary>
        public float AimDrainPerSec { get; set; } = 0.15f;

        /// <summary>照準していない間に毎秒回復する量（実時間、0〜1）。</summary>
        public float AimRegenPerSec { get; set; } = 0.05f;
    }
}

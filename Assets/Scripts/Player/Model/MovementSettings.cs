namespace BlinkExpress.Player.Model
{
    /// <summary>
    /// 移動の数値。単位は距離 m、速さ m/s、時間 秒、角度 rad。
    /// 値は PlayerTuning（ScriptableObject）から受け取る。
    /// </summary>
    public sealed class MovementSettings
    {
        /// <summary>速さの下限（m/s）。ブレーキをかけてもこれより下がらない。</summary>
        public float MinSpeedMps { get; set; }

        /// <summary>スケートだけで出せる速さの上限（m/s）。</summary>
        public float CruiseSpeedMps { get; set; }

        /// <summary>速さの上限（m/s）。ワープや破壊で、この手前まで超えられる。</summary>
        public float MaxSpeedMps { get; set; }

        /// <summary>開始時の速さ（m/s）。</summary>
        public float StartSpeedMps { get; set; }

        /// <summary>スケートの加速（m/s²）。上限のクルーズ速度まで働く。</summary>
        public float AccelMps2 { get; set; }

        /// <summary>ブレーキの減速（m/s²）。</summary>
        public float BrakeMps2 { get; set; }

        /// <summary>カメラの向きへ曲がる速さ（rad/s）。</summary>
        public float SteerRadPerSec { get; set; }

        /// <summary>A・Dで寄せる角度（rad）。</summary>
        public float LeanRad { get; set; }

        /// <summary>空中の重力（m/s²）。速さは変えず、向きを下へ曲げるために使う。</summary>
        public float GravityMps2 { get; set; }

        /// <summary>空中での旋回の倍率。地上の何倍の速さで曲がるか。</summary>
        public float AirSteerRatio { get; set; }
    }
}

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

        /// <summary>ジャンプの上向きの速さ（m/s）。速さが足りないときは、上向きの割合の上限で決まる。</summary>
        public float JumpSpeedMps { get; set; } = 12.5f;

        /// <summary>二段ジャンプができる最低の速さ（m/s）。</summary>
        public float DoubleJumpMinSpeedMps { get; set; } = 40f;

        /// <summary>スライディングができる最低の速さ（m/s）。</summary>
        public float SlideMinSpeedMps { get; set; } = 12f;

        /// <summary>スライディング中の旋回の倍率。地上の何倍で曲がるか。</summary>
        public float SlideSteerRatio { get; set; } = 0.4f;

        /// <summary>前から当たったとみなす、進む向きと壁の法線の内積の下限（約 45° 以内）。</summary>
        public float FrontDot { get; set; } = 0.7f;

        /// <summary>壁のぼりの上昇速度が毎秒減る量（m/s²）。</summary>
        public float ClimbDecelMps2 { get; set; } = 20f;

        /// <summary>壁のぼり中にジャンプを押したときの、上昇速度の上乗せ（m/s、1回だけ）。</summary>
        public float ClimbKickMps { get; set; } = 16f;

        /// <summary>壁走り・壁滑走のときの重力（m/s²）。</summary>
        public float WallGravityMps2 { get; set; } = 13f;

        /// <summary>壁走りで、まっすぐ走る時間（秒）。</summary>
        public float WallRunTimeSec { get; set; } = 1.2f;

        /// <summary>横から壁走りを始める最低の高さ（m）。</summary>
        public float WallRunMinHeightM { get; set; } = 2f;

        /// <summary>壁ジャンプの、壁から離れる向きの割合。</summary>
        public float WallJumpNormalRatio { get; set; } = 0.75f;

        /// <summary>壁ジャンプの、上向きの割合。</summary>
        public float WallJumpUpRatio { get; set; } = 0.6f;

        /// <summary>壁ジャンプの、進んでいた向きの割合。</summary>
        public float WallJumpForwardRatio { get; set; } = 0.6f;

        /// <summary>泥（減速床）の上で毎秒減る速さ（m/s²）。スケートの加速は打ち消される。</summary>
        public float MudDecelMps2 { get; set; } = 16f;

        /// <summary>レールの上で毎秒増える速さ（m/s²）。</summary>
        public float RailAccelMps2 { get; set; } = 4f;

        /// <summary>キノコで跳ねるときの、上向きの速さの上限（m/s）。</summary>
        public float MushroomBounceMaxMps { get; set; } = 24f;

        /// <summary>キノコで跳ねるときの、速さに対する上向きの速さの割合。</summary>
        public float MushroomBounceSpeedRatio { get; set; } = 0.9f;

        /// <summary>キノコで跳ねるときの、上向きの割合の上限。</summary>
        public float MushroomMaxUpRatio { get; set; } = 0.95f;

        /// <summary>登れない障害物に正面から当たったときの、速さの減り方の係数。</summary>
        public float ObstacleSlowFactor { get; set; } = 0.45f;

        /// <summary>障害物で速さが減るとみなす、進む向きと面の内積の下限。</summary>
        public float ObstacleMinIntoDot { get; set; } = 0.35f;

        /// <summary>接触トラップに触れたときの、速さの倍率（仕様書「トラップと障害物」の節。暫定の値）。</summary>
        public float ContactTrapSpeedRatio { get; set; } = 0.5f;

        /// <summary>天井に当たったあとの速さの倍率。</summary>
        public float CeilingSpeedRatio { get; set; } = 0.55f;
    }
}

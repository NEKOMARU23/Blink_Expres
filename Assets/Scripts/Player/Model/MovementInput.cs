namespace BlinkExpress.Player.Model
{
    /// <summary>
    /// 1フレーム分の移動の入力。View が読み取り、Presenter を通して Model に渡す。
    /// </summary>
    public readonly struct MovementInput
    {
        /// <summary>
        /// 入力を作る。
        /// </summary>
        /// <param name="brake">ブレーキ（S）を押しているか。</param>
        /// <param name="cameraYawRad">カメラの水平角（rad）。進む向きはここへ曲がる。</param>
        /// <param name="leanDirection">寄せる向き。左（A）で +1、右（D）で −1、なしで 0。</param>
        /// <param name="jumpPressed">このフレームでジャンプ（Space）を押したか。</param>
        /// <param name="slideHeld">スライディングのキー（Shift か C）を押しているか。</param>
        /// <param name="canStand">頭の上に天井が無く、立ち上がれるか。</param>
        /// <param name="wall">壁に触れている状態。触れていなければ既定値。</param>
        public MovementInput(bool brake, float cameraYawRad, float leanDirection, bool jumpPressed = false, bool slideHeld = false, bool canStand = true, WallContact wall = default)
        {
            Brake = brake;
            CameraYawRad = cameraYawRad;
            LeanDirection = leanDirection;
            JumpPressed = jumpPressed;
            SlideHeld = slideHeld;
            CanStand = canStand;
            Wall = wall;
        }

        /// <summary>壁に触れている状態。</summary>
        public WallContact Wall { get; }

        /// <summary>このフレームでジャンプ（Space）を押したか。地上ならジャンプ、空中なら二段ジャンプ。</summary>
        public bool JumpPressed { get; }

        /// <summary>スライディングのキーを押しているか。</summary>
        public bool SlideHeld { get; }

        /// <summary>立ち上がれるか（頭の上に天井が無いか）。</summary>
        public bool CanStand { get; }

        /// <summary>ブレーキ（S）を押しているか。</summary>
        public bool Brake { get; }

        /// <summary>カメラの水平角（rad）。0 は +Z 方向。</summary>
        public float CameraYawRad { get; }

        /// <summary>寄せる向き。左で +1、右で −1、0 は寄せない。</summary>
        public float LeanDirection { get; }
    }
}

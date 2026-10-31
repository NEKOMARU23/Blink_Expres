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
        public MovementInput(bool brake, float cameraYawRad, float leanDirection)
        {
            Brake = brake;
            CameraYawRad = cameraYawRad;
            LeanDirection = leanDirection;
        }

        /// <summary>ブレーキ（S）を押しているか。</summary>
        public bool Brake { get; }

        /// <summary>カメラの水平角（rad）。0 は +Z 方向。</summary>
        public float CameraYawRad { get; }

        /// <summary>寄せる向き。左で +1、右で −1、0 は寄せない。</summary>
        public float LeanDirection { get; }
    }
}

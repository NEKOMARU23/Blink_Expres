namespace BlinkExpress.Player.Presenter
{
    /// <summary>
    /// プレイヤーの View に求める機能。
    /// Presenter はこの型だけを通して View に触るので、テストで差し替えられる。
    /// </summary>
    public interface IPlayerView
    {
        /// <summary>地面に立っているか。</summary>
        bool IsGrounded { get; }

        /// <summary>
        /// このフレームの移動量（m）を渡して、体を動かす。
        /// </summary>
        /// <param name="dx">X 方向の移動量（m）。</param>
        /// <param name="dy">Y 方向の移動量（m）。</param>
        /// <param name="dz">Z 方向の移動量（m）。</param>
        void Move(float dx, float dy, float dz);

        /// <summary>
        /// 体の向きを変える。
        /// </summary>
        /// <param name="yawRad">水平角（rad）。0 は +Z 方向。</param>
        void Face(float yawRad);

        /// <summary>
        /// 現在の速さを画面側に伝える（カメラの引きなどに使う）。
        /// </summary>
        /// <param name="speedMps">速さ（m/s）。</param>
        void ShowSpeed(float speedMps);
    }
}

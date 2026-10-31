using BlinkExpress.Player.Model;

namespace BlinkExpress.Player.Presenter
{
    /// <summary>
    /// プレイヤーの移動の Presenter。入力を Model に渡し、結果を IPlayerView に反映する。
    /// Unity の API は使わない。
    /// </summary>
    public sealed class PlayerPresenter
    {
        private readonly IPlayerView _view;
        private readonly PlayerMovementModel _model;

        /// <summary>
        /// Presenter を作る。
        /// </summary>
        /// <param name="view">プレイヤーの View。</param>
        /// <param name="settings">移動の数値。</param>
        public PlayerPresenter(IPlayerView view, MovementSettings settings)
        {
            _view = view;
            _model = new PlayerMovementModel(settings);
        }

        /// <summary>
        /// 1フレーム分の移動を進めて、結果を View に反映する。
        /// </summary>
        /// <param name="input">このフレームの入力。</param>
        /// <param name="dt">経過時間（秒）。</param>
        public void Tick(MovementInput input, float dt)
        {
            _model.Tick(input, _view.IsGrounded, dt);

            _view.Move(_model.VelocityX * dt, _model.VelocityY * dt, _model.VelocityZ * dt);
            _view.Face(_model.YawRad);
            _view.ShowSpeed(_model.SpeedMps);
        }
    }
}

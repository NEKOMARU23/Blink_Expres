using BlinkExpress.Player.Model;
using UnityEngine;

namespace BlinkExpress.Player.Presenter
{
    /// <summary>
    /// プレイヤーの移動の Presenter。入力を Model に渡し、結果を IPlayerView に反映する。
    /// Unity の API は、位置と向きを表す Vector3 の型だけを使う。
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

        /// <summary>現在の速さ（m/s）。</summary>
        public float SpeedMps => _model.SpeedMps;

        /// <summary>スライディング中か。</summary>
        public bool IsSliding => _model.IsSliding;

        /// <summary>壁のぼり中か。</summary>
        public bool IsClimbing => _model.IsClimbing;

        /// <summary>壁走り中か。</summary>
        public bool IsRunning => _model.IsRunning;

        /// <summary>
        /// ワープで着地する。位置を移し、向きと速さを設定する。
        /// </summary>
        /// <param name="position">着地の位置（m）。</param>
        /// <param name="direction">着地後の進む向き。</param>
        /// <param name="speedMps">着地後の速さ（m/s）。</param>
        public void Warp(Vector3 position, Vector3 direction, float speedMps)
        {
            _model.SetDirection(direction.x, direction.y, direction.z, speedMps);
            _view.Teleport(position);
            _view.Face(_model.YawRad);
        }

        /// <summary>
        /// 速さだけを設定する（向きは変えない）。
        /// </summary>
        /// <param name="speedMps">設定する速さ（m/s）。</param>
        public void SetSpeed(float speedMps)
        {
            _model.SetSpeed(speedMps);
        }
    }
}

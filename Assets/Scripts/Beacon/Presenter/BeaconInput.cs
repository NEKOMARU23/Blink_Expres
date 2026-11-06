using UnityEngine;

namespace BlinkExpress.Beacon.Presenter
{
    /// <summary>
    /// 1フレーム分のビーコンの入力。View が読み取って Presenter に渡す。
    /// </summary>
    public readonly struct BeaconInput
    {
        /// <summary>
        /// 入力を作る。
        /// </summary>
        /// <param name="aimStarted">このフレームで照準を始めたか（左クリックを押した）。</param>
        /// <param name="aimReleased">このフレームで投げたか（左クリックを離した）。</param>
        /// <param name="cancelPressed">このフレームで照準をキャンセルしたか（右クリック）。</param>
        /// <param name="aimDirection">投げる向き（カメラの正面）。</param>
        public BeaconInput(bool aimStarted, bool aimReleased, bool cancelPressed, Vector3 aimDirection)
        {
            AimStarted = aimStarted;
            AimReleased = aimReleased;
            CancelPressed = cancelPressed;
            AimDirection = aimDirection;
        }

        /// <summary>このフレームで照準を始めたか。</summary>
        public bool AimStarted { get; }

        /// <summary>このフレームで投げたか。</summary>
        public bool AimReleased { get; }

        /// <summary>このフレームで照準をキャンセルしたか。</summary>
        public bool CancelPressed { get; }

        /// <summary>投げる向き（カメラの正面）。</summary>
        public Vector3 AimDirection { get; }
    }
}

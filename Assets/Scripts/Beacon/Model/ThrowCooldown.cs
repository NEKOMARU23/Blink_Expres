namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// ビーコンを投げた後の短いクールタイム。クールタイム中と、ビーコンが飛んでいる間は投げられない。
    /// 仕様書「照準とゲージ」の節（投げた後は 0.35 秒のクールタイム、ビーコンは同時に 1 つだけ）に対応する。
    /// </summary>
    public sealed class ThrowCooldown
    {
        private readonly float _cooldownSec;
        private float _remainingSec;

        /// <summary>
        /// クールタイムの長さを指定して作る。作った直後は投げられる。
        /// </summary>
        /// <param name="cooldownSec">投げた後に待つ時間（秒）。</param>
        public ThrowCooldown(float cooldownSec)
        {
            _cooldownSec = cooldownSec;
        }

        /// <summary>
        /// 投げられる状態か。クールタイムが終わっていて、ビーコンが飛んでいないこと。
        /// </summary>
        /// <param name="beaconInFlight">ビーコンが飛んでいるか。</param>
        /// <returns>投げられるか。</returns>
        public bool CanThrow(bool beaconInFlight)
        {
            return _remainingSec <= 0f && !beaconInFlight;
        }

        /// <summary>
        /// 投げたときに呼ぶ。クールタイムを開始する。
        /// </summary>
        public void OnThrown()
        {
            _remainingSec = _cooldownSec;
        }

        /// <summary>
        /// クールタイムを進める。ゲーム内時間（スローの影響を受ける）で dt（秒）だけ進める。
        /// </summary>
        /// <param name="worldDt">ゲーム内時間の経過（秒）。</param>
        public void Tick(float worldDt)
        {
            if (_remainingSec > 0f)
            {
                _remainingSec -= worldDt;
            }
        }
    }
}

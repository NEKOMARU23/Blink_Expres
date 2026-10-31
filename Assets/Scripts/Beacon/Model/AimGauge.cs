namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// 照準に使うゲージの Model。照準を始めると消費し、照準中は時間で減り、
    /// コインや破壊で回復する。0 の間は照準もワープもできない。
    /// 仕様書「照準とゲージ」の節に対応する。値は 0〜1 で持つ。
    /// </summary>
    public sealed class AimGauge
    {
        private readonly float _aimCost;
        private readonly float _drainPerSec;
        private readonly float _regenPerSec;

        /// <summary>
        /// 指定の初期値とパラメータでゲージを作る。
        /// </summary>
        /// <param name="initialValue">開始時の値（0〜1）。</param>
        /// <param name="aimCost">照準を始めるたびに消費する量。</param>
        /// <param name="drainPerSec">照準中に毎秒減る量（実時間）。</param>
        /// <param name="regenPerSec">照準していない間に毎秒回復する量（実時間）。</param>
        public AimGauge(float initialValue, float aimCost, float drainPerSec, float regenPerSec)
        {
            Value = Clamp01(initialValue);
            _aimCost = aimCost;
            _drainPerSec = drainPerSec;
            _regenPerSec = regenPerSec;
        }

        /// <summary>現在の値（0〜1）。</summary>
        public float Value { get; private set; }

        /// <summary>
        /// 照準を始められるか。照準の消費分が残っていないと始められない。
        /// </summary>
        public bool CanBeginAim => Value > _aimCost;

        /// <summary>
        /// 照準を始める。消費分を引く。残量が足りなければ何もせず false を返す。
        /// </summary>
        /// <returns>照準を始められたか。</returns>
        public bool TryBeginAim()
        {
            if (!CanBeginAim)
            {
                return false;
            }

            Value = Clamp01(Value - _aimCost);
            return true;
        }

        /// <summary>
        /// 照準中の消費を進める。実時間で dt（秒）だけ進める。
        /// </summary>
        /// <param name="realDt">実時間の経過（秒）。</param>
        /// <returns>まだ照準を続けられるか（0 になったら false）。</returns>
        public bool DrainWhileAiming(float realDt)
        {
            Value = Clamp01(Value - _drainPerSec * realDt);
            return Value > 0f;
        }

        /// <summary>
        /// 照準していない間の時間回復を進める。実時間で dt（秒）だけ進める。
        /// </summary>
        /// <param name="realDt">実時間の経過（秒）。</param>
        public void Regenerate(float realDt)
        {
            Value = Clamp01(Value + _regenPerSec * realDt);
        }

        /// <summary>
        /// コインや破壊などで回復する。
        /// </summary>
        /// <param name="amount">回復量（0〜1）。</param>
        public void Refill(float amount)
        {
            Value = Clamp01(Value + amount);
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}

using System;

namespace BlinkExpress.Effects.Model
{
    /// <summary>
    /// 粉砕のときに世界を一瞬止める「時間が止まる演出」のタイマー（仕様書「演出」の節）。
    /// 実時間で数える。<c>Time.timeScale</c> は使わず、ゲーム内の動きが <see cref="WorldScale"/> を掛けて止まる。
    /// </summary>
    public sealed class HitStopTimer
    {
        private float _remainingSec;

        /// <summary>止まっている間は true。</summary>
        public bool IsFrozen => _remainingSec > 0f;

        /// <summary>ゲーム内の時間に掛ける倍率。止まっている間は 0、それ以外は 1。</summary>
        public float WorldScale => IsFrozen ? 0f : 1f;

        /// <summary>
        /// 世界を止める。すでに止まっているときは、残りが長いほうを使う（足し算はしない）。
        /// </summary>
        /// <param name="durationSec">止める時間（秒）。0 以下なら何もしない。</param>
        public void Trigger(float durationSec)
        {
            if (durationSec > _remainingSec)
            {
                _remainingSec = durationSec;
            }
        }

        /// <summary>
        /// 実時間を進める。
        /// </summary>
        /// <param name="realDt">実時間の経過（秒）。</param>
        public void Tick(float realDt)
        {
            _remainingSec = Math.Max(0f, _remainingSec - realDt);
        }

        /// <summary>
        /// 止まっているのをすぐに解く（リスタートのとき）。
        /// </summary>
        public void Clear()
        {
            _remainingSec = 0f;
        }
    }
}

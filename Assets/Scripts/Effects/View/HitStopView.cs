using BlinkExpress.Core;
using BlinkExpress.Effects.Model;
using UnityEngine;

namespace BlinkExpress.Effects.View
{
    /// <summary>
    /// 粉砕のときに世界を一瞬止める。ステージに1つだけ置く。
    /// <c>Time.timeScale</c> は使わない。動く側（プレイヤー、逃げる敵）が <see cref="CurrentScale"/> を掛けて止まる。
    /// </summary>
    public sealed class HitStopView : Singleton<HitStopView>
    {
        private readonly HitStopTimer _timer = new HitStopTimer();

        /// <summary>
        /// ゲーム内の時間に掛ける倍率。止まっている間は 0。この View が無ければ 1（止まらない）。
        /// </summary>
        public static float CurrentScale => Instance != null ? Instance._timer.WorldScale : 1f;

        /// <summary>
        /// 世界を止める。
        /// </summary>
        /// <param name="durationSec">止める時間（秒、実時間）。</param>
        public void Trigger(float durationSec)
        {
            _timer.Trigger(durationSec);
        }

        /// <summary>
        /// 止まっているのをすぐに解く（リスタートのとき）。
        /// </summary>
        public void Clear()
        {
            _timer.Clear();
        }

        private void Update()
        {
            _timer.Tick(Time.deltaTime);
        }
    }
}

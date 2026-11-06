using System.Collections.Generic;

namespace BlinkExpress.Ghost.Model
{
    /// <summary>
    /// 1回の走りの記録（位置と向きの時系列）。ゴーストの再生に使う。
    /// 記録は時間の順に並べる。途中の時間は、前後の記録を補間して求める。
    /// 仕様書「追ってくるもの・ゴール・評価・ゴースト」の節に対応する。
    /// </summary>
    public sealed class GhostTrack
    {
        /// <summary>記録する間隔（ゲーム内時間、秒）。仕様どおり 0.05 秒。</summary>
        public const float IntervalSec = 0.05f;

        private readonly List<GhostSample> _samples = new List<GhostSample>();

        /// <summary>記録の数。</summary>
        public int Count => _samples.Count;

        /// <summary>記録されている最後の時間（秒）。記録が無ければ 0。</summary>
        public float DurationSec => _samples.Count > 0 ? _samples[_samples.Count - 1].TimeSec : 0f;

        /// <summary>
        /// 記録を末尾に足す。時間は、それまでの記録より後であること。
        /// </summary>
        /// <param name="sample">記録する位置と向き。</param>
        public void Add(GhostSample sample)
        {
            _samples.Add(sample);
        }

        /// <summary>
        /// 記録の番号で取り出す。保存のときに使う。
        /// </summary>
        /// <param name="index">番号（0 から Count − 1）。</param>
        /// <returns>その番号の記録。</returns>
        public GhostSample At(int index)
        {
            return _samples[index];
        }

        /// <summary>
        /// 指定の時間の位置と向きを、前後の記録から補間して求める。
        /// 記録の範囲の外は、一番近い記録を使う。記録が無ければ false。
        /// </summary>
        /// <param name="timeSec">求める時間（秒）。</param>
        /// <param name="pose">補間した位置と向き。</param>
        /// <returns>求められたか。</returns>
        public bool TryGetPose(float timeSec, out GhostSample pose)
        {
            pose = default;
            if (_samples.Count == 0)
            {
                return false;
            }

            if (timeSec <= _samples[0].TimeSec)
            {
                pose = _samples[0];
                return true;
            }

            var last = _samples[_samples.Count - 1];
            if (timeSec >= last.TimeSec)
            {
                pose = last;
                return true;
            }

            // 時間を挟む2つの記録を探す
            var next = 1;
            while (_samples[next].TimeSec < timeSec)
            {
                next++;
            }

            var before = _samples[next - 1];
            var after = _samples[next];
            var span = after.TimeSec - before.TimeSec;
            var u = span > 0f ? (timeSec - before.TimeSec) / span : 0f;

            pose = new GhostSample(
                timeSec,
                before.X + (after.X - before.X) * u,
                before.Y + (after.Y - before.Y) * u,
                before.Z + (after.Z - before.Z) * u,
                LerpAngle(before.YawRad, after.YawRad, u));
            return true;
        }

        /// <summary>
        /// 角度を、近い方向へ向けて補間する（−π と π の境目をまたいでも、遠回りしない）。
        /// </summary>
        private static float LerpAngle(float from, float to, float u)
        {
            var diff = to - from;
            while (diff > System.Math.PI)
            {
                diff -= (float)(System.Math.PI * 2);
            }

            while (diff < -System.Math.PI)
            {
                diff += (float)(System.Math.PI * 2);
            }

            return from + diff * u;
        }
    }
}

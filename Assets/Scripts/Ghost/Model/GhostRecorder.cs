namespace BlinkExpress.Ghost.Model
{
    /// <summary>
    /// 走っている間の位置と向きを、一定の間隔（0.05 秒）で記録する。
    /// ゲーム内時間で進めるので、照準中のスローも記録に反映される。
    /// </summary>
    public sealed class GhostRecorder
    {
        private readonly GhostTrack _track;
        private float _nextSampleSec;

        /// <summary>
        /// 記録先の走りを指定して作る。
        /// </summary>
        /// <param name="track">記録を入れる走り。</param>
        public GhostRecorder(GhostTrack track)
        {
            _track = track;
        }

        /// <summary>
        /// 現在の時間と位置を、間隔に達していれば記録する。
        /// 時間が大きく進んだときは、間隔ごとに同じ位置を記録する。
        /// </summary>
        /// <param name="worldTimeSec">ゲーム内時間（秒）。</param>
        /// <param name="x">位置の X（m）。</param>
        /// <param name="y">位置の Y（m）。</param>
        /// <param name="z">位置の Z（m）。</param>
        /// <param name="yawRad">水平角（rad）。</param>
        public void Tick(float worldTimeSec, float x, float y, float z, float yawRad)
        {
            while (worldTimeSec >= _nextSampleSec)
            {
                _track.Add(new GhostSample(_nextSampleSec, x, y, z, yawRad));
                _nextSampleSec += GhostTrack.IntervalSec;
            }
        }
    }
}

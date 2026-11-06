namespace BlinkExpress.Ghost.Model
{
    /// <summary>
    /// ある時間の、プレイヤーの位置と向き。ゴーストの記録の1点。
    /// 位置は m、時間は秒、向きは水平角の rad（0 は +Z 方向）。
    /// </summary>
    public readonly struct GhostSample
    {
        /// <summary>
        /// 記録の1点を作る。
        /// </summary>
        /// <param name="timeSec">その時点のゲーム内時間（秒）。</param>
        /// <param name="x">位置の X（m）。</param>
        /// <param name="y">位置の Y（m）。</param>
        /// <param name="z">位置の Z（m）。</param>
        /// <param name="yawRad">水平角（rad）。</param>
        public GhostSample(float timeSec, float x, float y, float z, float yawRad)
        {
            TimeSec = timeSec;
            X = x;
            Y = y;
            Z = z;
            YawRad = yawRad;
        }

        /// <summary>ゲーム内時間（秒）。</summary>
        public float TimeSec { get; }

        /// <summary>位置の X（m）。</summary>
        public float X { get; }

        /// <summary>位置の Y（m）。</summary>
        public float Y { get; }

        /// <summary>位置の Z（m）。</summary>
        public float Z { get; }

        /// <summary>水平角（rad）。0 は +Z 方向。</summary>
        public float YawRad { get; }
    }
}

namespace BlinkExpress.Player.Model
{
    /// <summary>
    /// 仕掛けに触れている状態。View が位置と衝突から作り、Model に渡す。
    /// 仕様書「スライディング・レール・キノコ」「トラップと障害物」の節に対応する。
    /// </summary>
    public readonly struct HazardContact
    {
        /// <summary>
        /// 仕掛けの状態を作る。
        /// </summary>
        /// <param name="onMud">泥（減速床）の上にいるか。</param>
        /// <param name="mushroomNear">跳ねるキノコの上にいるか（踏んだ瞬間を判定する）。</param>
        /// <param name="onRail">グラインドレールに乗っているか。</param>
        /// <param name="railDirX">レールに沿った進む向きの X 成分（水平、正規化されていること）。</param>
        /// <param name="railDirZ">レールに沿った進む向きの Z 成分（水平、正規化されていること）。</param>
        /// <param name="obstacleIntoDot">登れない障害物に当たったときの、進む向きと面の内積（当たっていなければ 0）。</param>
        /// <param name="ceilingHit">天井に当たったか。</param>
        public HazardContact(bool onMud, bool mushroomNear, bool onRail, float railDirX, float railDirZ, float obstacleIntoDot, bool ceilingHit)
        {
            OnMud = onMud;
            MushroomNear = mushroomNear;
            OnRail = onRail;
            RailDirX = railDirX;
            RailDirZ = railDirZ;
            ObstacleIntoDot = obstacleIntoDot;
            CeilingHit = ceilingHit;
        }

        /// <summary>泥（減速床）の上にいるか。</summary>
        public bool OnMud { get; }

        /// <summary>跳ねるキノコの上にいるか。</summary>
        public bool MushroomNear { get; }

        /// <summary>グラインドレールに乗っているか。</summary>
        public bool OnRail { get; }

        /// <summary>レールに沿った進む向きの X 成分（水平）。</summary>
        public float RailDirX { get; }

        /// <summary>レールに沿った進む向きの Z 成分（水平）。</summary>
        public float RailDirZ { get; }

        /// <summary>登れない障害物に当たったときの、進む向きと面の内積。当たっていなければ 0。</summary>
        public float ObstacleIntoDot { get; }

        /// <summary>天井に当たったか。</summary>
        public bool CeilingHit { get; }
    }
}

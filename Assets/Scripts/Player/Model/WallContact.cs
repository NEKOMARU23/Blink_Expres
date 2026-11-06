namespace BlinkExpress.Player.Model
{
    /// <summary>
    /// 壁に触れている状態。View が衝突の情報から作り、Model に渡す。
    /// 壁かどうか（登れるか）は View が判断し、前から当たったか・横から当たったかは Model が判断する。
    /// </summary>
    public readonly struct WallContact
    {
        /// <summary>
        /// 壁の接触を作る。
        /// </summary>
        /// <param name="touching">登れる壁に触れているか。</param>
        /// <param name="normalX">壁の法線の X 成分（壁から体へ向かう向き）。</param>
        /// <param name="normalZ">壁の法線の Z 成分（壁から体へ向かう向き）。</param>
        /// <param name="topReached">壁の上端に近づいて、登りきったか。</param>
        /// <param name="heightAboveGroundM">地面からの高さ（m）。横から壁走りを始める高さの判定に使う。</param>
        public WallContact(bool touching, float normalX, float normalZ, bool topReached, float heightAboveGroundM)
        {
            Touching = touching;
            NormalX = normalX;
            NormalZ = normalZ;
            TopReached = topReached;
            HeightAboveGroundM = heightAboveGroundM;
        }

        /// <summary>登れる壁に触れているか。</summary>
        public bool Touching { get; }

        /// <summary>壁の法線の X 成分（壁から体へ向かう向き）。</summary>
        public float NormalX { get; }

        /// <summary>壁の法線の Z 成分（壁から体へ向かう向き）。</summary>
        public float NormalZ { get; }

        /// <summary>壁の上端に登りきったか。</summary>
        public bool TopReached { get; }

        /// <summary>地面からの高さ（m）。</summary>
        public float HeightAboveGroundM { get; }
    }
}

using System;

namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// 投げたビーコンの飛び方の Model。放物線を、時間の刻みごとに進める。
    /// 速さは m/s、距離は m、時間は秒。仕様書「ビーコンの弾道」の節に対応する。
    /// </summary>
    public sealed class BeaconFlight
    {
        private readonly float _gravityMps2;
        private float _x;
        private float _y;
        private float _z;
        private float _vx;
        private float _vy;
        private float _vz;
        private float _remainingSec;

        /// <summary>
        /// 投げた直後のビーコンを作る。
        /// </summary>
        /// <param name="originX">投げた位置の X（m）。</param>
        /// <param name="originY">投げた位置の Y（m）。</param>
        /// <param name="originZ">投げた位置の Z（m）。</param>
        /// <param name="dirX">投げる向きの X 成分（正規化されていること）。</param>
        /// <param name="dirY">投げる向きの Y 成分（正規化されていること）。</param>
        /// <param name="dirZ">投げる向きの Z 成分（正規化されていること）。</param>
        /// <param name="speedMps">初速（m/s）。<see cref="InitialSpeedMps"/> で求める。</param>
        /// <param name="gravityMps2">ビーコンの重力（m/s²）。</param>
        /// <param name="lifetimeSec">寿命（秒）。過ぎると何にも当たらず消える。</param>
        public BeaconFlight(float originX, float originY, float originZ, float dirX, float dirY, float dirZ, float speedMps, float gravityMps2, float lifetimeSec)
        {
            _x = originX;
            _y = originY;
            _z = originZ;
            _vx = dirX * speedMps;
            _vy = dirY * speedMps;
            _vz = dirZ * speedMps;
            _gravityMps2 = gravityMps2;
            _remainingSec = lifetimeSec;
        }

        /// <summary>
        /// 初速（m/s）を求める。基本の速さに、投げる時点の自分の速さの半分を足す。
        /// 仕様書「ビーコンの弾道」の初速の式。
        /// </summary>
        /// <param name="playerSpeedMps">投げる時点のプレイヤーの速さ（m/s）。</param>
        /// <param name="baseSpeedMps">基本の初速（m/s）。</param>
        /// <returns>初速（m/s）。</returns>
        public static float InitialSpeedMps(float playerSpeedMps, float baseSpeedMps)
        {
            return baseSpeedMps + playerSpeedMps * 0.5f;
        }

        /// <summary>現在の X 座標（m）。</summary>
        public float X => _x;

        /// <summary>現在の Y 座標（m）。</summary>
        public float Y => _y;

        /// <summary>現在の Z 座標（m）。</summary>
        public float Z => _z;

        /// <summary>寿命が残っているか。false になったら、何にも当たらなかったものとして消す。</summary>
        public bool IsAlive => _remainingSec > 0f;

        /// <summary>
        /// 時間を dt（秒）だけ進める。重力は速度の Y 成分を下げ、位置はその速度で動かす。
        /// </summary>
        /// <param name="dt">進める時間（秒）。</param>
        public void Advance(float dt)
        {
            _vy -= _gravityMps2 * dt;
            _x += _vx * dt;
            _y += _vy * dt;
            _z += _vz * dt;
            _remainingSec -= dt;
        }
    }
}

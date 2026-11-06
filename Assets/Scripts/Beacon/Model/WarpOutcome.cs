namespace BlinkExpress.Beacon.Model
{
    /// <summary>
    /// ビーコンが当たったものの種類。View が Raycast の結果を、この種類に変換して Model に渡す。
    /// </summary>
    public enum HitKind
    {
        /// <summary>何にも当たらなかった。</summary>
        None,

        /// <summary>地面や箱の上面。</summary>
        Ground,

        /// <summary>登れる壁の側面。</summary>
        Wall,

        /// <summary>天井（箱の下面）。</summary>
        Ceiling,

        /// <summary>グラインドレール。</summary>
        Rail,

        /// <summary>紫の壁や倒木の側面など、ワープできない面。</summary>
        NoWarpSurface,

        /// <summary>敵。速さが足りるかどうかで結果が変わる。</summary>
        Enemy,
    }

    /// <summary>
    /// ビーコンを投げた結果。予測線の色と文言、ワープ後の状態を決めるのに使う。
    /// 仕様書「着弾したものごとの結果」の表に対応する。
    /// </summary>
    public enum WarpOutcome
    {
        /// <summary>何もない。届かない（ワープしない）。</summary>
        Miss,

        /// <summary>地面に着地する。</summary>
        Ground,

        /// <summary>壁に張り付いて壁滑走する。</summary>
        WallSlide,

        /// <summary>天井に当たり、速さが減って落下する。</summary>
        Ceiling,

        /// <summary>敵を粉砕する（速さが足りる）。</summary>
        Smash,

        /// <summary>敵に弾かれる（速さが足りない）。速さは半分になる。</summary>
        Bounce,

        /// <summary>レールに乗る。</summary>
        Rail,

        /// <summary>ワープできない面に当たった。</summary>
        NoWarp,
    }
}

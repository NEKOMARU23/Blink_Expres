using BlinkExpress.Beacon.Model;
using NUnit.Framework;
using UnityEngine;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// ビーコンがレールに着弾したとみなす判定を確かめるテスト。
    /// </summary>
    public sealed class RailWarpTests
    {
        private static readonly Vector3 Start = new Vector3(0f, 1f, 100f);
        private static readonly Vector3 End = new Vector3(0f, 1f, 140f);

        [Test]
        public void CatchesBeaconNearTheMiddleOfRail()
        {
            var caught = RailWarp.TryCatch(Start, End, new Vector3(0.5f, 1.5f, 120f), out var closest, out var distance);

            Assert.That(caught, Is.True);
            Assert.That(closest.z, Is.EqualTo(120f).Within(0.001f));
            Assert.That(distance, Is.EqualTo(Mathf.Sqrt(0.5f)).Within(0.001f));
        }

        [Test]
        public void MissesBeaconFartherThanCatchRadius()
        {
            Assert.That(RailWarp.TryCatch(Start, End, new Vector3(0f, 2f, 120f), out _, out _), Is.False);
        }

        [Test]
        public void CatchRadiusMatchesSpec()
        {
            // デモ版の 0.9 m に合わせる
            Assert.That(RailWarp.CatchRadiusM, Is.EqualTo(0.9f));
            Assert.That(RailWarp.TryCatch(Start, End, new Vector3(0.89f, 1f, 120f), out _, out _), Is.True);
            Assert.That(RailWarp.TryCatch(Start, End, new Vector3(0.91f, 1f, 120f), out _, out _), Is.False);
        }

        [Test]
        public void ClampsToTheEndOfRail()
        {
            // 終点の少し先でも、終点から 0.9 m 以内なら着弾する。着地点は終点
            var caught = RailWarp.TryCatch(Start, End, new Vector3(0f, 1f, 140.5f), out var closest, out _);

            Assert.That(caught, Is.True);
            Assert.That(closest.z, Is.EqualTo(140f).Within(0.001f));
        }

        [Test]
        public void MissesBeyondTheEndByMoreThanCatchRadius()
        {
            Assert.That(RailWarp.TryCatch(Start, End, new Vector3(0f, 1f, 141.5f), out _, out _), Is.False);
        }

        [Test]
        public void ZeroLengthRailIsNeverCaught()
        {
            Assert.That(RailWarp.TryCatch(Start, Start, Start, out _, out _), Is.False);
        }
    }
}

using System;
using BlinkExpress.Ghost.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// ゴーストの記録（間隔、補間、角度の境目）が仕様書どおりかを確かめるテスト。
    /// </summary>
    public sealed class GhostTrackTests
    {
        [Test]
        public void RecorderSamplesEveryFiftyMilliseconds()
        {
            // 0.49 秒まで走ると、0.00、0.05、…、0.45 の 10 点が記録される
            var track = new GhostTrack();
            var recorder = new GhostRecorder(track);

            for (var i = 0; i <= 49; i++)
            {
                recorder.Tick(i * 0.01f, i, 0f, 0f, 0f);
            }

            Assert.That(track.Count, Is.EqualTo(10));
            Assert.That(track.At(1).TimeSec, Is.EqualTo(0.05f).Within(0.0001f));
        }

        [Test]
        public void PoseIsInterpolatedBetweenSamples()
        {
            // 0 秒で x = 0、0.1 秒で x = 10 の記録。0.05 秒では x = 5
            var track = new GhostTrack();
            track.Add(new GhostSample(0f, 0f, 0f, 0f, 0f));
            track.Add(new GhostSample(0.1f, 10f, 0f, 0f, 0f));

            Assert.That(track.TryGetPose(0.05f, out var pose), Is.True);
            Assert.That(pose.X, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void PoseOutsideRangeUsesNearestSample()
        {
            var track = new GhostTrack();
            track.Add(new GhostSample(0f, 1f, 0f, 0f, 0f));
            track.Add(new GhostSample(1f, 2f, 0f, 0f, 0f));

            track.TryGetPose(-5f, out var before);
            track.TryGetPose(9f, out var after);

            Assert.That(before.X, Is.EqualTo(1f));
            Assert.That(after.X, Is.EqualTo(2f));
        }

        [Test]
        public void EmptyTrackHasNoPose()
        {
            var track = new GhostTrack();

            Assert.That(track.TryGetPose(0f, out _), Is.False);
        }

        [Test]
        public void YawInterpolationTakesShortestWayAcrossPi()
        {
            // 向きが +170° から −170° へ変わるとき、途中は 180°（+π）をまたぐ。遠回りしない
            var track = new GhostTrack();
            var from = (float)(170 * Math.PI / 180);
            var to = (float)(-170 * Math.PI / 180);
            track.Add(new GhostSample(0f, 0f, 0f, 0f, from));
            track.Add(new GhostSample(1f, 0f, 0f, 0f, to));

            track.TryGetPose(0.5f, out var mid);

            // 中間は ±180° 付近（|yaw| が大きい）で、0° 付近にはならない
            Assert.That(Math.Abs(mid.YawRad), Is.GreaterThan((float)(175 * Math.PI / 180)));
        }
    }
}

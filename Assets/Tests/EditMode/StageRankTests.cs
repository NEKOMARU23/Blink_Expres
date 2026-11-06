using BlinkExpress.Stage.Model;
using NUnit.Framework;

namespace BlinkExpress.Tests
{
    /// <summary>
    /// クリアのランク（S〜C）の判定が、仕様書の表どおりかを確かめるテスト。
    /// </summary>
    public sealed class StageRankTests
    {
        [Test]
        public void FastAndManyCoinsIsS()
        {
            Assert.That(StageRank.Decide(47f, 0.6f), Is.EqualTo("S"));
        }

        [Test]
        public void SlowerThanSIsA()
        {
            // 48 秒を超えると A。コインが 60% でも、タイムで S にはならない
            Assert.That(StageRank.Decide(50f, 0.6f), Is.EqualTo("A"));
            Assert.That(StageRank.Decide(57f, 0.35f), Is.EqualTo("A"));
        }

        [Test]
        public void FewCoinsLowersRank()
        {
            // 速くても、コインが少ないと S にならない（40% なら A）
            Assert.That(StageRank.Decide(40f, 0.4f), Is.EqualTo("A"));
            Assert.That(StageRank.Decide(40f, 0.2f), Is.EqualTo("B"));
        }

        [Test]
        public void SlowClearIsBOrC()
        {
            Assert.That(StageRank.Decide(70f, 0f), Is.EqualTo("B"));
            Assert.That(StageRank.Decide(80f, 1f), Is.EqualTo("C"));
        }
    }
}

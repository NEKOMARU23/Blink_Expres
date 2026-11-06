using BlinkExpress.Core;
using UnityEngine;

namespace BlinkExpress.Stage.View
{
    /// <summary>
    /// ステージの進み具合（コインの取得数など）を持つ、ステージに1つだけのコンポーネント。
    /// 結果の画面（ランクやタイム）から参照する。
    /// </summary>
    public sealed class StageProgress : Singleton<StageProgress>
    {
        private int _totalCoins;
        private int _coinsCollected;

        /// <summary>このステージにあるコインの数。</summary>
        public int TotalCoins => _totalCoins;

        /// <summary>取ったコインの数。</summary>
        public int CoinsCollected => _coinsCollected;

        /// <summary>
        /// 登録されるときに、ステージにあるコインの数を数える。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            _totalCoins = FindObjectsByType<CoinView>(FindObjectsSortMode.None).Length;
        }

        /// <summary>
        /// コインを増やす（取得、粉砕で出たコイン）。
        /// </summary>
        /// <param name="count">増やす数。</param>
        public void AddCoins(int count)
        {
            _coinsCollected += count;
        }
    }
}

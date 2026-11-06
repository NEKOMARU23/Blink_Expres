using UnityEngine;

namespace BlinkExpress.Stage.View
{
    /// <summary>
    /// コインの View。Trigger の範囲に入ったプレイヤーが取る。取ると消えて、ステージの集計に数える。
    /// 見た目は仮の円盤。
    /// </summary>
    public sealed class CoinView : MonoBehaviour
    {
        private bool _collected;

        /// <summary>取得済みか。</summary>
        public bool IsCollected => _collected;

        private void Awake()
        {
            var sphere = GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = gameObject.AddComponent<SphereCollider>();
            }

            sphere.isTrigger = true;
            sphere.radius = 0.6f;
        }

        private void Update()
        {
            if (!_collected)
            {
                transform.Rotate(0f, 180f * Time.deltaTime, 0f);
            }
        }

        /// <summary>
        /// 取る。まだ取っていなければ取り、ステージの集計に数えて、true を返す。
        /// </summary>
        /// <returns>このときに初めて取ったか。</returns>
        public bool TryCollect()
        {
            if (_collected)
            {
                return false;
            }

            _collected = true;
            gameObject.SetActive(false);
            if (StageProgress.Instance != null)
            {
                StageProgress.Instance.AddCoins(1);
            }

            return true;
        }
    }
}

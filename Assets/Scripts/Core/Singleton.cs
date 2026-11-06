using UnityEngine;

namespace BlinkExpress.Core
{
    /// <summary>
    /// シングルトンの基底クラス。ゲーム全体で1つだけあるコンポーネントは、これを継承して作る。
    /// 同じ型が2つ目に置かれたら、後から来たほうを破棄する。
    /// </summary>
    /// <typeparam name="T">継承する型自身。</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>この型の唯一のインスタンス。シーンに無ければ null。</summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// 最初に登録される。すでに別のインスタンスがあれば、このオブジェクトを破棄する。
        /// 継承先で Awake を書くときは、base.Awake() を先に呼ぶ。
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
        }

        /// <summary>
        /// 破棄されるとき、自分が登録されていれば登録を外す。
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

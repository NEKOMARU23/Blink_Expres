using BlinkExpress.Enemy.Model;
using UnityEngine;

namespace BlinkExpress.Enemy.View
{
    /// <summary>
    /// 敵の View。硬さに応じた当たりの範囲（Trigger のコライダー）を持ち、粉砕されると消える。
    /// 見た目は仮の立方体。動きは、左右に揺れるか、動かないかの2種類。
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private EnemyTier _tier = EnemyTier.Soft;

        /// <summary>左右に揺れる幅（m）。0 なら動かない。</summary>
        [SerializeField] private float _swayRangeM;

        /// <summary>左右に揺れる速さ（rad/s）。</summary>
        [SerializeField] private float _swaySpeedRadPerSec = 1.3f;

        private Vector3 _home;
        private bool _alive = true;

        /// <summary>敵の硬さ。</summary>
        public EnemyTier Tier => _tier;

        /// <summary>敵の当たりの半径（m）。</summary>
        public float RadiusM => EnemyRules.RadiusM(_tier);

        /// <summary>敵の中心（ワールド座標）。足元から半径の分だけ上。</summary>
        public Vector3 Center => transform.position + Vector3.up * RadiusM;

        /// <summary>まだ粉砕されていないか。</summary>
        public bool IsAlive => _alive;

        private void Awake()
        {
            _home = transform.position;
            EnsureTrigger();
        }

        private void Update()
        {
            if (!_alive || _swayRangeM <= 0f)
            {
                return;
            }

            var offset = Mathf.Sin(Time.time * _swaySpeedRadPerSec) * _swayRangeM;
            transform.position = _home + Vector3.right * offset;
        }

        /// <summary>
        /// 粉砕されたときに呼ぶ。見えなくして、当たりも無効にする。
        /// </summary>
        public void Smash()
        {
            _alive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// リスタートのとき、粉砕された敵を元の位置に戻して、また当たるようにする。
        /// </summary>
        public void ResetForRun()
        {
            _alive = true;
            transform.position = _home;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 当たりの Trigger コライダーが無ければ、硬さに合わせて付ける。
        /// </summary>
        private void EnsureTrigger()
        {
            var sphere = GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = gameObject.AddComponent<SphereCollider>();
            }

            sphere.isTrigger = true;
            sphere.radius = RadiusM;
            sphere.center = Vector3.up * RadiusM;
        }
    }
}

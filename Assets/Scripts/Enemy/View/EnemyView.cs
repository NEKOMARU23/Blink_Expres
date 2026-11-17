using BlinkExpress.Beacon.View;
using BlinkExpress.Enemy.Model;
using BlinkExpress.Player.View;
using UnityEngine;

namespace BlinkExpress.Enemy.View
{
    /// <summary>
    /// 敵の View。硬さに応じた当たりの範囲（Trigger のコライダー）を持ち、粉砕されると消える。
    /// 見た目は仮の立方体。動きは、動かない・左右に揺れる・逃げる、の3種類。
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private EnemyTier _tier = EnemyTier.Soft;

        /// <summary>左右に揺れる幅（m）。0 なら動かない。</summary>
        [SerializeField] private float _swayRangeM;

        /// <summary>左右に揺れる速さ（rad/s）。</summary>
        [SerializeField] private float _swaySpeedRadPerSec = 1.3f;

        /// <summary>true なら、プレイヤーが前方 35 m 以内に来たら横へ逃げる（揺れる設定より優先）。</summary>
        [SerializeField] private bool _fleesFromPlayer;

        private Vector3 _home;
        private bool _alive = true;
        private PlayerView _player;
        private BeaconView _beacon;

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

            // 逃げる敵だけ、プレイヤーと照準の状態を調べておく（毎フレーム探さない）
            if (_fleesFromPlayer)
            {
                _player = FindFirstObjectByType<PlayerView>();
                _beacon = FindFirstObjectByType<BeaconView>();
            }
        }

        private void Update()
        {
            if (!_alive)
            {
                return;
            }

            if (_fleesFromPlayer)
            {
                Flee();
                return;
            }

            if (_swayRangeM <= 0f)
            {
                return;
            }

            var offset = Mathf.Sin(Time.time * _swaySpeedRadPerSec) * _swayRangeM;
            transform.position = _home + Vector3.right * offset;
        }

        /// <summary>
        /// プレイヤーから横へ逃げる。照準中は世界が遅くなるので、逃げる量もそれに合わせる。
        /// プレイヤーがいなければ、逃げずにその場にいる（ゲームは止まらない）。
        /// </summary>
        private void Flee()
        {
            if (_player == null)
            {
                return;
            }

            var worldDt = Time.deltaTime * (_beacon != null ? _beacon.WorldTimeScale : 1f);
            var position = transform.position;
            var playerPosition = _player.transform.position;
            position.x = EnemyFlee.StepX(position.x, position.z, playerPosition.x, playerPosition.z, worldDt);
            transform.position = position;
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

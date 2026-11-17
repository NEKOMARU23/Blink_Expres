using BlinkExpress.Core;
using BlinkExpress.Effects.Model;
using UnityEngine;
using UnityEngine.UI;

namespace BlinkExpress.Stage.View
{
    /// <summary>
    /// 粉砕で出たコインを、敵のいた場所から画面上のコイン表示へ飛ばす（仕様書「敵・コイン・トラップ」の節）。
    /// 見た目だけの演出で、コインの数そのものは <see cref="StageProgress"/> が持つ。
    /// ステージに1つだけ置く。Image は決まった数を使い回す。
    /// </summary>
    public sealed class CoinFlyView : Singleton<CoinFlyView>
    {
        private const int PoolSize = 32;
        private const float StaggerSec = 0.06f;
        private const float CoinSizePx = 22f;

        private struct Flight
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Start;
            public float AgeSec;
            public bool Active;
        }

        private readonly Flight[] _flights = new Flight[PoolSize];
        private Vector2 _targetFromTopLeftPx = new Vector2(230f, 32f);
        private Camera _camera;

        /// <summary>
        /// コインが飛んでいく先を、画面の左上からの距離（ピクセル）で決める。
        /// </summary>
        /// <param name="fromTopLeftPx">画面の左上から、右へ・下へ何ピクセルか。</param>
        public void SetTarget(Vector2 fromTopLeftPx)
        {
            _targetFromTopLeftPx = fromTopLeftPx;
        }

        /// <summary>
        /// コインを飛ばす。数が多いときは、少しずつずらして飛ばす。
        /// </summary>
        /// <param name="worldPosition">出てくる場所（ワールド座標）。</param>
        /// <param name="count">飛ばすコインの数。</param>
        public void Fly(Vector3 worldPosition, int count)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            var screen = _camera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var index = FindFree();
                if (index < 0)
                {
                    return;
                }

                // 出る順に少しずつ遅らせるため、マイナスの経過時間から始める
                _flights[index].Start = new Vector2(screen.x, screen.y) + Random.insideUnitCircle * 25f;
                _flights[index].AgeSec = -i * StaggerSec;
                _flights[index].Active = true;
            }
        }

        /// <summary>
        /// 飛んでいるコインをすべて消す（リスタートのとき）。
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _flights.Length; i++)
            {
                _flights[i].Active = false;
                _flights[i].Image.enabled = false;
            }
        }

        /// <summary>
        /// 登録されるときに、コインを飛ばす Image を作っておく。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            var canvasGo = new GameObject("CoinFlyCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            for (var i = 0; i < _flights.Length; i++)
            {
                var go = new GameObject("Coin" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvasGo.transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(CoinSizePx, CoinSizePx);
                var image = go.GetComponent<Image>();
                image.color = new Color(1f, 0.82f, 0.2f);
                image.raycastTarget = false;
                image.enabled = false;
                _flights[i].Rect = rect;
                _flights[i].Image = image;
            }
        }

        private void Update()
        {
            var target = new Vector2(_targetFromTopLeftPx.x, Screen.height - _targetFromTopLeftPx.y);
            for (var i = 0; i < _flights.Length; i++)
            {
                if (!_flights[i].Active)
                {
                    continue;
                }

                // 粉砕で世界が止まっている間も、コインの演出は実時間で進める
                _flights[i].AgeSec += Time.deltaTime;
                var t = _flights[i].AgeSec / SmashEffectRules.CoinFlySec;
                if (t < 0f)
                {
                    _flights[i].Image.enabled = false;
                    continue;
                }

                if (t >= 1f)
                {
                    _flights[i].Active = false;
                    _flights[i].Image.enabled = false;
                    continue;
                }

                var progress = SmashEffectRules.CoinFlyProgress(t);
                var position = Vector2.Lerp(_flights[i].Start, target, progress);
                // 一直線ではなく、少し上へふくらむ弧にする
                position.y += Mathf.Sin(progress * Mathf.PI) * 80f;
                _flights[i].Rect.anchoredPosition = position;
                _flights[i].Image.enabled = true;
            }
        }

        private int FindFree()
        {
            for (var i = 0; i < _flights.Length; i++)
            {
                if (!_flights[i].Active)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

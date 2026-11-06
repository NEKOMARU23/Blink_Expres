using System.Collections.Generic;
using BlinkExpress.Beacon.Model;
using BlinkExpress.Beacon.Presenter;
using BlinkExpress.Player.View;
using BlinkExpress.Tuning;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlinkExpress.Beacon.View
{
    /// <summary>
    /// ビーコンの View。マウスの入力を読み、Raycast で当たりを調べ、予測線と飛んでいるビーコンを描く。
    /// 判断は BeaconPresenter が行う。
    /// </summary>
    public sealed class BeaconView : MonoBehaviour, IBeaconView
    {
        [SerializeField] private BeaconTuning _tuning;
        [SerializeField] private PlayerView _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _warpMask = ~0;

        private BeaconPresenter _presenter;
        private LineRenderer _preview;
        private GameObject _beaconVisual;
        private Text _label;

        /// <summary>ゲーム内時間の速さ（0〜1）。照準中は遅くなり、プレイヤーの移動もこれに合わせる。</summary>
        public float WorldTimeScale => _presenter != null ? _presenter.WorldTimeScale : 1f;

        /// <summary>
        /// ゲージを回復する（コイン、粉砕など）。
        /// </summary>
        /// <param name="amount">回復する量（0〜1）。</param>
        public void RefillGauge(float amount)
        {
            if (_presenter != null)
            {
                _presenter.RefillGauge(amount);
            }
        }

        private void Awake()
        {
            // 無いとビーコンが動かないため、ここで止めて原因を出す
            if (_tuning == null || _player == null || _camera == null)
            {
                Debug.LogError($"{name}: BeaconTuning、PlayerView、Camera のどれかが設定されていません。", this);
                enabled = false;
                return;
            }

            _presenter = new BeaconPresenter(this, _tuning.ToBeaconSettings());
            CreatePreviewLine();
            CreateBeaconVisual();
            CreateLabel();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var input = new BeaconInput(
                mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.wasReleasedThisFrame,
                mouse.rightButton.wasPressedThisFrame,
                _camera.transform.forward);
            var state = new BeaconPlayerState(_player.Position, _player.SpeedMps);

            _presenter.Tick(input, state, Time.deltaTime);
        }

        /// <summary>
        /// 2 点の間で最初に当たったものを調べる。プレイヤー自身は無視する。
        /// </summary>
        public bool Raycast(Vector3 from, Vector3 to, out BeaconHit hit)
        {
            hit = default;
            var delta = to - from;
            var length = delta.magnitude;
            if (length <= 0f)
            {
                return false;
            }

            var hits = Physics.RaycastAll(from, delta / length, length, _warpMask, QueryTriggerInteraction.Ignore);
            var found = false;
            var nearest = default(RaycastHit);
            foreach (var candidate in hits)
            {
                if (candidate.collider.transform.IsChildOf(_player.transform))
                {
                    continue;
                }

                if (!found || candidate.distance < nearest.distance)
                {
                    nearest = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            hit = new BeaconHit(ClassifySurface(nearest), nearest.point, nearest.normal, 0f);
            return true;
        }

        /// <summary>
        /// 当たった面の種類を決める。WarpSurface が付いていればそれを使い、無ければ法線の向きで決める。
        /// </summary>
        private static HitKind ClassifySurface(RaycastHit hit)
        {
            if (hit.collider.TryGetComponent(out WarpSurface surface))
            {
                return surface.Kind;
            }

            if (hit.normal.y > 0.5f)
            {
                return HitKind.Ground;
            }

            if (hit.normal.y < -0.5f)
            {
                return HitKind.Ceiling;
            }

            return HitKind.Wall;
        }

        /// <summary>
        /// 照準の予測線と文言を表示する。
        /// </summary>
        public void ShowAimPreview(IReadOnlyList<Vector3> points, AimFeedback feedback)
        {
            _preview.positionCount = points.Count;
            for (var i = 0; i < points.Count; i++)
            {
                _preview.SetPosition(i, points[i]);
            }

            var color = ToneColor(feedback.Tone);
            _preview.startColor = color;
            _preview.endColor = color;
            _preview.enabled = true;

            _label.text = feedback.Text;
            _label.color = color;
            _label.enabled = true;
        }

        /// <summary>照準の予測線と文言を消す。</summary>
        public void HideAimPreview()
        {
            _preview.enabled = false;
            _label.enabled = false;
        }

        /// <summary>飛んでいるビーコンを、指定の位置に表示する。</summary>
        public void ShowBeacon(Vector3 position)
        {
            _beaconVisual.SetActive(true);
            _beaconVisual.transform.position = position;
        }

        /// <summary>飛んでいるビーコンの表示を消す。</summary>
        public void HideBeacon()
        {
            _beaconVisual.SetActive(false);
        }

        /// <summary>ワープでプレイヤーを移動させる。</summary>
        public void WarpPlayer(Vector3 position, Vector3 direction, float speedMps, WarpOutcome outcome)
        {
            _player.WarpTo(position, direction, speedMps, outcome);
        }

        /// <summary>プレイヤーの速さだけを設定する。</summary>
        public void SetPlayerSpeed(float speedMps)
        {
            _player.SetSpeed(speedMps);
        }

        private void CreatePreviewLine()
        {
            var go = new GameObject("AimPreview");
            _preview = go.AddComponent<LineRenderer>();
            _preview.useWorldSpace = true;
            _preview.startWidth = 0.25f;
            _preview.endWidth = 0.25f;

            // 色を付けるため、Unlit のマテリアルを使う（無ければ標準のスプライト用）
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _preview.material = new Material(shader);
            _preview.enabled = false;
        }

        private void CreateBeaconVisual()
        {
            _beaconVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _beaconVisual.name = "Beacon";
            Destroy(_beaconVisual.GetComponent<Collider>());
            _beaconVisual.transform.localScale = Vector3.one * 0.6f;
            _beaconVisual.GetComponent<Renderer>().material.color = new Color(0.37f, 0.94f, 1f);
            _beaconVisual.SetActive(false);
        }

        private void CreateLabel()
        {
            var canvasGo = new GameObject("WarpHud", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var textGo = new GameObject("AimLabel", typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);

            _label = textGo.GetComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 28;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.UpperCenter;

            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(400f, 50f);

            _label.enabled = false;
        }

        private static Color ToneColor(AimTone tone)
        {
            switch (tone)
            {
                case AimTone.Cyan:
                    return new Color(0.37f, 0.94f, 1f);
                case AimTone.Green:
                    return new Color(0.29f, 0.89f, 0.48f);
                case AimTone.Yellow:
                    return new Color(1f, 0.82f, 0.25f);
                case AimTone.Red:
                    return new Color(1f, 0.3f, 0.37f);
                case AimTone.White:
                    return Color.white;
                case AimTone.Orange:
                    return new Color(1f, 0.61f, 0.24f);
                default:
                    return new Color(0.66f, 0.7f, 0.78f);
            }
        }
    }
}

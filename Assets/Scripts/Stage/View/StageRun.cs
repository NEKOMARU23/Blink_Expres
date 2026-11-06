using BlinkExpress.Enemy.View;
using BlinkExpress.Ghost.Model;
using BlinkExpress.Ghost.Store;
using BlinkExpress.Ghost.View;
using BlinkExpress.Player.View;
using BlinkExpress.Stage.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlinkExpress.Stage.View
{
    /// <summary>
    /// 1回のステージの進行を担当する View。タイムを数え、追ってくる魔物を動かし、
    /// ゴールとの判定、クリアのランク、ゴーストの記録と再生を行う。
    /// 見た目は仮の文字と立方体。仕様書「追ってくるもの・ゴール・評価・ゴースト」の節に対応する。
    /// </summary>
    public sealed class StageRun : MonoBehaviour
    {
        /// <summary>ステージの結果。</summary>
        public enum Result
        {
            /// <summary>走っている。</summary>
            Running,

            /// <summary>ゴールした。</summary>
            Cleared,

            /// <summary>追ってくるものに追いつかれた。</summary>
            Caught,
        }

        [SerializeField] private Transform _chaser;
        [SerializeField] private float _chaserSpeedMps = 26f;
        [SerializeField] private float _chaserDelaySec = 1.5f;
        [SerializeField] private float _chaserStartZ = -50f;
        [SerializeField] private float _chaserCatchGapM = 1.5f;
        [SerializeField] private float _goalZ = 1500f;

        private PlayerView _player;
        private GhostView _ghost;
        private GhostTrack _currentTrack;
        private GhostRecorder _recorder;
        private GhostTrack _bestTrack;
        private float _bestTimeSec;
        private bool _hasBest;
        private float _clockSec;
        private float _chaserZ;
        private Text _hud;
        private Text _resultText;

        /// <summary>今の走りの結果。</summary>
        public Result State { get; private set; }

        /// <summary>経過時間（秒）。</summary>
        public float ClockSec => _clockSec;

        private void Awake()
        {
            _player = FindFirstObjectByType<PlayerView>();
            _ghost = GetComponentInChildren<GhostView>();
            CreateHud();
        }

        private void Start()
        {
            StartRun();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                StartRun();
                return;
            }

            if (State != Result.Running || _player == null)
            {
                return;
            }

            _clockSec += Time.deltaTime;
            var position = _player.Position;
            _recorder.Tick(_clockSec, position.x, position.y, position.z, _player.YawRad);
            ShowGhost();

            if (_clockSec > _chaserDelaySec)
            {
                _chaserZ += _chaserSpeedMps * Time.deltaTime;
            }

            if (_chaser != null)
            {
                _chaser.position = new Vector3(0f, 0f, _chaserZ);
            }

            if (_chaserZ >= position.z - _chaserCatchGapM)
            {
                Finish(Result.Caught);
            }
            else if (position.z >= _goalZ)
            {
                Finish(Result.Cleared);
            }

            UpdateHud();
        }

        /// <summary>
        /// 走りを最初からやり直す。保存されている自己ベストを読み込み、ゴーストを用意する。
        /// </summary>
        public void StartRun()
        {
            State = Result.Running;
            _clockSec = 0f;
            _chaserZ = _chaserStartZ;
            ResetCoins();
            ResetEnemies();
            _currentTrack = new GhostTrack();
            _recorder = new GhostRecorder(_currentTrack);
            _hasBest = GhostStore.TryLoad(out _bestTrack, out _bestTimeSec);
            _resultText.enabled = false;
            if (_chaser != null)
            {
                _chaser.position = new Vector3(0f, 0f, _chaserZ);
            }

            if (_player != null)
            {
                _player.ResetToStart(Vector3.zero);
            }

            UpdateHud();
        }

        /// <summary>
        /// 粉砕された敵を、元の位置に戻す（リスタートのため）。
        /// </summary>
        private static void ResetEnemies()
        {
            foreach (var enemy in FindObjectsByType<EnemyView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.ResetForRun();
            }
        }

        /// <summary>
        /// 取られたコインを元に戻し、取ったコインの数を 0 にする（リスタートのため）。
        /// </summary>
        private static void ResetCoins()
        {
            foreach (var coin in FindObjectsByType<CoinView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                coin.ResetForRun();
            }

            if (StageProgress.Instance != null)
            {
                StageProgress.Instance.ResetCoins();
            }
        }

        /// <summary>
        /// 結果を決めて、自己ベストなら保存する。
        /// </summary>
        /// <param name="result">結果（クリアか、追いつかれたか）。</param>
        private void Finish(Result result)
        {
            State = result;
            if (_ghost != null)
            {
                _ghost.Hide();
            }

            _resultText.enabled = true;

            if (result == Result.Caught)
            {
                _resultText.text = "追いつかれた…（R でリスタート）";
                return;
            }

            var coinRatio = StageProgress.Instance != null && StageProgress.Instance.TotalCoins > 0
                ? (float)StageProgress.Instance.CoinsCollected / StageProgress.Instance.TotalCoins
                : 0f;
            var rank = StageRank.Decide(_clockSec, coinRatio);
            var isBest = !_hasBest || _clockSec < _bestTimeSec;
            if (isBest)
            {
                GhostStore.Save(_currentTrack, _clockSec);
                _hasBest = true;
                _bestTimeSec = _clockSec;
            }

            _resultText.text = $"配達完了！ ランク {rank}  {FormatTime(_clockSec)}" + (isBest ? "  自己ベスト更新" : "") + "（R でリスタート）";
        }

        /// <summary>
        /// 自己ベストの走りを、今と同じ時間の位置に描く。無ければ隠す。
        /// </summary>
        private void ShowGhost()
        {
            if (_ghost == null)
            {
                return;
            }

            if (_hasBest && _bestTrack.TryGetPose(_clockSec, out var pose))
            {
                _ghost.Show(pose);
            }
            else
            {
                _ghost.Hide();
            }
        }

        private void UpdateHud()
        {
            var coins = StageProgress.Instance != null ? StageProgress.Instance.CoinsCollected : 0;
            var total = StageProgress.Instance != null ? StageProgress.Instance.TotalCoins : 0;
            var best = _hasBest ? "  BEST " + FormatTime(_bestTimeSec) : "";
            _hud.text = $"TIME {FormatTime(_clockSec)}  COIN {coins} / {total}{best}";
        }

        private static string FormatTime(float seconds)
        {
            var minutes = Mathf.FloorToInt(seconds / 60f);
            var rest = seconds - minutes * 60f;
            return $"{minutes:00}:{rest:00.00}";
        }

        /// <summary>
        /// 画面の上に、タイムとコインの数、結果の文字を置く。見た目は仮。
        /// </summary>
        private void CreateHud()
        {
            var canvasGo = new GameObject("StageHud", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            _hud = CreateText(canvasGo.transform, "Hud", new Vector2(0f, 1f), new Vector2(20f, -20f), TextAnchor.UpperLeft, 22);
            _resultText = CreateText(canvasGo.transform, "Result", new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), TextAnchor.MiddleCenter, 30);
            _resultText.enabled = false;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 position, TextAnchor alignment, int size)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 60f);
            return text;
        }
    }
}

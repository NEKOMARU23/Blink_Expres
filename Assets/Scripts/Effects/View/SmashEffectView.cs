using BlinkExpress.Core;
using BlinkExpress.Effects.Model;
using BlinkExpress.Enemy.Model;
using UnityEngine;

namespace BlinkExpress.Effects.View
{
    /// <summary>
    /// 粉砕の見た目（破片と、広がる衝撃波の輪）を出す。ステージに1つだけ置く。
    /// 破片と輪は、それぞれ1つの ParticleSystem を使い回して出す（粉砕のたびにオブジェクトを作らない）。
    /// 見た目は仮のもので、後で差し替える。
    /// </summary>
    public sealed class SmashEffectView : Singleton<SmashEffectView>
    {
        private const int RingTextureSize = 64;

        private ParticleSystem _debris;
        private ParticleSystem _ring;

        /// <summary>
        /// 粉砕の見た目を、敵の中心に出す。
        /// </summary>
        /// <param name="center">敵の中心（ワールド座標）。</param>
        /// <param name="tier">敵の硬さ。破片の数、速さ、輪の大きさが変わる。</param>
        /// <param name="color">敵の色。破片の色になる。</param>
        public void Play(Vector3 center, EnemyTier tier, Color color)
        {
            if (_debris == null || _ring == null)
            {
                return;
            }

            var speed = SmashEffectRules.DebrisSpeedMps(tier);
            var count = SmashEffectRules.DebrisCount(tier);
            for (var i = 0; i < count; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = center + Random.insideUnitSphere * EnemyRules.RadiusM(tier) * 0.5f,
                    velocity = (Random.onUnitSphere + Vector3.up * 0.6f) * (speed * Random.Range(0.5f, 1f)),
                    startColor = color,
                    startSize = Random.Range(0.15f, 0.4f),
                    startLifetime = SmashEffectRules.DebrisLifeSec * Random.Range(0.7f, 1f),
                };
                _debris.Emit(emit, 1);
            }

            var ringEmit = new ParticleSystem.EmitParams
            {
                position = center,
                velocity = Vector3.zero,
                startColor = Color.white,
                startSize = SmashEffectRules.RingMaxRadiusM(tier) * 2f,
                startLifetime = SmashEffectRules.RingLifeSec,
            };
            _ring.Emit(ringEmit, 1);
        }

        /// <summary>
        /// 破片と輪の ParticleSystem を作る。表示用のシェーダーが無ければ、エラーを出して何も出さない。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("粉砕の演出に使うシェーダー（Sprites/Default）が見つかりません。演出は出ません。");
                return;
            }

            _debris = CreateParticleSystem("Debris", new Material(shader), 1.6f);
            _ring = CreateParticleSystem("Ring", CreateRingMaterial(), 0f);

            // 輪は、小さく始まって広がりきり、だんだん消える
            var ringSize = _ring.sizeOverLifetime;
            ringSize.enabled = true;
            ringSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.1f, 4f, 4f),
                new Keyframe(1f, 1f, 0f, 0f)));
            SetFadeOut(_ring);

            // 破片は、小さくなりながら消える
            var debrisSize = _debris.sizeOverLifetime;
            debrisSize.enabled = true;
            debrisSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }

        private ParticleSystem CreateParticleSystem(string objectName, Material material, float gravity)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = 600;

            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;

            go.GetComponent<ParticleSystemRenderer>().material = material;
            return system;
        }

        private static Material CreateRingMaterial()
        {
            // 輪は大きいので、地面に埋まって欠けないよう、奥行きの判定をしない（常に手前に描く）シェーダーを使う
            var shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            // 外側が明るい輪の画像を、コードで作る（画像のファイルは使わない）
            var texture = new Texture2D(RingTextureSize, RingTextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
            };
            var half = RingTextureSize * 0.5f;
            for (var y = 0; y < RingTextureSize; y++)
            {
                for (var x = 0; x < RingTextureSize; x++)
                {
                    var distance = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    var alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.85f) / 0.12f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            var material = new Material(shader) { mainTexture = texture };

            // UI/Default の奥行きの判定を「常に通す」（8 = Always）にする
            material.SetFloat("unity_GUIZTestMode", 8f);
            return material;
        }

        private static void SetFadeOut(ParticleSystem system)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = gradient;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using BlinkExpress.Ghost.Model;
using UnityEngine;

namespace BlinkExpress.Ghost.Store
{
    /// <summary>
    /// 自己ベストの走りを、端末の中に JSON で保存し、読み込む。
    /// 保存できなくても、ゲームは止めない（記録は次の走りで作り直せる）。
    /// </summary>
    public static class GhostStore
    {
        private const string FileName = "ghost-best.json";

        /// <summary>保存の中身。位置と向きは 1 点ごとに 5 つの数値で並べる。</summary>
        [Serializable]
        private sealed class SaveData
        {
            public float bestTimeSec;
            public List<float> values = new List<float>();
        }

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// 自己ベストの走りを保存する。
        /// </summary>
        /// <param name="track">保存する走り。</param>
        /// <param name="bestTimeSec">そのときのクリアタイム（秒）。</param>
        public static void Save(GhostTrack track, float bestTimeSec)
        {
            var data = new SaveData { bestTimeSec = bestTimeSec };
            for (var i = 0; i < track.Count; i++)
            {
                var sample = track.At(i);
                data.values.Add(sample.TimeSec);
                data.values.Add(sample.X);
                data.values.Add(sample.Y);
                data.values.Add(sample.Z);
                data.values.Add(sample.YawRad);
            }

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            }
            catch (IOException e)
            {
                Debug.LogWarning($"ゴーストを保存できませんでした：{e.Message}");
            }
        }

        /// <summary>
        /// 保存されている自己ベストを読み込む。無ければ false。
        /// </summary>
        /// <param name="track">読み込んだ走り。</param>
        /// <param name="bestTimeSec">保存されていたクリアタイム（秒）。</param>
        /// <returns>読み込めたか。</returns>
        public static bool TryLoad(out GhostTrack track, out float bestTimeSec)
        {
            track = new GhostTrack();
            bestTimeSec = 0f;
            try
            {
                if (!File.Exists(FilePath))
                {
                    return false;
                }

                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null || data.values.Count % 5 != 0)
                {
                    return false;
                }

                for (var i = 0; i < data.values.Count; i += 5)
                {
                    track.Add(new GhostSample(data.values[i], data.values[i + 1], data.values[i + 2], data.values[i + 3], data.values[i + 4]));
                }

                bestTimeSec = data.bestTimeSec;
                return track.Count > 0;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                Debug.LogWarning($"ゴーストを読み込めませんでした：{e.Message}");
                track = new GhostTrack();
                return false;
            }
        }
    }
}

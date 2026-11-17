using BlinkExpress.Beacon.Model;
using BlinkExpress.Beacon.View;
using BlinkExpress.Enemy.Model;
using BlinkExpress.Enemy.View;
using BlinkExpress.Ghost.View;
using BlinkExpress.Stage.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlinkExpress.EditorTools
{
    /// <summary>
    /// 平原ステージ（ステージ1）を、現在のシーンに組み立てる Editor の処理。
    /// 配置の座標は Webデモ版のソースから写したもの。作り直しのときは、ここを書き換える。
    /// 単位は m。コースは +Z 方向に進み、ゴールは Z = 1500。
    /// </summary>
    public static class StageOneBuilder
    {
        private const string RootName = "Stage1";

        /// <summary>
        /// メニューから呼ぶ。同名の既存のステージを消してから、組み立て直す。
        /// </summary>
        [MenuItem("BlinkExpress/Build Stage 1")]
        public static void Build()
        {
            var old = GameObject.Find(RootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            var root = new GameObject(RootName).transform;

            // 地面は、壁や仕掛けと区別がつくように、少し明るい緑にする
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.45f, 0.7f, 0.42f) };
            }

            BuildWalls(root);
            BuildMudAndMushrooms(root);
            BuildLogs(root);
            BuildRails(root);
            BuildCoins(root);
            BuildEnemies(root);
            BuildGoal(root);
            BuildRunObjects(root);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 箱を1つ作る（座標は最小の角と最大の角）。色と、ワープ不可かどうかを指定する。
        /// </summary>
        private static GameObject Box(Transform parent, string name, float x0, float y0, float z0, float x1, float y1, float z1, Color color, bool noWarp = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f);
            go.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            go.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            if (noWarp)
            {
                var surface = go.AddComponent<WarpSurface>();
                SetSurfaceKind(surface, HitKind.NoWarpSurface);
            }

            return go;
        }

        private static void SetSurfaceKind(WarpSurface surface, HitKind kind)
        {
            var so = new SerializedObject(surface);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.ApplyModifiedProperties();
        }

        private static void SetZoneKind(BlinkExpress.Stage.View.HazardZone zone, BlinkExpress.Stage.View.HazardZone.Kind kind)
        {
            var so = new SerializedObject(zone);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.ApplyModifiedProperties();
        }

        private static readonly Color WallColor = new Color(0.72f, 0.68f, 0.62f);
        private static readonly Color BlockColor = new Color(0.31f, 0.18f, 0.39f);
        private static readonly Color LogColor = new Color(0.54f, 0.35f, 0.2f);

        /// <summary>コースの両脇の壁、入口の壁、紫の壁、低い屋根、柱、段々畑、三本道、塔と大壁、沼の柱、最後の関門。</summary>
        private static void BuildWalls(Transform root)
        {
            var parent = new GameObject("Walls").transform;
            parent.SetParent(root, false);

            Box(parent, "WallLeft", -25f, 0f, -120f, -22f, 14f, 1580f, WallColor);
            Box(parent, "WallRight", 22f, 0f, -120f, 25f, 14f, 1580f, WallColor);

            // 入口：最初の壁（登る練習）
            Box(parent, "EntryWall", -8f, 0f, 138f, 8f, 14f, 142f, WallColor);

            // 紫のワープ禁止の壁
            Box(parent, "PurpleA", -22f, 0f, 358f, -4f, 12f, 361f, BlockColor, true);
            Box(parent, "PurpleB", 4f, 0f, 358f, 22f, 12f, 361f, BlockColor, true);

            // 低い屋根（下を通れる。上に当たると天井）
            Box(parent, "LowRoof", -22f, 6f, 420f, 22f, 7.5f, 470f, new Color(0.79f, 0.7f, 0.54f));

            // 柱の林（壁ジャンプでジグザグ）
            var pillarZ = new[] { 560f, 585f, 610f };
            for (var i = 0; i < pillarZ.Length; i++)
            {
                var s = i % 2 == 1 ? 1f : -1f;
                var z = pillarZ[i];
                Box(parent, $"PillarA{i}", s * 14f - 2f, 0f, z, s * 14f + 2f, 18f, z + 4f, WallColor);
                Box(parent, $"PillarB{i}", -s * 6f - 2f, 0f, z + 12f, -s * 6f + 2f, 18f, z + 16f, WallColor);
            }

            // 段々畑（左は段を登る）
            var stepHeights = new[] { 3f, 6f, 9f, 12f };
            for (var i = 0; i < stepHeights.Length; i++)
            {
                Box(parent, $"Step{i}", -22f, 0f, 680f + i * 20f, 8f, stepHeights[i], 700f + i * 20f, new Color(0.85f, 0.73f, 0.49f));
            }

            // 三本道（真ん中は泥、両脇は横壁を越える）
            Box(parent, "ForkLeft", -8f, 0f, 840f, -7f, 10f, 960f, WallColor);
            Box(parent, "ForkRight", 7f, 0f, 840f, 8f, 10f, 960f, WallColor);
            Box(parent, "SideWallA", -22f, 0f, 870f, -8f, 6f, 872f, WallColor);
            Box(parent, "SideWallB", 8f, 0f, 900f, 22f, 6f, 902f, WallColor);
            Box(parent, "SideWallC", -22f, 0f, 930f, -8f, 6f, 932f, WallColor);

            // 塔と大きな壁
            Box(parent, "Tower", -4f, 0f, 1000f, 4f, 30f, 1008f, new Color(0.56f, 0.53f, 0.46f));
            Box(parent, "BigWall", -22f, 0f, 1060f, 22f, 9f, 1064f, WallColor);

            // 泥の沼の柱
            var columns = new[] { new[] { -10f, 4f }, new[] { 6f, 5f }, new[] { -6f, 6f }, new[] { 10f, 4f }, new[] { -2f, 5f }, new[] { 8f, 6f }, new[] { -10f, 5f } };
            for (var i = 0; i < columns.Length; i++)
            {
                var cx = columns[i][0];
                var h = columns[i][1];
                Box(parent, $"Column{i}", cx - 3f, 0f, 1120f + i * 20f, cx + 3f, h, 1126f + i * 20f, new Color(0.61f, 0.56f, 0.49f));
            }

            // 最後の関門（隙間の位置がずれた紫の壁）
            Box(parent, "FinalA", -22f, 0f, 1300f, -6f, 10f, 1303f, BlockColor, true);
            Box(parent, "FinalB", 6f, 0f, 1300f, 22f, 10f, 1303f, BlockColor, true);
            Box(parent, "FinalC", -22f, 0f, 1360f, -2f, 10f, 1363f, BlockColor, true);
            Box(parent, "FinalD", 10f, 0f, 1360f, 22f, 10f, 1363f, BlockColor, true);
        }

        /// <summary>泥（減速床）と、跳ねるキノコ。</summary>
        private static void BuildMudAndMushrooms(Transform root)
        {
            var parent = new GameObject("Hazards").transform;
            parent.SetParent(root, false);

            var muds = new[]
            {
                new[] { -22f, 6f, 200f, 270f }, new[] { -22f, -4f, 480f, 500f }, new[] { 4f, 22f, 630f, 650f },
                new[] { 8f, 22f, 680f, 780f }, new[] { -7f, 7f, 840f, 960f }, new[] { -22f, 22f, 1110f, 1262f },
                new[] { -22f, -8f, 1410f, 1430f }, new[] { 8f, 22f, 1410f, 1430f },
            };
            for (var i = 0; i < muds.Length; i++)
            {
                var m = muds[i];
                var mud = new GameObject($"Mud{i}");
                mud.transform.SetParent(parent, false);
                mud.transform.position = new Vector3((m[0] + m[1]) / 2f, 0.5f, (m[2] + m[3]) / 2f);
                var col = mud.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(m[1] - m[0], 1f, m[3] - m[2]);
                var zone = mud.AddComponent<BlinkExpress.Stage.View.HazardZone>();
                SetZoneKind(zone, BlinkExpress.Stage.View.HazardZone.Kind.Mud);

                // 見た目の泥（地面の上に薄く敷く。当たりは上の Trigger が担当する）
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(mud.transform, false);
                visual.transform.localPosition = new Vector3(0f, -0.45f, 0f);
                visual.transform.localScale = new Vector3(m[1] - m[0], 0.1f, m[3] - m[2]);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.48f, 0.34f, 0.2f) };
            }

            var mushrooms = new[] { new[] { 12f, 90f }, new[] { 15f, 700f }, new[] { 12f, 1048f }, new[] { -12f, 1048f }, new[] { -15f, 1185f }, new[] { 0f, 1410f } };
            for (var i = 0; i < mushrooms.Length; i++)
            {
                var pos = new Vector3(mushrooms[i][0], 0f, mushrooms[i][1]);
                var go = new GameObject($"Mushroom{i}");
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                var sphere = go.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 2.2f;
                sphere.center = new Vector3(0f, 1f, 0f);
                var zone = go.AddComponent<BlinkExpress.Stage.View.HazardZone>();
                SetZoneKind(zone, BlinkExpress.Stage.View.HazardZone.Kind.Mushroom);

                // 見た目の傘（仮）
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = "Cap";
                cap.transform.SetParent(go.transform, false);
                cap.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                cap.transform.localScale = new Vector3(3.2f, 1.6f, 3.2f);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
                cap.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.91f, 0.27f, 0.24f) };
            }
        }

        /// <summary>倒木（スライディングでくぐる。登れない障害物）。</summary>
        private static void BuildLogs(Transform root)
        {
            var parent = new GameObject("Logs").transform;
            parent.SetParent(root, false);
            foreach (var z in new[] { 325f, 805f, 1275f })
            {
                Box(parent, $"Log{z}", -22f, 1.1f, z, 22f, 2.3f, z + 1.6f, LogColor, true);
            }
        }

        /// <summary>グラインドレール（8本）。始点と終点は子の空オブジェクト。</summary>
        private static void BuildRails(Transform root)
        {
            var parent = new GameObject("Rails").transform;
            parent.SetParent(root, false);
            var rails = new[]
            {
                new[] { -18f, 0.6f, 95f, -18f, 4f, 200f }, new[] { -8f, 2.5f, 192f, -8f, 2.5f, 278f },
                new[] { 0f, 0.6f, 372f, 0f, 8.2f, 420f }, new[] { 0f, 11f, 548f, 0f, 11f, 632f },
                new[] { 0f, 0.6f, 836f, 0f, 2.2f, 962f }, new[] { 0f, 30.4f, 1008.6f, 0f, 9.7f, 1059f },
                new[] { 15f, 0.6f, 1100f, 15f, 3f, 1262f }, new[] { -12f, 0.6f, 1435f, -12f, 2f, 1495f },
            };
            for (var i = 0; i < rails.Length; i++)
            {
                var r = rails[i];
                var rail = new GameObject($"Rail{i}");
                rail.transform.SetParent(parent, false);
                var start = new GameObject("Start").transform;
                start.SetParent(rail.transform, false);
                start.position = new Vector3(r[0], r[1], r[2]);
                var end = new GameObject("End").transform;
                end.SetParent(rail.transform, false);
                end.position = new Vector3(r[3], r[4], r[5]);

                var grind = rail.AddComponent<GrindRail>();
                var so = new SerializedObject(grind);
                so.FindProperty("_start").objectReferenceValue = start;
                so.FindProperty("_end").objectReferenceValue = end;
                so.ApplyModifiedProperties();

                // 見た目の黄色い棒（円柱を、始点から終点へ向ける。当たりはなく、乗るのは GrindRail が担当する）
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bar.name = "Bar";
                bar.transform.SetParent(rail.transform, false);
                var delta = end.position - start.position;
                bar.transform.position = (start.position + end.position) / 2f;
                bar.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
                bar.transform.localScale = new Vector3(0.22f, delta.magnitude / 2f, 0.22f);
                Object.DestroyImmediate(bar.GetComponent<Collider>());
                bar.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.82f, 0.2f) };
            }
        }

        /// <summary>
        /// コインを直線に並べる。n が 1 のときは始点に 1 枚だけ置く。
        /// </summary>
        private static void CoinLine(Transform parent, float x0, float y0, float z0, float x1, float y1, float z1, int n)
        {
            for (var i = 0; i < n; i++)
            {
                var t = n > 1 ? (float)i / (n - 1) : 0f;
                var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.SetParent(parent, false);
                coin.transform.position = new Vector3(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, z0 + (z1 - z0) * t);
                coin.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
                coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Object.DestroyImmediate(coin.GetComponent<Collider>());
                coin.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.81f, 0.2f) };
                coin.AddComponent<CoinView>();
            }
        }

        /// <summary>コインの配置。仕様書の約 140 枚。</summary>
        private static void BuildCoins(Transform root)
        {
            var parent = new GameObject("Coins").transform;
            parent.SetParent(root, false);

            CoinLine(parent, 0f, 1.2f, 30f, 8f, 1.2f, 95f, 9);
            CoinLine(parent, 15f, 1.2f, 112f, 15f, 1.2f, 175f, 8);
            CoinLine(parent, -15f, 1.2f, 150f, -15f, 1.2f, 175f, 4);
            CoinLine(parent, 14f, 1.2f, 200f, 14f, 1.2f, 270f, 9);
            CoinLine(parent, 0f, 1.2f, 300f, 0f, 1.2f, 350f, 6);
            CoinLine(parent, 0f, 1.2f, 366f, 0f, 1.2f, 402f, 5);
            CoinLine(parent, -10f, 9f, 425f, 10f, 9f, 465f, 8);
            CoinLine(parent, 0f, 1.2f, 478f, 0f, 1.2f, 512f, 5);
            CoinLine(parent, 0f, 6f, 556f, 0f, 6f, 612f, 8);
            CoinLine(parent, 0f, 1.2f, 625f, 0f, 1.2f, 655f, 4);
            CoinLine(parent, -6f, 15.2f, 140f, 6f, 15.2f, 140f, 5);
            CoinLine(parent, -7f, 4.2f, 690f, -7f, 13.2f, 750f, 8);
            CoinLine(parent, -15f, 1.2f, 850f, -15f, 1.2f, 955f, 8);
            CoinLine(parent, 15f, 1.2f, 850f, 15f, 1.2f, 955f, 8);
            CoinLine(parent, -7.5f, 11.2f, 850f, -7.5f, 11.2f, 950f, 6);
            CoinLine(parent, 0f, 4f, 999f, 0f, 26f, 999f, 6);
            CoinLine(parent, -2f, 31.2f, 1004f, 2f, 31.2f, 1004f, 3);
            CoinLine(parent, -12f, 10.2f, 1062f, 12f, 10.2f, 1062f, 6);
            var columnsCoinHeights = new[] { 4f, 5f, 6f, 4f, 5f, 6f, 5f };
            var columnsCoinX = new[] { -10f, 6f, -6f, 10f, -2f, 8f, -10f };
            for (var i = 0; i < columnsCoinX.Length; i++)
            {
                CoinLine(parent, columnsCoinX[i], columnsCoinHeights[i] + 1.2f, 1123f + i * 20f, columnsCoinX[i], columnsCoinHeights[i] + 1.2f, 1123f + i * 20f, 1);
            }

            CoinLine(parent, 0f, 4f, 1268f, 0f, 1.2f, 1290f, 4);
            CoinLine(parent, 0f, 1.2f, 1310f, 0f, 1.2f, 1350f, 5);
            CoinLine(parent, 4f, 1.2f, 1366f, 4f, 1.2f, 1400f, 5);
            CoinLine(parent, 0f, 1.2f, 1445f, 0f, 1.2f, 1495f, 6);
        }

        /// <summary>敵（39体）。硬さと、左右に揺れるかは仕様どおり。逃げる敵は、今は動かない（後で実装）。</summary>
        private static void BuildEnemies(Transform root)
        {
            var parent = new GameObject("Enemies").transform;
            parent.SetParent(root, false);

            // tier（0 やわらかい・1 ふつう・2 かたい）、x、z、動き（true で左右に揺れる）、足元の高さ
            var defs = new[]
            {
                new[] { 0f, -6f, 70f, 0f, 0f }, new[] { 0f, 0f, 76f, 1f, 0f }, new[] { 0f, 6f, 70f, 0f, 0f },
                new[] { 0f, -12f, 110f, 0f, 0f }, new[] { 0f, 12f, 110f, 0f, 0f },
                new[] { 1f, -10f, 180f, 0f, 0f }, new[] { 1f, 10f, 186f, 1f, 0f }, new[] { 0f, 14f, 250f, 0f, 0f }, new[] { 0f, -14f, 252f, 0f, 0f },
                new[] { 1f, 0f, 300f, 0f, 0f }, new[] { 1f, -12f, 306f, 0f, 0f }, new[] { 0f, 12f, 300f, 0f, 0f }, new[] { 0f, 0f, 400f, 1f, 0f },
                new[] { 2f, 0f, 520f, 0f, 0f }, new[] { 1f, -12f, 526f, 0f, 0f }, new[] { 1f, 12f, 526f, 1f, 0f }, new[] { 0f, 0f, 632f, 1f, 0f },
                new[] { 1f, -8f, 655f, 0f, 0f }, new[] { 1f, 8f, 655f, 0f, 0f },
                new[] { 0f, -12f, 690f, 0f, 3f }, new[] { 0f, -4f, 730f, 0f, 9f }, new[] { 0f, -14f, 750f, 0f, 12f }, new[] { 1f, 15f, 720f, 1f, 0f },
                new[] { 0f, -15f, 885f, 0f, 0f }, new[] { 1f, 15f, 915f, 0f, 0f }, new[] { 0f, -15f, 945f, 0f, 0f }, new[] { 1f, 0f, 880f, 1f, 0f },
                new[] { 2f, -14f, 1030f, 0f, 0f }, new[] { 2f, 14f, 1030f, 0f, 0f }, new[] { 1f, 0f, 1040f, 0f, 0f },
                new[] { 0f, -10f, 1123f, 0f, 4f }, new[] { 0f, 6f, 1143f, 0f, 5f }, new[] { 0f, 10f, 1183f, 0f, 4f },
                new[] { 2f, 0f, 1330f, 0f, 0f }, new[] { 1f, -10f, 1340f, 1f, 0f }, new[] { 2f, -6f, 1400f, 0f, 0f }, new[] { 2f, 6f, 1400f, 0f, 0f }, new[] { 1f, 4f, 1440f, 0f, 0f }, new[] { 0f, -4f, 1465f, 0f, 0f },
            };

            for (var i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var tier = (EnemyTier)(int)d[0];
                var radius = EnemyRules.RadiusM(tier);

                // 基準点は足元（EnemyView の親）。見た目の立方体は、その上に半径分だけ置く
                var go = new GameObject($"Enemy{i}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(d[1], d[4], d[2]);

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, radius, 0f);
                body.transform.localScale = Vector3.one * radius * 2f;
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = TierColor(tier) };

                var view = go.AddComponent<EnemyView>();
                var so = new SerializedObject(view);
                so.FindProperty("_tier").enumValueIndex = (int)tier;
                so.FindProperty("_swayRangeM").floatValue = d[3] > 0.5f ? 5f : 0f;
                so.ApplyModifiedProperties();
            }
        }

        private static Color TierColor(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Normal:
                    return new Color(0.95f, 0.95f, 0.93f);
                case EnemyTier.Hard:
                    return new Color(0.55f, 0.48f, 0.42f);
                default:
                    return new Color(0.29f, 0.79f, 0.75f);
            }
        }

        /// <summary>ゴール（Z = 1500）の門。</summary>
        private static void BuildGoal(Transform root)
        {
            var parent = new GameObject("Goal").transform;
            parent.SetParent(root, false);
            var gold = new Color(1f, 0.81f, 0.2f);
            Box(parent, "GoalLeft", -11.7f, 0f, 1499.3f, -9.3f, 10f, 1500.7f, gold);
            Box(parent, "GoalRight", 9.3f, 0f, 1499.3f, 11.7f, 10f, 1500.7f, gold);
            Box(parent, "GoalTop", -12f, 10f, 1499.3f, 12f, 12.4f, 1500.7f, gold);
        }

        /// <summary>
        /// 走りの進行（タイム、追ってくるもの、ゴーストの記録と再生）と、コインの集計、
        /// 追ってくるものの見た目を置く。
        /// </summary>
        private static void BuildRunObjects(Transform root)
        {
            var progress = new GameObject("StageProgress");
            progress.transform.SetParent(root, false);
            progress.AddComponent<StageProgress>();

            var chaser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chaser.name = "Chaser";
            chaser.transform.SetParent(root, false);
            chaser.transform.localScale = new Vector3(44f, 9f, 10f);
            chaser.transform.position = new Vector3(0f, 4.5f, -50f);
            Object.DestroyImmediate(chaser.GetComponent<Collider>());
            chaser.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.36f, 0.17f, 0.51f) };

            var run = new GameObject("StageRun");
            run.transform.SetParent(root, false);
            var stageRun = run.AddComponent<StageRun>();

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "Ghost";
            ghost.transform.SetParent(run.transform, false);
            ghost.transform.localScale = new Vector3(1f, 2f, 1f);
            Object.DestroyImmediate(ghost.GetComponent<Collider>());
            ghost.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.5f, 0.88f, 1f) };
            ghost.AddComponent<GhostView>();

            var so = new SerializedObject(stageRun);
            so.FindProperty("_chaser").objectReferenceValue = chaser.transform;
            so.ApplyModifiedProperties();
        }
    }
}

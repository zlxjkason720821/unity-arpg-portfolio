#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ARPG.Data;
using UnityEditor;
using UnityEngine;

namespace ARPG.EditorTools
{
    /// <summary>
    /// 关卡编辑器：从 EnemyData 列表选类型，在 Scene 点击放置，保存为 LevelData SO。
    /// 菜单：ARPG Tools > Level Editor
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        enum PlaceMode
        {
            None,
            Enemy,
            Goal
        }

        LevelData _level;
        EnemyData _selectedEnemy;
        List<EnemyData> _enemies = new();
        PlaceMode _mode;
        Vector2 _enemyListScroll;
        Vector2 _spawnListScroll;

        [MenuItem("ARPG Tools/Level Editor")]
        public static void Open()
        {
            var w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(420, 460);
            w.RefreshEnemyList();
        }

        void OnEnable()
        {
            RefreshEnemyList();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;
        void OnFocus() => RefreshEnemyList();

        void RefreshEnemyList()
        {
            _enemies = AssetDatabase.FindAssets("t:EnemyData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnemyData>)
                .Where(e => e != null)
                .OrderBy(e => e.displayName)
                .ToList();

            if (_selectedEnemy != null && !_enemies.Contains(_selectedEnemy))
                _selectedEnemy = null;
        }

        void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);

            _level = (LevelData)EditorGUILayout.ObjectField("Level Data", _level, typeof(LevelData), false);
            if (_level == null)
            {
                EditorGUILayout.HelpBox(
                    "尚未选择 LevelData。\n点击 Create Level Data，或从 Project 拖入一个关卡资产。",
                    MessageType.Info);
                if (GUILayout.Button("Create Level Data", GUILayout.Height(28)))
                    CreateLevelAsset();
                return;
            }

            Undo.RecordObject(_level, "Edit LevelData");
            _level.levelName = EditorGUILayout.TextField("Level Name", _level.levelName);
            _level.playerSpawn = EditorGUILayout.Vector3Field("Player Spawn", _level.playerSpawn);
            _level.hasGoalPoint = EditorGUILayout.Toggle("Has Goal Point", _level.hasGoalPoint);
            using (new EditorGUI.DisabledScope(!_level.hasGoalPoint))
                _level.goalPoint = EditorGUILayout.Vector3Field("Goal Point", _level.goalPoint);

            EditorGUILayout.Space(8);
            DrawEnemyPicker();
            DrawPlaceModes();
            DrawSpawnList();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Level Asset", GUILayout.Height(28)))
                {
                    EditorUtility.SetDirty(_level);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[LevelEditor] Saved {_level.enemies.Count} enemies → {AssetDatabase.GetAssetPath(_level)}");
                }

                if (GUILayout.Button("Clear Spawns"))
                {
                    if (EditorUtility.DisplayDialog("Level Editor", "清空当前关卡的全部刷怪点？", "Clear", "Cancel"))
                    {
                        _level.enemies.Clear();
                        EditorUtility.SetDirty(_level);
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "流程：选 EnemyData → Place Enemy → 在 Scene 视图点地面 → Save。\n" +
                "运行时：场景挂 LevelLoader，把该 LevelData 拖进去，Play 即可生成。",
                MessageType.None);

            if (GUI.changed)
                EditorUtility.SetDirty(_level);
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    RefreshEnemyList();
                if (GUILayout.Button("Create Level Data", EditorStyles.toolbarButton, GUILayout.Width(120)))
                    CreateLevelAsset();
                if (GUILayout.Button("Create Sample Data", EditorStyles.toolbarButton, GUILayout.Width(120)))
                    SampleDataGenerator.Generate();
                GUILayout.FlexibleSpace();
            }
        }

        void DrawEnemyPicker()
        {
            EditorGUILayout.LabelField($"Enemy Types ({_enemies.Count})", EditorStyles.boldLabel);
            if (_enemies.Count == 0)
            {
                EditorGUILayout.HelpBox("没有 EnemyData。请先 ARPG Tools > Create Sample Data。", MessageType.Warning);
                return;
            }

            _enemyListScroll = EditorGUILayout.BeginScrollView(_enemyListScroll, GUILayout.Height(90));
            foreach (var enemy in _enemies)
            {
                bool on = enemy == _selectedEnemy;
                if (GUILayout.Toggle(on, enemy.displayName, "Button"))
                    _selectedEnemy = enemy;
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawPlaceModes()
        {
            EditorGUILayout.LabelField("Scene Place Mode", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(_mode == PlaceMode.None, "Off", "Button"))
                    _mode = PlaceMode.None;
                using (new EditorGUI.DisabledScope(_selectedEnemy == null))
                {
                    if (GUILayout.Toggle(_mode == PlaceMode.Enemy, "Place Enemy", "Button"))
                        _mode = PlaceMode.Enemy;
                }
                using (new EditorGUI.DisabledScope(!_level.hasGoalPoint))
                {
                    if (GUILayout.Toggle(_mode == PlaceMode.Goal, "Place Goal", "Button"))
                        _mode = PlaceMode.Goal;
                }
            }

            if (_mode == PlaceMode.Enemy && _selectedEnemy == null)
                EditorGUILayout.HelpBox("请先在上方选择一种 EnemyData。", MessageType.Warning);
            if (_mode != PlaceMode.None)
                EditorGUILayout.HelpBox("在 Scene 视图左键点击地面放置（按住 Alt 可旋转视角不放置）。", MessageType.Info);
        }

        void DrawSpawnList()
        {
            if (_level.enemies == null)
                _level.enemies = new List<EnemySpawnEntry>();

            EditorGUILayout.LabelField($"Spawns ({_level.enemies.Count})", EditorStyles.boldLabel);
            _spawnListScroll = EditorGUILayout.BeginScrollView(_spawnListScroll, GUILayout.Height(140));
            for (int i = 0; i < _level.enemies.Count; i++)
            {
                var entry = _level.enemies[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = entry.enemyData != null ? entry.enemyData.displayName : "(Missing)";
                    EditorGUILayout.LabelField($"{i}. {label}  {entry.position}", GUILayout.MinWidth(180));
                    if (GUILayout.Button("Focus", GUILayout.Width(50)))
                    {
                        SceneView.lastActiveSceneView?.LookAt(entry.position);
                    }
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        _level.enemies.RemoveAt(i);
                        EditorUtility.SetDirty(_level);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView view)
        {
            if (_level == null || _mode == PlaceMode.None)
                return;

            // 画已有刷怪点
            if (_level.enemies != null)
            {
                Handles.color = new Color(1f, 0.3f, 0.2f, 0.9f);
                foreach (var spawn in _level.enemies)
                {
                    if (spawn == null)
                        continue;
                    Handles.SphereHandleCap(0, spawn.position + Vector3.up, Quaternion.identity, 0.4f, EventType.Repaint);
                    Handles.Label(spawn.position + Vector3.up * 1.5f, spawn.enemyData != null ? spawn.enemyData.displayName : "?");
                }
            }

            if (_level.hasGoalPoint)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(_level.goalPoint, Vector3.up, 0.8f);
                Handles.Label(_level.goalPoint + Vector3.up, "GOAL");
            }

            Event evt = Event.current;
            if (evt.type != EventType.MouseDown || evt.button != 0 || evt.alt)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            Vector3 point;
            if (Physics.Raycast(ray, out RaycastHit hit))
                point = hit.point;
            else
            {
                var ground = new Plane(Vector3.up, Vector3.zero);
                if (!ground.Raycast(ray, out float enter))
                    return;
                point = ray.GetPoint(enter);
            }

            Undo.RecordObject(_level, "Place Level Item");
            if (_mode == PlaceMode.Enemy)
            {
                if (_selectedEnemy == null)
                    return;
                _level.enemies.Add(new EnemySpawnEntry
                {
                    enemyData = _selectedEnemy,
                    position = point + Vector3.up,
                    eulerAngles = Vector3.zero
                });
            }
            else if (_mode == PlaceMode.Goal)
            {
                _level.goalPoint = point;
                _level.hasGoalPoint = true;
            }

            EditorUtility.SetDirty(_level);
            evt.Use();
            Repaint();
        }

        void CreateLevelAsset()
        {
            string folder = "Assets/Data/Levels";
            SampleDataGenerator.EnsureFolder(folder);
            var level = CreateInstance<LevelData>();
            level.levelName = "New Level";
            level.playerSpawn = new Vector3(0f, 1f, 0f);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewLevel.asset");
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            _level = level;
            Selection.activeObject = level;
            EditorGUIUtility.PingObject(level);
        }
    }
}
#endif

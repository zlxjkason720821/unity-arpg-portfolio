using ARPG.AI;
using ARPG.Combat;
using ARPG.Data;
using ARPG.Skills;
using UnityEngine;

namespace ARPG.Level
{
    /// <summary>
    /// 运行时根据 LevelData 生成敌人（及可选目标点）。
    /// 挂到场景空物体上，指定 Level Data 后 Play 即可。
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] LevelData levelData;
        [SerializeField] bool loadOnStart = true;
        [SerializeField] bool clearChildrenFirst = true;
        [SerializeField] Transform enemyRoot;
        [SerializeField] bool moveExistingPlayerToSpawn = true;

        Transform _runtimeRoot;

        public LevelData LevelData
        {
            get => levelData;
            set => levelData = value;
        }

        void Start()
        {
            if (loadOnStart)
                Load();
        }

        /// <summary>清空旧刷怪并按 LevelData 重新生成。</summary>
        [ContextMenu("Load Level")]
        public void Load()
        {
            if (levelData == null)
            {
                Debug.LogWarning("[LevelLoader] 未指定 LevelData。");
                return;
            }

            EnsureRoot();
            if (clearChildrenFirst)
                ClearSpawned();

            if (moveExistingPlayerToSpawn)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    player.transform.position = levelData.playerSpawn;
            }

            for (int i = 0; i < levelData.enemies.Count; i++)
            {
                var entry = levelData.enemies[i];
                if (entry == null || entry.enemyData == null)
                    continue;
                SpawnEnemy(entry);
            }

            if (levelData.hasGoalPoint)
                SpawnGoalMarker(levelData.goalPoint);

            Debug.Log($"[LevelLoader] Loaded '{levelData.levelName}' enemies={levelData.enemies.Count}");
        }

        void EnsureRoot()
        {
            if (enemyRoot != null)
            {
                _runtimeRoot = enemyRoot;
                return;
            }

            var existing = transform.Find("RuntimeSpawns");
            if (existing != null)
            {
                _runtimeRoot = existing;
                return;
            }

            var go = new GameObject("RuntimeSpawns");
            go.transform.SetParent(transform, false);
            _runtimeRoot = go.transform;
        }

        void ClearSpawned()
        {
            if (_runtimeRoot == null)
                return;
            for (int i = _runtimeRoot.childCount - 1; i >= 0; i--)
                Destroy(_runtimeRoot.GetChild(i).gameObject);
        }

        void SpawnEnemy(EnemySpawnEntry entry)
        {
            GameObject go;
            if (entry.enemyData.prefab != null)
            {
                go = Instantiate(entry.enemyData.prefab, entry.position, Quaternion.Euler(entry.eulerAngles), _runtimeRoot);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(_runtimeRoot, false);
                go.transform.SetPositionAndRotation(entry.position, Quaternion.Euler(entry.eulerAngles));
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                    rend.material.color = new Color(0.85f, 0.25f, 0.25f);
            }

            go.name = $"Enemy_{entry.enemyData.displayName}";

            var combatant = go.GetComponent<Combatant>();
            if (combatant == null)
                combatant = go.AddComponent<Combatant>();
            combatant.ApplyEnemyData(entry.enemyData, destroyOnDeath: true);

            if (go.GetComponent<HitFeedback>() == null)
                go.AddComponent<HitFeedback>();
            if (go.GetComponent<DeathDissolve>() == null)
                go.AddComponent<DeathDissolve>();

            var skills = go.GetComponent<SkillExecutor>();
            if (skills == null)
                skills = go.AddComponent<SkillExecutor>();
            if (skills.EquippedSkills == null || skills.EquippedSkills.Count == 0)
            {
                var bite = ScriptableObject.CreateInstance<SkillData>();
                bite.skillId = "enemy_bite";
                bite.displayName = "Bite";
                bite.baseDamage = Mathf.Max(1f, entry.enemyData.attackPower);
                bite.cooldown = 1.4f;
                bite.range = entry.enemyData.attackRange;
                bite.radius = 1.1f;
                bite.hitMask = ~0;
                skills.SetEquippedSkills(new[] { bite });
            }

            var ai = go.GetComponent<SimpleEnemyAI>();
            if (ai == null)
                ai = go.AddComponent<SimpleEnemyAI>();
            var player = GameObject.FindGameObjectWithTag("Player");
            ai.Configure(entry.enemyData, player != null ? player.transform : null);
        }

        void SpawnGoalMarker(Vector3 position)
        {
            var goal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goal.name = "GoalPoint";
            goal.transform.SetParent(_runtimeRoot, false);
            goal.transform.position = position + Vector3.up * 0.5f;
            goal.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            var col = goal.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
            var rend = goal.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
        }
    }
}

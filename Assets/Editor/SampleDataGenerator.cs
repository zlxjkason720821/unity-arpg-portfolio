#if UNITY_EDITOR
using ARPG.Data;
using ARPG.Skills;
using UnityEditor;
using UnityEngine;

namespace ARPG.EditorTools
{
    /// <summary>
    /// 一键生成示例 SkillData / EnemyData / LevelData，方便阶段二验收。
    /// </summary>
    public static class SampleDataGenerator
    {
        [MenuItem("ARPG Tools/Create Sample Data")]
        public static void Generate()
        {
            EnsureFolder("Assets/Data/Skills");
            EnsureFolder("Assets/Data/Enemies");
            EnsureFolder("Assets/Data/Levels");

            var slash = CreateSkill("Assets/Data/Skills/Skill_Slash.asset", "basic_slash", "Slash", 12f, 0.35f, 2f, 1.1f, true);
            var cleave = CreateSkill("Assets/Data/Skills/Skill_Cleave.asset", "arc_cleave", "Cleave", 22f, 2.5f, 2.6f, 1.8f, false);
            var wave = CreateSkill("Assets/Data/Skills/Skill_Shockwave.asset", "shockwave", "Shockwave", 18f, 4f, 0.5f, 3.2f, false);
            wave.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);
            EditorUtility.SetDirty(wave);

            var grunt = CreateEnemy("Assets/Data/Enemies/Enemy_Grunt.asset", "grunt", "Grunt", 60f, 6f, 14f, 2.4f, 3.2f);
            var brute = CreateEnemy("Assets/Data/Enemies/Enemy_Brute.asset", "brute", "Brute", 120f, 12f, 10f, 2.0f, 2.6f);

            string levelPath = "Assets/Data/Levels/Level_01.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(level, levelPath);
            }

            level.levelName = "Level 01";
            level.playerSpawn = new Vector3(0f, 1f, 0f);
            level.hasGoalPoint = true;
            level.goalPoint = new Vector3(0f, 0.1f, 14f);
            level.enemies.Clear();
            level.enemies.Add(new EnemySpawnEntry { enemyData = grunt, position = new Vector3(0f, 1f, 6f) });
            level.enemies.Add(new EnemySpawnEntry { enemyData = grunt, position = new Vector3(3f, 1f, 8f) });
            level.enemies.Add(new EnemySpawnEntry { enemyData = brute, position = new Vector3(-3f, 1f, 9f) });
            EditorUtility.SetDirty(level);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Sample Data",
                "已生成：\n- 3 个 SkillData\n- 2 个 EnemyData\n- Level_01\n\n打开 ARPG Tools > Skill Editor / Level Editor 验收。",
                "OK");

            Selection.activeObject = level;
            EditorGUIUtility.PingObject(slash);
            EditorGUIUtility.PingObject(cleave);
        }

        static SkillData CreateSkill(string path, string id, string name, float dmg, float cd, float range, float radius, bool basic)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(skill, path);
            }

            skill.skillId = id;
            skill.displayName = name;
            skill.baseDamage = dmg;
            skill.cooldown = cd;
            skill.range = range;
            skill.radius = radius;
            skill.isBasicAttack = basic;
            skill.hitMask = ~0;
            skill.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        static EnemyData CreateEnemy(string path, string id, string name, float hp, float atk, float detect, float attackRange, float speed)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy == null)
            {
                enemy = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(enemy, path);
            }

            enemy.enemyId = id;
            enemy.displayName = name;
            enemy.maxHealth = hp;
            enemy.attackPower = atk;
            enemy.detectRange = detect;
            enemy.attackRange = attackRange;
            enemy.moveSpeed = speed;
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string[] parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif

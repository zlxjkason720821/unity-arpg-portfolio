using UnityEngine;

namespace ARPG.Data
{
    /// <summary>
    /// 敌人属性资产：生命、攻击、移速、索敌/攻击距离全部放在 SO 上。
    /// 与 SkillData 同一套路——改数值不改 AI 代码，关卡编辑器也只引用这份数据。
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "ARPG/Enemy Data", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "enemy_grunt";
        public string displayName = "Grunt";

        [Header("Combat Stats")]
        [Min(1f)] public float maxHealth = 50f;
        [Min(0f)] public float attackPower = 8f;

        [Header("AI Ranges")]
        [Min(0f)] public float detectRange = 12f;
        [Min(0f)] public float attackRange = 2.2f;
        [Min(0f)] public float moveSpeed = 3.5f;

        [Header("Optional Prefab")]
        [Tooltip("关卡编辑器放置时可选；运行时也可由 Bootstrap 生成简单 Capsule")]
        public GameObject prefab;
    }
}

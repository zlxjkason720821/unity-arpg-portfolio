using UnityEngine;

namespace ARPG.Skills
{
    /// <summary>
    /// Skill definition asset. All tunable numbers and presentation references live here.
    /// Runtime code (<see cref="SkillExecutor"/>) only reads this data — no hardcoded damage/CD/range in logic.
    /// Interview hook: “skills are data; the executor is an interpreter.”
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "ARPG/Skill Data", order = 0)]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "skill_basic";
        public string displayName = "Basic Attack";
        [TextArea(2, 4)] public string description;

        [Header("Timing")]
        [Min(0f)] public float cooldown = 0.5f;
        [Min(0f)] public float castTime;
        [Min(0f)] public float activeDuration = 0.15f;

        [Header("Damage & Range")]
        [Min(0f)] public float baseDamage = 10f;
        [Tooltip("Optional bonus scaled by caster AttackPower")]
        public float attackPowerScale = 1f;
        [Min(0f)] public float range = 2f;
        [Min(0f)] public float radius = 1.2f;
        public LayerMask hitMask = ~0;

        [Header("Feedback")]
        public GameObject vfxPrefab;
        public AudioClip sfxClip;
        public string vfxSocketName = "Weapon";
        [Tooltip("Sampled by distance or by level preview; Unity evaluates keyed hermite-style segments")]
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Header("Tags")]
        public bool isBasicAttack;
        public bool canMoveWhileCasting;

        public float EvaluateDamage(float normalizedDistance, float attackPower)
        {
            float falloff = damageFalloff != null
                ? damageFalloff.Evaluate(Mathf.Clamp01(normalizedDistance))
                : 1f;
            return (baseDamage + attackPower * attackPowerScale) * falloff;
        }
    }
}

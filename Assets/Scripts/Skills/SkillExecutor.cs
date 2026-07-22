using System;
using System.Collections.Generic;
using ARPG.Combat;
using ARPG.Pooling;
using UnityEngine;

namespace ARPG.Skills
{
    /// <summary>
    /// Executes skills by reading <see cref="SkillData"/> only.
    /// No damage/cooldown/range constants belong here — change the asset, not this class.
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        [SerializeField] Combatant owner;
        [SerializeField] Transform defaultVfxSocket;
        [SerializeField] List<SkillData> equippedSkills = new();
        [SerializeField] SimpleObjectPool vfxPool;
        [SerializeField] bool debugLogCasts;

        readonly Dictionary<SkillData, float> _cooldownUntil = new();
        readonly Collider[] _hitBuffer = new Collider[64];

        public event Action<SkillData> OnSkillCast;
        public event Action<SkillData, float> OnCooldownChanged;

        public IReadOnlyList<SkillData> EquippedSkills => equippedSkills;

        void Awake()
        {
            if (owner == null)
                owner = GetComponent<Combatant>();
        }

        public void SetDefaultVfxSocket(Transform socket) => defaultVfxSocket = socket;
        public void SetDebugLogCasts(bool enabled) => debugLogCasts = enabled;
        public void SetVfxPool(SimpleObjectPool pool) => vfxPool = pool;

        public void SetEquippedSkills(IList<SkillData> skills)
        {
            equippedSkills.Clear();
            if (skills == null)
                return;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null)
                    equippedSkills.Add(skills[i]);
            }
        }

        public bool TryCast(int skillIndex, Vector3 aimPoint)
        {
            if (skillIndex < 0 || skillIndex >= equippedSkills.Count)
                return false;
            return TryCast(equippedSkills[skillIndex], aimPoint);
        }

        public bool TryCast(SkillData skill, Vector3 aimPoint)
        {
            if (skill == null || owner == null || !owner.IsAlive)
                return false;
            if (IsOnCooldown(skill))
                return false;

            Vector3 origin = transform.position;
            Vector3 toAim = aimPoint - origin;
            toAim.y = 0f;
            if (toAim.sqrMagnitude < 0.0001f)
                toAim = transform.forward;
            Vector3 dir = toAim.normalized;

            if (toAim.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            int hits = ApplyHits(skill, origin, dir);
            SpawnFeedback(skill, origin, dir);
            StartCooldown(skill);
            OnSkillCast?.Invoke(skill);

            if (debugLogCasts)
                Debug.Log($"[SkillExecutor] {name} cast {skill.displayName}, hits={hits}");
            return true;
        }

        public bool IsOnCooldown(SkillData skill)
        {
            return _cooldownUntil.TryGetValue(skill, out float until) && Time.time < until;
        }

        public float GetCooldownRemaining(SkillData skill)
        {
            if (!_cooldownUntil.TryGetValue(skill, out float until))
                return 0f;
            return Mathf.Max(0f, until - Time.time);
        }

        public float GetCooldownNormalized(SkillData skill)
        {
            if (skill == null || skill.cooldown <= 0f)
                return 0f;
            return Mathf.Clamp01(GetCooldownRemaining(skill) / skill.cooldown);
        }

        void StartCooldown(SkillData skill)
        {
            _cooldownUntil[skill] = Time.time + skill.cooldown;
            OnCooldownChanged?.Invoke(skill, skill.cooldown);
        }

        int ApplyHits(SkillData skill, Vector3 origin, Vector3 dir)
        {
            int mask = skill.hitMask.value == 0 ? ~0 : skill.hitMask.value;
            float searchRadius = Mathf.Max(skill.radius, skill.range) + 0.75f;
            Vector3 center = origin + Vector3.up * 0.9f;
            int count = Physics.OverlapSphereNonAlloc(
                center, searchRadius, _hitBuffer, mask, QueryTriggerInteraction.Collide);

            bool isAoe = skill.radius >= skill.range;
            int applied = 0;

            for (int i = 0; i < count; i++)
            {
                var col = _hitBuffer[i];
                if (col == null)
                    continue;

                var target = col.GetComponentInParent<Combatant>();
                if (target == null || target == owner || !target.IsAlive)
                    continue;

                Vector3 toTarget = target.transform.position - origin;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;
                if (dist > searchRadius)
                    continue;

                if (!isAoe && dist > 0.05f && Vector3.Dot(dir, toTarget.normalized) < -0.15f)
                    continue;

                float normalized = skill.range > 0f ? dist / skill.range : 0f;
                float damage = skill.EvaluateDamage(normalized, owner.AttackPower);
                target.ApplyDamage(damage, owner);
                applied++;
            }

            return applied;
        }

        void SpawnFeedback(SkillData skill, Vector3 origin, Vector3 dir)
        {
            Transform socket = ResolveSocket(skill.vfxSocketName);
            Vector3 pos = socket != null ? socket.position : origin + Vector3.up + dir * 0.8f;
            Quaternion rot = socket != null ? socket.rotation : Quaternion.LookRotation(dir);
            float scale = Mathf.Clamp(skill.radius * 0.55f, 0.35f, 2.8f);

            if (skill.vfxPrefab != null)
            {
                // 优化前：Instantiate(skill.vfxPrefab, pos, rot);
                // 优化后：有对象池则复用，Profiler 可对比 GC Alloc
                if (vfxPool != null)
                {
                    var go = vfxPool.Get(pos, rot);
                    if (go != null)
                    {
                        go.transform.localScale = Vector3.one * scale;
                        var life = go.GetComponent<PooledLifetime>();
                        if (life != null)
                            life.SetLifetime(0.22f);
                    }
                }
                else
                {
                    var go = Instantiate(skill.vfxPrefab, pos, rot);
                    go.transform.localScale = Vector3.one * scale;
                    Destroy(go, 0.22f);
                }
            }
            else if (vfxPool != null)
            {
                var go = vfxPool.Get(pos, rot);
                if (go != null)
                {
                    go.transform.localScale = Vector3.one * scale;
                    var r = go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        // PropertyBlock 改色，避免拆批
                        var block = new MaterialPropertyBlock();
                        r.GetPropertyBlock(block);
                        Color c = skill.isBasicAttack
                            ? new Color(1f, 0.85f, 0.2f, 0.85f)
                            : new Color(0.25f, 0.75f, 1f, 0.85f);
                        if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                            block.SetColor("_BaseColor", c);
                        else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                            block.SetColor("_Color", c);
                        r.SetPropertyBlock(block);
                    }
                    var life = go.GetComponent<PooledLifetime>();
                    if (life != null)
                        life.SetLifetime(0.2f);
                }
            }

            if (skill.sfxClip != null)
                AudioSource.PlayClipAtPoint(skill.sfxClip, pos);
        }

        Transform ResolveSocket(string socketName)
        {
            if (string.IsNullOrEmpty(socketName))
                return defaultVfxSocket != null ? defaultVfxSocket : transform;

            var t = FindChildRecursive(transform, socketName);
            return t != null ? t : (defaultVfxSocket != null ? defaultVfxSocket : transform);
        }

        static Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (equippedSkills == null || equippedSkills.Count == 0)
                return;
            var skill = equippedSkills[0];
            if (skill == null)
                return;
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            float searchRadius = Mathf.Max(skill.radius, skill.range) + 0.75f;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, searchRadius);
        }
#endif
    }
}

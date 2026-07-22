using System;
using ARPG.Data;
using UnityEngine;

namespace ARPG.Combat
{
    /// <summary>
    /// 可受击单位：生命、攻击力、受击回调。玩家与敌人共用，便于技能系统统一结算。
    /// </summary>
    public class Combatant : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float attackPower = 10f;
        [SerializeField] bool destroyOnDeath;

        float _health;
        bool _dead;

        public float MaxHealth => maxHealth;
        public float Health => _health;
        public float AttackPower => attackPower;
        public bool IsAlive => _health > 0f && !_dead;
        public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) : 0f;

        public event Action<float, Combatant> OnDamaged;
        public event Action<Combatant> OnDied;
        public event Action OnHealthChanged;

        void Awake()
        {
            if (_health <= 0f)
                _health = maxHealth;
        }

        public void Configure(float newMaxHealth, float newAttackPower, bool destroyOnDeath = false)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
            attackPower = Mathf.Max(0f, newAttackPower);
            this.destroyOnDeath = destroyOnDeath;
            _health = maxHealth;
            _dead = false;
            OnHealthChanged?.Invoke();
        }

        public void ApplyEnemyData(EnemyData data, bool destroyOnDeath = true)
        {
            if (data == null)
                return;
            Configure(data.maxHealth, data.attackPower, destroyOnDeath);
        }

        public void SetAttackPower(float value) => attackPower = Mathf.Max(0f, value);

        public void ApplyDamage(float amount, Combatant source)
        {
            if (!IsAlive || amount <= 0f)
                return;

            _health = Mathf.Max(0f, _health - amount);
            OnDamaged?.Invoke(amount, source);
            OnHealthChanged?.Invoke();

            if (_health <= 0f && !_dead)
            {
                _dead = true;
                OnDied?.Invoke(this);

                // 有 DeathDissolve 时由它负责销毁（播完溶解）；否则按开关立即 Destroy
                if (destroyOnDeath && GetComponent<DeathDissolve>() == null)
                    Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;
            _health = Mathf.Min(maxHealth, _health + amount);
            OnHealthChanged?.Invoke();
        }
    }
}

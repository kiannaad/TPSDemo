using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private string entityId;

        [SerializeField]
        [Min(0.01f)]
        private float maxHealth = 100f;

        private readonly HashSet<string> appliedDamageEventIds = new HashSet<string>();

        public string EntityId => entityId;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;
        public bool IsDead => !IsAlive;

        public event Action<DamageEvent> Damaged;
        public event Action<DamageEvent> Died;

        private void Awake()
        {
            ResetHealth();
        }

        public void Configure(string configuredEntityId, float configuredMaxHealth)
        {
            if (string.IsNullOrWhiteSpace(configuredEntityId))
            {
                throw new ArgumentException("A valid entity ID is required.", nameof(configuredEntityId));
            }

            if (float.IsNaN(configuredMaxHealth) || float.IsInfinity(configuredMaxHealth) || configuredMaxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredMaxHealth));
            }

            entityId = configuredEntityId;
            maxHealth = configuredMaxHealth;
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = Mathf.Max(0.01f, maxHealth);
            appliedDamageEventIds.Clear();
        }

        public bool ApplyDamage(in DamageEvent damageEvent)
        {
            if (!IsAlive || !damageEvent.IsValid)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(damageEvent.TargetEntityId)
                && !string.Equals(damageEvent.TargetEntityId, entityId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!appliedDamageEventIds.Add(damageEvent.EventId))
            {
                return false;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageEvent.Amount);
            Damaged?.Invoke(damageEvent);
            if (CurrentHealth <= 0f)
            {
                Died?.Invoke(damageEvent);
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Player.Events;
using Mirror;
using UnityEngine;

namespace Game.Player.Health
{
    // TODO: ServerMovePlayer resets observed position?
    public class PlayerHealthModule : DamageTarget
    {
        [Header("Objects")]
        public PlayerBase player;
        public PlayerHealthConfig config;

        [Header("Runtime")]
        [SyncVar(hook = nameof(OnHealthChange))] public float health;
        public Dictionary<Guid, float> invincibilityTimers;

        [HideInInspector] public Stack<Damage> damageHistory;
        private Stack<KeyValuePair<Guid, float>> _invincibilityRequests;

        [Server]
        public void ClearInvincibility()
        {
            _invincibilityRequests.Clear();
            invincibilityTimers.Clear();
        }

        [Server]
        public void RequestInvincibility(Guid source, float duration)
        {
            _invincibilityRequests.Push(new(source, duration));
        }

        public void ResetHealth()
        {
            damageHistory?.Clear();
            health = config.maxHealth;
        }

        public override void OnStartServer()
        {
            _invincibilityRequests = new();
            invincibilityTimers = new();

            damageHistory = new();
            family = Guid.NewGuid();
        }

        public override bool ApplyDamage(Damage damage)
        {
            if (invincibilityTimers.ContainsKey(damage.source)) return false;

            var sharingFamily = family != Guid.Empty && damage.family != Guid.Empty && family == damage.family;
            if (damage.author && !sharingFamily)
            {
                damage.author.ReportDealtDamage
                (
                    Mathf.Clamp(damage.amount, 0f, health),
                    damage.type
                );
            }

            damageHistory.Push(damage);
            health = Mathf.Clamp(health - damage.amount, 0f, config.maxHealth);
            RequestInvincibility(damage.source, config.invincibilityDuration);
            return true;
        }

        [Server]
        public void Heal(float value)
        {
            var targetHealth = Mathf.Clamp(health + value, 0f, config.maxHealth);
            var difference = targetHealth - health;

            var differenceCounter = 0f;
            while (damageHistory.Count > 0)
            {
                var damage = damageHistory.Pop();
                differenceCounter += damage.amount;

                if (differenceCounter <= difference) continue;
                damageHistory.Push(new(damage.type, differenceCounter - difference, damage.author, damage.source, damage.family));
                break;
            }

            health = targetHealth;
        }

        private void OnHealthChange(float old, float _new)
        {
            EventBus<OnPlayerHealthChanged>.Invoke(new()
            {
                healthModule = this,
                oldHealth = old,
                newHealth = _new
            });
        }

        public override void BeforeHitScan()
        {
            if (player.dead) return;

            while (_invincibilityRequests.Count > 0)
            {
                var request = _invincibilityRequests.Pop();
                if (invincibilityTimers.TryAdd(request.Key, request.Value)) continue;
                invincibilityTimers[request.Key] = request.Value;
            }

            var endedInvincibilities = new Stack<Guid>();
            foreach (var (source, timer) in invincibilityTimers.ToList())
            {
                if (timer <= 0f)
                {
                    endedInvincibilities.Push(source);
                    continue;
                }

                invincibilityTimers[source] -= Time.deltaTime;
            }

            while (endedInvincibilities.Count > 0)
            {
                var source = endedInvincibilities.Pop();
                invincibilityTimers.Remove(source);
            }
        }
    }
}
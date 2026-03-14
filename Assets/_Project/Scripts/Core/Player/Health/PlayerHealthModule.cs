using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Core.Player.Health.Events;
using Game.Core.Player.Locks;
using Mirror;
using UnityEngine;

namespace Game.Core.Player.Health
{
    [RequireComponent(typeof(PlayerCore))]
    public class PlayerHealthModule : DamageTarget
    {
        public PlayerCore player;
        public PlayerHealthConfig config;

        [Header("Runtime")]
        [SyncVar(hook = nameof(OnHealthChange))] public float health;
        public Dictionary<Guid, float> invincibilityTimers;

        [HideInInspector] public Stack<Damage> damageHistory;
        private Stack<KeyValuePair<Guid, float>> _invincibilityRequests;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;
            player = GetComponent<PlayerCore>();
        }

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
            ResetHealth();

            player.locks.onLockStateChange.AddListener((plock, locked) =>
            {
                if (plock == PlayerLock.Health) active = !locked;
            });
        }

        public override bool ApplyDamage(Damage damage)
        {
            if (player.locks.Locked(PlayerLock.Health)) return false;
            if (invincibilityTimers.ContainsKey(damage.identification.guid)) return false;

            EventBus<OnPlayerDamage>.Invoke(new()
            {
                player = player,
                damage = damage,
                finalAmount = Mathf.Clamp(damage.amount, 0f, health)
            });

            damageHistory.Push(damage);
            health = Mathf.Clamp(health - damage.amount, 0f, config.maxHealth);
            RequestInvincibility(damage.identification.guid, config.invincibilityDuration);
            return true;
        }

        [Server]
        public void Heal(float value)
        {
            if (player.locks.Locked(PlayerLock.Health)) return;
            var targetHealth = Mathf.Clamp(health + value, 0f, config.maxHealth);
            var difference = targetHealth - health;

            var differenceCounter = 0f;
            while (damageHistory.Count > 0)
            {
                var damage = damageHistory.Pop();
                differenceCounter += damage.amount;

                if (differenceCounter <= difference) continue;
                damage.amount = differenceCounter - difference;
                damageHistory.Push(damage);
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
            if (player.deathModule.Dead) return;

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
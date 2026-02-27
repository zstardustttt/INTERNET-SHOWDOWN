using System;
using System.Collections.Generic;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Player.Events;
using Mirror;
using UnityEngine;

namespace Game.Player
{
    // TODO: ServerMovePlayer resets observed position?
    public class PlayerHealthModule : DamageTarget
    {
        [Header("Objects")]
        public PlayerBase player;

        [Header("Runtime")]
        [SyncVar(hook = nameof(OnHealthChange))] public float health;
        public bool invincible;

        [HideInInspector] public Stack<Damage> damageHistory;

        private float _invincibleTimer;
        private bool _wasHit;

        [Server]
        public void ForceRemoveInvincibility()
        {
            _invincibleTimer = 0f;
            invincible = false;
            active = true;
        }

        [Server]
        public void ActivateInvincibility(float duration)
        {
            _invincibleTimer = duration;
            invincible = true;
            active = false;
        }

        // TODO: PlayerDamageTargetConfig
        public void ResetHealth()
        {
            damageHistory?.Clear();
            health = player.config.maxHealth;
        }

        public override void OnStartServer()
        {
            damageHistory = new();
            family = Guid.NewGuid();
        }

        private void Update()
        {
            if (!player.dead)
            {
                _invincibleTimer -= Time.deltaTime;
                if (_invincibleTimer <= 0f && invincible)
                {
                    invincible = false;
                    active = true;
                }
            }
        }

        public override bool ApplyDamage(Damage damage)
        {
            if (invincible) return false;

            var sharingFamily = family != Guid.Empty && damage.family != Guid.Empty && family == damage.family;
            if (damage.author && !sharingFamily)
            {
                damage.author.ReportDealtDamage
                (
                    Mathf.Min(health, damage.amount),
                    damage.type
                );
            }

            damageHistory.Push(damage);
            health -= damage.amount;
            _wasHit = true;
            return true;
        }

        [Server]
        public void Heal(float value)
        {
            var targetHealth = Mathf.Clamp(health + value, 0f, player.config.maxHealth);
            var difference = targetHealth - health;

            var differenceCounter = 0f;
            while (damageHistory.Count > 0)
            {
                var damage = damageHistory.Pop();
                differenceCounter += damage.amount;

                if (differenceCounter <= difference) continue;
                damageHistory.Push(new(damage.author, damage.type, differenceCounter - difference, damage.family));
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
            if (_wasHit) ActivateInvincibility(player.config.damageInvincibilityDuration);
            _wasHit = false;
        }
    }
}
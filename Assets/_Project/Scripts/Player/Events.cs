using System;
using Game.Core.Damages;
using Game.Core.Events;
using Game.Player.Death;
using Game.Player.Health;

namespace Game.Player.Events
{
    public struct OnPlayerHealthChanged : IEvent
    {
        public PlayerHealthModule healthModule;
        public float oldHealth;
        public float newHealth;
    }

    public struct OnPlayerDamage : IEvent
    {
        public PlayerBase player;
        public Damage damage;
        public float finalAmount;
    }

    public struct OnLocalPlayerAddedToMap : IEvent { }
    public struct OnLocalPlayerRemovedFromMap : IEvent { }
    public struct OnLocalPlayerDealtDamage : IEvent
    {
        public PlayerIdentification target;
        public DamageIdentification source;
        public DamageType type;
        public float amount;
    }

    public struct OnPlayerInitialized : IEvent
    {
        public PlayerBase player;
    }

    public struct OnPlayerDestroy : IEvent
    {
        public Guid guid;
    }

    public struct OnPlayerStatsChanged : IEvent
    {
        public PlayerBase player;
        public PlayerStats previous;
        public PlayerStats current;
    }

    public struct OnCameraShakerSpawn : IEvent
    {
        public CameraShaker shaker;
    }

    public struct OnDash : IEvent
    {
        public PlayerBase player;
        public float cooldown;
    }

    public struct OnEndDash : IEvent
    {
        public PlayerBase player;
        public bool reset;
    }

    public struct OnItemUsed : IEvent
    {
        public PlayerBase player;
        public bool fullyUsed;
    }

    public struct OnPlayerRespawnInvincibilityStateChanged : IEvent
    {
        public PlayerBase player;
        public RespawnInvincibilityState state;
    }
}
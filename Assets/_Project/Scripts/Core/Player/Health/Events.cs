using Game.Core.Damages;
using Game.Core.Events;

namespace Game.Core.Player.Health.Events
{
    public struct OnPlayerHealthChanged : IEvent
    {
        public PlayerHealthModule healthModule;
        public float oldHealth;
        public float newHealth;
    }

    public struct OnPlayerDamage : IEvent
    {
        public PlayerCore player;
        public Damage damage;
        public float finalAmount;
    }
}
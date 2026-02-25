using Game.Core.Events;

namespace Game.Core.Damages.Events
{
    public struct DamageEvent : IEvent
    {
        public DamageSource source;
        public DamageTarget target;
        public Damage damage;

        public DamageEvent(DamageSource source, DamageTarget target, Damage damage)
        {
            this.source = source;
            this.target = target;
            this.damage = damage;
        }
    }
}
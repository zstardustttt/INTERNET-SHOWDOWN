using Game.Core.Events;
using Game.Core.Projectiles;

namespace Game.Events.HitWatcher
{
    public struct OnDamageDealerCreate : IEvent
    {
        public DamageDealer dealer;
    }

    public struct OnDamageDealerDestroy : IEvent
    {
        public DamageDealer dealer;
    }
}
using Game.Core.Damages;
using Game.Core.Events;
using Game.Core.Player;

namespace Game.Player.Online.Events
{
    public struct OnLocalPlayerAddedToMap : IEvent { }
    public struct OnLocalPlayerRemovedFromMap : IEvent { }
    public struct OnLocalPlayerDealtDamage : IEvent
    {
        public PlayerIdentification target;
        public DamageIdentification source;
        public DamageType type;
        public float amount;
    }

    public struct OnLocalPlayerDash : IEvent
    {
        public float cooldown;
    }

    public struct OnLocalPlayerEndDash : IEvent
    {
        public bool reset;
    }

    public struct OnCameraShakerSpawn : IEvent
    {
        public CameraShaker shaker;
    }
}
using Game.Core.Events;

namespace Game.Core.Player.Death.Events
{
    public struct OnPlayerRespawnInvincibilityStateChanged : IEvent
    {
        public PlayerCore player;
        public RespawnInvincibilityState state;
    }
}
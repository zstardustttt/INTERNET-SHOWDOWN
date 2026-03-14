using Game.Core.Events;

namespace Game.Core.Player.Events
{
    public struct OnPlayerDestroy : IEvent
    {
        public PlayerIdentification identification;
    }

    public struct OnPlayerInitialized : IEvent
    {
        public PlayerCore player;
    }
}
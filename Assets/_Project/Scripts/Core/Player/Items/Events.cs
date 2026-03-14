using Game.Core.Events;

namespace Game.Core.Player.Items.Events
{
    public struct OnItemUsed : IEvent
    {
        public PlayerCore player;
        public bool fullyUsed;
    }
}
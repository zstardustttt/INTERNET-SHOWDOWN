using Game.Core.Events;

namespace Game.GameLoop.Events
{
    public struct OnGameStateChange : IEvent
    {
        public GameState state;
    }
}
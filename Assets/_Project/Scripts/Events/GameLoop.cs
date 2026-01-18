using Game.Core.Events;
using Game.Systems;

namespace Game.Events.GameLoop
{
    public struct OnGameStateChange : IEvent
    {
        public GameState state;
    }
}
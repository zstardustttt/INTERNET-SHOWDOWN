using Game.Core.Events;
using Game.Gameplay;

namespace Game.Events.GameLoop
{
    public struct OnGameStateChange : IEvent
    {
        public GameState state;
    }
}
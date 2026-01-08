using Game.Core.Events;

namespace Game.Events.BoxSpawner
{
    public struct SetBoxSpawnerActive : IEvent
    {
        public bool active;
    }
}
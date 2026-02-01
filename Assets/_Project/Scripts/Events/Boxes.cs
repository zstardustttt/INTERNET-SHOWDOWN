using Game.Core.Events;
using Game.Other;

namespace Game.Events.Boxes
{
    public struct SetBoxSpawnerActive : IEvent
    {
        public bool active;
    }

    public struct OnBoxSpawn : IEvent
    {
        public ItemBox box;
    }

    public struct OnBoxDestroy : IEvent
    {
        public ItemBox box;
    }
}
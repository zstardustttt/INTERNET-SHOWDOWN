using Game.Core.Events;

namespace Game.Boxes.Events
{
    public struct OnBoxSpawn : IEvent
    {
        public ItemBox box;
    }

    public struct OnBoxDestroy : IEvent
    {
        public ItemBox box;
    }
}
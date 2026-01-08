using UnityEngine;

namespace Game.Core.Items
{
    public static class ItemPool
    {
        public static ItemConfig[] items;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            items = Resources.LoadAll<ItemConfig>("");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Core.Items
{
    public static class ItemPool
    {
        public static List<ItemConfig>[] items;
        public static ItemRarity[] rarities;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            rarities = Resources.LoadAll<ItemRarity>("");
            Debug.Log($"Loaded rarities: {string.Join(' ', rarities.Select(x => x.name))}");

            var includeItems = Resources.LoadAll<ItemConfig>("")
                .Where(x => x.include).ToArray();

            items = new List<ItemConfig>[rarities.Length];
            foreach (var item in includeItems)
            {
                var rarityIdx = Array.IndexOf(rarities, item.rarity);
                if (items[rarityIdx] == null)
                {
                    items[rarityIdx] = new() { item };
                    continue;
                }

                items[rarityIdx].Add(item);
            }
        }
    }
}
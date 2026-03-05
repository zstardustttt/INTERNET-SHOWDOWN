using UnityEngine;

namespace Game.Core.Items
{
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "Items/Item Config", order = 0)]
    public class ItemConfig : ScriptableObject
    {
        public bool include = true;
        public GameObject prefab;
        public ItemRarity rarity;
        public string displayName;
        public ItemSeries series;
        [TextArea] public string description;
        public ItemTag[] tags;
    }
}
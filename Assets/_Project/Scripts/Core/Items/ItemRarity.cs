using UnityEngine;

namespace Game.Core.Items
{
    [CreateAssetMenu(fileName = "ItemRarity", menuName = "Items/Item Rarity", order = 0)]
    public class ItemRarity : ScriptableObject
    {
        public Color color;
    }
}
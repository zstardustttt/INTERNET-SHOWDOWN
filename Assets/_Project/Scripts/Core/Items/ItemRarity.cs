using UnityEngine;

namespace Game.Core.Items
{
    [CreateAssetMenu(fileName = "Item Rarity", menuName = "ItemRarity", order = 0)]
    public class ItemRarity : ScriptableObject
    {
        public Color color;
    }
}
using UnityEngine;

namespace Game.Core.Items
{
    [CreateAssetMenu(fileName = "ItemSeries", menuName = "Items/Item Series", order = 0)]
    public class ItemSeries : ScriptableObject
    {
        public string displayName;
    }
}
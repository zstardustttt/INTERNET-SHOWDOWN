using UnityEngine;

namespace Game.Core.Items
{
    [CreateAssetMenu(fileName = "ItemTag", menuName = "Item Tag", order = 0)]
    public class ItemTag : ScriptableObject
    {
        public string tagName;
    }
}
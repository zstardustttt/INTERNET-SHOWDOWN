using UnityEngine;

namespace Game.Core.Items
{
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "Item Config", order = 0)]
    public class ItemConfig : ScriptableObject
    {
        public GameObject prefab;
    }
}
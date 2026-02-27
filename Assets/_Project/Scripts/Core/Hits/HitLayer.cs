using UnityEngine;

namespace Game.Core.Hits
{
    [CreateAssetMenu(fileName = "HitLayer", menuName = "Hit Layer", order = 0)]
    public class HitLayer : ScriptableObject
    {
        public int cachedIndex;
    }
}
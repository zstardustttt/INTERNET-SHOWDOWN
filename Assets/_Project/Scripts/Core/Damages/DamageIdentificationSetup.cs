using UnityEngine;

namespace Game.Core.Damages
{
    [CreateAssetMenu(fileName = "DamageIdentificationSetup", menuName = "Damages/Damage Identification Setup", order = 0)]
    public class DamageIdentificationSetup : ScriptableObject
    {
        public string displayName;
    }
}
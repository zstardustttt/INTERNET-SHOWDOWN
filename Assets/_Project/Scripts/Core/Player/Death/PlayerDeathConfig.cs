using UnityEngine;

namespace Game.Core.Player.Death
{
    [CreateAssetMenu(fileName = "PlayerDeathConfig", menuName = "Player/Player Death Config", order = 0)]
    public class PlayerDeathConfig : ScriptableObject
    {
        public Material respawnEffectMaterial;
        public float ascendSpeed;
        public float awakeInvincibilityDuration;
        public float endingInvincibilityDuration;
        public float moveThreshold;
    }
}
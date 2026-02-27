using UnityEngine;

namespace Game.Player.Health
{
    [CreateAssetMenu(fileName = "PlayerHealthConfig", menuName = "Player/Player Health Config", order = 0)]
    public class PlayerHealthConfig : ScriptableObject
    {
        [Header("Health")]
        public float maxHealth;
        public float invincibilityDuration;
    }
}
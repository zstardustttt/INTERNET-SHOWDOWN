using UnityEngine;

namespace Game.Player.Death
{
    [CreateAssetMenu(fileName = "PlayerDeathConfig", menuName = "Player/Player Death Config", order = 0)]
    public class PlayerDeathConfig : ScriptableObject
    {
        public float ascendSpeed;
    }
}
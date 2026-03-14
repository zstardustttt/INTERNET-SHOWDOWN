namespace Game.Core.Player
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/Player Config", order = 0)]
    public class PlayerConfig : ScriptableObject
    {
        public float hitCapsuleRadius;
        public float hitCapsuleOffset;
        public float hitCapsuleHeight;

        [Space(9)]
        public int triggerBufferCapacity;
        public LayerMask localTriggerLayerMask;
    }
}
using Game.Core.Events;
using Game.Player.Events;
using UnityEngine;

namespace Game.Player
{
    public class CameraShaker : MonoBehaviour
    {
        [Min(0f)] public float amplitude;
        [Min(0f)] public float minDistance;
        [Min(0f)] public float maxDistance;

        private void Awake()
        {
            EventBus<OnCameraShakerSpawn>.Invoke(new() { shaker = this });
        }
    }
}
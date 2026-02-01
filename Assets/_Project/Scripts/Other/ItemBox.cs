using Game.Core.Events;
using Game.Events.Boxes;
using UnityEngine;

namespace Game.Other
{
    public class ItemBox : MonoBehaviour
    {
        public MeshFilter outerFieldFilter;
        public MeshFilter outlineFilter;
        public Transform visual;

        [HideInInspector] public float hash;
        [HideInInspector] public int meshIdx;
        [HideInInspector] public float timer;

        private void Awake()
        {
            hash = transform.position.magnitude / 3f;
            timer = hash;
            EventBus<OnBoxSpawn>.Invoke(new() { box = this });
        }

        private void OnDestroy()
        {
            EventBus<OnBoxDestroy>.Invoke(new() { box = this });
        }
    }
}
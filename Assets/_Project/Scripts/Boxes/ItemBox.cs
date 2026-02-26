using Game.Boxes.Events;
using Game.Core.Events;
using Game.Core.Hits;
using UnityEngine;

namespace Game.Boxes
{
    // TODO: implement with layers
    public class ItemBox : HitListener
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
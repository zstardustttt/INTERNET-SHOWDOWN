using Game.Core.Hits.Events;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Hits
{
    public class HitListener : NetworkBehaviour
    {
        public bool Active { get; private set; }

        public HitEntity hitEntity;
        public bool active = true;
        public HitLayer layer;
        public UnityEvent<HitEvent> onHit = new();
        [HideInInspector] public UnityEvent beforeHitScan = new();

        public void UpdateActivity()
        {
            Active = gameObject.activeInHierarchy && enabled && active;
        }

        public virtual void BeforeHitScan() { }
    }
}
using Game.Core.Hits.Events;
using Mirror;
using UnityEngine.Events;

namespace Game.Core.Hits
{
    public class HitListener : NetworkBehaviour
    {
        public bool Active { get; private set; }

        public HitEntity hitEntity;
        public bool active = true;
        public UnityEvent<HitEvent> onHit = new();
        public UnityEvent beforeHitScan = new();

        public void UpdateActivity()
        {
            Active = gameObject.activeInHierarchy && enabled && active;
        }

        public virtual void BeforeHitScan() { }
    }
}
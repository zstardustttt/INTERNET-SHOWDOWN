using System;
using Game.Core.Events;
using Game.Core.Hits.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core.Hits
{
    public abstract class HitEntity : MonoBehaviour
    {
        public abstract Collider Collider { get; }
        public bool Active { get; private set; }
        public bool SourcesActive { get; private set; }
        public bool TargetsActive { get; private set; }

        public bool active = true;
        public HitListener[] sources;
        public HitListener[] targets;
        public UnityEvent<HitEvent> onHit = new();

        [HideInInspector] public Guid guid;
        [HideInInspector] public Vector3 previousObservedPosition;
        [HideInInspector] public Vector3 observedDelta;
        [HideInInspector] public bool completedSelfChecks;

        public abstract int CastNonAlloc(Vector3 origin, Vector3 direction, float length, LayerMask layerMask, RaycastHit[] results, QueryTriggerInteraction triggerInteraction);
        public abstract int OverlapNonAlloc(Vector3 origin, LayerMask layerMask, Collider[] results, QueryTriggerInteraction triggerInteraction);

        protected virtual void OnValidate()
        {
            Collider.isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer("HitEntity");

            foreach (var source in sources)
            {
                if (!source) continue;
                source.hitEntity = this;
            }

            foreach (var target in targets)
            {
                if (!target) continue;
                target.hitEntity = this;
            }
        }

        private void Awake()
        {
            previousObservedPosition = transform.position;
            EventBus<OnHitEntityCreate>.Invoke(new() { entity = this });
        }

        private void OnDestroy()
        {
            EventBus<OnHitEntityDestroy>.Invoke(new() { guid = guid });
        }

        public void UpdateActivity(bool sourcesActive, bool targetsActive)
        {
            Active = gameObject.activeInHierarchy && enabled && active;
            if (!Active)
            {
                SourcesActive = false;
                TargetsActive = false;
                return;
            }

            SourcesActive = sourcesActive;
            TargetsActive = targetsActive;
        }
    }
}
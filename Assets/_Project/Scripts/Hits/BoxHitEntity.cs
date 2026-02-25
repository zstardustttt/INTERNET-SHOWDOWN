using UnityEngine;

namespace Game.Core.Hits
{
    [RequireComponent(typeof(BoxCollider))]
    public class BoxHitEntity : HitEntity
    {
        public BoxCollider boxCollider;
        public override Collider Collider => boxCollider;

        protected override void OnValidate()
        {
            boxCollider = GetComponent<BoxCollider>();
            base.OnValidate();
        }

        public override int CastNonAlloc(Vector3 origin, Vector3 direction, float length, LayerMask layerMask, RaycastHit[] results, QueryTriggerInteraction triggerInteraction)
        {
            var halfExtents = boxCollider.size / 2f;
            return Physics.BoxCastNonAlloc(origin, halfExtents, direction, results, transform.rotation, length, layerMask, triggerInteraction);
        }

        public override int OverlapNonAlloc(Vector3 origin, LayerMask layerMask, Collider[] results, QueryTriggerInteraction triggerInteraction)
        {
            var halfExtents = boxCollider.size / 2f;
            return Physics.OverlapBoxNonAlloc(origin, halfExtents, results, transform.rotation, layerMask, triggerInteraction);
        }
    }
}
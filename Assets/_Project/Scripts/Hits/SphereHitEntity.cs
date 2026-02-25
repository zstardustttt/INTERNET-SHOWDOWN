using UnityEngine;

namespace Game.Core.Hits
{
    [RequireComponent(typeof(SphereCollider))]
    public class SphereHitEntity : HitEntity
    {
        public SphereCollider sphereCollider;
        public override Collider Collider => sphereCollider;

        protected override void OnValidate()
        {
            sphereCollider = GetComponent<SphereCollider>();
            base.OnValidate();
        }

        public override int CastNonAlloc(Vector3 origin, Vector3 direction, float length, LayerMask layerMask, RaycastHit[] results, QueryTriggerInteraction triggerInteraction)
        {
            return Physics.SphereCastNonAlloc(origin, sphereCollider.radius, direction, results, length, layerMask, triggerInteraction);
        }

        public override int OverlapNonAlloc(Vector3 origin, LayerMask layerMask, Collider[] results, QueryTriggerInteraction triggerInteraction)
        {
            return Physics.OverlapSphereNonAlloc(origin, sphereCollider.radius, results, layerMask, triggerInteraction);
        }
    }
}
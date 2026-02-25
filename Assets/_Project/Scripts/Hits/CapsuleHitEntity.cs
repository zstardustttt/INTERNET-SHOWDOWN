using UnityEngine;

namespace Game.Core.Hits
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class CapsuleHitEntity : HitEntity
    {
        public CapsuleCollider capsuleCollider;
        public override Collider Collider => capsuleCollider;

        protected override void OnValidate()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            base.OnValidate();
        }

        public override int CastNonAlloc(Vector3 origin, Vector3 direction, float length, LayerMask layerMask, RaycastHit[] results, QueryTriggerInteraction triggerInteraction)
        {
            var halfVector = GetHalfVector();
            var point1 = origin + capsuleCollider.center - halfVector;
            var point2 = origin + capsuleCollider.center + halfVector;
            return Physics.CapsuleCastNonAlloc(point1, point2, capsuleCollider.radius, direction, results, length, layerMask, triggerInteraction);
        }

        public override int OverlapNonAlloc(Vector3 origin, LayerMask layerMask, Collider[] results, QueryTriggerInteraction triggerInteraction)
        {
            var halfVector = GetHalfVector();
            var point1 = origin + capsuleCollider.center - halfVector;
            var point2 = origin + capsuleCollider.center + halfVector;
            return Physics.OverlapCapsuleNonAlloc(point1, point2, capsuleCollider.radius, results, layerMask, triggerInteraction);
        }

        private Vector3 GetHalfVector()
        {
            var halfHeight = Mathf.Max(capsuleCollider.height * 0.5f, capsuleCollider.radius);
            var radius = capsuleCollider.radius;

            if (capsuleCollider.direction == 0) // X-axis
                return capsuleCollider.transform.right * (halfHeight - radius);
            else if (capsuleCollider.direction == 1) // Y-axis (default)
                return capsuleCollider.transform.up * (halfHeight - radius);
            else if (capsuleCollider.direction == 2) // Z-axis
                return capsuleCollider.transform.forward * (halfHeight - radius);

            Debug.LogWarning($"Invalid capsule collider direction {capsuleCollider.direction} of {capsuleCollider.gameObject.name}");
            return Vector3.zero;
        }
    }
}
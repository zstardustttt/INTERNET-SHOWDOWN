using Game.Core.Damage;
using Game.Core.Projectiles;
using UnityEngine;

namespace Game.Projectiles
{
    public class BOOMerangProjectle : Projectile
    {
        public float loopDuration;
        public float wishPositionDistance;

        [HideInInspector] public Vector3 spawnOwnerVelocity;
        [HideInInspector] public Vector3 wishPosition;
        [HideInInspector] public Vector3 endPosition;

        protected override void OnCollision(Vector3 point, Vector3 normal, Collider other)
        {
        }

        protected override void OnDealerHit(DamageDealer dealer, DamageReceiver player, float damage)
        {
        }

        protected override void OnUpdate()
        {
            var t = _lifetime / loopDuration;
            /*var ac = Vector3.Lerp(_spawnPosition, wishPosition, t);
            var cb = Vector3.Lerp(wishPosition, endPosition, t);

            rb.MovePosition(Vector3.Lerp(ac, cb, t));*/
            rb.linearVelocity = spawnOwnerVelocity + (1f - t) * (wishPosition - _spawnPosition) + t * (endPosition - wishPosition);
        }
    }
}
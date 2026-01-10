using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class HuananV2Projectile : Projectile
    {
        public Rigidbody rb;
        public float speed;
        public float rotationSpeed;
        public float redirectionTimeWindow;
        public LayerMask layerMask;

        private Vector3 _direction;
        private float _lifetime;

        private void Awake()
        {
            _direction = transform.forward;
        }

        private void Update()
        {
            if (!NetworkServer.active) return;
            if (_lifetime <= redirectionTimeWindow && Physics.Raycast(owner.verticalOrientation.position, owner.verticalOrientation.forward, out var hitinfo, 1000f, layerMask, QueryTriggerInteraction.Ignore))
            {
                _direction = (hitinfo.point - transform.position).normalized;
            }

            rb.linearVelocity = _direction * speed;
            rb.angularVelocity = _direction * rotationSpeed;

            _lifetime += Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
            NetworkServer.Destroy(gameObject);
        }

        public override void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage) { }
    }
}
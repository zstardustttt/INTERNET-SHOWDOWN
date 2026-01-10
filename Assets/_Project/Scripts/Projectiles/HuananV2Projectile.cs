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
        public Transform visual;

        [SyncVar] private Vector3 _direction;
        private float _lifetime;

        public override void OnStartServer()
        {
            _direction = transform.forward;
        }

        private void Update()
        {
            visual.Rotate(_direction, rotationSpeed * Time.deltaTime, Space.World);

            if (!NetworkServer.active) return;

            if (_lifetime <= redirectionTimeWindow && Physics.Raycast(owner.verticalOrientation.position, owner.verticalOrientation.forward, out var hitinfo, 1000f, layerMask, QueryTriggerInteraction.Ignore))
            {
                _direction = (hitinfo.point - transform.position).normalized;
            }

            rb.linearVelocity = _direction * speed;
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
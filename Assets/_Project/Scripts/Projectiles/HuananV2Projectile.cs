using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class HuananV2Projectile : Projectile
    {
        public Rigidbody rb;
        public BoxCollider bc;
        public float speed;
        public float rotationSpeed;
        public LayerMask layerMask;
        public Transform visual;

        [SyncVar] private Vector3 _direction;

        public override void OnStartServer()
        {
            if (Physics.BoxCast(_owner.verticalOrientation.position, bc.size / 2f, _owner.verticalOrientation.forward, out var hitinfo, transform.rotation, 1000f, layerMask, QueryTriggerInteraction.Ignore))
                _direction = (hitinfo.point - transform.position).normalized;
            else _direction = transform.forward;

            rb.linearVelocity = _direction * speed;
            foreach (var dealer in damageDealers)
            {
                dealer.velocity = rb.linearVelocity;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
            NetworkServer.Destroy(gameObject);
        }

        protected override void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage) { }

        protected override void OnUpdate()
        {
            visual.Rotate(_direction, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}
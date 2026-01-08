using Game.Core.Projectiles;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Projectiles
{
    public class HuananV2Projectile : NetworkBehaviour
    {
        public Rigidbody rb;
        public float speed;
        public float rotationSpeed;
        public float redirectionTimeWindow;
        public LayerMask layerMask;
        public DamageDealer dealer;

        private Vector3 _direction;
        private float _lifetime;

        private void Awake()
        {
            _direction = transform.forward;
        }

        private void Update()
        {
            if (!NetworkServer.active) return;
            if (_lifetime <= redirectionTimeWindow && Physics.Raycast(dealer.owner.verticalOrientation.position, dealer.owner.verticalOrientation.forward, out var hitinfo, 1000f, layerMask, QueryTriggerInteraction.Ignore))
            {
                _direction = (hitinfo.point - transform.position).normalized;
            }

            rb.linearVelocity = _direction * speed;
            rb.angularVelocity = _direction * rotationSpeed;

            _lifetime += Time.deltaTime;
        }

        // TODO: hit other projectiles
        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active) return;
            if (other.TryGetComponent(out PlayerBase hittedPlayer))
            {
                if (hittedPlayer == dealer.owner) return;
                hittedPlayer.health -= dealer.EvaluateDamage(hittedPlayer);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!NetworkServer.active) return;
            NetworkServer.Destroy(gameObject);
        }
    }
}
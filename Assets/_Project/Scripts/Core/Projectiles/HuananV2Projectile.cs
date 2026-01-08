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
        public LayerMask layerMask;
        public DamageDealer dealer;

        public override void OnStartServer()
        {
            if (Physics.Raycast(dealer.owner.verticalOrientation.position, dealer.owner.verticalOrientation.forward, out var hitinfo, 1000f, layerMask, QueryTriggerInteraction.Ignore))
            {
                var direction = (hitinfo.point - dealer.owner.itemHolder.position).normalized;
                rb.linearVelocity = direction * speed;
                rb.angularVelocity = direction * rotationSpeed;
            }
            else
            {
                rb.linearVelocity = transform.forward * speed;
                rb.angularVelocity = transform.forward * rotationSpeed;
            }
        }

        // TODO: hit other projectiles
        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active) return;
            if (other.TryGetComponent(out PlayerBase hittedPlayer))
            {
                if (hittedPlayer == dealer.owner) return;
                hittedPlayer.health -= dealer.EvaluateDamage(hittedPlayer);
                dealer.OnDamageDealt.Invoke(hittedPlayer);
            }

            NetworkServer.Destroy(gameObject);
        }
    }
}
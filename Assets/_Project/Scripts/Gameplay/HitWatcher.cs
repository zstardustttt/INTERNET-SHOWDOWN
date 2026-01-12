using UnityEngine;
using Game.Core.Projectiles;
using System.Collections.Generic;
using System;
using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Core.Maps;
using Game.Player;
using Mirror;
using Random = UnityEngine.Random;
using Game.Core.Items;

namespace Game.Gameplay
{
    public class HitWatcher : MonoBehaviour
    {
        public LayerMask playerDealerCheckLayerMask;
        public LayerMask playerBoxCheckLayerMask;

        private List<DamageDealer> _dealers;

        private Guid _onDamageDealerCreateListenerGuid;
        private Guid _onDamageDealerDestroyListenerGuid;

        public void Awake()
        {
            _dealers = new();

            _onDamageDealerCreateListenerGuid = EventBus<OnDamageDealerCreate>.Listen((data) => _dealers.Add(data.dealer));
            _onDamageDealerDestroyListenerGuid = EventBus<OnDamageDealerDestroy>.Listen((data) => _dealers.Remove(data.dealer));
        }

        public void OnDestroy()
        {
            EventBus<OnDamageDealerCreate>.TryCancel(_onDamageDealerCreateListenerGuid);
            EventBus<OnDamageDealerDestroy>.TryCancel(_onDamageDealerDestroyListenerGuid);
        }

        private void FixedUpdate()
        {
            if (MapLoader.loadedMap == null) return;
            var players = MapLoader.loadedMap.players;

            foreach (var player in players)
            {
                foreach (var dealer in _dealers)
                {
                    if (player.invincible) break;
                    if (dealer.owner == player) continue;
                    PlayerDealerCheck(player, dealer);
                }

                if (player.itemIndex != -1) continue;
                var box = PlayerBoxCheck(player);
                if (box)
                {
                    player.itemIndex = Random.Range(0, ItemPool.items.Length);
                    NetworkServer.Destroy(box);
                }
            }
        }

        private GameObject PlayerBoxCheck(PlayerBase player)
        {
            var radius = player.motor.Capsule.radius;
            var p1 = player.transform.position + Vector3.up * radius;
            var p2 = player.transform.position + Vector3.up * (player.motor.Capsule.height - radius);
            var velDir = player.serverObservedVelocity.normalized;
            var delta = player.serverObservedVelocity.magnitude * Time.fixedDeltaTime;

            if (!Physics.CapsuleCast(p1, p2, radius, velDir, out var hit, delta, playerBoxCheckLayerMask, QueryTriggerInteraction.Collide))
                return null;

            return hit.collider.gameObject;
        }

        private void PlayerDealerCheck(PlayerBase player, DamageDealer dealer)
        {
            player.gameObject.layer = LayerMask.NameToLayer("PlayerCheckingForHit");

            var relativeVel = dealer.velocity - player.serverObservedVelocity;
            var delta = relativeVel * Time.fixedDeltaTime;

            var deltaLength = delta.magnitude;

            bool didHit;
            RaycastHit hit;
            if (dealer.coll is BoxCollider bc)
                didHit = Physics.BoxCast(dealer.transform.position, bc.size / 2f, delta.normalized, out hit, bc.transform.rotation, deltaLength, playerDealerCheckLayerMask);
            else if (dealer.coll is SphereCollider sc)
                didHit = Physics.SphereCast(dealer.transform.position, sc.radius, delta.normalized, out hit, deltaLength, playerDealerCheckLayerMask);
            else
            {
                Debug.LogError("Collider not supported");
                return;
            }

            if (!didHit) return;
            var hitPoint = Vector3.Lerp(dealer.transform.position, dealer.transform.position + dealer.velocity * Time.fixedDeltaTime, hit.distance / deltaLength);
            var damage = dealer.EvaluateDamage(player);
            player.health -= damage;
            dealer.OnHit.Invoke(player, damage);
            player.TargetOnHit(dealer.owner.netIdentity.connectionToClient);

            Debug.Log($"Hit! {hit.collider.gameObject.name} {hitPoint}");

            player.gameObject.layer = LayerMask.NameToLayer("Player");
        }
    }
}
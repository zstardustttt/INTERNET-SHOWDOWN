using UnityEngine;
using Game.Core.Projectiles;
using System.Collections.Generic;
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
        public float castMargin;

        private List<DamageDealer> _dealers;

        public void Awake()
        {
            _dealers = new();

            EventBus<OnDamageDealerCreate>.Listen((data) => _dealers.Add(data.dealer));
            EventBus<OnDamageDealerDestroy>.Listen((data) => _dealers.Remove(data.dealer));
            EventBus<RequestTwoPointsDealerCheck>.Listen((data) => TwoPointsDealerCheck(data.dealer, data.point1, data.point2));
        }

        private void Update()
        {
            if (MapLoader.loadedMap == null) return;
            var players = MapLoader.loadedMap.players;

            foreach (var player in players)
            {
                player.observedDelta = player.transform.position - player.previousObservedPosition;

                foreach (var dealer in _dealers)
                {
                    dealer.observedDelta = dealer.transform.position - dealer.previousObservedPosition;
                    if (player.invincible) break;
                    if (dealer.owner == player) continue;

                    PlayerDealerCheck(player, dealer, dealer.previousObservedPosition, dealer.observedDelta);
                }

                if (player.itemIndex == -1 && PlayerBoxCheck(player, out var box))
                {
                    player.itemIndex = Random.Range(0, ItemPool.items.Length);
                    NetworkServer.Destroy(box);
                }

                player.previousObservedPosition = player.transform.position;
            }

            foreach (var dealer in _dealers)
            {
                dealer.previousObservedPosition = dealer.transform.position;
            }
        }

        private bool PlayerBoxCheck(PlayerBase player, out GameObject box)
        {
            var radius = player.motor.Capsule.radius;

            var pos = player.previousObservedPosition;
            var p1 = pos + Vector3.up * radius;
            var p2 = pos + Vector3.up * (player.motor.Capsule.height - radius);

            var velDir = player.observedDelta.normalized;
            var delta = player.observedDelta.magnitude + castMargin;

            if (!Physics.CapsuleCast(p1, p2, radius, velDir, out var hit, delta, playerBoxCheckLayerMask, QueryTriggerInteraction.Collide))
            {
                box = null;
                return false;
            }

            box = hit.collider.gameObject;
            return true;
        }

        public void TwoPointsDealerCheck(DamageDealer dealer, Vector3 point1, Vector3 point2)
        {
            if (MapLoader.loadedMap == null) return;
            var players = MapLoader.loadedMap.players;

            foreach (var player in players)
            {
                if (player.invincible || dealer.owner == player) continue;
                PlayerDealerCheck(player, dealer, point1, point2 - point1);
            }
        }

        public void PlayerDealerCheck(PlayerBase player, DamageDealer dealer, Vector3 dealerPosition, Vector3 dealerDelta)
        {
            player.gameObject.layer = LayerMask.NameToLayer("PlayerCheckingForHit");
            InsidePlayerDealerCheck(player, dealer, dealerPosition, dealerDelta);
            player.gameObject.layer = LayerMask.NameToLayer("Player");
        }

        private void InsidePlayerDealerCheck(PlayerBase player, DamageDealer dealer, Vector3 dealerPosition, Vector3 dealerDelta)
        {
            var delta = dealerDelta - player.observedDelta;
            var deltaLength = delta.magnitude + castMargin;

            bool didHit;
            RaycastHit hit;
            if (dealer.coll is BoxCollider bc)
                didHit = Physics.BoxCast(dealerPosition, bc.size / 2f, delta.normalized, out hit, bc.transform.rotation, deltaLength, playerDealerCheckLayerMask);
            else if (dealer.coll is SphereCollider sc)
                didHit = Physics.SphereCast(dealerPosition, sc.radius, delta.normalized, out hit, deltaLength, playerDealerCheckLayerMask);
            else
            {
                Debug.LogError("Collider not supported");
                return;
            }

            if (!didHit) return;
            var hitPoint = Vector3.Lerp(dealerPosition, dealerPosition + dealerDelta, hit.distance / deltaLength);
            RegisterHit(player, dealer, hitPoint);
        }

        private void RegisterHit(PlayerBase player, DamageDealer dealer, Vector3 point)
        {
            var damage = dealer.EvaluateDamage(player);
            player.health -= damage;
            dealer.OnHit.Invoke(player, damage);
            player.TargetOnHit(dealer.owner.netIdentity.connectionToClient);

            Debug.Log($"Hit! on: {player.gameObject.name} by: {dealer.owner.gameObject.name} at: {point}");
        }
    }
}
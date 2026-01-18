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
using System;

namespace Game.Systems
{
    public class HitWatcher : MonoBehaviour
    {
        public LayerMask playerDealerCheckLayerMask;
        public LayerMask playerBoxCheckLayerMask;
        public float castMargin;
        public CapsuleCollider playerPrefabCollider;

        private Dictionary<Guid, DamageDealer> _dealers;
        private List<Guid> _dealersToRemove;
        private CapsuleCollider _playerColliderForChecking;

        public void Awake()
        {
            _dealers = new();
            _dealersToRemove = new();
            InitPlayerColliderForChecking();

            EventBus<OnDamageDealerCreate>.Listen((data) => _dealers.Add(data.dealer.DealerGuid, data.dealer));
            EventBus<OnDamageDealerDestroy>.Listen((data) => _dealersToRemove.Add(data.guid));
            EventBus<RequestTwoPointsDealerCheck>.Listen((data) => TwoPointsDealerCheck(data.dealer, data.point1, data.point2));
        }

        private void InitPlayerColliderForChecking()
        {
            var playerColliderForChecking = new GameObject("Player Collider For Checking")
            {
                layer = LayerMask.NameToLayer("PlayerCheckingForHit")
            };
            _playerColliderForChecking = playerColliderForChecking.AddComponent<CapsuleCollider>();

            _playerColliderForChecking.height = playerPrefabCollider.height;
            _playerColliderForChecking.radius = playerPrefabCollider.radius;
            _playerColliderForChecking.center = playerPrefabCollider.center;
        }

        private void Update()
        {
            if (MapLoader.loadedMap == null) return;
            var players = MapLoader.loadedMap.players;

            foreach (var (_, player) in players)
            {
                if (!player) continue;
                player.observedDelta = player.transform.position - player.previousObservedPosition;

                foreach (var (_, dealer) in _dealers)
                {
                    if (!dealer) continue;

                    dealer.observedDelta = dealer.transform.position - dealer.previousObservedPosition;
                    if (player.invincible) break;
                    if (dealer.singleHitScan && dealer.hitScanCount > 0) continue;

                    PlayerDealerCheck(player, dealer, player.previousObservedPosition, player.observedDelta, dealer.previousObservedPosition, dealer.observedDelta);
                }

                if (player.itemIndex == -1 && PlayerBoxCheck(player, out var box))
                {
                    player.itemIndex = Random.Range(0, ItemPool.items.Length);
                    NetworkServer.Destroy(box);
                }

                player.previousObservedPosition = player.transform.position;
            }

            foreach (var guid in _dealersToRemove)
            {
                _dealers.Remove(guid);
            }

            foreach (var (_, dealer) in _dealers)
            {
                dealer.previousObservedPosition = dealer.transform.position;
                if (dealer.singleHitScan && dealer.hitScanCount > 0) continue;
                dealer.hitScanCount++;
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

            foreach (var (_, player) in players)
            {
                if (!player || player.invincible) continue;
                PlayerDealerCheck(player, dealer, player.previousObservedPosition, player.observedDelta, point1, point2 - point1);
            }
        }

        public void PlayerDealerCheck(PlayerBase player, DamageDealer dealer, Vector3 playerPosition, Vector3 playerDelta, Vector3 dealerPosition, Vector3 dealerDelta)
        {
            var relativePosition = dealerPosition - playerPosition;
            var relativeDelta = dealerDelta - playerDelta;

            var deltaLength = relativeDelta.magnitude + castMargin;

            bool didHit;
            RaycastHit hit;
            if (dealer.coll is BoxCollider bc)
            {
                var halfExtents = bc.size / 2f;
                var rotation = bc.transform.rotation;
                didHit = Physics.BoxCast(relativePosition, halfExtents, relativeDelta.normalized, out hit, rotation, deltaLength, playerDealerCheckLayerMask)
                || Physics.CheckBox(relativePosition, halfExtents, rotation, playerDealerCheckLayerMask);
            }
            else if (dealer.coll is SphereCollider sc)
                didHit = Physics.SphereCast(relativePosition, sc.radius, relativeDelta.normalized, out hit, deltaLength, playerDealerCheckLayerMask)
                || Physics.CheckSphere(relativePosition, sc.radius, playerDealerCheckLayerMask);
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
            if (dealer.knockbackForce != 0f)
            {
                var playerCenter = player.transform.position + player.motor.Capsule.center;
                var direction = (playerCenter - dealer.transform.position).normalized;
                player.TargetKnockback(direction * dealer.knockbackForce);
            }

            if (dealer.owner == player && !dealer.canDamageOwner) return;

            var damage = dealer.EvaluateDamage(player);
            player.Damage(damage, dealer.owner);
            dealer.OnHit.Invoke(player, damage);

            var direct = dealer.Direct;
            if (dealer.owner && dealer.owner != player)
            {
                dealer.owner.TargetOnHit();
                if (direct) dealer.owner.stats.directHits++;
                else dealer.owner.stats.indirectHits++;
            }

            Debug.Log($"{(direct ? "Direct" : "Indirect")} hit! on: {player.gameObject.name} by: {dealer.owner.gameObject.name} at: {point}");
        }
    }
}
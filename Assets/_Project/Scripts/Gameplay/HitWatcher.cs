using UnityEngine;
using Mirror;
using Game.Core.Projectiles;
using System.Collections.Generic;
using System;
using Game.Core.Events;
using Game.Events.HitWatcher;
using Game.Core.Maps;
using Game.Player;

namespace Game.Gameplay
{
    public class HitWatcher : NetworkBehaviour
    {
        public static List<DamageDealer> dealers;
        private Guid _onDamageDealerCreateListenerGuid;
        private Guid _onDamageDealerDestroyListenerGuid;

        private RaycastHit[] _hits;
        private int _hitsCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            dealers = new();
        }

        private void Update()
        {
            if (MapLoader.loadedMap == null) return;

            var players = MapLoader.loadedMap.players;
            var layerMask = LayerMask.GetMask("Player");
            foreach (var dealer in dealers)
            {
                foreach (var player in players)
                {
                    if (dealer.owner == player || player.invincible) continue;

                    var relativeVel = dealer.velocity - player.serverSyncedVelocity;
                    var delta = relativeVel * Time.deltaTime;

                    var deltaLength = delta.magnitude;
                    if (dealer.coll is BoxCollider bc)
                        _hitsCount = Physics.BoxCastNonAlloc(dealer.transform.position, bc.size / 2f, delta.normalized, _hits, bc.transform.rotation, deltaLength, layerMask);
                    else if (dealer.coll is SphereCollider sc)
                        _hitsCount = Physics.SphereCastNonAlloc(dealer.transform.position, sc.radius, delta.normalized, _hits, deltaLength, layerMask);
                    else
                    {
                        Debug.LogError("Collider not supported");
                        break;
                    }

                    for (int i = 0; i < _hitsCount; i++)
                    {
                        var hit = _hits[i];
                        if (!hit.collider.TryGetComponent(out PlayerBase hitPlayer) || dealer.owner == hitPlayer || hitPlayer.invincible) continue;

                        var hitPoint = Vector3.Lerp(dealer.transform.position, dealer.transform.position + dealer.velocity * Time.deltaTime, hit.distance / deltaLength);
                        var damage = dealer.EvaluateDamage(hitPlayer);
                        hitPlayer.health -= damage;
                        dealer.OnHit.Invoke(hitPlayer, damage);
                        hitPlayer.TargetOnHit(dealer.owner.netIdentity.connectionToClient);

                        Debug.Log($"Hit! {hit.collider.gameObject.name} {hitPoint}");
                    }
                }
            }
        }

        public override void OnStartServer()
        {
            _hits = new RaycastHit[16];

            _onDamageDealerCreateListenerGuid = EventBus<OnDamageDealerCreate>.Listen((data) => dealers.Add(data.dealer));
            _onDamageDealerDestroyListenerGuid = EventBus<OnDamageDealerDestroy>.Listen((data) => dealers.Remove(data.dealer));
        }

        public override void OnStopServer()
        {
            EventBus<OnDamageDealerCreate>.TryCancel(_onDamageDealerCreateListenerGuid);
            EventBus<OnDamageDealerDestroy>.TryCancel(_onDamageDealerDestroyListenerGuid);
        }
    }
}
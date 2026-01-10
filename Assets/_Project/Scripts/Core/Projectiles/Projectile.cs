using System.Collections.Generic;
using Game.Core.Maps;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Core.Projectiles
{
    public abstract class Projectile : NetworkBehaviour
    {
        public List<DamageDealer> damageDealers = new();
        protected PlayerBase owner;

        [Server]
        public static T Spawn<T>(T prefab, PlayerBase owner, Vector3 position, Quaternion rotation) where T : Projectile
        {
            var projectileObject = Instantiate(prefab.gameObject, position, rotation, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene,
            });
            var projectile = projectileObject.GetComponent<T>();
            projectile.owner = owner;
            foreach (var dealer in projectile.damageDealers)
            {
                dealer.owner = owner;
                dealer.OnHit.AddListener((player, damage) => projectile.OnDealerHit(dealer, player, damage));
            }

            NetworkServer.Spawn(projectileObject);
            return projectile;
        }

        public abstract void OnDealerHit(DamageDealer dealer, PlayerBase player, float damage);
    }
}
using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles;
using Mirror;
using UnityEngine;

namespace Game.Items
{
    public class HuananV2 : Item
    {
        public GameObject projectilePrefab;

        public override void Use(PlayerBase user)
        {
            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid()) return;

            var projectile = Instantiate(projectilePrefab, transform.position, transform.rotation, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene,
            });
            projectile.GetComponent<DamageDealer>().owner = user;
            NetworkServer.Spawn(projectile);
        }
    }
}
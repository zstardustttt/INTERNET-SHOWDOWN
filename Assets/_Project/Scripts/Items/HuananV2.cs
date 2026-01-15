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
        public HuananV2Projectile projectile;

        public override void Use(PlayerBase user, ItemUseClientContext context)
        {
            // TODO: generalize
            if (Physics.Linecast(context.headPosition, context.visualPosition, out var hit, LayerMask.GetMask("Enviroment")))
            {
                var explosion = Instantiate(projectile.explosionPrefab.gameObject, hit.point, Quaternion.identity, new InstantiateParameters()
                {
                    scene = MapLoader.loadedMap.scene
                });
                explosion.GetComponent<DamageDealer>().owner = user;
                NetworkServer.Spawn(explosion);
                return;
            }

            var finalRotation = context.crosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = PredictableProjectile.Spawn(projectile, user, context.visualPosition, finalRotation, context.useTime);
            proj.Init();
        }
    }
}
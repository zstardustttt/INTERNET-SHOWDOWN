using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles;
using UnityEngine;

namespace Game.Items
{
    public class ShockGerenade : Item
    {
        public ShockGerenadeProjectile projectile;

        public override void Use(PlayerBase user, ItemUseClientContext context)
        {
            var finalRotation = context.crosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, finalRotation);
            proj.SetupPrediction(context.useTime, 8);
        }
    }
}
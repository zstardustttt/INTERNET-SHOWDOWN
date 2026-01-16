using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles;
using UnityEngine;

namespace Game.Items
{
    public class HuananV2 : Item
    {
        public HuananV2Projectile projectile;

        public override void Use(PlayerBase user, ItemUseClientContext context)
        {
            var finalRotation = context.crosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = PredictableProjectile.Spawn(projectile, user, context.visualPosition, finalRotation, context.useTime);
            proj.Init();
            proj.collision.CheckLinecastBetweenTwoPoints(context.headPosition, context.visualPosition);
        }
    }
}
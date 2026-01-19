using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles;
using UnityEngine;

namespace Game.Items
{
    public class TeslaCocktail : Item
    {
        public TeslaCocktailProjectile projectile;

        public override void Use(PlayerBase user, ItemUseClientContext context)
        {
            var finalRotation = context.crosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = PredictableProjectile.Spawn(projectile, user, context.visualPosition, finalRotation, context.useTime, projectile.spawnCheckIterations);
            proj.collision.CheckLinecastBetweenTwoPoints(context.headPosition, context.visualPosition);
        }
    }
}
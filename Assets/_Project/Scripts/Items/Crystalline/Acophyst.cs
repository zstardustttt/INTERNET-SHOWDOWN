using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Crystalline;
using UnityEngine;

namespace Game.Items.Crystalline
{
    public class Acophyst : Item
    {
        public AcophystProjectile projectile;

        public override bool Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] _)
        {
            var finalRotation = context.didCrosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, finalRotation);
            proj.SetupPrediction(context.useTime, 1);
            return true;
        }
    }
}
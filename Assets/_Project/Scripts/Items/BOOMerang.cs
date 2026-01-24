using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles;
using UnityEngine;

namespace Game.Items
{
    public class BOOMerang : Item
    {
        public BOOMerangProjectle projectile;

        public override void Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] args)
        {
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, context.visualRotation);

            proj.damageMultiply = 1;
            foreach (var arg in args)
            {
                if (arg is not BOOMerangDamageMultiplier dmarg) continue;
                proj.damageMultiply = dmarg.damageMultiplier;
            }
            proj.damageHitBox.baseDamage = proj.directDamage * proj.DamageMultiply;

            var wishPosDist = context.didCrosshairHit ?
                Mathf.Clamp(context.crosshairHit.distance, proj.minWishPositionDistance, proj.maxWishPositionDistance) :
                proj.maxWishPositionDistance;

            proj.wishPosition = context.headPosition + proj.transform.forward * wishPosDist;

            var vel = new Vector3(
                context.velocity.x,
                Mathf.Min(context.velocity.y, 0f),
                context.velocity.z
            );
            proj.endPosition = context.headPosition + vel * 0.5f / proj.loopDuration;
            proj.SetupPrediction(context.useTime, 2);
        }
    }

    public class BOOMerangDamageMultiplier : ItemArgument
    {
        public int damageMultiplier;
    }
}
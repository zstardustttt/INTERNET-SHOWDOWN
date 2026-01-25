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

            // Retrieve damage multiplier
            proj.damageMultiply = 1;
            foreach (var arg in args)
            {
                if (arg is not BOOMerangDamageMultiplier dmarg) continue;
                proj.damageMultiply = dmarg.damageMultiplier;
            }
            proj.damageHitBox.baseDamage = proj.directDamage * proj.DamageMultiply;

            proj.secondary = context.secondary;
            if (context.secondary)
            {
                proj.flyDirection = context.didCrosshairHit ?
                    (context.crosshairHit.point - context.visualPosition).normalized :
                    context.headRotation * Vector3.forward;
            }
            else
            {
                // Get wish position
                var projBounds = proj.collision.Bounds;
                var didBoxHit = Physics.BoxCast
                (
                    context.headPosition,
                    projBounds.extents,
                    context.headRotation * Vector3.forward,
                    out var boxHitInfo,
                    proj.transform.rotation,
                    proj.maxWishPositionDistance,
                    LayerMask.GetMask("Enviroment")
                );

                // I dont fucking know okay
                var hitWishPos = boxHitInfo.point + new Vector3(
                    projBounds.extents.x * boxHitInfo.normal.x,
                    projBounds.extents.y * boxHitInfo.normal.y,
                    projBounds.extents.z * boxHitInfo.normal.z
                );

                var wishPosDist = didBoxHit ?
                    Mathf.Min(Vector3.Distance(context.headPosition, hitWishPos), proj.maxWishPositionDistance) :
                    proj.maxWishPositionDistance;

                proj.wishPositionDistance = wishPosDist;
                proj.wishPosition = context.headPosition + proj.transform.forward * wishPosDist;
            }

            proj.SetupPrediction(context.useTime, 2);
        }
    }

    public class BOOMerangDamageMultiplier : ItemArgument
    {
        public int damageMultiplier;
    }
}
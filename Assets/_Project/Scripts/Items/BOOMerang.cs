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

        public override void Use(PlayerBase user, ItemUseClientContext context)
        {
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, context.visualRotation);
            var right = context.headRotation * Vector3.right;
            proj.spawnOwnerVelocity = new
            (
                context.velocity.x * right.x,
                0f,
                context.velocity.z * right.z
            );
            proj.wishPosition = context.headPosition + proj.transform.forward * proj.wishPositionDistance;
            proj.endPosition = context.headPosition;
        }
    }
}
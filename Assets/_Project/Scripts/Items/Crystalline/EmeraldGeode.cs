using Game.Core.Items;
using Game.Core.Player;
using Game.Core.Projectiles;
using Game.Projectiles.Crystalline.EmeraldGeode;
using UnityEngine;

namespace Game.Items.Crystalline
{
    public class EmeraldGeode : Item
    {
        public EmeraldGeodeSpawnerProjectile projectile;

        public override ItemUseOptions Use(PlayerCore user, ItemUseClientContext context)
        {
            var forwardOffset = context.headRotation * Vector3.forward * 0.5f;
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition + forwardOffset, context.headRotation, context.useTime);
            return new(true, true, true);
        }
    }
}
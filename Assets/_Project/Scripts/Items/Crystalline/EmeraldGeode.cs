using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Crystalline.EmeraldGeode;

namespace Game.Items.Crystalline
{
    public class EmeraldGeode : Item
    {
        public EmeraldGeodeSpawnerProjectile projectile;

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, context.headPosition, context.headRotation, context.useTime);
            return true;
        }
    }
}
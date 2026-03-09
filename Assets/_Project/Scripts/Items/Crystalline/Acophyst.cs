using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Crystalline;

namespace Game.Items.Crystalline
{
    public class Acophyst : Item
    {
        public AcophystProjectile projectile;

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime);
            return true;
        }
    }
}
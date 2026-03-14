using Game.Core.Items;
using Game.Core.Player;
using Game.Core.Projectiles;
using Game.Projectiles.Psycheshock.TeslaCocktail;

namespace Game.Items.Psycheshock
{
    public class TeslaCocktail : Item
    {
        public TeslaCocktailProjectile projectile;

        public override bool Use(PlayerCore user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime);
            return true;
        }
    }
}
using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Psycheshock.TeslaCocktail;

namespace Game.Items.Psycheshock
{
    public class TeslaCocktail : Item
    {
        public TeslaCocktailProjectile projectile;

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime);
            return true;
        }
    }
}
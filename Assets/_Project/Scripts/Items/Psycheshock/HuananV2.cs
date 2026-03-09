using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Psycheshock;

namespace Game.Items.Psycheshock
{
    public class HuananV2 : Item
    {
        public HuananV2Projectile projectile;

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime);
            return true;
        }
    }
}
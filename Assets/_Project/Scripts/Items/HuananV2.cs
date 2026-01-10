using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles;

namespace Game.Items
{
    public class HuananV2 : Item
    {
        public HuananV2Projectile projectile;

        public override void Use(PlayerBase user)
        {
            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid()) return;
            Projectile.Spawn(projectile, user, transform.position, transform.rotation);
        }
    }
}
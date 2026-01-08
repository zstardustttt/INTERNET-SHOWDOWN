using Game.Core.Items;
using Game.Core.Maps;
using Mirror;
using UnityEngine;

namespace Game.Items
{
    public class TestItem : Item
    {
        public GameObject projectilePrefab;

        public override void Use(Transform head)
        {
            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid()) return;

            var projectile = Instantiate(projectilePrefab, transform.position, transform.rotation, new InstantiateParameters()
            {
                scene = MapLoader.loadedMap.scene,
            });
            NetworkServer.Spawn(projectile);
        }
    }
}
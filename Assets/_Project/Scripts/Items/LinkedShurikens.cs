using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.LinkedShurikens;
using Mirror;
using UnityEngine;

namespace Game.Items
{
    public class LinkedShurikens : Item
    {
        public LinkedShurikenProjectile projectilePrefab;
        public LinkedShurikensManager managerPrefab;
        public int maxUses;
        private int _uses;

        private LinkedShurikensManager _manager;

        public override bool Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] args)
        {
            var finalRotation = context.didCrosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = Projectile.Spawn(projectilePrefab, user, context.headPosition, context.visualPosition, finalRotation);
            proj.SetupPrediction(context.useTime, 1);

            if (!_manager)
            {
                _manager = Instantiate(managerPrefab.gameObject, new InstantiateParameters()
                {
                    scene = MapLoader.loadedMap.scene
                }).GetComponent<LinkedShurikensManager>();
                _manager.hitDealer.owner = user;
                NetworkServer.Spawn(_manager.gameObject);
            }

            _manager.AddProjectile(proj);

            _uses++;
            return _uses >= maxUses;
        }
    }
}
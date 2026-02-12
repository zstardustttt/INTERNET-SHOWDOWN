using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Psycheshock.LinkedShurikens;
using Mirror;
using UnityEngine;

namespace Game.Items.Psycheshock
{
    public class LinkedShurikens : Item
    {
        public LinkedShurikenProjectile projectilePrefab;
        public LinkedShurikensManager managerPrefab;
        public float initialPitch;
        public float pitchIncrease;
        public int maxUses;
        private int _uses;

        private LinkedShurikensManager _manager;

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            var finalRotation = context.didCrosshairHit ? Quaternion.LookRotation(context.crosshairHitPoint - context.visualPosition) : context.visualRotation;
            var proj = Projectile.Spawn(projectilePrefab, user, context.headPosition, context.visualPosition, finalRotation);
            proj.SetupPrediction(context.useTime, 1);
            proj.collideAudioPitch = initialPitch + _uses * pitchIncrease;

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
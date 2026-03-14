using Game.Core.Items;
using Game.Core.Maps;
using Game.Core.Player;
using Game.Core.Projectiles;
using Game.Projectiles.Psycheshock.LinkedShurikens;
using UnityEngine;

namespace Game.Items.Psycheshock
{
    public class LinkedShurikens : Item
    {
        public LinkedShurikenProjectile projectilePrefab;
        public LinkedShurikensManager managerPrefab;
        public float initialPitch;
        public float pitchIncrease;

        private LinkedShurikensManager _manager;
        public int maxUses;
        private int _uses;

        public override bool Use(PlayerCore user, ItemUseClientContext context)
        {
            var proj = Projectile.Spawn(projectilePrefab, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime, (proj) =>
            {
                proj.collideAudioPitch = initialPitch + _uses * pitchIncrease;

                if (!_manager)
                {
                    _manager = MapLoader.NetworkSpawnOnMap(managerPrefab.gameObject, Vector3.zero, Quaternion.identity)
                        .GetComponent<LinkedShurikensManager>();

                    _manager.authorReference.author = user;
                    _manager.teamReference.team = user.teamReference.team;
                }
                _manager.AddProjectile(proj);
            });

            _uses++;
            return _uses >= maxUses;
        }
    }
}
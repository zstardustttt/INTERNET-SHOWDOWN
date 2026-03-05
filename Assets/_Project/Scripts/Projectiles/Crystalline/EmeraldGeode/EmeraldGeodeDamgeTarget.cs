using Game.Core.Damages;
using Mirror;
using UnityEngine;

namespace Game.Projectiles.Crystalline.EmeraldGeode
{
    public class EmeraldGeodeDamageTarget : DamageTarget
    {
        public float invincibiliyDuration = 0.2f;
        private float _invincibilityTimer;

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= Time.deltaTime;
        }

        public override bool ApplyDamage(Damage damage)
        {
            if (_invincibilityTimer > 0f) return false;

            _invincibilityTimer = invincibiliyDuration;
            return true;
        }
    }
}
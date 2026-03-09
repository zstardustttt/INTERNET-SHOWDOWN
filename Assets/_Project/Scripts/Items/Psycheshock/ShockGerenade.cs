using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.Psycheshock.ShockGerenade;
using TMPro;
using UnityEngine;

namespace Game.Items.Psycheshock
{
    public class ShockGerenade : Item
    {
        public TMP_Text displayText;
        public string[] textVariants;
        public float textChangeIntervalMin;
        public float textChangeIntervalMax;
        public ShockGerenadeProjectile projectile;

        private float _textChangeTimer;

        private void Update()
        {
            if (displayText.alpha == 0f) displayText.color = Color.white;

            if (_textChangeTimer <= 0f)
            {
                displayText.text = textVariants[Random.Range(0, textVariants.Length)];
                _textChangeTimer = Random.Range(textChangeIntervalMin, textChangeIntervalMax);

                displayText.color = Color.clear;
            }

            _textChangeTimer -= Time.deltaTime;
        }

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime, (proj) =>
            {
                proj.flySpeed = context.secondary ? proj.secondarySpeed : proj.speed;
                proj.explodeAfter = context.secondary ? proj.explodeAfterSecondary : proj.explodeAfterPrimary;
                proj.sourceSpeedMultiplier = context.secondary ? (proj.explodeAfterPrimary / proj.explodeAfterSecondary) : 1f;
            });

            return true;
        }
    }
}
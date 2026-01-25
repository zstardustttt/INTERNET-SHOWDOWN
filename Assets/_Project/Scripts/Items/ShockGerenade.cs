using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Player;
using Game.Projectiles.ShockGerenade;
using TMPro;
using UnityEngine;

namespace Game.Items
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

        public override void Use(PlayerBase user, ItemUseClientContext context, ItemArgument[] _)
        {
            var finalRotation = context.didCrosshairHit ? Quaternion.LookRotation(context.crosshairHit.point - context.visualPosition) : context.visualRotation;
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, finalRotation);
            proj.flySpeed = context.secondary ? proj.secondarySpeed : proj.speed;
            proj.SetupPrediction(context.useTime, 8);
        }
    }
}
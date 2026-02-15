using Game.Core.Items;
using Game.Core.Projectiles;
using Game.Other;
using Game.Player;
using Game.Projectiles.Psycheshock;
using UnityEngine;

namespace Game.Items.Psycheshock
{
    public class BOOMerang : Item
    {
        public BOOMerangProjectle projectile;
        public Transform visual;

        private ShakeGenerator _shakeGenerator;
        private int _parsedDamageMultiplier;
        private int _parsedReturns;

        private void Start()
        {
            _parsedDamageMultiplier = arguments.ParseArgument("boomerang_damage_multiplier", 1);
            _parsedReturns = arguments.ParseArgument("boomerang_returns", 0);

            _shakeGenerator = new();
            _shakeGenerator.Shake(0.0001f * Mathf.Pow(_parsedReturns, 3f), 7f, 0f);
        }

        private void Update()
        {
            visual.localPosition = _shakeGenerator.GetShake();
        }

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, context.visualRotation);
            proj.damageMultiply = _parsedDamageMultiplier;
            proj.returns = _parsedReturns;

            var secondary = proj.returns > proj.maxReturns || context.secondary;
            proj.secondary = secondary;
            if (secondary)
            {
                proj.flyDirection = context.didCrosshairHit ?
                    (context.crosshairHitPoint - context.visualPosition).normalized :
                    context.headRotation * Vector3.forward;
            }
            else
            {
                // Get wish position
                var projBounds = proj.collision.Collider.bounds;
                var didBoxHit = Physics.BoxCast
                (
                    context.headPosition,
                    projBounds.extents,
                    context.headRotation * Vector3.forward,
                    out var boxHitInfo,
                    proj.transform.rotation,
                    proj.maxWishPositionDistance,
                    LayerMask.GetMask("Enviroment")
                );

                var wishPosDist = (didBoxHit ?
                    Mathf.Min(boxHitInfo.distance, proj.maxWishPositionDistance) :
                    proj.maxWishPositionDistance) * 1.02f;

                proj.wishPositionDistance = wishPosDist;
                proj.wishPosition = context.headPosition + proj.transform.forward * wishPosDist;
            }

            proj.SetupPrediction(context.useTime, 2);
            return true;
        }
    }
}
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
        private BOOMerangDamageMultiplier _parsedArg;

        private void Start()
        {
            foreach (var arg in args)
            {
                if (arg is not BOOMerangDamageMultiplier dmarg) continue;

                _parsedArg = dmarg;
                break;
            }

            if (_parsedArg != null)
            {
                _shakeGenerator = new();
                _shakeGenerator.Shake(0.001f * _parsedArg.returns * _parsedArg.returns, 7f, 0f);
            }
        }

        private void Update()
        {
            if (_shakeGenerator != null) visual.localPosition = _shakeGenerator.GetShake();
        }

        public override bool Use(PlayerBase user, ItemUseClientContext context)
        {
            var proj = Projectile.Spawn(projectile, user, context.headPosition, context.visualPosition, context.visualRotation);

            if (_parsedArg != null)
            {
                proj.damageMultiply = _parsedArg.damageMultiplier;
                proj.returns = _parsedArg.returns;
            }
            else proj.damageMultiply = 1;

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

    public class BOOMerangDamageMultiplier : ItemArgument
    {
        public int damageMultiplier;
        public int returns;
    }
}
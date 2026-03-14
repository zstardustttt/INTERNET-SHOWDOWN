using Game.Core.Items;
using Game.Core.Player;
using Game.Core.Projectiles;
using Game.Other;
using Game.Projectiles.Psycheshock;
using UnityEngine;

namespace Game.Items.Psycheshock
{
    public class BOOMerang : Item
    {
        public BOOMerangProjectle projectile;
        public Transform visual;
        public MeshRenderer[] edgeRenderers;
        public Material edgeMaterial;

        [Space(9)]
        [ColorUsage(true, true)] public Color cleanColorA;
        [ColorUsage(true, true)] public Color cleanColorB;
        [ColorUsage(true, true)] public Color buffedColorA;
        [ColorUsage(true, true)] public Color buffedColorB;

        private ShakeGenerator _shakeGenerator;
        private int _parsedDamageMultiplier;
        private int _parsedReturns;

        private void Start()
        {
            _parsedDamageMultiplier = arguments.ParseArgument("boomerang_damage_multiplier", 1);
            _parsedReturns = arguments.ParseArgument("boomerang_returns", 0);

            _shakeGenerator = new();
            _shakeGenerator.Shake(0.0001f * Mathf.Pow(_parsedReturns, 3f), 7f, 0f);

            var newEdgeMaterial = Instantiate(edgeMaterial);
            var t = Mathf.Pow((_parsedDamageMultiplier - 1f) / (projectile.damageMultiplyCap - 1f), 0.2f);
            var colorA = Color.Lerp(cleanColorA, buffedColorA, t);
            var colorB = Color.Lerp(cleanColorB, buffedColorB, t);

            newEdgeMaterial.SetColor("_ColorA", colorA);
            newEdgeMaterial.SetColor("_ColorB", colorB);

            foreach (var renderer in edgeRenderers)
            {
                renderer.material = newEdgeMaterial;
            }
        }

        private void Update()
        {
            visual.localPosition = _shakeGenerator.GetShake();
        }

        public override bool Use(PlayerCore user, ItemUseClientContext context)
        {
            Projectile.Spawn(projectile, user, user.teamReference.team, context.headPosition, context.headRotation, context.useTime, (proj) =>
            {
                proj.damageMultiply = _parsedDamageMultiplier;
                proj.returns = _parsedReturns;

                var secondary = proj.returns > proj.maxReturns || context.secondary;
                proj.secondary = secondary;

                if (secondary) return;

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
            });

            return true;
        }
    }
}
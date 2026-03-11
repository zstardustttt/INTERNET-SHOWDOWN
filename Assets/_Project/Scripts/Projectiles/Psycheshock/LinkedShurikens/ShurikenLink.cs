using Game.Core.Damages;
using Game.Core.Hits;
using UnityEngine;

namespace Game.Projectiles.Psycheshock.LinkedShurikens
{
    public class ShurikenLink : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public AudioSource audioSource;
        public CapsuleHitEntity hitEntity;
        public DamageSource damageSource;

        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 endPos;
        [HideInInspector] public int lineRendererPointsCount;
    }
}
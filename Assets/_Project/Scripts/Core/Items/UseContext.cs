using UnityEngine;

namespace Game.Core.Items
{
    public struct ItemUseClientContext
    {
        public Vector3 visualPosition;
        public Quaternion visualRotation;
        public Vector3 headPosition;
        public Quaternion headRotation;
        public bool didCrosshairHit;
        public Vector3 crosshairHitPoint;
        public Vector3 crosshairHitNormal;
        public float crosshairHitDistance;
        public double useTime;
        public Vector3 velocity;
        public bool secondary;
    }
}
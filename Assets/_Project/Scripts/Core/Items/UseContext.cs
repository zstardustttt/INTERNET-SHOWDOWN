using UnityEngine;

namespace Game.Core.Items
{
    public struct ItemUseClientContext
    {
        public Vector3 visualPosition;
        public Quaternion visualRotation;
        public Vector3 headPosition;
        public Quaternion headRotation;
        public Vector3 crosshairHitPoint;
        public bool crosshairHit;
    }
}
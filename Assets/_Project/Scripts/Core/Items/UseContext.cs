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
        public RaycastHit crosshairHit;
        public double useTime;
        public Vector3 velocity;
        public bool secondary;
    }
}
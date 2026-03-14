using UnityEngine;

namespace Game.Core.Items
{
    public struct ItemUseClientContext
    {
        public Vector3 headPosition;
        public Quaternion headRotation;

        public double useTime;
        public Vector3 velocity;

        public bool secondary;
    }
}
using System;
using UnityEngine;

namespace Game.Player.AI.Navigation
{
    public enum AIMovementType
    {
        Flat,
        Ascend,
        Descend
    }

    [Serializable]
    public struct AIMovementDescriptor
    {
        public readonly Vector3 Direction => (endPosition - startPosition).normalized;
        public readonly float Length => (endPosition - startPosition).magnitude;

        public Vector3 startPosition;
        public Vector3 endPosition;
        public AIMovementType type;

        public AIMovementDescriptor(Vector3 startPosition, Vector3 endPosition, AIMovementType type)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.type = type;
        }

        public readonly bool TryMerge(AIMovementDescriptor other, out AIMovementDescriptor merged)
        {
            if (type != other.type)
            {
                merged = default;
                return false;
            }

            merged = new(startPosition, other.endPosition, type);
            return true;
        }

        public readonly Color GetGizmosColor()
        {
            return type switch
            {
                AIMovementType.Flat => Color.skyBlue,
                AIMovementType.Ascend => Color.hotPink,
                AIMovementType.Descend => Color.crimson,
                _ => Color.black
            };
        }
    }
}
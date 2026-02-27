using System;
using Game.Player;

namespace Game.Core.Damages
{
    public enum DamageType
    {
        Direct,
        Indirect
    }

    [Serializable]
    public struct Damage
    {
        public PlayerBase author;
        public DamageType type;
        public float amount;
        public Guid family;

        public Damage(PlayerBase author, DamageType type, float amount, Guid family)
        {
            this.author = author;
            this.type = type;
            this.amount = amount;
            this.family = family;
        }
    }
}
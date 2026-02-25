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

        public Damage(PlayerBase author, DamageType type, float amount)
        {
            this.author = author;
            this.type = type;
            this.amount = amount;
        }
    }
}
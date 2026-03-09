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
        public DamageType type;
        public float amount;
        public PlayerBase author;
        public Guid source;
        public Guid team;

        public Damage(DamageType type, float amount, PlayerBase author, Guid source, Guid team)
        {
            this.type = type;
            this.amount = amount;
            this.author = author;
            this.source = source;
            this.team = team;
        }
    }
}
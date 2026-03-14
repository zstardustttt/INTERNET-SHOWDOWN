using System;
using Game.Core.Player;

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
        public PlayerCore author;
        public Guid team;
        public DamageIdentification identification;

        public Damage(DamageType type, float amount, PlayerCore author, Guid team, DamageIdentification identification)
        {
            this.type = type;
            this.amount = amount;
            this.author = author;
            this.team = team;
            this.identification = identification;
        }
    }

    [Serializable]
    public struct DamageIdentification
    {
        public Guid guid;
        public string name;

        public static DamageIdentification From(DamageIdentificationSetup setup)
        {
            var identification = new DamageIdentification
            {
                guid = Guid.NewGuid()
            };

            if (setup)
            {
                identification.name = setup.displayName;
            }

            return identification;
        }
    }
}
using System;
using Game.Core.Projectiles;

namespace Game.Player
{
    public struct PlayerStats : IEquatable<PlayerStats>
    {
        public int activity;
        public int continuousHits;
        public int indirectHits;
        public int directHits;
        public int supportingKills;
        public int finishingKills;
        public int pureKills;

        public void AddHit(DamageType type)
        {
            switch (type)
            {
                case DamageType.Continuous:
                    continuousHits++;
                    break;

                case DamageType.Indirect:
                    indirectHits++;
                    break;

                case DamageType.Direct:
                    directHits++;
                    break;

                default:
                    return;
            }
        }

        public readonly bool Equals(PlayerStats other)
        {
            return activity.Equals(other.activity)
                && continuousHits.Equals(other.continuousHits)
                && indirectHits.Equals(other.indirectHits)
                && directHits.Equals(other.directHits)
                && supportingKills.Equals(other.supportingKills)
                && finishingKills.Equals(other.finishingKills)
                && pureKills.Equals(other.pureKills);
        }

        public readonly int GetScore()
        {
            return continuousHits
                + indirectHits * 2
                + directHits * 3
                + supportingKills * 2
                + finishingKills * 2
                + pureKills * 4;
        }
    }

}
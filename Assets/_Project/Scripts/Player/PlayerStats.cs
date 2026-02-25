using System;
using Game.Core.Damages;

namespace Game.Player
{
    public struct PlayerStats : IEquatable<PlayerStats>
    {
        public int activity;
        public int indirectHits;
        public int directHits;
        public int supportingKills;
        public int finishingKills;
        public int pureKills;

        public readonly bool Equals(PlayerStats other)
        {
            return activity.Equals(other.activity)
                && indirectHits.Equals(other.indirectHits)
                && directHits.Equals(other.directHits)
                && supportingKills.Equals(other.supportingKills)
                && finishingKills.Equals(other.finishingKills)
                && pureKills.Equals(other.pureKills);
        }

        public readonly int GetScore()
        {
            return indirectHits
                + directHits * 3
                + supportingKills * 2
                + finishingKills * 2
                + pureKills * 4;
        }
    }

}
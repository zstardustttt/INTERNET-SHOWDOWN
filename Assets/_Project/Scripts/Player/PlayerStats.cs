using UnityEngine;

namespace Game.Player
{
    public struct PlayerStats
    {
        public int activity;
        public int indirectHits;
        public int directHits;
        public int supportingKills;
        public int finishingKills;
        public int pureKills;
        public float damageDealt;

        public readonly int GetScore()
        {
            return Mathf.RoundToInt(damageDealt);
        }
    }
}
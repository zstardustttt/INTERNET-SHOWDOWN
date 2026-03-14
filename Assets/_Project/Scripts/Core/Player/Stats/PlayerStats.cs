using UnityEngine;

namespace Game.Core.Player.Stats
{
    public struct PlayerStats
    {
        public int activity;
        public int directHits;
        public int indirectHits;
        public int pureKills;
        public int supportingKills;
        public int finishingKills;
        public float damageDealt;

        public readonly int EvaluateScore()
        {
            return Mathf.RoundToInt(damageDealt);
        }
    }
}
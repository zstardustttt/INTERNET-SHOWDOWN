using Game.Core.Projectiles;
using Game.Player;

namespace Game.Projectiles
{
    public class DirectDamage : DamageDealer
    {
        public float baseDamage = 10f;

        public override bool Direct => true;

        public override float EvaluateDamage(PlayerBase player)
        {
            return baseDamage;
        }
    }
}
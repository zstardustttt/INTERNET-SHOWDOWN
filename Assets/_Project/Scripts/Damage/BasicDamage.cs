using Game.Core.Damage;
using Game.Player;

namespace Game.Damage
{
    public class BasicDamage : DamageDealer
    {
        public float baseDamage = 10f;

        public override float EvaluateDamage(PlayerBase player)
        {
            return baseDamage;
        }
    }
}
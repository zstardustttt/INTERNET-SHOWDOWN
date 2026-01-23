using Game.Core.Damage;

namespace Game.Damage
{
    public class BasicDamage : DamageDealer
    {
        public float baseDamage = 10f;

        public override float EvaluateDamage(DamageReceiver _)
        {
            return baseDamage;
        }
    }
}
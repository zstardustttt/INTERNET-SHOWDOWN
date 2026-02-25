using Game.Core.Damages;

namespace Game.Damages
{
    public class BasicDamageSource : DamageSource
    {
        public DamageType damageType;
        public float damageAmount = 10f;

        public override Damage? EvaluateDamage(DamageTarget target)
        {
            return new(author, damageType, damageAmount);
        }
    }
}
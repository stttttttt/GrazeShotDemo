namespace ShotGame.Gameplay.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(float appliedDamage, float healthAfterDamage, bool killed)
        {
            AppliedDamage = appliedDamage;
            HealthAfterDamage = healthAfterDamage;
            Killed = killed;
        }

        public float AppliedDamage { get; }
        public float HealthAfterDamage { get; }
        public bool Killed { get; }
    }
}

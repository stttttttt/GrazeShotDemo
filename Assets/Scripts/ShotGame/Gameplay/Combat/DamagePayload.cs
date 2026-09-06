using System;

namespace ShotGame.Gameplay.Combat
{
    public readonly struct DamagePayload
    {
        public DamagePayload(float amount)
        {
            if (amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }

        public float Amount { get; }
    }
}

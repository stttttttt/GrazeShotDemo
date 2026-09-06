using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    public sealed class AttributeValue
    {
        public AttributeValue(float baseValue, float minValue, float maxValue)
        {
            MinValue = minValue;
            MaxValue = Mathf.Max(minValue, maxValue);
            BaseValue = Mathf.Clamp(baseValue, MinValue, MaxValue);
            CurrentValue = BaseValue;
        }

        public float BaseValue { get; private set; }
        public float CurrentValue { get; private set; }
        public float MinValue { get; }
        public float MaxValue { get; private set; }

        public void SetBase(float value) => BaseValue = Mathf.Clamp(value, MinValue, MaxValue);
        public void SetCurrent(float value) => CurrentValue = Mathf.Clamp(value, MinValue, MaxValue);

        public void SetMaximum(float value)
        {
            MaxValue = Mathf.Max(MinValue, value);
            BaseValue = Mathf.Clamp(BaseValue, MinValue, MaxValue);
            CurrentValue = Mathf.Clamp(CurrentValue, MinValue, MaxValue);
        }
    }
}

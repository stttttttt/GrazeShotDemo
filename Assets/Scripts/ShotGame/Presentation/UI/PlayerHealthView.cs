using ShotGame.Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class PlayerHealthView : MonoBehaviour
    {
        [SerializeField] private Image _immediateFill;
        [SerializeField] private Image _delayedDamageFill;
        [SerializeField] private Text _healthText;
        [SerializeField] private Graphic _lowHealthFrame;

        private float _target;
        private float _immediateValue = 1f;
        private float _delayedValue = 1f;
        private float _holdRemaining;
        private float _pulse;

        public void SetImmediate(float current, float maximum, GameplayFeelConfig config)
        {
            var normalized = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            _immediateValue = normalized;
            ApplyFill(_immediateFill, _immediateValue);
            if (_delayedValue < normalized)
            {
                _delayedValue = normalized;
                ApplyFill(_delayedDamageFill, _delayedValue);
            }
            if (_healthText != null) _healthText.text = $"HP  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            if (_lowHealthFrame != null) _lowHealthFrame.enabled = normalized <= config.LowHealthThreshold;
            _target = normalized;
            _holdRemaining = config.HealthDamageHold;
            _pulse = 1f;
        }

        public void Initialize(float current, float maximum, GameplayFeelConfig config)
        {
            if (_immediateFill != null) _immediateFill.color = config.PlayerHealthBarColor;
            if (_delayedDamageFill != null) _delayedDamageFill.color = config.PlayerDelayedHealthBarColor;
            var normalized = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            _immediateValue = normalized;
            _delayedValue = normalized;
            ApplyFill(_immediateFill, _immediateValue);
            ApplyFill(_delayedDamageFill, _delayedValue);
            SetImmediate(current, maximum, config);
            _holdRemaining = 0f;
            _pulse = 0f;
        }

        public void Tick(float deltaTime, GameplayFeelConfig config)
        {
            if (_holdRemaining > 0f) _holdRemaining = Mathf.Max(0f, _holdRemaining - deltaTime);
            else if (_delayedDamageFill != null)
            {
                _delayedValue = Mathf.MoveTowards(_delayedValue,
                    _target, deltaTime / config.HealthDelayedDuration);
                ApplyFill(_delayedDamageFill, _delayedValue);
            }
            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - deltaTime * 6f);
                transform.localScale = Vector3.one * (1f + Mathf.Sin(_pulse * Mathf.PI) * 0.08f);
            }
            else transform.localScale = Vector3.one;
        }

        private static void ApplyFill(Image image, float normalized)
        {
            if (image == null) return;
            normalized = Mathf.Clamp01(normalized);
            image.fillAmount = normalized;
            var scale = image.rectTransform.localScale;
            scale.x = normalized;
            image.rectTransform.localScale = scale;
            image.rectTransform.pivot = new Vector2(0f, 0.5f);
        }
    }
}

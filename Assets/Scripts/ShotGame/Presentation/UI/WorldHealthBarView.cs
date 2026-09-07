using ShotGame.Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class WorldHealthBarView : MonoBehaviour
    {
        [SerializeField] private Image _immediateFill;
        [SerializeField] private Image _delayedDamageFill;
        [SerializeField] private CanvasGroup _canvasGroup;
        private float _target;
        private float _immediateValue = 1f;
        private float _delayedValue = 1f;
        private float _hold;

        public void SetStyle(Color immediateColor, Color delayedColor, Vector2 size)
        {
            if (_immediateFill != null) _immediateFill.color = immediateColor;
            if (_delayedDamageFill != null) _delayedDamageFill.color = delayedColor;
            var rect = transform as RectTransform;
            if (rect != null) rect.sizeDelta = size;
        }

        public void Show(float normalized, bool visible)
        {
            normalized = Mathf.Clamp01(normalized);
            _target = normalized;
            _immediateValue = normalized;
            _delayedValue = normalized;
            ApplyFill(_immediateFill, _immediateValue);
            ApplyFill(_delayedDamageFill, _delayedValue);
            if (_canvasGroup != null) _canvasGroup.alpha = visible ? 1f : 0f;
            gameObject.SetActive(true);
        }

        public void SetHealth(float normalized, GameplayFeelConfig config)
        {
            _target = Mathf.Clamp01(normalized);
            _immediateValue = _target;
            ApplyFill(_immediateFill, _immediateValue);
            if (_delayedValue < _target)
            {
                _delayedValue = _target;
                ApplyFill(_delayedDamageFill, _delayedValue);
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            _hold = config.HealthDamageHold;
        }

        public void SetScreenPosition(Vector2 position) => ((RectTransform)transform).position = position;

        public void Tick(float deltaTime, GameplayFeelConfig config)
        {
            if (_hold > 0f) _hold = Mathf.Max(0f, _hold - deltaTime);
            else if (_delayedDamageFill != null)
            {
                _delayedValue = Mathf.MoveTowards(_delayedValue,
                    _target, deltaTime / config.HealthDelayedDuration);
                ApplyFill(_delayedDamageFill, _delayedValue);
            }
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

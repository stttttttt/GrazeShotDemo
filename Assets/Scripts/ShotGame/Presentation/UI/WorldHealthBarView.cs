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
        private float _impactRemaining;
        private Vector2 _screenPosition;

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
            _impactRemaining = 0f;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void SetHealth(float normalized, GameplayFeelConfig config)
        {
            _target = Mathf.Clamp01(normalized);
            // 前景条必须在伤害事件当帧反映真实血量；平滑反馈由延迟条负责。
            _immediateValue = _target;
            ApplyFill(_immediateFill, _immediateValue);
            if (_delayedValue < _target)
            {
                _delayedValue = _target;
                ApplyFill(_delayedDamageFill, _delayedValue);
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            _hold = config.HealthDamageHold;
            _impactRemaining = config.HealthBarHitDuration;
        }

        public void SetScreenPosition(Vector2 position)
        {
            _screenPosition = position;
            if (_impactRemaining <= 0f) ((RectTransform)transform).position = position;
        }

        public void Tick(float deltaTime, GameplayFeelConfig config)
        {
            if (_hold > 0f) _hold = Mathf.Max(0f, _hold - deltaTime);
            else if (_delayedDamageFill != null)
            {
                _delayedValue = Mathf.MoveTowards(_delayedValue,
                    _target, deltaTime / config.HealthDelayedDuration);
                ApplyFill(_delayedDamageFill, _delayedValue);
            }
            if (_impactRemaining > 0f)
            {
                _impactRemaining = Mathf.Max(0f, _impactRemaining - deltaTime);
                var progress = 1f - _impactRemaining / config.HealthBarHitDuration;
                var envelope = 1f - progress;
                var offset = Mathf.Sin(progress * Mathf.PI * 6f) *
                             config.HealthBarHitShakeDistance * envelope;
                ((RectTransform)transform).position = _screenPosition + Vector2.right * offset;
                var pulse = Mathf.Sin(progress * Mathf.PI) * config.HealthBarHitPulseScale;
                transform.localScale = Vector3.one * (1f + pulse);
            }
            else
            {
                ((RectTransform)transform).position = _screenPosition;
                transform.localScale = Vector3.one;
            }
        }

        private static void ApplyFill(Image image, float normalized)
        {
            if (image == null) return;
            image.fillAmount = Mathf.Clamp01(normalized);
        }
    }
}

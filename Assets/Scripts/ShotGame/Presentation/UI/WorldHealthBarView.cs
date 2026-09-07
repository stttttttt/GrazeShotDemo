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
            if (_immediateFill != null) _immediateFill.fillAmount = normalized;
            if (_delayedDamageFill != null) _delayedDamageFill.fillAmount = normalized;
            if (_canvasGroup != null) _canvasGroup.alpha = visible ? 1f : 0f;
            gameObject.SetActive(true);
        }

        public void SetHealth(float normalized, GameplayFeelConfig config)
        {
            _target = Mathf.Clamp01(normalized);
            if (_immediateFill != null) _immediateFill.fillAmount = _target;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            _hold = config.HealthDamageHold;
        }

        public void SetScreenPosition(Vector2 position) => ((RectTransform)transform).position = position;

        public void Tick(float deltaTime, GameplayFeelConfig config)
        {
            if (_hold > 0f) _hold = Mathf.Max(0f, _hold - deltaTime);
            else if (_delayedDamageFill != null)
                _delayedDamageFill.fillAmount = Mathf.MoveTowards(_delayedDamageFill.fillAmount,
                    _target, deltaTime / config.HealthDelayedDuration);
        }
    }
}

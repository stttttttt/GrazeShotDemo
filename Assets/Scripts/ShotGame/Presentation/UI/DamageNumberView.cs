using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        [SerializeField] private Text _text;
        private Vector2 _start;
        private float _duration;
        private float _elapsed;
        private Color _color;
        private float _intensity;
        private float _jumpHeight;
        private float _horizontalVelocity;
        private float _settledScale;

        public bool IsFinished => _elapsed >= _duration;

        public void Show(Vector2 screenPosition, float damage, Color color, float duration, bool lethal)
        {
            ShowValue(screenPosition, damage, color, duration, lethal, false);
        }

        public void ShowHealing(Vector2 screenPosition, float healing, Color color, float duration)
        {
            ShowValue(screenPosition, healing, color, duration, false, true);
        }

        private void ShowValue(Vector2 screenPosition, float value, Color color, float duration,
            bool lethal, bool healing)
        {
            _start = screenPosition;
            _intensity = Mathf.Clamp01(Mathf.InverseLerp(5f, 45f, value));
            if (lethal) _intensity = Mathf.Max(_intensity, 0.8f);
            _duration = Mathf.Max(0.01f, duration * Mathf.Lerp(0.9f, 1.25f, _intensity));
            _elapsed = 0f;
            _color = color;
            _jumpHeight = Mathf.Lerp(42f, 92f, _intensity);
            _horizontalVelocity = Random.Range(-18f, 18f) * Mathf.Lerp(0.45f, 1f, _intensity);
            _settledScale = Mathf.Lerp(0.92f, 1.38f, _intensity);
            if (_text == null) _text = GetComponent<Text>();
            if (_text != null)
            {
                var rounded = Mathf.Max(0, Mathf.RoundToInt(value));
                _text.text = healing ? $"+{rounded}" : rounded.ToString();
                _text.color = color;
                _text.fontSize = Mathf.RoundToInt(Mathf.Lerp(21f, 34f, _intensity)) + (lethal ? 3 : 0);
            }
            ((RectTransform)transform).position = screenPosition;
            transform.localScale = Vector3.one * 0.25f;
            transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f) * _intensity);
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            var jump = 4f * t * (1f - t) * _jumpHeight;
            var rise = t * Mathf.Lerp(18f, 30f, _intensity);
            var impactJitter = t < 0.18f ? Mathf.Sin(t * 70f) * 3f * _intensity : 0f;
            ((RectTransform)transform).position = _start + new Vector2(
                _horizontalVelocity * t + impactJitter, jump + rise);

            float scale;
            if (t < 0.12f) scale = Mathf.Lerp(0.25f, _settledScale * 1.32f, t / 0.12f);
            else if (t < 0.32f) scale = Mathf.Lerp(_settledScale * 1.32f,
                _settledScale, (t - 0.12f) / 0.2f);
            else scale = _settledScale * Mathf.Lerp(1f, 0.86f, (t - 0.32f) / 0.68f);
            transform.localScale = Vector3.one * scale;
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity,
                1f - Mathf.Exp(-10f * deltaTime));
            if (_text != null)
            {
                var color = _color;
                color.a = 1f - Mathf.Clamp01((t - 0.65f) / 0.35f);
                _text.color = color;
            }
        }
    }
}

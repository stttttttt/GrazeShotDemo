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

        public bool IsFinished => _elapsed >= _duration;

        public void Show(Vector2 screenPosition, float damage, Color color, float duration, bool lethal)
        {
            _start = screenPosition;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _color = color;
            if (_text == null) _text = GetComponent<Text>();
            if (_text != null)
            {
                _text.text = Mathf.Max(0, Mathf.RoundToInt(damage)).ToString();
                _text.color = color;
                _text.fontSize = lethal ? 30 : 23;
            }
            ((RectTransform)transform).position = screenPosition;
            transform.localScale = Vector3.one * 0.65f;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            var eased = 1f - (1f - t) * (1f - t);
            ((RectTransform)transform).position = _start + new Vector2(Mathf.Sin(t * 8f) * 8f, eased * 54f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1f, Mathf.Min(1f, t * 5f));
            if (_text != null)
            {
                var color = _color;
                color.a = 1f - Mathf.Clamp01((t - 0.65f) / 0.35f);
                _text.color = color;
            }
        }
    }
}

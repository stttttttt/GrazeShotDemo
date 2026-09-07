using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>由表现控制器统一驱动的短生命周期世界特效。</summary>
    [DisallowMultipleComponent]
    public sealed class WorldEffectView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        private Sprite _defaultSprite;
        private bool _defaultSpriteCaptured;

        public void Show(Vector2 position, float rotation, Color color, float size)
        {
            EnsureRenderer();
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            transform.localScale = Vector3.one * size;
            if (_renderer != null)
            {
                _renderer.sprite = _defaultSprite;
                _renderer.color = color;
            }
            gameObject.SetActive(true);
        }

        public void SetVisual(Color color, float size)
        {
            EnsureRenderer();
            transform.localScale = Vector3.one * size;
            if (_renderer != null) _renderer.color = color;
        }

        public void ShowRectangle(Vector2 position, float rotation, Color color, Vector2 size)
        {
            EnsureRenderer();
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            transform.localScale = new Vector3(Mathf.Max(0.001f, size.x),
                Mathf.Max(0.001f, size.y), 1f);
            if (_renderer != null)
            {
                _renderer.sprite = _defaultSprite;
                _renderer.color = color;
            }
            gameObject.SetActive(true);
        }

        public void SetRectangleVisual(Color color, Vector2 size)
        {
            EnsureRenderer();
            transform.localScale = new Vector3(Mathf.Max(0.001f, size.x),
                Mathf.Max(0.001f, size.y), 1f);
            if (_renderer != null) _renderer.color = color;
        }

        public void ShowSprite(Vector2 position, float rotation, Sprite sprite, float size)
        {
            EnsureRenderer();
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            var spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
            var scale = Mathf.Max(0.001f, size) / Mathf.Max(0.001f, spriteWidth);
            transform.localScale = Vector3.one * scale;
            if (_renderer != null)
            {
                _renderer.sprite = sprite;
                _renderer.color = Color.white;
            }
            gameObject.SetActive(true);
        }

        public void SetSprite(Sprite sprite)
        {
            EnsureRenderer();
            if (_renderer != null) _renderer.sprite = sprite;
        }

        private void EnsureRenderer()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_defaultSpriteCaptured || _renderer == null) return;
            _defaultSprite = _renderer.sprite;
            _defaultSpriteCaptured = true;
        }
    }
}

using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>由表现控制器统一驱动的短生命周期世界特效。</summary>
    [DisallowMultipleComponent]
    public sealed class WorldEffectView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        public void Show(Vector2 position, float rotation, Color color, float size)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            transform.localScale = Vector3.one * size;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _renderer.color = color;
            gameObject.SetActive(true);
        }

        public void SetVisual(Color color, float size)
        {
            transform.localScale = Vector3.one * size;
            if (_renderer != null) _renderer.color = color;
        }
    }
}

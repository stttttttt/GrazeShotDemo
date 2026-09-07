using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>Entity Prefab 的被动表现引用，不持有 Gameplay 状态。</summary>
    [DisallowMultipleComponent]
    public sealed class EntityFeedbackView : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _healthBarAnchor;
        [SerializeField] private SpriteRenderer[] _renderers;

        private Color[] _defaultColors;
        private Vector3 _defaultScale;
        private Vector3 _defaultLocalPosition;

        public Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;
        public Transform HealthBarAnchor => _healthBarAnchor != null ? _healthBarAnchor : transform;
        public SpriteRenderer[] Renderers
        {
            get
            {
                EnsureDefaults();
                return _renderers;
            }
        }

        public void CaptureDefaults()
        {
            if (_visualRoot == null) _visualRoot = transform;
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _defaultScale = _visualRoot.localScale;
            _defaultLocalPosition = _visualRoot.localPosition;
            _defaultColors = new Color[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
                _defaultColors[i] = _renderers[i] != null ? _renderers[i].color : Color.white;
        }

        public void SetFlash(Color color)
        {
            EnsureDefaults();
            for (var i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].color = color;
        }

        public void SetScale(float scale)
        {
            EnsureDefaults();
            _visualRoot.localScale = _defaultScale * scale;
        }

        public void SetHitOffset(Vector2 offset)
        {
            EnsureDefaults();
            _visualRoot.localPosition = _defaultLocalPosition + (Vector3)offset;
        }

        public void ResetVisual()
        {
            EnsureDefaults();
            _visualRoot.localScale = _defaultScale;
            _visualRoot.localPosition = _defaultLocalPosition;
            for (var i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].color = _defaultColors[i];
        }

        private void EnsureDefaults()
        {
            if (_defaultColors == null) CaptureDefaults();
        }
    }
}

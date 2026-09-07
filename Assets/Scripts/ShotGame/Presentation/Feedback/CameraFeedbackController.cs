using ShotGame.Gameplay.Config;
using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    public sealed class CameraFeedbackController
    {
        private readonly Transform _camera;
        private readonly Vector3 _baseLocalPosition;
        private readonly GameplayFeelConfig _config;
        private Vector2 _kick;
        private float _shakeStrength;
        private float _shakeRemaining;

        public CameraFeedbackController(Camera camera, GameplayFeelConfig config)
        {
            _camera = camera != null ? camera.transform : null;
            _baseLocalPosition = _camera != null ? _camera.localPosition : Vector3.zero;
            _config = config;
        }

        public void AddKick(Vector2 direction, float amount)
        {
            _kick += -direction.normalized * amount;
            _kick = Vector2.ClampMagnitude(_kick, _config.MaxCameraKick);
        }

        public void AddShake(float strength, float duration)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeRemaining = Mathf.Max(_shakeRemaining, duration);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_camera == null) return;
            _kick = Vector2.Lerp(_kick, Vector2.zero,
                1f - Mathf.Exp(-_config.CameraRecovery * unscaledDeltaTime));
            var shake = Vector2.zero;
            if (_shakeRemaining > 0f)
            {
                _shakeRemaining = Mathf.Max(0f, _shakeRemaining - unscaledDeltaTime);
                shake = Random.insideUnitCircle * Mathf.Min(_shakeStrength, _config.MaxShakeOffset);
                if (_shakeRemaining <= 0f) _shakeStrength = 0f;
            }
            _camera.localPosition = _baseLocalPosition + (Vector3)(_kick + shake);
        }

        public void Reset()
        {
            _kick = Vector2.zero;
            _shakeStrength = 0f;
            _shakeRemaining = 0f;
            if (_camera != null) _camera.localPosition = _baseLocalPosition;
        }
    }
}

using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Config;
using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>玩家预制体上的擦弹圆环表现桥接，不持有或修改 Gameplay 状态。</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGrazeEffectView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int ArcAmountId = Shader.PropertyToID("_ArcAmount");
        private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");

        [SerializeField] private SpriteRenderer _mainRenderer;
        [SerializeField] private SpriteRenderer _pulseRenderer;

        private MaterialPropertyBlock _mainProperties;
        private MaterialPropertyBlock _pulseProperties;
        private GameplayFeelConfig _config;
        private float _worldDiameter;
        private float _mainSpriteWidth = 1f;
        private float _pulseSpriteWidth = 1f;
        private float _pulseRemaining;
        private float _startupVisualElapsed;
        private bool _startupVisualActive;
        private GrazePhase _phase = GrazePhase.Idle;
        private GrazePhase _notifiedPhase = GrazePhase.Idle;
        private bool _initialized;

        public static PlayerGrazeEffectView GetOrCreate(GameObject player, Material material)
        {
            if (player == null || material == null) return null;
            var existing = player.GetComponent<PlayerGrazeEffectView>();
            if (existing != null) return existing;

            var source = player.GetComponentInChildren<SpriteRenderer>();
            if (source == null || source.sprite == null) return null;
            var effectRoot = new GameObject("GrazeEffectRoot");
            effectRoot.layer = player.layer;
            effectRoot.transform.SetParent(player.transform, false);
            var main = CreateRenderer(effectRoot.transform, "GrazeRing", source, material,
                source.sortingOrder - 2);
            var pulse = CreateRenderer(effectRoot.transform, "PerfectPulse", source, material,
                source.sortingOrder - 1);
            var view = player.AddComponent<PlayerGrazeEffectView>();
            view._mainRenderer = main;
            view._pulseRenderer = pulse;
            main.enabled = false;
            pulse.enabled = false;
            return view;
        }

        public void Initialize(GameplayFeelConfig config, float sensorRadius)
        {
            _config = config;
            if (_mainProperties == null) _mainProperties = new MaterialPropertyBlock();
            if (_pulseProperties == null) _pulseProperties = new MaterialPropertyBlock();
            _worldDiameter = Mathf.Max(0.01f, sensorRadius * 2f);
            _mainSpriteWidth = GetSpriteWidth(_mainRenderer);
            _pulseSpriteWidth = GetSpriteWidth(_pulseRenderer);
            _initialized = _config != null;
            ResetVisual();
        }

        public void SetPhase(GrazePhase phase, float normalizedProgress)
        {
            if (!_initialized || _mainRenderer == null) return;
            var changed = phase != _phase;
            if (changed && phase != _notifiedPhase) NotifyPhaseChanged(phase);
            _phase = phase;
            var progress = Mathf.Clamp01(normalizedProgress);

            switch (phase)
            {
                case GrazePhase.Startup:
                {
                    var eased = progress * progress;
                    SetMain(_config.GrazeStartupColor, Mathf.Lerp(0.2f, 0.85f, eased),
                        Mathf.Lerp(0.7f, 1.8f, eased), 1f, 1f, 0f, 0.06f, 0f);
                    break;
                }
                case GrazePhase.Perfect:
                    SetMain(_config.GrazePerfectColor, 0.95f, _config.GrazePerfectBrightness,
                        1f + Mathf.Sin(progress * Mathf.PI) * 0.12f, 1.15f, 0f, 0.04f,
                        progress * 0.4f);
                    break;
                case GrazePhase.Active:
                    SetMain(_config.GrazeActiveColor, _config.GrazeActiveAlpha, 0.9f,
                        1f + Mathf.Sin(progress * Mathf.PI * 2f) * 0.025f,
                        0.65f, 0.75f, 0.1f, progress * 2f);
                    break;
                case GrazePhase.Cooldown:
                    SetMain(_config.GrazeCooldownColor, Mathf.Lerp(0.3f, 0f, progress),
                        Mathf.Lerp(0.7f, 0.2f, progress), Mathf.Lerp(1f, 1.18f, progress),
                        0.55f, 0.7f, 0.04f, progress * 0.5f);
                    break;
                default:
                    _mainRenderer.enabled = false;
                    break;
            }
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!_initialized) return;
            if (_startupVisualActive)
            {
                _startupVisualElapsed += Mathf.Max(0f, unscaledDeltaTime);
                if (_startupVisualElapsed >= _config.GrazeStartupVisualDuration)
                    _startupVisualActive = false;
            }
            if (_pulseRenderer == null || _pulseRemaining <= 0f) return;
            _pulseRemaining = Mathf.Max(0f, _pulseRemaining - Mathf.Max(0f, unscaledDeltaTime));
            var duration = _config.GrazePerfectPulseDuration;
            var progress = 1f - _pulseRemaining / duration;
            var alpha = (1f - progress) * (1f - progress);
            SetRenderer(_pulseRenderer, _pulseProperties, _config.GrazePerfectColor, alpha,
                _config.GrazePerfectBrightness * Mathf.Lerp(1.35f, 0.65f, progress),
                Mathf.Lerp(0.7f, _config.GrazePerfectPulseScale, EaseOutCubic(progress)), 1.35f,
                0f, 0.02f, progress * 0.6f, _pulseSpriteWidth);
            if (_pulseRemaining <= 0f) _pulseRenderer.enabled = false;
        }

        public void PlayPerfectPulse()
        {
            if (!_initialized || _pulseRenderer == null) return;
            _pulseRemaining = _config.GrazePerfectPulseDuration;
            _pulseRenderer.enabled = true;
        }

        public void NotifyPhaseChanged(GrazePhase phase)
        {
            if (!_initialized) return;
            _notifiedPhase = phase;
            if (phase == GrazePhase.Startup)
            {
                _startupVisualElapsed = 0f;
                _startupVisualActive = true;
            }
            else if (phase == GrazePhase.Perfect) PlayPerfectPulse();
        }

        public void ResetVisual()
        {
            _phase = GrazePhase.Idle;
            _notifiedPhase = GrazePhase.Idle;
            _pulseRemaining = 0f;
            _startupVisualElapsed = 0f;
            _startupVisualActive = false;
            if (_mainRenderer != null) _mainRenderer.enabled = false;
            if (_pulseRenderer != null) _pulseRenderer.enabled = false;
        }

        private void SetMain(Color color, float alpha, float brightness, float scale,
            float thicknessScale, float arcAmount, float noiseStrength, float rotation)
        {
            SetRenderer(_mainRenderer, _mainProperties, color, alpha, brightness,
                scale * GetStartupVisualScale(),
                thicknessScale, arcAmount, noiseStrength, rotation, _mainSpriteWidth);
        }

        private float GetStartupVisualScale()
        {
            if (!_startupVisualActive) return 1f;
            var progress = Mathf.Clamp01(_startupVisualElapsed / _config.GrazeStartupVisualDuration);
            const float expandRatio = 0.35f;
            if (progress < expandRatio)
                return Mathf.Lerp(1f, _config.GrazeStartupScale,
                    EaseOutCubic(progress / expandRatio));
            return Mathf.Lerp(_config.GrazeStartupScale, 1f,
                Mathf.SmoothStep(0f, 1f, (progress - expandRatio) / (1f - expandRatio)));
        }

        private void SetRenderer(SpriteRenderer target, MaterialPropertyBlock properties,
            Color color, float alpha, float brightness, float scale, float thicknessScale,
            float arcAmount, float noiseStrength, float rotation, float spriteWidth)
        {
            if (target == null) return;
            target.enabled = alpha > 0.001f;
            target.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, color);
            properties.SetFloat(AlphaId, Mathf.Clamp01(alpha));
            properties.SetFloat(ThicknessId, _config.GrazeRingThickness * thicknessScale);
            properties.SetFloat(SoftnessId, _config.GrazeRingSoftness);
            properties.SetFloat(BrightnessId, Mathf.Max(0f, brightness));
            properties.SetFloat(ArcAmountId, Mathf.Clamp01(arcAmount));
            properties.SetFloat(NoiseStrengthId, Mathf.Clamp01(noiseStrength));
            properties.SetFloat(RotationId, rotation);
            target.SetPropertyBlock(properties);
            target.transform.localScale = Vector3.one * (_worldDiameter * scale / spriteWidth);
        }

        private static float GetSpriteWidth(SpriteRenderer target)
        {
            if (target == null || target.sprite == null) return 1f;
            return Mathf.Max(0.001f, target.sprite.bounds.size.x);
        }

        private static float EaseOutCubic(float value)
        {
            value = 1f - Mathf.Clamp01(value);
            return 1f - value * value * value;
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name,
            SpriteRenderer source, Material material, int sortingOrder)
        {
            var target = new GameObject(name, typeof(SpriteRenderer));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.sharedMaterial = material;
            renderer.color = Color.white;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}

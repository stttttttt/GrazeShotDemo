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
        [SerializeField] private RectTransform _reloadRoot;
        [SerializeField] private Image _reloadFill;
        [SerializeField] private Text _reloadText;
        private float _target;
        private float _immediateValue = 1f;
        private float _delayedValue = 1f;
        private float _hold;
        private float _impactRemaining;
        private Vector2 _screenPosition;

        public void SetStyle(Color immediateColor, Color delayedColor, Vector2 size,
            bool supportsReload = false)
        {
            if (_immediateFill != null) _immediateFill.color = immediateColor;
            if (_delayedDamageFill != null) _delayedDamageFill.color = delayedColor;
            var rect = transform as RectTransform;
            if (rect != null) rect.sizeDelta = size;
            if (supportsReload)
            {
                EnsureReloadUi(size);
                SetReloadProgress(false, 0f);
            }
            else if (_reloadRoot != null) _reloadRoot.gameObject.SetActive(false);
        }

        /// <summary>显示玩家当前武器的换弹进度；非换弹状态立即隐藏。</summary>
        public void SetReloadProgress(bool reloading, float progress)
        {
            if (_reloadRoot == null)
            {
                if (!reloading) return;
                EnsureReloadUi((transform as RectTransform)?.sizeDelta ?? new Vector2(112f, 14f));
            }
            _reloadRoot.gameObject.SetActive(reloading);
            if (!reloading) return;
            progress = Mathf.Clamp01(progress);
            if (_reloadFill != null) _reloadFill.fillAmount = progress;
            if (_reloadText != null) _reloadText.text = "换弹中";
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

        private void EnsureReloadUi(Vector2 healthBarSize)
        {
            if (_reloadRoot != null)
            {
                LayoutReloadUi(healthBarSize);
                var sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                var existingBackground = _reloadRoot.GetComponent<Image>();
                if (existingBackground != null && existingBackground.sprite == null)
                    existingBackground.sprite = sprite;
                if (_reloadFill != null && _reloadFill.sprite == null) _reloadFill.sprite = sprite;
                return;
            }

            var rootObject = new GameObject("ReloadProgress", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            rootObject.layer = gameObject.layer;
            rootObject.transform.SetParent(transform, false);
            _reloadRoot = rootObject.GetComponent<RectTransform>();
            _reloadRoot.anchorMin = _reloadRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _reloadRoot.pivot = new Vector2(0.5f, 0.5f);
            LayoutReloadUi(healthBarSize);
            var background = rootObject.GetComponent<Image>();
            background.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            background.color = new Color(0.025f, 0.035f, 0.06f, 0.9f);
            background.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = gameObject.layer;
            fillObject.transform.SetParent(_reloadRoot, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            _reloadFill = fillObject.GetComponent<Image>();
            _reloadFill.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            _reloadFill.color = new Color(0.25f, 0.82f, 1f, 1f);
            _reloadFill.type = Image.Type.Filled;
            _reloadFill.fillMethod = Image.FillMethod.Horizontal;
            _reloadFill.fillOrigin = 0;
            _reloadFill.raycastTarget = false;

            var textObject = new GameObject("Label", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(_reloadRoot, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            _reloadText = textObject.GetComponent<Text>();
            _reloadText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _reloadText.fontSize = 8;
            _reloadText.fontStyle = FontStyle.Bold;
            _reloadText.alignment = TextAnchor.MiddleCenter;
            _reloadText.color = Color.white;
            _reloadText.raycastTarget = false;
        }

        private void LayoutReloadUi(Vector2 healthBarSize)
        {
            _reloadRoot.sizeDelta = new Vector2(Mathf.Max(88f, healthBarSize.x * 0.85f), 12f);
            _reloadRoot.anchoredPosition = new Vector2(0f, healthBarSize.y * 0.5f + 9f);
        }
    }
}

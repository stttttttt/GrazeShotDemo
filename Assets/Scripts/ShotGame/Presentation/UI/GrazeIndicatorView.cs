using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class GrazeIndicatorView : MonoBehaviour
    {
        [SerializeField] private Image _phaseFill;
        [SerializeField] private Graphic _perfectMarker;
        [SerializeField] private Text _phaseText;
        [SerializeField] private Text _chargeText;
        [SerializeField] private Text _resultText;
        private float _resultRemaining;

        public void SetShockwaveCharge(bool charging, float progress, float radius)
        {
            ApplyFill(_phaseFill, charging ? progress : 0f);
            if (_phaseFill != null) _phaseFill.color = progress >= 1f
                ? new Color(0.35f, 1f, 1f) : Color.white;
            if (_perfectMarker != null) _perfectMarker.enabled = charging && progress >= 1f;
            if (_phaseText != null) _phaseText.text = charging
                ? $"冲击波蓄力  {Mathf.RoundToInt(progress * 100f)}%" : "按住右键蓄力";
            if (_chargeText != null) _chargeText.text = $"释放范围  {radius:0.0}";
        }

        public void ShowShockwaveResult(int absorbed, int ammo, float healing)
        {
            if (_resultText == null) return;
            _resultText.text = absorbed > 0
                ? $"吸收 {absorbed} 发  +{ammo} 弹药  +{Mathf.RoundToInt(healing)} 生命"
                : "冲击波释放";
            _resultText.gameObject.SetActive(true);
            _resultText.transform.localScale = Vector3.one * 1.25f;
            _resultRemaining = 0.7f;
        }

        public void Tick(float deltaTime)
        {
            if (_resultRemaining <= 0f || _resultText == null) return;
            _resultRemaining = Mathf.Max(0f, _resultRemaining - deltaTime);
            _resultText.transform.localScale = Vector3.Lerp(_resultText.transform.localScale,
                Vector3.one, 1f - Mathf.Exp(-12f * deltaTime));
            if (_resultRemaining <= 0f) _resultText.gameObject.SetActive(false);
        }

        private static void ApplyFill(Image image, float value)
        {
            if (image == null) return;
            value = Mathf.Clamp01(value);
            if (image.sprite != null && image.type == Image.Type.Filled) image.fillAmount = value;
            else image.rectTransform.localScale = new Vector3(value, 1f, 1f);
        }
    }
}

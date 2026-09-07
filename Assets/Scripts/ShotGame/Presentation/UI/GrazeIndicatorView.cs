using ShotGame.Gameplay.Character;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class GrazeIndicatorView : MonoBehaviour
    {
        [SerializeField] private Image _phaseFill;
        [SerializeField] private Graphic _perfectMarker;
        [SerializeField] private Image _chargeFill;
        [SerializeField] private Text _phaseText;
        [SerializeField] private Text _chargeText;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _resultText;
        private float _resultRemaining;

        public void SetPhase(GrazePhase phase, float progress)
        {
            if (_phaseFill != null)
            {
                _phaseFill.fillAmount = Mathf.Clamp01(progress);
                _phaseFill.color = PhaseColor(phase);
            }
            if (_perfectMarker != null) _perfectMarker.enabled = phase == GrazePhase.Perfect;
            if (_phaseText != null) _phaseText.text = PhaseName(phase);
        }

        public void SetCharge(int level, int combo, float remaining, float maxDuration)
        {
            if (_chargeFill != null) _chargeFill.fillAmount = maxDuration > 0f
                ? Mathf.Clamp01(remaining / maxDuration) : (level > 0 ? 1f : 0f);
            if (_chargeText != null) _chargeText.text = $"CHARGE  Lv.{level}";
            if (_comboText != null)
            {
                _comboText.gameObject.SetActive(combo > 0);
                _comboText.text = $"COMBO  ×{combo}";
            }
        }

        public void ShowResult(GrazeResultType result)
        {
            if (_resultText == null) return;
            _resultText.text = result == GrazeResultType.PerfectMomentum ||
                result == GrazeResultType.PerfectDefensive ? "完美擦弹" : "擦弹成功";
            _resultText.gameObject.SetActive(true);
            _resultText.transform.localScale = Vector3.one * 1.25f;
            _resultRemaining = 0.55f;
        }

        public void Tick(float deltaTime)
        {
            if (_resultRemaining <= 0f || _resultText == null) return;
            _resultRemaining = Mathf.Max(0f, _resultRemaining - deltaTime);
            _resultText.transform.localScale = Vector3.Lerp(_resultText.transform.localScale,
                Vector3.one, 1f - Mathf.Exp(-12f * deltaTime));
            if (_resultRemaining <= 0f) _resultText.gameObject.SetActive(false);
        }

        private static Color PhaseColor(GrazePhase phase)
        {
            switch (phase)
            {
                case GrazePhase.Startup: return new Color(0.3f, 0.75f, 1f);
                case GrazePhase.Perfect: return new Color(0.35f, 1f, 0.55f);
                case GrazePhase.Active: return new Color(1f, 0.82f, 0.25f);
                case GrazePhase.Cooldown: return new Color(0.35f, 0.38f, 0.45f);
                default: return new Color(0.18f, 0.2f, 0.26f);
            }
        }

        private static string PhaseName(GrazePhase phase) => phase == GrazePhase.Perfect
            ? "PERFECT"
            : phase.ToString().ToUpperInvariant();
    }
}

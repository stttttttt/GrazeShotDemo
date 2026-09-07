using System;
using GameFoundation.Service.UI;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Run;
using ShotGame.Gameplay.Weapon;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    /// <summary>显示当前玩家的生命、武器、弹药和换弹状态。</summary>
    public sealed class GameplayScreen : UIScreen
    {
        [SerializeField] private Text _weaponText;
        [SerializeField] private Text _healthText;
        [SerializeField] private Text _grazeText;
        [SerializeField] private Text _chargeText;
        [SerializeField] private Text _timeText;
        [SerializeField] private Text _waveText;
        [SerializeField] private Text _objectiveText;

        [Header("第七阶段正式 HUD")]
        [SerializeField] private PlayerHealthView _playerHealthView;
        [SerializeField] private GrazeIndicatorView _grazeIndicatorView;
        [SerializeField] private RectTransform _worldUiRoot;
        [SerializeField] private DamageNumberView _damageNumberPrefab;
        [SerializeField] private WorldHealthBarView _worldHealthBarPrefab;

        private GameplaySession _session;
        private IDisposable _weaponSubscription;
        private IDisposable _damageSubscription;
        private IDisposable _grazePhaseSubscription;
        private IDisposable _grazeSucceededSubscription;
        private IDisposable _chargeSubscription;
        private IDisposable _timeSubscription;
        private IDisposable _countdownSubscription;
        private IDisposable _waveStartedSubscription;
        private IDisposable _waveProgressSubscription;
        private IDisposable _waveIntervalSubscription;
        private string _currentObjective = "等待第一波";
        private SpriteRenderer _playerRenderer;
        private Color _playerDefaultColor;

        public static GameplayScreen ActiveInstance { get; private set; }
        public PlayerHealthView PlayerHealthView => _playerHealthView;
        public GrazeIndicatorView GrazeIndicatorView => _grazeIndicatorView;
        public RectTransform WorldUiRoot => _worldUiRoot;
        public DamageNumberView DamageNumberPrefab => _damageNumberPrefab;
        public WorldHealthBarView WorldHealthBarPrefab => _worldHealthBarPrefab;

        protected override void OnOpened(object args)
        {
            ActiveInstance = this;
            _session = args as GameplaySession
                ?? throw new ArgumentException("GameplayScreen 需要 GameplaySession。", nameof(args));
            EnsureFifthPhaseLabels();
            EnsureSixthPhaseLabels();
            if (_weaponText == null || _healthText == null || _grazeText == null ||
                _chargeText == null || _timeText == null)
                throw new InvalidOperationException("GameplayScreen 缺少 HUD 文本引用。");

            RefreshAll();
            _weaponSubscription = _session.Facts.Subscribe<WeaponStateChangedFact>(OnWeaponStateChanged);
            _damageSubscription = _session.Facts.Subscribe<CharacterDamagedFact>(OnCharacterDamaged);
            _grazePhaseSubscription = _session.Facts.Subscribe<GrazePhaseChangedFact>(OnGrazePhaseChanged);
            _grazeSucceededSubscription = _session.Facts.Subscribe<GrazeSucceededFact>(OnGrazeSucceeded);
            _chargeSubscription = _session.Facts.Subscribe<ChargeChangedFact>(OnChargeChanged);
            _timeSubscription = _session.Facts.Subscribe<TimeDilationChangedFact>(OnTimeDilationChanged);
            _countdownSubscription = _session.Facts.Subscribe<RunCountdownChangedFact>(OnCountdownChanged);
            _waveStartedSubscription = _session.Facts.Subscribe<WaveStartedFact>(OnWaveStarted);
            _waveProgressSubscription = _session.Facts.Subscribe<WaveProgressChangedFact>(OnWaveProgressChanged);
            _waveIntervalSubscription = _session.Facts.Subscribe<WaveIntervalStartedFact>(OnWaveIntervalStarted);
        }

        protected override void OnClosing()
        {
            if (ActiveInstance == this) ActiveInstance = null;
            _weaponSubscription?.Dispose();
            _weaponSubscription = null;
            _damageSubscription?.Dispose();
            _damageSubscription = null;
            _grazePhaseSubscription?.Dispose();
            _grazePhaseSubscription = null;
            _grazeSucceededSubscription?.Dispose();
            _grazeSucceededSubscription = null;
            _chargeSubscription?.Dispose();
            _chargeSubscription = null;
            _timeSubscription?.Dispose();
            _timeSubscription = null;
            _countdownSubscription?.Dispose();
            _countdownSubscription = null;
            _waveStartedSubscription?.Dispose();
            _waveStartedSubscription = null;
            _waveProgressSubscription?.Dispose();
            _waveProgressSubscription = null;
            _waveIntervalSubscription?.Dispose();
            _waveIntervalSubscription = null;
            if (_playerRenderer != null) _playerRenderer.color = _playerDefaultColor;
            _playerRenderer = null;
            _session = null;
        }

        private void RefreshAll()
        {
            if (!_session.World.TryGetEntity(_session.PlayerEntityId, out var player)) return;
            var equipment = player.GetComponent<EquipmentComponent>();
            if (equipment != null) ShowWeapon(equipment.CurrentWeapon);
            var attributes = player.GetComponent<AttributeComponent>();
            if (attributes != null) ShowHealth(attributes.GetCurrent(AttributeType.Health));
            var graze = player.GetComponent<GrazeComponent>();
            _grazeText.text = $"擦弹  {graze?.Phase ?? GrazePhase.Idle}";
            var charge = player.GetComponent<ChargeComponent>();
            ShowCharge(charge?.ChargeLevel ?? 0, charge?.ComboCount ?? 0);
            var timeDilation = _session.TimeDilation;
            ShowTime(timeDilation?.CurrentScale ?? 1f, timeDilation != null && timeDilation.IsActive);
            _playerRenderer = player.UnityObject.GameObject.GetComponentInChildren<SpriteRenderer>();
            if (_playerRenderer != null) _playerDefaultColor = _playerRenderer.color;
            _waveText.text = $"开始倒计时  {Mathf.CeilToInt(_session.Run.CountdownRemaining)}";
            _objectiveText.text = _currentObjective;
        }

        private void OnWeaponStateChanged(WeaponStateChangedFact fact)
        {
            if (_session == null || fact.OwnerId != _session.PlayerEntityId) return;
            var reloading = fact.State == WeaponState.Reloading ? "  换弹中" : string.Empty;
            _weaponText.text = $"{fact.DisplayName}  {fact.MagazineAmmo}/{fact.ReserveAmmo}{reloading}";
        }

        private void OnCharacterDamaged(CharacterDamagedFact fact)
        {
            if (_session == null || fact.TargetId != _session.PlayerEntityId) return;
            ShowHealth(fact.Result.HealthAfterDamage);
        }

        private void OnGrazePhaseChanged(GrazePhaseChangedFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            _grazeText.text = $"擦弹  {fact.Current}";
            if (_playerRenderer != null) _playerRenderer.color = GetGrazeColor(fact.Current);
        }

        private void OnGrazeSucceeded(GrazeSucceededFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            _grazeText.text = $"擦弹  {GetResultName(fact.ResultType)}";
        }

        private void OnChargeChanged(ChargeChangedFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            ShowCharge(fact.CurrentLevel, fact.Combo);
        }

        private void OnTimeDilationChanged(TimeDilationChangedFact fact) =>
            ShowTime(fact.TimeScale, fact.IsActive);

        private void OnCountdownChanged(RunCountdownChangedFact fact)
        {
            _waveText.text = $"开始倒计时  {fact.SecondsRemaining}";
            _objectiveText.text = "准备战斗";
        }

        private void OnWaveStarted(WaveStartedFact fact)
        {
            _waveText.text = $"波次 {fact.WaveIndex}/{fact.TotalWaves}  {fact.DisplayName}";
            _currentObjective = fact.ObjectiveType == WaveObjectiveType.KeyTarget
                ? "目标：击败关键目标"
                : "目标：消灭所有敌人";
            _objectiveText.text = _currentObjective;
        }

        private void OnWaveProgressChanged(WaveProgressChangedFact fact)
        {
            _objectiveText.text = $"{_currentObjective}  |  场上 {fact.Alive}  待生成 {fact.Pending}";
        }

        private void OnWaveIntervalStarted(WaveIntervalStartedFact fact)
        {
            _waveText.text = $"第 {fact.NextWaveIndex} 波即将开始";
            _objectiveText.text = $"休整 {fact.Duration:0.0} 秒";
        }

        private void ShowWeapon(WeaponRuntime weapon)
        {
            var reloading = weapon.State == WeaponState.Reloading ? "  换弹中" : string.Empty;
            _weaponText.text = $"{weapon.Config.DisplayName}  {weapon.MagazineAmmo}/{weapon.ReserveAmmo}{reloading}";
        }

        private void ShowHealth(float health) => _healthText.text = $"生命  {Mathf.CeilToInt(health)}";

        private void ShowCharge(int level, int combo) =>
            _chargeText.text = $"充能  Lv.{level}  连段 {combo}";

        private void ShowTime(float scale, bool active) =>
            _timeText.text = active ? $"子弹时间  ×{scale:0.00}" : "子弹时间  ×1.00";

        private Color GetGrazeColor(GrazePhase phase)
        {
            switch (phase)
            {
                case GrazePhase.Startup: return new Color(0.15f, 0.48f, 0.25f);
                case GrazePhase.Perfect: return new Color(0.35f, 1f, 0.45f);
                case GrazePhase.Active: return new Color(0.25f, 0.72f, 0.35f);
                default: return _playerDefaultColor;
            }
        }

        private static string GetResultName(GrazeResultType result)
        {
            switch (result)
            {
                case GrazeResultType.PerfectMomentum: return "完美动势";
                case GrazeResultType.Momentum: return "动势";
                case GrazeResultType.PerfectDefensive: return "完美防守";
                default: return "防守";
            }
        }

        private void EnsureFifthPhaseLabels()
        {
            if (_grazeText == null) _grazeText = CreateRuntimeLabel("GrazeStatus", "擦弹  Idle", -120f);
            if (_chargeText == null) _chargeText = CreateRuntimeLabel("ChargeStatus", "充能  Lv.0  连段 0", -155f);
            if (_timeText == null) _timeText = CreateRuntimeLabel("TimeStatus", "子弹时间  ×1.00", -190f);
        }

        private void EnsureSixthPhaseLabels()
        {
            if (_waveText == null) _waveText = CreateRuntimeLabel("WaveStatus", "开始倒计时", -225f);
            if (_objectiveText == null) _objectiveText = CreateRuntimeLabel("WaveObjective", "等待第一波", -260f);
        }

        private Text CreateRuntimeLabel(string objectName, string content, float y)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, y);
            rect.sizeDelta = new Vector2(700f, 32f);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = content;
            label.fontSize = 18;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }
    }
}

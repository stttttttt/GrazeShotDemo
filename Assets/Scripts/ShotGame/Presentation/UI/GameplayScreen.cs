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
        [SerializeField] private Text _controlsHelpText;

        private GameplaySession _session;
        private IDisposable _weaponSubscription;
        private IDisposable _damageSubscription;
        private IDisposable _healingSubscription;
        private IDisposable _grazePhaseSubscription;
        private IDisposable _grazeSucceededSubscription;
        private IDisposable _chargeSubscription;
        private IDisposable _timeSubscription;
        private IDisposable _countdownSubscription;
        private IDisposable _waveStartedSubscription;
        private IDisposable _waveProgressSubscription;
        private IDisposable _waveIntervalSubscription;
        private string _currentObjective = "等待第一波";

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
            EnsureControlsHelp();
            if (_weaponText == null || _healthText == null || _grazeText == null ||
                _chargeText == null || _timeText == null)
                throw new InvalidOperationException("GameplayScreen 缺少 HUD 文本引用。");

            RefreshAll();
            _weaponSubscription = _session.Facts.Subscribe<WeaponStateChangedFact>(OnWeaponStateChanged);
            _damageSubscription = _session.Facts.Subscribe<CharacterDamagedFact>(OnCharacterDamaged);
            _healingSubscription = _session.Facts.Subscribe<CharacterHealedFact>(OnCharacterHealed);
            _grazeSucceededSubscription = _session.Facts.Subscribe<GrazeShockwaveReleasedFact>(OnShockwaveReleased);
            _chargeSubscription = _session.Facts.Subscribe<AmmoRewardedFact>(OnAmmoRewarded);
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
            _healingSubscription?.Dispose();
            _healingSubscription = null;
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
            _grazeText.text = "冲击波  按住右键蓄力";
            _chargeText.text = graze != null
                ? $"范围  {graze.PreviewRadius:0.0}" : "范围  --";
            _timeText.text = "冲击波消除敌弹可补充弹药并恢复生命";
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

        private void OnCharacterHealed(CharacterHealedFact fact)
        {
            if (_session == null || fact.TargetId != _session.PlayerEntityId) return;
            ShowHealth(fact.HealthAfterHealing);
        }

        private void OnGrazePhaseChanged(GrazePhaseChangedFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            _grazeText.text = $"擦弹  {fact.Current}";
        }

        private void OnGrazeSucceeded(GrazeSucceededFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            _grazeText.text = $"擦弹  {GetResultName(fact.ResultType)}";
        }

        private void OnShockwaveReleased(GrazeShockwaveReleasedFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            _grazeText.text = fact.AbsorbedProjectiles > 0
                ? $"冲击波  吸收 {fact.AbsorbedProjectiles} 发" : "冲击波  已释放";
            _chargeText.text = $"弹药 +{fact.AmmoReward}  生命 +{Mathf.RoundToInt(fact.RestoredHealth)}";
        }

        private void OnAmmoRewarded(AmmoRewardedFact fact)
        {
            if (_session == null || fact.PlayerId != _session.PlayerEntityId) return;
            _chargeText.text = $"吸收补给  +{fact.Amount}";
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
            _waveText.text = fact.TotalWaves > 0
                ? $"波次 {fact.WaveIndex}/{fact.TotalWaves}  {fact.DisplayName}"
                : $"无限模式  第 {fact.WaveIndex} 波";
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
            if (_grazeText == null) _grazeText = CreateRuntimeLabel("GrazeStatus", "冲击波  按住右键蓄力", -120f);
            if (_chargeText == null) _chargeText = CreateRuntimeLabel("ChargeStatus", "范围  --", -155f);
            if (_timeText == null) _timeText = CreateRuntimeLabel("TimeStatus", "消除敌弹可补充弹药并回血", -190f);
        }

        private void EnsureSixthPhaseLabels()
        {
            if (_waveText == null) _waveText = CreateRuntimeLabel("WaveStatus", "开始倒计时", -225f);
            if (_objectiveText == null) _objectiveText = CreateRuntimeLabel("WaveObjective", "等待第一波", -260f);
        }

        private void EnsureControlsHelp()
        {
            if (_controlsHelpText == null)
            {
                var existing = transform.Find("SeventhPhaseHud/ControlsHelp");
                _controlsHelpText = existing != null ? existing.GetComponent<Text>() : null;
            }
            if (_controlsHelpText == null)
            {
                var helpObject = new GameObject("ControlsHelp", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                helpObject.layer = gameObject.layer;
                helpObject.transform.SetParent(transform, false);
                _controlsHelpText = helpObject.GetComponent<Text>();
                _controlsHelpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _controlsHelpText.fontSize = 16;
                _controlsHelpText.alignment = TextAnchor.UpperRight;
                _controlsHelpText.color = new Color(1f, 1f, 1f, 0.86f);
                _controlsHelpText.raycastTarget = false;
                var rect = _controlsHelpText.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-24f, -24f);
                rect.sizeDelta = new Vector2(470f, 190f);
            }
            _controlsHelpText.text =
                "操作说明\n" +
                "WASD  移动     鼠标  瞄准\n" +
                "左键  开火     右键按住  蓄力冲击波\n" +
                "Space  冲刺（无无敌）     R  换弹\n" +
                "1  冲锋枪     2  霰弹枪     3  狙击枪\n" +
                "滚轮  切换武器     Q  上一把     Esc  暂停";
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

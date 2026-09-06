using System;
using GameFoundation.Service.UI;
using ShotGame.Gameplay.Character;
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

        private GameplaySession _session;
        private IDisposable _weaponSubscription;
        private IDisposable _damageSubscription;

        protected override void OnOpened(object args)
        {
            _session = args as GameplaySession
                ?? throw new ArgumentException("GameplayScreen 需要 GameplaySession。", nameof(args));
            if (_weaponText == null || _healthText == null)
                throw new InvalidOperationException("GameplayScreen 缺少 HUD 文本引用。");

            RefreshAll();
            _weaponSubscription = _session.Facts.Subscribe<WeaponStateChangedFact>(OnWeaponStateChanged);
            _damageSubscription = _session.Facts.Subscribe<CharacterDamagedFact>(OnCharacterDamaged);
        }

        protected override void OnClosing()
        {
            _weaponSubscription?.Dispose();
            _weaponSubscription = null;
            _damageSubscription?.Dispose();
            _damageSubscription = null;
            _session = null;
        }

        private void RefreshAll()
        {
            if (!_session.World.TryGetEntity(_session.PlayerEntityId, out var player)) return;
            var equipment = player.GetComponent<EquipmentComponent>();
            if (equipment != null) ShowWeapon(equipment.CurrentWeapon);
            var attributes = player.GetComponent<AttributeComponent>();
            if (attributes != null) ShowHealth(attributes.GetCurrent(AttributeType.Health));
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

        private void ShowWeapon(WeaponRuntime weapon)
        {
            var reloading = weapon.State == WeaponState.Reloading ? "  换弹中" : string.Empty;
            _weaponText.text = $"{weapon.Config.DisplayName}  {weapon.MagazineAmmo}/{weapon.ReserveAmmo}{reloading}";
        }

        private void ShowHealth(float health) => _healthText.text = $"生命  {Mathf.CeilToInt(health)}";
    }
}

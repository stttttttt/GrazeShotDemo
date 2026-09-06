using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Intent;
using ShotGame.Gameplay.Weapon;

namespace ShotGame.Gameplay.Character
{
    /// <summary>管理角色武器槽以及当前、上一把武器。</summary>
    public sealed class EquipmentComponent : EntityComponent
    {
        private readonly List<WeaponRuntime> _weapons = new List<WeaponRuntime>();
        private int _currentIndex;
        private int _previousIndex;

        public EquipmentComponent(IReadOnlyList<WeaponConfig> weaponConfigs)
        {
            if (weaponConfigs == null) throw new ArgumentNullException(nameof(weaponConfigs));
            if (weaponConfigs.Count < 1 || weaponConfigs.Count > 3)
                throw new ArgumentOutOfRangeException(nameof(weaponConfigs), "角色需要配置 1 至 3 把武器。");
            for (var i = 0; i < weaponConfigs.Count; i++)
                _weapons.Add(new WeaponRuntime(weaponConfigs[i]));
        }

        public IReadOnlyList<WeaponRuntime> Weapons => _weapons;
        public int WeaponCount => _weapons.Count;
        public int CurrentWeaponSlot => _currentIndex + 1;
        public int PreviousWeaponSlot => _previousIndex + 1;
        public WeaponRuntime CurrentWeapon => _weapons[_currentIndex];

        public bool ApplyIntent(in PawnIntent intent)
        {
            if (intent.SwitchWeaponSlot > 0) return TrySelectSlot(intent.SwitchWeaponSlot);
            if (intent.SwitchWeaponStep != 0) return SelectStep(intent.SwitchWeaponStep);
            if (intent.QuickSwapPressed) return TrySelectSlot(PreviousWeaponSlot);
            return false;
        }

        public bool TrySelectSlot(int slot)
        {
            var index = slot - 1;
            if (index < 0 || index >= _weapons.Count || index == _currentIndex) return false;
            CurrentWeapon.CancelReload();
            _previousIndex = _currentIndex;
            _currentIndex = index;
            return true;
        }

        private bool SelectStep(int step)
        {
            if (_weapons.Count <= 1 || step == 0) return false;
            var direction = step > 0 ? 1 : -1;
            var next = (_currentIndex + direction + _weapons.Count) % _weapons.Count;
            return TrySelectSlot(next + 1);
        }
    }
}

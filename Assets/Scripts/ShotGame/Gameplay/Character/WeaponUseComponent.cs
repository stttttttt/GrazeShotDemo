using System;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Intent;
using ShotGame.Gameplay.Weapon;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    /// <summary>接收角色开火意图，并驱动当前武器完成一次射击。</summary>
    public sealed class WeaponUseComponent : EntityComponent, IEntityTickable
    {
        private readonly EquipmentComponent _equipment;
        private readonly AttributeComponent _attributes;
        private readonly WeaponExecution _execution;
        private readonly GameplayFactHub _facts;
        private readonly LayerMask _targetMask;
        private readonly LayerMask _wallMask;
        private WeaponRuntime _lastCurrentWeapon;
        private Vector2 _aimDirection = Vector2.right;

        public WeaponUseComponent(EquipmentComponent equipment, AttributeComponent attributes,
            WeaponExecution execution, GameplayFactHub facts, LayerMask targetMask, LayerMask wallMask)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            _execution = execution ?? throw new ArgumentNullException(nameof(execution));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _targetMask = targetMask;
            _wallMask = wallMask;
        }

        public bool IsTriggerHeld { get; private set; }
        public Vector2 AimDirection => _aimDirection;

        public override void Initialize()
        {
            var facing = (Vector2)Owner.UnityObject.Transform.right;
            if (facing.sqrMagnitude > 0.0001f) _aimDirection = facing.normalized;
            _lastCurrentWeapon = _equipment.CurrentWeapon;
            PublishCurrentState();
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _equipment.Weapons.Count; i++)
            {
                var weapon = _equipment.Weapons[i];
                if (weapon.Tick(deltaTime) && weapon == _equipment.CurrentWeapon) PublishCurrentState();
            }
        }

        public void ApplyIntent(in PawnIntent intent)
        {
            if (intent.AimDirection.sqrMagnitude > 0.0001f)
                _aimDirection = intent.AimDirection.normalized;
            IsTriggerHeld = intent.FireHeld;

            var weapon = _equipment.CurrentWeapon;
            if (weapon != _lastCurrentWeapon)
            {
                _lastCurrentWeapon = weapon;
                PublishCurrentState();
            }

            var beforeState = weapon.State;
            var beforeMagazine = weapon.MagazineAmmo;
            var beforeReserve = weapon.ReserveAmmo;
            if (intent.ReloadPressed) weapon.TryStartReload();
            var damageMultiplier = _attributes.GetCurrent(AttributeType.DamageMultiplier);
            if (weapon.TryCreateShotPackage(intent.FirePressed, intent.FireHeld, Owner.Id, Owner.Team,
                    Owner.UnityObject.Transform.position, _aimDirection, damageMultiplier,
                    _targetMask, _wallMask, out var package))
            {
                _execution.Execute(package);
            }

            if (beforeState != weapon.State || beforeMagazine != weapon.MagazineAmmo ||
                beforeReserve != weapon.ReserveAmmo)
                PublishCurrentState();
        }

        private void PublishCurrentState()
        {
            var weapon = _equipment.CurrentWeapon;
            _facts.Publish(new WeaponStateChangedFact(Owner.Id, _equipment.CurrentWeaponSlot,
                weapon.Config.DisplayName, weapon.State, weapon.MagazineAmmo, weapon.ReserveAmmo));
        }
    }
}

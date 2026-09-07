using System;
using ShotGame.Gameplay.Entity;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Weapon
{
    /// <summary>单把武器的弹药、冷却和换弹状态。</summary>
    public sealed class WeaponRuntime
    {
        public WeaponRuntime(WeaponConfig config)
        {
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
            Config.Validate();
            MagazineAmmo = Config.MagazineSize;
            ReserveAmmo = Config.InitialReserveAmmo;
        }

        public WeaponConfig Config { get; }
        public WeaponState State { get; private set; } = WeaponState.Ready;
        public int MagazineAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public float CooldownRemaining { get; private set; }
        public float ReloadRemaining { get; private set; }

        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return false;
            var beforeState = State;
            var beforeMagazine = MagazineAmmo;
            var beforeReserve = ReserveAmmo;

            if (State == WeaponState.Cooldown)
            {
                CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
                if (CooldownRemaining <= 0f)
                {
                    State = WeaponState.Ready;
                    if (MagazineAmmo <= 0 && ReserveAmmo > 0) TryStartReload();
                }
            }
            else if (State == WeaponState.Reloading)
            {
                ReloadRemaining = Mathf.Max(0f, ReloadRemaining - deltaTime);
                if (ReloadRemaining <= 0f) FinishReload();
            }

            return HasPublicStateChanged(beforeState, beforeMagazine, beforeReserve);
        }

        public bool TryStartReload()
        {
            if (State == WeaponState.Reloading || MagazineAmmo >= Config.MagazineSize || ReserveAmmo <= 0)
                return false;
            State = WeaponState.Reloading;
            CooldownRemaining = 0f;
            ReloadRemaining = Config.ReloadDuration;
            return true;
        }

        public bool CancelReload()
        {
            if (State != WeaponState.Reloading) return false;
            ReloadRemaining = 0f;
            State = WeaponState.Ready;
            return true;
        }

        /// <summary>向武器备弹中加入奖励弹药，返回实际增加数量。</summary>
        public int AddReserveAmmo(int amount)
        {
            if (amount <= 0) return 0;
            var before = ReserveAmmo;
            ReserveAmmo = Math.Min(999, ReserveAmmo + amount);
            return ReserveAmmo - before;
        }

        public bool TryCreateShotPackage(bool firePressed, bool fireHeld, GameEntityId sourceId,
            EntityTeam sourceTeam, Vector2 ownerPosition, Vector2 aimDirection, float damageMultiplier,
            LayerMask targetMask, LayerMask wallMask, out ShotPackage shotPackage)
        {
            shotPackage = default;
            var wantsFire = Config.FireMode == WeaponFireMode.SemiAutomatic ? firePressed : fireHeld;
            if (!wantsFire || State == WeaponState.Reloading || State == WeaponState.Cooldown) return false;
            if (MagazineAmmo <= 0)
            {
                TryStartReload();
                return false;
            }
            if (aimDirection.sqrMagnitude <= 0.0001f) return false;

            var direction = aimDirection.normalized;
            var origin = ownerPosition + direction * Config.MuzzleOffset;
            MagazineAmmo--;
            State = WeaponState.Cooldown;
            CooldownRemaining = Config.FireInterval;
            shotPackage = new ShotPackage(sourceId, sourceTeam, Config, origin, direction,
                Config.ProjectilePrefab, Config.ProjectileCount, Config.SpreadAngle,
                Config.Damage * Mathf.Max(0f, damageMultiplier), Config.ProjectileSpeed,
                Config.ProjectileLifetime, Config.ProjectileRadius, Config.RecoilImpulse,
                targetMask, wallMask, remainingPenetrations: Config.Penetrations);
            return true;
        }

        private void FinishReload()
        {
            var needed = Config.MagazineSize - MagazineAmmo;
            var loaded = Math.Min(needed, ReserveAmmo);
            MagazineAmmo += loaded;
            ReserveAmmo -= loaded;
            ReloadRemaining = 0f;
            State = WeaponState.Ready;
        }

        private bool HasPublicStateChanged(WeaponState state, int magazine, int reserve) =>
            state != State || magazine != MagazineAmmo || reserve != ReserveAmmo;
    }
}

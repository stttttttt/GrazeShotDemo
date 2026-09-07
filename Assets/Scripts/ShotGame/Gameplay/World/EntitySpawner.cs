using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Intent;
using ShotGame.Gameplay.Projectile;
using ShotGame.Gameplay.Scene;
using ShotGame.Gameplay.Weapon;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Time;
using UnityEngine;
using GameplayEntity = ShotGame.Gameplay.Entity.Entity;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;
using GameplayCharacterController = ShotGame.Gameplay.Character.CharacterController;

namespace ShotGame.Gameplay.World
{
    public sealed class EntitySpawner : IDisposable
    {
        private readonly GameplayWorld _world;
        private readonly GameplayFactHub _facts;
        private readonly EntityIdGenerator _idGenerator;
        private bool _disposed;

        public EntitySpawner(GameplayWorld world, GameplayFactHub facts, EntityIdGenerator idGenerator)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        }

        public CharacterEntity SpawnPlayer(GameObject prefab, Vector3 position, Quaternion rotation,
            IReadOnlyList<WeaponConfig> weapons, LayerMask targetMask, LayerMask wallMask,
            LayerMask grazeProjectileMask, float maxRecoilSpeed, float recoilRecovery,
            GrazeConfig grazeConfig, TimeDilationController timeDilation, Bounds movementBounds,
            float dashDistance, float dashDuration, float dashCooldown,
            Transform parent = null)
        {
            return SpawnCharacter(prefab, position, rotation, parent, EntityCategory.Player, EntityTeam.Player,
                character =>
                {
                    var attributes = character.AddComponent(new AttributeComponent());
                    character.AddComponent(new PlayerInputComponent());
                    var movement = character.AddComponent(new MovementComponent(attributes,
                        maxRecoilSpeed, recoilRecovery, timeDilation, movementBounds));
                    var equipment = character.AddComponent(new EquipmentComponent(weapons));
                    var execution = new WeaponExecution(this, movement, _facts, parent);
                    character.AddComponent(new WeaponUseComponent(equipment, attributes, execution,
                        _facts, targetMask, wallMask));
                    var ammoReward = character.AddComponent(new AmmoRewardComponent(equipment, _facts));
                    character.AddComponent(new GrazeComponent(_world, ammoReward, _facts,
                        grazeConfig, grazeProjectileMask));
                    character.AddComponent(new AimRotationComponent());
                    character.AddComponent(new DashComponent(movement, dashDistance, dashDuration,
                        dashCooldown));
                    character.AddComponent(new GameplayCharacterController());
                });
        }

        public CharacterEntity SpawnEnemy(EnemyConfig config, Vector3 position, Quaternion rotation,
            GameEntityId targetId, LayerMask targetMask, LayerMask wallMask, Bounds movementBounds,
            Transform parent = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            return SpawnCharacter(config.Prefab, position, rotation, parent, EntityCategory.Enemy, EntityTeam.Enemy,
                character =>
                {
                    var attributes = character.AddComponent(new AttributeComponent(config.MaxHealth, config.MoveSpeed));
                    character.AddComponent(new AIComponent(_world, targetId, config.AttackRange,
                        config.InitialAttackDelay));
                    var movement = character.AddComponent(new MovementComponent(attributes,
                        movementBounds: movementBounds));
                    var equipment = character.AddComponent(new EquipmentComponent(new[] { config.Weapon }));
                    var execution = new WeaponExecution(this, movement, _facts, parent);
                    character.AddComponent(new WeaponUseComponent(equipment, attributes, execution,
                        _facts, targetMask, wallMask));
                    character.AddComponent(new GameplayCharacterController());
                });
        }

        public GameplayEntity SpawnProjectile(in ProjectileSpawnData data)
        {
            ThrowIfDisposed();
            if (data.Prefab == null) throw new ArgumentNullException(nameof(data), "弹丸 Prefab 为空。");
            if (!data.SourceId.IsValid) throw new ArgumentException("弹丸 SourceId 无效。", nameof(data));
            if (data.Direction.sqrMagnitude <= 0.0001f) throw new ArgumentException("弹丸方向无效。", nameof(data));

            var angle = Mathf.Atan2(data.Direction.y, data.Direction.x) * Mathf.Rad2Deg;
            var unityObject = CreateUnityObject(data.Prefab, data.Position,
                Quaternion.Euler(0f, 0f, angle), data.Parent);
            var entity = new GameplayEntity(_idGenerator.Next(), EntityCategory.Projectile,
                data.SourceTeam, unityObject);
            var spawnData = data;
            return Register(entity, item => item.AddComponent(new ProjectileComponent(
                _world, this, _facts, spawnData)));
        }

        public GameplayEntity SpawnWeapon(GameObject prefab, Vector3 position, Quaternion rotation, EntityTeam team,
            Transform parent = null) => SpawnSimple(prefab, position, rotation, parent, EntityCategory.Weapon, team);

        public GameplayEntity SpawnPickup(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null) =>
            SpawnSimple(prefab, position, rotation, parent, EntityCategory.Pickup, EntityTeam.Neutral);

        public GameplayEntity SpawnWall(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null) =>
            SpawnSimple(prefab, position, rotation, parent, EntityCategory.Wall, EntityTeam.Neutral);

        public GameplayEntity RegisterSceneEntity(SceneEntityAuthoring authoring)
        {
            ThrowIfDisposed();
            if (authoring == null) throw new ArgumentNullException(nameof(authoring));
            var entity = new GameplayEntity(_idGenerator.Next(), authoring.Category, authoring.Team,
                new EntityUnityObject(authoring.gameObject, false));
            return Register(entity, null);
        }

        public void Despawn(GameEntityId entityId) => _world.Despawn(entityId);
        public void Dispose() => _disposed = true;

        private CharacterEntity SpawnCharacter(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent, EntityCategory category, EntityTeam team, Action<CharacterEntity> configure)
        {
            ThrowIfDisposed();
            var unityObject = CreateUnityObject(prefab, position, rotation, parent);
            var entity = new CharacterEntity(_idGenerator.Next(), category, team, unityObject, _facts);
            return Register(entity, configure);
        }

        private GameplayEntity SpawnSimple(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent, EntityCategory category, EntityTeam team)
        {
            ThrowIfDisposed();
            var entity = new GameplayEntity(_idGenerator.Next(), category, team,
                CreateUnityObject(prefab, position, rotation, parent));
            return Register(entity, null);
        }

        private T Register<T>(T entity, Action<T> configure) where T : GameplayEntity
        {
            try
            {
                configure?.Invoke(entity);
                _world.Register(entity);
                return entity;
            }
            catch
            {
                entity.Dispose();
                throw;
            }
        }

        private static EntityUnityObject CreateUnityObject(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return new EntityUnityObject(UnityEngine.Object.Instantiate(prefab, position, rotation, parent));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntitySpawner));
        }
    }
}

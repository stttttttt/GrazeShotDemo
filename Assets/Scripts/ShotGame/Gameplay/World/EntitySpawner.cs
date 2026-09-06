using System;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Intent;
using ShotGame.Gameplay.Scene;
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
            Transform parent = null)
        {
            return SpawnCharacter(prefab, position, rotation, parent, EntityCategory.Player, EntityTeam.Player,
                character =>
                {
                    var attributes = character.AddComponent(new AttributeComponent());
                    character.AddComponent(new PlayerInputComponent());
                    character.AddComponent(new GameplayCharacterController());
                    character.AddComponent(new MovementComponent(attributes));
                    character.AddComponent(new WeaponUseComponent());
                    character.AddComponent(new EquipmentComponent());
                    character.AddComponent(new GrazeComponent());
                });
        }

        public CharacterEntity SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation,
            GameEntityId targetId, float attackRange = 5f, Transform parent = null)
        {
            return SpawnCharacter(prefab, position, rotation, parent, EntityCategory.Enemy, EntityTeam.Enemy,
                character =>
                {
                    var attributes = character.AddComponent(new AttributeComponent());
                    character.AddComponent(new AIComponent(_world, targetId, attackRange));
                    character.AddComponent(new GameplayCharacterController());
                    character.AddComponent(new MovementComponent(attributes));
                    character.AddComponent(new WeaponUseComponent());
                });
        }

        public GameplayEntity SpawnWeapon(GameObject prefab, Vector3 position, Quaternion rotation, EntityTeam team,
            Transform parent = null) => SpawnSimple(prefab, position, rotation, parent, EntityCategory.Weapon, team);

        public GameplayEntity SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation, EntityTeam team,
            Transform parent = null) => SpawnSimple(prefab, position, rotation, parent, EntityCategory.Projectile, team);

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

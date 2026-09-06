using System;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Intent;
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

        public CharacterEntity SpawnPlayer(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return SpawnCharacter(prefab, position, rotation, EntityCategory.Player, EntityTeam.Player,
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
            GameEntityId targetId, float attackRange = 5f)
        {
            return SpawnCharacter(prefab, position, rotation, EntityCategory.Enemy, EntityTeam.Enemy,
                character =>
                {
                    var attributes = character.AddComponent(new AttributeComponent());
                    character.AddComponent(new AIComponent(_world, targetId, attackRange));
                    character.AddComponent(new GameplayCharacterController());
                    character.AddComponent(new MovementComponent(attributes));
                    character.AddComponent(new WeaponUseComponent());
                });
        }

        public GameplayEntity SpawnWeapon(GameObject prefab, Vector3 position, Quaternion rotation, EntityTeam team) =>
            SpawnSimple(prefab, position, rotation, EntityCategory.Weapon, team);

        public GameplayEntity SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation, EntityTeam team) =>
            SpawnSimple(prefab, position, rotation, EntityCategory.Projectile, team);

        public GameplayEntity SpawnPickup(GameObject prefab, Vector3 position, Quaternion rotation) =>
            SpawnSimple(prefab, position, rotation, EntityCategory.Pickup, EntityTeam.Neutral);

        public GameplayEntity SpawnWall(GameObject prefab, Vector3 position, Quaternion rotation) =>
            SpawnSimple(prefab, position, rotation, EntityCategory.Wall, EntityTeam.Neutral);

        public void Despawn(GameEntityId entityId) => _world.Despawn(entityId);
        public void Dispose() => _disposed = true;

        private CharacterEntity SpawnCharacter(GameObject prefab, Vector3 position, Quaternion rotation,
            EntityCategory category, EntityTeam team, Action<CharacterEntity> configure)
        {
            ThrowIfDisposed();
            var unityObject = CreateUnityObject(prefab, position, rotation);
            var entity = new CharacterEntity(_idGenerator.Next(), category, team, unityObject, _facts);
            return Register(entity, configure);
        }

        private GameplayEntity SpawnSimple(GameObject prefab, Vector3 position, Quaternion rotation,
            EntityCategory category, EntityTeam team)
        {
            ThrowIfDisposed();
            var entity = new GameplayEntity(_idGenerator.Next(), category, team,
                CreateUnityObject(prefab, position, rotation));
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

        private static EntityUnityObject CreateUnityObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return new EntityUnityObject(UnityEngine.Object.Instantiate(prefab, position, rotation));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntitySpawner));
        }
    }
}

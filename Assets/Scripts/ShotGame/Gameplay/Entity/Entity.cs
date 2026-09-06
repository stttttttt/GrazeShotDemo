using System;
using System.Collections.Generic;

namespace ShotGame.Gameplay.Entity
{
    public class Entity : IDisposable
    {
        private readonly List<IEntityComponent> _components = new List<IEntityComponent>();
        private readonly HashSet<Type> _componentTypes = new HashSet<Type>();

        public Entity(EntityId id, EntityCategory category, EntityTeam team, EntityUnityObject unityObject)
        {
            if (!id.IsValid) throw new ArgumentException("EntityId 无效。", nameof(id));
            Id = id;
            Category = category;
            Team = team;
            UnityObject = unityObject ?? throw new ArgumentNullException(nameof(unityObject));
        }

        public EntityId Id { get; }
        public EntityCategory Category { get; }
        public EntityTeam Team { get; }
        public EntityUnityObject UnityObject { get; }
        public bool IsAlive { get; private set; } = true;
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public T AddComponent<T>(T component) where T : EntityComponent
        {
            ThrowIfInitialized();
            if (component == null) throw new ArgumentNullException(nameof(component));
            var type = component.GetType();
            if (!_componentTypes.Add(type))
                throw new InvalidOperationException($"Entity {Id} 已包含组件 {type.Name}。");
            component.BindOwner(this);
            _components.Add(component);
            return component;
        }

        public bool TryGetComponent<T>(out T component) where T : class
        {
            for (var i = 0; i < _components.Count; i++)
            {
                if (_components[i] is T match)
                {
                    component = match;
                    return true;
                }
            }
            component = null;
            return false;
        }

        public T GetComponent<T>() where T : class
        {
            TryGetComponent(out T component);
            return component;
        }

        public virtual void Initialize()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Entity));
            if (IsInitialized) throw new InvalidOperationException($"Entity {Id} 不能重复初始化。");
            for (var i = 0; i < _components.Count; i++) _components[i].Initialize();
            IsInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            if (!CanUpdate()) return;
            for (var i = 0; i < _components.Count; i++)
                if (_components[i] is IEntityTickable tickable) tickable.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (!CanUpdate()) return;
            for (var i = 0; i < _components.Count; i++)
                if (_components[i] is IEntityFixedTickable tickable) tickable.FixedTick(fixedDeltaTime);
        }

        public void Kill() => IsAlive = false;

        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            IsAlive = false;
            for (var i = _components.Count - 1; i >= 0; i--) _components[i].Dispose();
            _components.Clear();
            _componentTypes.Clear();
            UnityObject.Dispose();
        }

        private bool CanUpdate() => IsInitialized && IsAlive && !IsDisposed;

        private void ThrowIfInitialized()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Entity));
            if (IsInitialized) throw new InvalidOperationException("Entity 初始化后不能再添加组件。");
        }
    }
}

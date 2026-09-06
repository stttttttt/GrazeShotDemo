using System;

namespace ShotGame.Gameplay.Entity
{
    public interface IEntityComponent : IDisposable
    {
        Entity Owner { get; }
        void Initialize();
    }

    public interface IEntityTickable
    {
        void Tick(float deltaTime);
    }

    public interface IEntityFixedTickable
    {
        void FixedTick(float fixedDeltaTime);
    }

    public abstract class EntityComponent : IEntityComponent
    {
        public Entity Owner { get; private set; }

        internal void BindOwner(Entity owner)
        {
            if (Owner != null) throw new InvalidOperationException($"{GetType().Name} 已绑定 Entity。");
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }
}

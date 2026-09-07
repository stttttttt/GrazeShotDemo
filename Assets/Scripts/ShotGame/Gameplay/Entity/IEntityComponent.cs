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

    /// <summary>只用于不应被子弹时间拉长的局内计时。</summary>
    public interface IEntityUnscaledTickable
    {
        void UnscaledTick(float unscaledDeltaTime);
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

using System;

namespace ShotGame.Gameplay.Entity
{
    public sealed class EntityIdGenerator
    {
        private ulong _nextValue = 1;

        public EntityId Next()
        {
            if (_nextValue == 0) throw new InvalidOperationException("EntityId 已耗尽。");
            return new EntityId(_nextValue++);
        }
    }
}

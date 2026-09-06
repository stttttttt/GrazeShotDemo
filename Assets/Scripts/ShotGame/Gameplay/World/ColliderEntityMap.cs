using System.Collections.Generic;
using ShotGame.Gameplay.Entity;
using UnityEngine;
using GameplayEntity = ShotGame.Gameplay.Entity.Entity;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.World
{
    public sealed class ColliderEntityMap
    {
        private readonly Dictionary<Collider2D, GameEntityId> _entityIds =
            new Dictionary<Collider2D, GameEntityId>();

        public void Register(GameplayEntity entity)
        {
            var colliders = entity.UnityObject.Colliders;
            for (var i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) _entityIds[colliders[i]] = entity.Id;
        }

        public void Unregister(GameplayEntity entity)
        {
            var colliders = entity.UnityObject.Colliders;
            for (var i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) _entityIds.Remove(colliders[i]);
        }

        public bool TryGetEntityId(Collider2D collider, out GameEntityId entityId)
        {
            if (collider != null) return _entityIds.TryGetValue(collider, out entityId);
            entityId = default;
            return false;
        }

        public void Clear() => _entityIds.Clear();
    }
}

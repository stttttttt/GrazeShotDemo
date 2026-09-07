using ShotGame.Gameplay.Entity;

namespace ShotGame.Gameplay.Combat
{
    public readonly struct DamageRequest
    {
        public DamageRequest(EntityId sourceId, EntityTeam sourceTeam, DamagePayload payload,
            EntityId damageSourceEntityId = default)
        {
            SourceId = sourceId;
            SourceTeam = sourceTeam;
            Payload = payload;
            DamageSourceEntityId = damageSourceEntityId;
        }

        public EntityId SourceId { get; }
        public EntityTeam SourceTeam { get; }
        public DamagePayload Payload { get; }
        /// <summary>本次直接造成伤害的实体；弹丸伤害传弹丸自身 Id。</summary>
        public EntityId DamageSourceEntityId { get; }
    }
}

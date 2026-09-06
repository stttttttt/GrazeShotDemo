using ShotGame.Gameplay.Entity;

namespace ShotGame.Gameplay.Combat
{
    public readonly struct DamageRequest
    {
        public DamageRequest(EntityId sourceId, EntityTeam sourceTeam, DamagePayload payload)
        {
            SourceId = sourceId;
            SourceTeam = sourceTeam;
            Payload = payload;
        }

        public EntityId SourceId { get; }
        public EntityTeam SourceTeam { get; }
        public DamagePayload Payload { get; }
    }
}

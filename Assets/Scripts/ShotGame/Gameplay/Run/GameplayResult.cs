using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Entity;

namespace ShotGame.Gameplay.Run
{
    public enum GameplayResultType
    {
        None,
        Victory,
        Defeat
    }

    public readonly struct GameplayResult
    {
        public GameplayResult(GameplayResultType type, double elapsedTime, int reachedWaveIndex,
            int totalWaveCount, EntityId lastDamageSourceId, float lastDamageAmount,
            string playerWeaponName, GrazePhase grazePhaseAtDefeat)
        {
            Type = type;
            ElapsedTime = elapsedTime;
            ReachedWaveIndex = reachedWaveIndex;
            TotalWaveCount = totalWaveCount;
            LastDamageSourceId = lastDamageSourceId;
            LastDamageAmount = lastDamageAmount;
            PlayerWeaponName = playerWeaponName;
            GrazePhaseAtDefeat = grazePhaseAtDefeat;
        }

        public GameplayResultType Type { get; }
        public double ElapsedTime { get; }
        public int ReachedWaveIndex { get; }
        public int TotalWaveCount { get; }
        public EntityId LastDamageSourceId { get; }
        public float LastDamageAmount { get; }
        public string PlayerWeaponName { get; }
        public GrazePhase GrazePhaseAtDefeat { get; }
        public bool HasResult => Type != GameplayResultType.None;
    }
}

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
        public GameplayResult(GameplayResultType type, double elapsedTime)
        {
            Type = type;
            ElapsedTime = elapsedTime;
        }

        public GameplayResultType Type { get; }
        public double ElapsedTime { get; }
        public bool HasResult => Type != GameplayResultType.None;
    }
}

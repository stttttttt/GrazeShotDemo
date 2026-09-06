namespace GameFoundation.Core
{
    /// <summary>一局游戏的时间源，也是暂停与子弹时间的唯一控制入口。</summary>
    public interface IGameTimeService
    {
        float DeltaTime { get; }
        float UnscaledDeltaTime { get; }
        double ElapsedTime { get; }
        bool IsPaused { get; }
        float TimeScale { get; }
        void Tick(float unscaledDeltaTime);
        void SetPaused(bool isPaused);
        void SetTimeScale(float timeScale);
        void Reset();
    }
}

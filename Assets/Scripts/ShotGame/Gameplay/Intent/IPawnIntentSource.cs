namespace ShotGame.Gameplay.Intent
{
    public interface IPawnIntentSource
    {
        PawnIntent GetIntent();
        void ClearFrameIntent();
    }
}

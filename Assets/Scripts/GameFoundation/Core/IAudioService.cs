namespace GameFoundation.Core
{
    public interface IAudioService
    {
        void ApplyVolume(float master, float music, float sfx);
        void PlayUiOneShot(UnityEngine.AudioClip clip);
    }
}

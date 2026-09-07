using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    public sealed class AudioFeedbackController
    {
        private readonly AudioSource _source;

        public AudioFeedbackController(AudioSource source) => _source = source;

        public void Play(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
        {
            if (_source == null || clip == null) return;
            _source.pitch = pitchVariance > 0f ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
            _source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void Stop()
        {
            if (_source == null) return;
            _source.Stop();
            _source.pitch = 1f;
        }
    }
}

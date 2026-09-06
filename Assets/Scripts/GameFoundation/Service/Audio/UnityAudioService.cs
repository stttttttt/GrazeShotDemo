using System.Threading.Tasks;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace GameFoundation.Service.Audio
{
    public sealed class UnityAudioService : IAudioService, IAppService
    {
        private readonly Transform _root;
        private readonly AudioMixer _mixer;
        private readonly string _masterParameter;
        private readonly string _musicParameter;
        private readonly string _sfxParameter;
        private AudioSource _uiSource;

        public UnityAudioService(
            Transform root,
            AudioMixer mixer,
            string masterParameter,
            string musicParameter,
            string sfxParameter)
        {
            _root = root;
            _mixer = mixer;
            _masterParameter = masterParameter;
            _musicParameter = musicParameter;
            _sfxParameter = sfxParameter;
        }

        public string Name => "音频服务";

        public Task InitializeAsync()
        {
            var sourceObject = new GameObject("UIAudioSource");
            sourceObject.transform.SetParent(_root, false);
            _uiSource = sourceObject.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            return Task.CompletedTask;
        }

        public void ApplyVolume(float master, float music, float sfx)
        {
            if (_mixer == null) return;
            SetVolume(_masterParameter, master);
            SetVolume(_musicParameter, music);
            SetVolume(_sfxParameter, sfx);
        }

        public void PlayUiOneShot(AudioClip clip)
        {
            if (_uiSource != null && clip != null) _uiSource.PlayOneShot(clip);
        }

        public void Shutdown()
        {
            if (_uiSource != null) UnityEngine.Object.Destroy(_uiSource.gameObject);
            _uiSource = null;
        }

        private void SetVolume(string parameter, float normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(parameter)) return;
            var decibels = Mathf.Log10(Mathf.Max(0.0001f, normalizedValue)) * 20f;
            _mixer.SetFloat(parameter, decibels);
        }
    }
}

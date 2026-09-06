using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace GameFoundation.Service.Config
{
    [CreateAssetMenu(fileName = "GameFoundationConfig", menuName = "Game Foundation/Foundation Config")]
    public sealed class GameFoundationConfig : ScriptableObject
    {
        [Header("输入")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _gameplayActionMap = "Player";
        [SerializeField] private string _uiActionMap = "UI";

        [Header("音频")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _masterVolumeParameter = "MasterVolume";
        [SerializeField] private string _musicVolumeParameter = "MusicVolume";
        [SerializeField] private string _sfxVolumeParameter = "SfxVolume";

        public InputActionAsset InputActions => _inputActions;
        public string GameplayActionMap => _gameplayActionMap;
        public string UiActionMap => _uiActionMap;
        public AudioMixer AudioMixer => _audioMixer;
        public string MasterVolumeParameter => _masterVolumeParameter;
        public string MusicVolumeParameter => _musicVolumeParameter;
        public string SfxVolumeParameter => _sfxVolumeParameter;
    }
}

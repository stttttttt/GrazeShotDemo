using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>Gameplay 场景的被动表现引用桥。</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayPresentationBindings : MonoBehaviour
    {
        [SerializeField] private Transform _temporaryEffectRoot;
        [SerializeField] private Transform _persistentEffectRoot;
        [SerializeField] private Transform _cameraAnchor;
        [SerializeField] private AudioSource _audioSource;

        public Transform TemporaryEffectRoot => _temporaryEffectRoot != null ? _temporaryEffectRoot : transform;
        public Transform PersistentEffectRoot => _persistentEffectRoot != null ? _persistentEffectRoot : transform;
        public Transform CameraAnchor => _cameraAnchor;
        public AudioSource AudioSource => _audioSource;
    }
}

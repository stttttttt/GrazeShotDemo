using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private UIPageId _pageId;
        [SerializeField] private UILayer _layer = UILayer.Page;
        [SerializeField] private bool _blocksInput = true;
        private CanvasGroup _canvasGroup;

        public UIPageId PageId => _pageId;
        public UILayer Layer => _layer;
        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        internal void Open(object args)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = _blocksInput;
            IsOpen = true;
            OnOpened(args);
        }

        internal void Close()
        {
            if (!IsOpen && !gameObject.activeSelf) return;
            OnClosing();
            IsOpen = false;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        protected virtual void OnOpened(object args) { }
        protected virtual void OnClosing() { }
    }
}

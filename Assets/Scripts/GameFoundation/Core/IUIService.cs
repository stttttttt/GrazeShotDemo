using System;
using System.Threading.Tasks;
using UnityEngine;

namespace GameFoundation.Core
{
    [Serializable]
    public struct UIPageId : IEquatable<UIPageId>
    {
        [SerializeField] private string _value;

        public UIPageId(string value) => _value = value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);
        public bool Equals(UIPageId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is UIPageId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(UIPageId left, UIPageId right) => left.Equals(right);
        public static bool operator !=(UIPageId left, UIPageId right) => !left.Equals(right);
    }

    public enum UILayer
    {
        Background,
        Page,
        Popup,
        Overlay,
        Debug
    }

    public interface IUIService
    {
        bool IsOpen(UIPageId pageId);
        Task OpenAsync(UIPageId pageId, object args);
        void Close(UIPageId pageId);
        void CloseLayer(UILayer layer);
        void CloseAll();
    }
}

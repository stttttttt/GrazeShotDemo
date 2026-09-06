using System;
using UnityEngine;

namespace GameFoundation.Core
{
    [Serializable]
    public struct SceneId : IEquatable<SceneId>
    {
        [SerializeField] private string _value;

        public SceneId(string value) => _value = value;

        public string Value => _value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public bool Equals(SceneId other) =>
            string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SceneId other && Equals(other);
        public override int GetHashCode() =>
            _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(SceneId left, SceneId right) => left.Equals(right);
        public static bool operator !=(SceneId left, SceneId right) => !left.Equals(right);
    }
}

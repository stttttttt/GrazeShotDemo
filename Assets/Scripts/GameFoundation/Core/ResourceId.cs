using System;
using UnityEngine;

namespace GameFoundation.Core
{
    [Serializable]
    public struct ResourceId : IEquatable<ResourceId>
    {
        [SerializeField] private string _value;

        public ResourceId(string value) => _value = value;

        public string Value => _value;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);
        public bool Equals(ResourceId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ResourceId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
    }
}

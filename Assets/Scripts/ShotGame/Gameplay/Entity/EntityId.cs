using System;

namespace ShotGame.Gameplay.Entity
{
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public EntityId(ulong value) => Value = value;

        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}
